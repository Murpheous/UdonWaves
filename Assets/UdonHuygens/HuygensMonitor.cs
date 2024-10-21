using UnityEngine;
using TMPro;
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class HuygensMonitor : UdonSharpBehaviour
{
    [SerializeField, Tooltip("Custom Render texture (only if required)")] CustomRenderTexture simCRT;
    [SerializeField, Tooltip("Use Render texture mode")] bool useCRT = false;

    [Tooltip("DisplayPanel")] public MeshRenderer thePanel;
    [SerializeField, FieldChangeCallback(nameof(Brightness))]
    private float brightness = 1;
    [SerializeField, Range(0.1f, 1f), FieldChangeCallback(nameof(Visibility))]
    private float visibility = 0.2f;

    [SerializeField, FieldChangeCallback(nameof(ContrastVal))]
    public float contrastVal = 40f;

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
    [SerializeField]
    private Toggle togWaves;
    [SerializeField]
    private Toggle togParticles;
    [SerializeField]
    private Toggle togBoth;

    [SerializeField] 
    private UdonSlider speedSlider;

    [SerializeField]
    private UdonSlider lambdaSlider;
    [SerializeField]
    private UdonSlider pitchSlider;
    private bool iHavePitchControl = false;

    [SerializeField]
    private UdonSlider scaleSlider;
    private bool iHaveScaleControl = false;

    [SerializeField]
    private UdonSlider widthSlider;
    private bool iHaveWidthControl = false;

    [SerializeField] UdonSlider contrastSlider;

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
    [SerializeField, FieldChangeCallback(nameof(DisplayColor))]
    Color displayColor;


    [SerializeField, Range(1, 10), FieldChangeCallback(nameof(SimScale))]
    private float simScale = 1f;
    [SerializeField]
    private VectorDiagram vectorDrawing;
    [SerializeField]
    private UdonBehaviour particleSim;

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

    [Header("Default Values")]
    [SerializeField]
    private float defaultLambda = 50;
    [SerializeField]
    private float defaultPitch = 564;
    [SerializeField]
    private float defaultWidth = 10;
    [SerializeField]
    private float defaultScale = 1;

    //[Header("Serialized for monitoring in Editor")]
    //[SerializeField]
    private Material matPanel = null;
    //[SerializeField]
    private Material matSimControl = null;
    //[SerializeField]
    private Material matSimDisplay = null;
    //[SerializeField]
    private bool iHaveCRT = false;
    //[SerializeField]
    private bool iHavePanelMaterial = false;
    //[SerializeField]
    private bool iHaveSimDisplay = false;
    //[SerializeField]
    private bool iHaveWaveCRT = false;
    //[SerializeField]
    private bool iHaveSimControl = false;

    private float prevVisibility = -1;
    private void reviewContrast()
    {
        if (!iHaveSimDisplay)
            return;
        float targetViz = (contrastVal / 50) * visibility;
        if (targetViz == prevVisibility)
            return;
        prevVisibility = targetViz;
        matSimDisplay.SetFloat("_Brightness", targetViz);
    }

    private float ContrastVal
    {
        get => contrastVal;
        set
        {
            contrastVal = value;
            reviewContrast();
        }
    }

    private float Visibility
    {
        get => visibility;
        set
        {
            visibility = value;
            reviewContrast();
        }
    }
    private float Brightness
    {
        get => brightness;
        set
        {
            brightness = Mathf.Clamp01(value);
            if (iHaveSimDisplay)
            {
                matSimDisplay.SetFloat("_Brightness", brightness*visibility);
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
                    if (togParticles != null)
                        togParticles.interactable = true;
                    if (togWaves != null)
                        togWaves.interactable = true;
                    if (togBoth != null)
                        togBoth.interactable = true;
                    if (iHavePitchControl)
                        pitchSlider.Interactable = true;
                    if (iHaveWidthControl)
                        widthSlider.Interactable = true;
                    if (lambdaSlider != null)
                        lambdaSlider.Interactable = true;
                    if (iHaveScaleControl)
                        scaleSlider.Interactable = true;
                }
                if (vectorDrawing != null)
                    vectorDrawing.DisplayRect =  displayRect;
            }
            else
            {
                if (togParticles != null)
                    togParticles.interactable = false;
                if (togBoth != null)
                    togBoth.interactable = false;
                if (togWaves != null)
                {
                    if (!togWaves.isOn)
                        togWaves.isOn = true;
                    togWaves.interactable = false;
                }
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
                if (lambdaSlider != null)
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
                    vectorDrawing.DisplayRect = defaultDisplayRect;
            }
            displayMode = value;
            updateNeeded = true;
        }
    }

    private void updateGrating()
    {
        if (particleSim != null)
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
                vectorDrawing.SlitWidth = slitWidth;
            if (particleSim != null)
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
                vectorDrawing.SlitPitch = slitPitch;
            if (particleSim != null)
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
                vectorDrawing.SlitCount = slitCount;
            if (particleSim != null)
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
            simScale = Mathf.Max(1.0f,value);
            if (iHaveWaveCRT)
                matSimControl.SetFloat("_Scale", simScale);
            if (iHaveScaleControl)
                scaleSlider.SetValue(simScale);
            if (vectorDrawing != null)
                vectorDrawing.VecScale = mmToMetres/simScale;
            if (particleSim != null)
                particleSim.SetProgramVariable<float>("simScale",simScale);
            updateNeeded = true;
        }
    }


    private void updateMomentum()
    {
        momentum = 1 / (lambda * mmToMetres);
        if (lblMomentum != null)
            lblMomentum.text = string.Format("p={0:0.0}", momentum);
        if (particleSim != null)
        {
            particleSim.SetProgramVariable<float>("maxParticleK", 1 / (minLambda * mmToMetres));
            particleSim.SetProgramVariable<float>("minParticleK", 1 / (maxLambda * mmToMetres));
            particleSim.SetProgramVariable<float>("particleK", momentum);
        }
    }

    public Color spectrumColour(float wavelength, float gamma = 0.8f)
    {
        Color result = Color.white;
        if (wavelength >= 380 & wavelength <= 440)
        {
            float attenuation = 0.3f + 0.7f * (wavelength - 380.0f) / (440.0f - 380.0f);
            result.r = Mathf.Pow(((-(wavelength - 440) / (440 - 380)) * attenuation), gamma);
            result.g = 0.0f;
            result.b = Mathf.Pow((1.0f * attenuation), gamma);
        }

        else if (wavelength >= 440 & wavelength <= 490)
        {
            result.r = 0.0f;
            result.g = Mathf.Pow((wavelength - 440f) / (490f - 440f), gamma);
            result.b = 1.0f;
        }
        else if (wavelength >= 490 & wavelength <= 510)
        {
            result.r = 0.0f;
            result.g = 1.0f;
            result.b = Mathf.Pow(-(wavelength - 510f) / (510f - 490f), gamma);
        }
        else if (wavelength >= 510 & wavelength <= 580)
        {
            result.r = Mathf.Pow((wavelength - 510f) / (580f - 510f), gamma);
            result.g = 1.0f;
            result.b = 0.0f;
        }
        else if (wavelength >= 580f & wavelength <= 645f)
        {
            result.r = 1.0f;
            result.g = Mathf.Pow(-(wavelength - 645f) / (645f - 580f), gamma);
            result.b = 0.0f;
        }
        else if (wavelength >= 645 & wavelength <= 750)
        {
            float attenuation = 0.3f + 0.7f * (750 - wavelength) / (750 - 645);
            result.r = Mathf.Pow(1.0f * attenuation, gamma);
            result.g = 0.0f;
            result.b = 0.0f;
        }
        else
        {
            result.r = 0.0f;
            result.g = 0.0f;
            result.b = 0.0f;
            result.a = 0.1f;
        }
        return result;
    }

    private Color flowColour = Color.magenta;

    public Color FlowColour
    {
        get => flowColour;
        set
        {
            flowColour = value;
            if (matPanel != null)
            {
                matPanel.SetColor("_ColorFlow", flowColour);
            }
        }
    }

    public Color DisplayColor
    {
        get => displayColor;
        set
        {
            displayColor = value;
            if (particleSim != null)
                particleSim.SetProgramVariable<Color>("displayColor", displayColor);
            FlowColour = displayColor;
            RequestSerialization();
        }
    }
    private void SetColour()
    {
        float frac = Mathf.InverseLerp(minLambda, maxLambda, lambda);
        Color dColour = spectrumColour(Mathf.Lerp(400, 700, frac));
        dColour.r = Mathf.Clamp(dColour.r, 0.2f, 2f);
        dColour.g = Mathf.Clamp(dColour.g, 0.2f, 2f);
        dColour.b = Mathf.Clamp(dColour.b, 0.2f, 2f);
        DisplayColor = dColour;
    }

    private float Lambda
    {
        get => lambda;
        set
        {
            lambda = value;
            //phaseRate = 35f/value;
            if (vectorDrawing != null)
                vectorDrawing.Lambda = lambda;
            if (iHaveWaveCRT)
                matSimControl.SetFloat("_Lambda", lambda * mmToPixels);
            if (lambdaSlider != null) 
                lambdaSlider.SetValue(lambda);
            SetColour();
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
        iHaveScaleControl = scaleSlider != null;

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
        if (lambdaSlider != null)
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
        ContrastVal = contrastVal;
        if (contrastSlider != null)
            contrastSlider.SetValue(contrastVal);

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
