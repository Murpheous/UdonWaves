using UnityEngine;
using TMPro;
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class HuygensMonitor : UdonSharpBehaviour
{
    [SerializeField, Tooltip("Custom Render texture (only if required)")] CustomRenderTexture simCRT;
    [SerializeField, Tooltip("Use Render texture mode")] bool useCRT = false;

    [Tooltip("DisplayPanel")] public MeshRenderer thePanel;
    [SerializeField, FieldChangeCallback(nameof(Visibility))]
    private float visibility = 1;
    [SerializeField, FieldChangeCallback(nameof(DisplayMode))]
    public int displayMode = 1;
    [SerializeField] Vector2Int simResolution = new Vector2Int(2048, 1280);
    [Tooltip("Panel Width (mm)"), SerializeField] float panelWidth = 2.048f;
    [SerializeField] Vector2 displayRect = new Vector2(1.99f, 1.33f);
    [SerializeField] Vector2 defaultDisplayRect = new Vector2(1.95f, 0.95f);
    [SerializeField] float sourceOffset;

    [Header("Source Settings")]
    [SerializeField, Tooltip("Scales control settings in mm to lengths in metres")]
    private float mmToMetres = 0.001f;
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(SlitCount))] 
    private int slitCount = 2;
    [SerializeField] private TextMeshProUGUI lblSourceCount;
    [SerializeField, Range(50, 600), FieldChangeCallback(nameof(SlitPitch))]
    private float slitPitch = 564f;

    [SerializeField, Range(5, 60), FieldChangeCallback(nameof(SlitWidth))]
    private float slitWidth = 10;

    [Header("Controls")]
    [SerializeField, FieldChangeCallback(nameof(WaveSpeed))] public float waveSpeed;

    [SerializeField] UdonSlider speedSlider;

    [SerializeField]
    private UdonSlider lambdaSlider;
    private bool iHaveLambdaControl = false;
    [SerializeField]
    private UdonSlider pitchSlider;
    private bool iHavePitchControl = false;

    [SerializeField]
    private UdonSlider scaleSlider;
    private bool iHaveScaleControl = false;

    [SerializeField]
    private UdonSlider widthSlider;
    private bool iHaveWidthControl = false;

    [SerializeField]
    private float momentum;
    [SerializeField]
    private TextMeshProUGUI lblMomentum;

    [SerializeField]
    private float minLambda = 30;
    [SerializeField]
    private float maxLambda = 80;

    [SerializeField, Range(30, 80), FieldChangeCallback(nameof(Lambda))]
    private float lambda = 1f;

    [SerializeField, Range(1, 10), FieldChangeCallback(nameof(SimScale))]
    private float simScale = 1f;
    [SerializeField] 
    private UdonBehaviour vectorDrawing;
    [SerializeField]
    private UdonBehaviour particleSim;
    private bool iHaveParticleSim;

    private bool iamOwner;
    private VRCPlayerApi player;

    private void UpdateOwnerShip()
    {
        iamOwner = Networking.IsOwner(this.gameObject);
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        UpdateOwnerShip();
    }

    [Header("Serialized for monitoring in Editor")]
    [SerializeField]
    private Material matPanel = null;
    [SerializeField]
    private Material matSimControl = null;
    [SerializeField]
    private Material matSimDisplay = null;
    [SerializeField]
    private bool iHaveCRT = false;
    [SerializeField]
    private bool iHavePanelMaterial = false;
    [SerializeField]
    private bool iHaveSimDisplay = false;
    [SerializeField]
    private bool iHaveWaveCRT = false;
    [SerializeField]
    private bool iHaveSimControl = false;
    [SerializeField]
    private float defaultLambda = 50;
    [SerializeField]
    private float defaultPitch = 564;
    [SerializeField]
    private float defaultWidth = 10;
    [SerializeField]
    private float defaultScale = 1;

    private float Visibility
    {
        get => visibility;
        set
        {
            visibility = Mathf.Clamp01(value);
            if (iHaveSimDisplay)
            {
                matSimDisplay.SetFloat("_Visibility",visibility);
            }
        }
    }
    private void configureSimControl(bool vanillaDisplay)
    {
        if (vanillaDisplay)
        {
            if (iHaveCRT)
            { // Display mode and wave speed controls get handled by the panel material
                matSimDisplay = simCRT.material;
                matSimControl = simCRT.material;
                simCRT.material.SetFloat("_OutputRaw", 0);
            }
            else
            {
                // No CRT and not a compatible display
                matSimDisplay = null;
                matSimControl = null;
                Debug.Log("Warning:configureSimControl() no Interference control/display material");
            }
        }
        else
        {
            if (iHaveCRT)
            { // Display mode and wave speed controls get handled by the CRT
                matSimDisplay = matPanel;
                matSimControl = simCRT.material;
                if (iHaveCRT && iHaveSimControl)
                    matSimControl.SetFloat("_OutputRaw", 1);
            }
            else
            {
                matSimDisplay = matPanel;
                matSimControl = matPanel;
            }

        }
        iHaveSimControl = matSimControl != null;
        iHaveSimDisplay = matSimDisplay != null;
    }

    
    private bool PanelHasVanillaMaterial
    {
        get
        {
            if (!iHavePanelMaterial)
                return true;
            return !matPanel.HasProperty("_DisplayMode");
        }
    }

    private int DisplayMode
    {
        get => displayMode; 
        set
        {
            if (iHaveSimDisplay) 
                matSimDisplay.SetFloat("_DisplayMode", value);
            if (value >= 0)
            {
                if (displayMode < 0) 
                {
                    if (iHavePitchControl)
                        pitchSlider.Interactable = true;
                    if (iHaveWidthControl)
                        widthSlider.Interactable = true;
                    if (iHaveLambdaControl)
                        lambdaSlider.Interactable = true;
                    if (iHaveScaleControl)
                        scaleSlider.Interactable = true;
                }
                if (vectorDrawing != null)
                    vectorDrawing.SetProgramVariable<Vector2>("displayRect", displayRect);
            }
            else
            {
                SlitPitch = defaultPitch;
                if (iHavePitchControl)
                {
                    pitchSlider.SetValue(defaultPitch);
                    pitchSlider.Interactable = false;
                }
                SlitWidth = defaultWidth;
                if (iHaveWidthControl)
                {
                    widthSlider.SetValue(defaultWidth);
                    widthSlider.Interactable = false;
                }
                Lambda = defaultLambda;
                if (iHaveLambdaControl)
                {
                    lambdaSlider.SetValue(defaultLambda);
                    lambdaSlider.Interactable = false;
                }
                SimScale = defaultScale;
                if (iHaveScaleControl)
                {
                    scaleSlider.SetValue(defaultScale);
                    scaleSlider.Interactable = false;
                }
                if (useCRT)
                    simCRT.Initialize();
                SlitCount = 2;
                if (vectorDrawing != null)
                    vectorDrawing.SetProgramVariable<Vector2>("displayRect", defaultDisplayRect);
            }
            displayMode = value;
            updateNeeded = true;
        }
    }

    private void updateGrating()
    {
        if (iHaveParticleSim)
        {
            particleSim.SetProgramVariable<int>("slitCount", slitCount);
            particleSim.SetProgramVariable<float>("slitWidth", slitWidth * mmToMetres);
            particleSim.SetProgramVariable<float>("slitPitch", slitPitch * mmToMetres);
            //particleSim.SetProgramVariable<float>("gratingOffset", gratingOffset);
        }
        if (!iHaveWaveCRT)
            return;
        matSimControl.SetFloat("_SlitPitch", slitPitch * mmToPixels);
        if (slitCount > 1 && slitPitch <= slitWidth)
        {
            float gratingWidth = (slitCount - 1) * slitPitch + slitWidth;
            matSimControl.SetFloat("_SlitCount", 1f);
            matSimControl.SetFloat("_SlitWidth", gratingWidth * mmToPixels);
            return;
        }
        matSimControl.SetFloat("_SlitCount", slitCount);
        matSimControl.SetFloat("_SlitWidth", slitWidth * mmToPixels);
        updateNeeded = true;
    }

    public float SlitWidth
    {
        get => slitWidth;
        set
        {
            slitWidth = value;
            if (vectorDrawing != null)
                vectorDrawing.SetProgramVariable<float>("slitWidth", slitWidth);
            if (iHaveParticleSim)
                particleSim.SetProgramVariable<float>("slitWidth", slitWidth*mmToMetres);
            updateGrating();
        }
    }
    public float SlitPitch
    {
        get => slitPitch;
        set
        {
            slitPitch = value;
            if (vectorDrawing != null)
                vectorDrawing.SetProgramVariable<float>("slitPitch", slitPitch);
            if (iHaveParticleSim)
                particleSim.SetProgramVariable<float>("slitPitch", slitPitch * mmToMetres);
            updateGrating();
        }
    }

    public int SlitCount
    {
        get => slitCount;
        set
        {
            if (value < 1)
                value = 1;
            if (value > 7) 
                value = 7;
            slitCount = value;
            updateGrating();
            if (lblSourceCount != null)
                lblSourceCount.text = slitCount.ToString();
            if (vectorDrawing != null)
                vectorDrawing.SetProgramVariable<int>("slitCount", slitCount);
            if (iHaveParticleSim)
                particleSim.SetProgramVariable<int>("slitCount",slitCount);
            RequestSerialization();
        }
    }

    public void decSrc()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        SlitCount -= 1;
    }
    public void incSrc()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        SlitCount += 1;
    }

    public float SimScale
    {
        get => simScale;
        set
        {
            simScale = value;
            if (iHaveWaveCRT)
                matSimControl.SetFloat("_Scale", simScale);
            if (iHaveScaleControl)
                scaleSlider.SetValue(simScale);
            if (iHaveParticleSim)
                particleSim.SetProgramVariable<float>("simScale",simScale);
            updateNeeded = true;
        }
    }


    private void updateMomentum()
    {
        momentum = 1 / (lambda * mmToMetres);
        if (lblMomentum != null)
            lblMomentum.text = string.Format("p={0:0.0}", momentum);
        if (iHaveParticleSim)
        {
            particleSim.SetProgramVariable<float>("maxParticleK", 1 / (minLambda * mmToMetres));
            particleSim.SetProgramVariable<float>("minParticleK", 1 / (maxLambda * mmToMetres));
            particleSim.SetProgramVariable<float>("particleK", momentum);
        }
    }

    private float Lambda
    {
        get => lambda;
        set
        {
            lambda = value;
            //phaseRate = 35f/value;
            if (vectorDrawing != null)
                vectorDrawing.SetProgramVariable<float>("lambda", lambda);
            if (iHaveWaveCRT)
                matSimControl.SetFloat("_Lambda", lambda * mmToPixels);
            if (iHaveLambdaControl) 
                lambdaSlider.SetValue(lambda);
            updateMomentum();
            UpdateWaveSpeed();
            updateNeeded = true;
        } 
    }

    private void UpdateWaveSpeed()
    {
        if (iHaveSimDisplay)
            matSimDisplay.SetFloat("_Frequency", playSim ? waveSpeed * defaultLambda/lambda : 0);
    }

    float WaveSpeed
    {
        get => waveSpeed;
        set
        {
            waveSpeed = Mathf.Clamp(value, 0, 5);
            UpdateWaveSpeed();
        }
    }

    [SerializeField, FieldChangeCallback(nameof(PlaySim))]
    public bool playSim = true;
    private bool PlaySim
    {
        get => playSim;
        set
        {
            playSim = value;
            UpdateWaveSpeed();
        }
    }
    bool updateNeeded = false;
    [SerializeField]
    float mmToPixels = 1;
    void UpdateWaves()
    {   
        if(useCRT && displayMode >= 0)
            simCRT.Update(1);
        updateNeeded = false;
    }

    private void Update()
    {
        if (!useCRT) return;
        if (updateNeeded)
            UpdateWaves();
    }
    void Start()
    {
        player = Networking.LocalPlayer;
        UpdateOwnerShip();

        iHavePitchControl = pitchSlider != null;
        iHaveWidthControl = widthSlider != null;
        iHaveLambdaControl = lambdaSlider != null;
        iHaveScaleControl = scaleSlider != null;
        iHaveParticleSim = particleSim != null;

        if (thePanel != null)
            matPanel = thePanel.material;
        iHavePanelMaterial = matPanel != null;

        if (simCRT != null)
        {
            iHaveCRT = true;
            simCRT.Initialize();
        }
        configureSimControl(PanelHasVanillaMaterial);

        mmToPixels = simResolution.x/panelWidth;
        if (useCRT)
        {
            if (simCRT != null)
            {
                matSimControl = simCRT.material;
                simCRT.Initialize();
            }
            else
                useCRT = false;
        }
        else
        {
            matSimControl = thePanel.material;
        }
        iHaveWaveCRT = matSimControl != null;
        if (iHaveLambdaControl)
        {
            lambdaSlider.SetLimits( minLambda, maxLambda);
            lambdaSlider.SetValue(defaultLambda);
        }
        Lambda = defaultLambda;
        if (iHavePitchControl)
        {
            pitchSlider.SetLimits(50, 600);
            pitchSlider.SetValue(defaultPitch);
        }
        SlitPitch = defaultPitch;
        if (iHaveWidthControl)
        {
            widthSlider.SetLimits(5, 50);
            widthSlider.SetValue(defaultWidth);
        }
        SlitWidth = defaultWidth;
        Lambda = lambda;
        SlitPitch = slitPitch;
        SlitWidth = slitWidth;
        DisplayMode = displayMode;
        SimScale = simScale;
        if (iHaveScaleControl)
        {
            scaleSlider.SetLimits(1, 10);
            scaleSlider.SetValue(simScale);
        }
    }
}
