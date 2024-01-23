using System.Diagnostics;
using System.Diagnostics.Contracts;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]

public class WavePanelControl : UdonSharpBehaviour
{
    [Tooltip("Wave Display Mesh")] public MeshRenderer thePanel;
    private Material matSIM = null;
    private bool iHaveSimMaterial = false;
 
    [SerializeField] UdonSlider speedSlider;
    [SerializeField] public bool speedPtr = false;
    private bool iHaveSpeedControl = false;
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(WaveSpeed))] public float waveSpeed;
    [SerializeField] float defaultSpeed = 1;

    [SerializeField] Toggle togPlay;
    [SerializeField,UdonSynced, FieldChangeCallback(nameof(PlaySim))] bool playSim;

    [SerializeField, UdonSynced, FieldChangeCallback(nameof(DisplayMode))]
    public int displayMode;

    [SerializeField] Toggle togReal;
    bool iHaveTogReal = false;
    [SerializeField] Toggle togImaginary;
    bool iHaveTogIm = false;
    [SerializeField] Toggle togRealPwr;
    bool iHaveTogRealPwr = false;
    [SerializeField] Toggle togImPwr;
    bool iHaveToImPwr = false;
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

    private void UpdatePhaseSpeed()
    {
        if (iHaveSimMaterial)
            matSIM.SetFloat("_PhaseSpeed", playSim ? waveSpeed * defaultLambda / lambda : 0f);
    }

    float WaveSpeed
    {
        get => waveSpeed;
        set
        {
            waveSpeed = Mathf.Clamp(value,0,5);
            if (!speedPtr && iHaveSpeedControl)
                speedSlider.SetValue(waveSpeed);
            UpdatePhaseSpeed();
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
            UpdatePhaseSpeed();
            RequestSerialization();
        }
    }

    private void updateGrating()
    {
        if (!iHaveSimMaterial)
            return;
        matSIM.SetFloat("_SlitPitchPx", slitPitch);
        if (numSources > 1 && slitPitch <= slitWidth)
        {
            float gratingWidth = (numSources - 1) * slitPitch + slitWidth;
            matSIM.SetFloat("_NumSources", 1f);
            matSIM.SetFloat("_SlitWidePx", gratingWidth);
            return;
        }
        matSIM.SetFloat("_NumSources", numSources);
        matSIM.SetFloat("_SlitWidePx", slitWidth);
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
            if (iHaveSimMaterial)
                matSIM.SetFloat("_DisplayMode", value);
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
                    if (iHaveToImPwr && !togImPwr.isOn)
                        togImPwr.isOn = true;
                    break;
                case 4:
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
            if (iHaveSimMaterial)
                matSIM.SetFloat("_LambdaPx", lambda);
            if (!lambdaPtr && iHaveLambdaControl)
                    lambdaSlider.SetValue(value);
            UpdatePhaseSpeed();
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
            if (iHaveSimMaterial)
                matSIM.SetFloat("_Scale", simScale);
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
        if (iHaveToImPwr && togImPwr.isOn && displayMode != 3)
        {
            DisplayMode = 3;
            return;
        }
        if (iHaveTogProb && togProbability.isOn && displayMode != 4)
        {
            DisplayMode = 4;
            return;
        }
    }

    void Start()
    {
        if (thePanel != null)
            matSIM = thePanel.material;
        iHaveSimMaterial = matSIM != null;
        if (iHaveSimMaterial)
        {
            defaultWidth = matSIM.GetFloat("_SlitWidePx");
            defaultLambda = matSIM.GetFloat("_LambdaPx");
            defaultScale = matSIM.GetFloat("_Scale");
            defaultSpeed = matSIM.GetFloat("_PhaseSpeed");
            defaultPitch = matSIM.GetFloat("_SlitPitchPx"); 
            defaultSources = Mathf.RoundToInt(matSIM.GetFloat("_NumSources"));
        }
        slitPitch = defaultPitch;
        slitWidth = defaultWidth;
        numSources = defaultSources;

        iHaveTogReal = togReal != null;
        iHaveTogIm = togImaginary  != null;
        iHaveTogRealPwr = togRealPwr != null;
        iHaveToImPwr = togImPwr != null;
        iHaveTogProb = togProbability  != null;
        iHaveWidthControl = widthSlider != null;
        iHaveSpeedControl = speedSlider != null;
        iHaveLambdaControl = lambdaSlider != null;
        iHaveScaleControl = scaleSlider != null;
        iHavePitchControl = pitchSlider != null;
        if (iHaveSimMaterial)
        {
            DisplayMode = Mathf.RoundToInt(matSIM.GetFloat("_DisplayMode"));
        }

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
