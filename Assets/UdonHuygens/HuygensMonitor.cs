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
    [SerializeField,FieldChangeCallback(nameof(DisplayMode))]
    public int displayMode = 1;
    [SerializeField] Vector2Int simResolution = new Vector2Int(2048,1280);
    [Tooltip("Panel Width (mm)"),SerializeField] float panelWidth = 2.048f;
    [SerializeField] Vector2 displayRect = new Vector2(1.99f, 1.33f);
    [SerializeField] Vector2 defaultDisplayRect = new Vector2(1.95f,0.95f);
    [SerializeField] float sourceOffset;
    [SerializeField,UdonSynced,FieldChangeCallback(nameof(NumSources))] int numSources = 2;
    [SerializeField] TextMeshProUGUI lblSourceCount;
    [SerializeField, Range(20,500), FieldChangeCallback(nameof(SlitPitch))] 
    private float slitPitch = 447f;

    [SerializeField, Range(20, 500), FieldChangeCallback(nameof(SlitWidth))]
    private float slitWidth = 10;

    [Header("Controls")]
    [SerializeField, FieldChangeCallback(nameof(WaveSpeed))] public float waveSpeed;

    [SerializeField] UdonSlider speedSlider;

    [SerializeField]
    private SyncedSlider lambdaSlider;
    private bool iHaveLambdaControl = false;
    [SerializeField]
    private SyncedSlider pitchSlider;
    private bool iHavePitchControl = false;

    [SerializeField]
    private SyncedSlider scaleSlider;
    private bool iHaveScaleControl = false;

    [SerializeField]
    private SyncedSlider widthSlider;
    private bool iHaveWidthControl = false;

    [SerializeField,Range(30,80), FieldChangeCallback(nameof(Lambda))]
    private float lambda = 1f;

    [SerializeField,Range(1,10),FieldChangeCallback(nameof(SimScale))] 
    private float simScale = 1f;
    [SerializeField] private UdonBehaviour vectorDrawing;

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
    private bool iHaveSimMaterial = false;
    [SerializeField]
    private bool iHaveSimControl = false;

    private float defaultLambda;
    private float defaultPitch;
    private float defaultScale;

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
                if (iHavePitchControl)
                    pitchSlider.Interactable = false;
                if (iHaveWidthControl)
                    widthSlider.Interactable = false;
                if (iHaveLambdaControl)
                    lambdaSlider.Interactable = false;
                if (iHaveScaleControl)
                    scaleSlider.Interactable = false;
                if (useCRT)
                    simCRT.Initialize();
                SlitPitch = defaultPitch;
                Lambda = defaultLambda;
                NumSources = 2;
                SimScale = defaultScale;
                if (vectorDrawing != null)
                    vectorDrawing.SetProgramVariable<Vector2>("displayRect", defaultDisplayRect);
            }
            displayMode = value;
            updateNeeded = true;
        }
    }

    private void updateGrating()
    {
        if (!iHaveSimMaterial)
            return;
        matSimControl.SetFloat("_SlitPitch", slitPitch * mmToPixels);
        if (numSources > 1 && slitPitch <= slitWidth)
        {
            float gratingWidth = (numSources - 1) * slitPitch + slitWidth;
            matSimControl.SetFloat("_SlitCount", 1f);
            matSimControl.SetFloat("_SlitWidth", gratingWidth * mmToPixels);
            return;
        }
        matSimControl.SetFloat("_SlitCount", numSources);
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
            updateGrating();
        }
    }

    public int NumSources
    {
        get => numSources;
        set
        {
            if (value < 1)
                value = 1;
            if (value > 7) 
                value = 7;
            numSources = value;
            updateGrating();
            if (lblSourceCount != null)
                lblSourceCount.text = numSources.ToString();
            if (vectorDrawing != null)
                vectorDrawing.SetProgramVariable<int>("numSources", numSources);
            RequestSerialization();
        }
    }

    public void decSrc()
    {
        NumSources -= 1;
    }
    public void incSrc()
    {
        NumSources += 1;
    }

    public float SimScale
    {
        get => simScale;
        set
        {
            simScale = value;
            if (iHaveSimMaterial)
                matSimControl.SetFloat("_Scale", simScale);
            updateNeeded = true;
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
            if (iHaveSimMaterial)
                matSimControl.SetFloat("_Lambda", lambda * mmToPixels);
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
        iHavePitchControl = pitchSlider != null;
        iHaveWidthControl = widthSlider != null;
        iHaveLambdaControl = lambdaSlider != null;
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

        defaultLambda = lambda;
        defaultPitch = slitPitch;
        defaultScale = simScale;

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
        iHaveSimMaterial = matSimControl != null;
        Lambda = lambda;
        if (iHaveLambdaControl)
            lambdaSlider.SetValues(lambda, 30, 80);
        SlitPitch = slitPitch;
        if (iHavePitchControl)
            pitchSlider.SetValues(slitPitch, 50, 500);
        SlitWidth = slitWidth;
        if (iHaveWidthControl)
            widthSlider.SetValues(slitWidth, 1, 30);
       // if (iHaveSimMaterial)
       //     defaultWidth = matSimControl.GetFloat("_SlitWidth") / mmToPixels;
        Lambda = lambda;
        SlitPitch = slitPitch;
        SlitWidth = slitWidth;
        DisplayMode = displayMode;
        SimScale = simScale;
        if (iHaveScaleControl)
            scaleSlider.SetValues(simScale, 1, 10);
    }
}
