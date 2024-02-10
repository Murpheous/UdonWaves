using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]

public class WavePanelControl : UdonSharpBehaviour
{
    [SerializeField, Tooltip("Custom Render texture")]
    private CustomRenderTexture simCRT;

    [Tooltip("Wave Display Mesh")] 
    public MeshRenderer thePanel;

    [SerializeField] UdonSlider speedSlider;
    [SerializeField] public bool speedPtr = false;
    private bool iHaveSpeedControl = false;
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(WaveSpeed))] public float waveSpeed;
    [SerializeField] float defaultSpeed = 1;

    [SerializeField] Toggle togPlay;
    [SerializeField,UdonSynced, FieldChangeCallback(nameof(PlaySim))] bool playSim;

    [Header("Display Mode")]
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(DisplayMode))]
    public int displayMode;

    [SerializeField] Toggle togReal;
    bool iHaveTogReal = false;
    [SerializeField] Toggle togImaginary;
    bool iHaveTogIm = false;
    [SerializeField] Toggle togRealPwr;
    bool iHaveTogRealPwr = false;
    [SerializeField] Toggle togImPwr;
    bool iHaveTogImPwr = false;
    [SerializeField] Toggle togAmplitude;
    bool iHaveTogAmp = false;
    [SerializeField] Toggle togProbability;
    bool iHaveTogProb = false;

    [SerializeField] UdonSlider lambdaSlider;
    private bool iHaveLambdaControl = false;
    public bool lambdaPtr = false;
    [SerializeField, Range(1, 100)] float defaultLambda = 24;
    [SerializeField, Range(1, 100),UdonSynced,FieldChangeCallback(nameof(Lambda))] public float lambda = 24;

    [SerializeField]
    private UdonSlider pitchSlider;
    private bool iHavePitchControl = false;
    public bool pitchPtr = false;

    [SerializeField]
    private UdonSlider widthSlider;
    private bool iHaveWidthControl = false;
    public bool widthPtr = false;


    [SerializeField, Range(20, 500), UdonSynced, FieldChangeCallback(nameof(SlitPitch))]
    float slitPitch = 250;
    float defaultPitch = 250;

    [SerializeField, Range(20, 500), UdonSynced, FieldChangeCallback(nameof(SlitWidth))]
    float slitWidth = 10;
    float defaultWidth = 10;

    int defaultSources = 2;
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(NumSources))] private int numSources = 2;
    [SerializeField] TextMeshProUGUI lblSourceCount;

    [SerializeField] UdonSlider scaleSlider;
    private bool iHaveScaleControl = false;
    public bool scalePtr = false;
    [SerializeField, Range(1, 10)] float defaultScale = 24;
    [SerializeField, Range(1, 10), UdonSynced, FieldChangeCallback(nameof(SimScale))] public float simScale = 24;

    [Header("Serialized for monitoring in Editor")]
    [SerializeField]
    private Material matPanel = null;
    [SerializeField]
    private Material matSimControl = null;
    [SerializeField]
    private Material matSimDisplay = null;
    [SerializeField, Tooltip("Check to invoke CRT Update")]
    private bool crtUpdateNeeded = false;
    [SerializeField]
    private bool iHaveCRT = false;
    [SerializeField]
    private bool iHavePanelMaterial = false;
    [SerializeField]
    private bool iHaveSimDisplay = false;
    [SerializeField]
    private bool iHaveSimControl = false;

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

    private void UpdateWaveSpeed()
    {
        if (iHaveSimDisplay)
            matSimDisplay.SetFloat("_Frequency", playSim ? waveSpeed * defaultLambda / lambda : 0f);
    }

    float WaveSpeed
    {
        get => waveSpeed;
        set
        {
            waveSpeed = Mathf.Clamp(value,0,5);
            if (!speedPtr && iHaveSpeedControl)
                speedSlider.SetValue(waveSpeed);
            UpdateWaveSpeed();
            RequestSerialization();
        }
    }

    private bool PlaySim
    {
        get => playSim;
        set
        {
            playSim = value;
            if (togPlay != null && togPlay.isOn != value)
                togPlay.isOn = value;
            UpdateWaveSpeed();
            RequestSerialization();
        }
    }

    private void updateGrating()
    {
        if (!iHaveSimControl)
            return;
        matSimControl.SetFloat("_SlitPitchPx", slitPitch);
        if (numSources > 1 && slitPitch <= slitWidth)
        {
            float gratingWidth = (numSources - 1) * slitPitch + slitWidth;
            matSimControl.SetFloat("_NumSources", 1f);
            matSimControl.SetFloat("_SlitWidePx", gratingWidth);
            return;
        }
        matSimControl.SetFloat("_NumSources", numSources);
        matSimControl.SetFloat("_SlitWidePx", slitWidth);
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
            RequestSerialization();
        }
    }
    
    public void incSources()
    {
        NumSources = numSources + 1;
    }

    public void decSources()
    {
        NumSources = numSources - 1;
    }

    private int DisplayMode
    {
        get => displayMode;
        set
        {
            displayMode = value;
            if (iHaveSimDisplay)
                matSimDisplay.SetFloat("_DisplayMode", value);
            switch (displayMode)
            {
                case 0:
                    if (iHaveTogReal && !togReal.isOn)
                        togReal.isOn = true;
                    break;
                case 1:
                    if (iHaveTogRealPwr&& !togRealPwr.isOn)
                        togRealPwr.isOn = true;
                    break;
                case 2:
                    if (iHaveTogIm && !togImaginary.isOn)
                        togImaginary.isOn = true;
                    break;
                case 3:
                    if (iHaveTogImPwr && !togImPwr.isOn)
                        togImPwr.isOn = true;
                    break;
                case 4:
                    if (iHaveTogAmp && !togAmplitude.isOn)
                        togAmplitude.isOn = true;
                    break;
                default:
                    if (iHaveTogProb && !togProbability.isOn)
                        togProbability.isOn = true;
                    break;
            }
            RequestSerialization();
        }
    }

    public void onPlayState()
    {
        if (togPlay == null)
        {
            PlaySim = !playSim;
            return;
        }
        if (playSim != togPlay.isOn)
            PlaySim = !playSim;
    }

    public float Lambda
    {
        get => lambda;
        set
        {
            lambda = value;
            if (iHaveSimControl)
                matSimControl.SetFloat("_LambdaPx", lambda);
            if (!lambdaPtr && iHaveLambdaControl)
                    lambdaSlider.SetValue(value);
            UpdateWaveSpeed();
            RequestSerialization();
        }
    }


    public float SlitPitch
    {
        get => slitPitch;
        set
        {
            slitPitch = value;
            updateGrating();
            if (!pitchPtr && iHavePitchControl)
                pitchSlider.SetValue(value);
            RequestSerialization();
        }
    }

    public float SlitWidth
    {
        get => slitWidth;
        set
        {
            slitWidth = value;
            updateGrating() ;
            if (!widthPtr && iHaveWidthControl)
                widthSlider.SetValue(value);
            RequestSerialization();
        }
    }


    public float SimScale
    {
        get => simScale;
        set
        {
            simScale = value;
            if (iHaveSimControl)
                matSimControl.SetFloat("_Scale", simScale);
            if (!scalePtr && iHaveScaleControl)
                scaleSlider.SetValue(value);
            RequestSerialization();
        }
    }

    public void onMode()
    {
        if (iHaveTogReal && togReal.isOn && displayMode != 0)
        {
            DisplayMode = 0;
            return;
        }
        if (iHaveTogIm && togImaginary.isOn && displayMode != 2)
        {
            DisplayMode = 2;
            return;
        }
        if (iHaveTogRealPwr && togRealPwr.isOn && displayMode != 1)
        {
            DisplayMode = 1;
            return;
        }
        if (iHaveTogImPwr && togImPwr.isOn && displayMode != 3)
        {
            DisplayMode = 3;
            return;
        }
        if (iHaveTogAmp && togAmplitude.isOn && displayMode != 4)
        {
            DisplayMode = 4;
            return;
        }
        if (iHaveTogProb && togProbability.isOn && displayMode != 5)
        {
            DisplayMode = 5;
            return;
        }
    }

    void Start()
    {
        iHaveTogReal = togReal != null;
        iHaveTogIm = togImaginary != null;
        iHaveTogRealPwr = togRealPwr != null;
        iHaveTogImPwr = togImPwr != null;
        iHaveTogProb = togProbability != null;
        iHaveTogAmp = togAmplitude != null;

        if (thePanel != null)
            matPanel = thePanel.material;
        iHavePanelMaterial = matPanel != null;

        if (simCRT != null)
        {
            iHaveCRT = true;
            simCRT.Initialize();
        }
        configureSimControl(PanelHasVanillaMaterial);
        if (iHaveSimControl)
        {
            defaultWidth = matSimControl.GetFloat("_SlitWidePx");
            defaultLambda = matSimControl.GetFloat("_LambdaPx");
            defaultScale = matSimControl.GetFloat("_Scale");
            defaultPitch = matSimControl.GetFloat("_SlitPitchPx"); 
            defaultSources = Mathf.RoundToInt(matSimControl.GetFloat("_NumSources"));
        }
        defaultSpeed = waveSpeed;
        if (!iHaveCRT && iHaveSimDisplay)
            defaultSpeed = matSimDisplay.GetFloat("_Frequency");

        slitPitch = defaultPitch;
        slitWidth = defaultWidth;
        numSources = defaultSources;


        iHaveWidthControl = widthSlider != null;
        iHaveSpeedControl = speedSlider != null;
        iHaveLambdaControl = lambdaSlider != null;
        iHaveScaleControl = scaleSlider != null;
        iHavePitchControl = pitchSlider != null;
        if (iHaveSimDisplay)
            DisplayMode = Mathf.RoundToInt(matSimDisplay.GetFloat("_DisplayMode"));

        Lambda = defaultLambda;
        WaveSpeed = defaultSpeed;
        SimScale = defaultScale;
        if (iHavePitchControl)
        {
            pitchSlider.MaxValue = 500;
            pitchSlider.MinValue = 20;
        }
        NumSources = defaultSources;
        SlitPitch = defaultPitch;
        SlitWidth = defaultWidth;
    }
}
