using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRC.SDKBase;
using VRC.Udon;
using System;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]

public class WavePanelControl : UdonSharpBehaviour
{
    [SerializeField, Tooltip("Custom Render texture")]
    private CustomRenderTexture simCRT;

    [Tooltip("Wave Display Mesh")] 
    public MeshRenderer thePanel;

    [SerializeField] UdonSlider speedSlider;
    private bool iHaveSpeedControl = false;
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(WaveSpeed))] public float waveSpeed;
    [SerializeField] float defaultSpeed = 1;
    [SerializeField, Tooltip("CRT Update Cadence"), Range(0.01f, 0.5f)] float dt = 0.3f;

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
    [SerializeField, Range(1, 100)] float defaultLambda = 24;
    [SerializeField, Range(1, 100),UdonSynced,FieldChangeCallback(nameof(Lambda))] public float lambda = 24;

    [SerializeField]
    private UdonSlider pitchSlider;
    private bool iHavePitchControl = false;

    [SerializeField]
    private UdonSlider widthSlider;
    private bool iHaveWidthControl = false;

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
    [SerializeField]
    private bool CRTUpdatesMovement;
    private VRCPlayerApi player;
    private bool iamOwner = false;

    private void ReviewOwnerShip()
    {
        iamOwner = Networking.IsOwner(this.gameObject);
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        ReviewOwnerShip();
    }

    public void slidePtr()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
    }

    private void configureSimControl(bool vanillaDisplay)
    {
        CRTUpdatesMovement = false;
        if (vanillaDisplay)
        { 
            if (iHaveCRT)
            { // Display mode and wave speed controls get handled by the panel material
                matSimDisplay = simCRT.material;
                matSimControl = simCRT.material;
                CRTUpdatesMovement = true;
                simCRT.material.SetFloat("_OutputRaw", 0);
                crtUpdateNeeded = true;
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
                matSimControl.SetFloat("_OutputRaw", 1);
                crtUpdateNeeded= true;
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
            if (matPanel.HasProperty("_ShowCRT"))
                return false; 
            if (matPanel.HasProperty("_DisplayMode"))
                return false;
            return true;
        }
    }

    private void UpdateWaveSpeed()
    {
        if (iHaveSimDisplay)
            matSimDisplay.SetFloat("_Frequency", playSim ? waveSpeed * defaultLambda / lambda : 0f);
        crtUpdateNeeded |= iHaveCRT;
    }

    float WaveSpeed
    {
        get => waveSpeed;
        set
        {
            waveSpeed = Mathf.Clamp(value,0,5);
            if (iHaveSpeedControl && !speedSlider.PointerDown)
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
        matSimControl.SetFloat("_SlitPitch", slitPitch);
        crtUpdateNeeded |= iHaveCRT;
        if (numSources > 1 && slitPitch <= slitWidth)
        {
            float gratingWidth = (numSources - 1) * slitPitch + slitWidth;
            matSimControl.SetFloat("_SlitCount", 1f);
            matSimControl.SetFloat("_SlitWidth", gratingWidth);
            return;
        }
        matSimControl.SetFloat("_SlitCount", numSources);
        matSimControl.SetFloat("_SlitWidth", slitWidth);
    }
    public int NumSources
    {
        get => numSources;
        set
        {
            if (value < 1)
                value = 1;
            if (value > 17)
                value = 17;
            numSources = value;
            updateGrating();
            if (lblSourceCount != null)
                lblSourceCount.text = numSources.ToString();
            RequestSerialization();
        }
    }
    
    public void incSources()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        NumSources = numSources + 1;
    }

    public void decSources()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        NumSources = numSources - 1;
    }

    private void updateDisplayTxture(int displayMode)
    {
        if (!iHaveSimDisplay)
            return;
        if (matSimDisplay.HasProperty("_DisplayMode"))
        { 
            matSimDisplay.SetFloat("_DisplayMode", displayMode);
            return;
        }
        matSimDisplay.SetFloat("_ShowCRT", displayMode >= 0 ? 1f : 0f);
        matSimDisplay.SetFloat("_ShowReal", displayMode == 0 || displayMode == 1 || displayMode >= 4 ? 1f : 0f);
        matSimDisplay.SetFloat("_ShowImaginary", displayMode == 2 || displayMode == 3 || displayMode >= 4 ? 1f : 0f);
        matSimDisplay.SetFloat("_ShowSquare", displayMode == 1 || displayMode == 3 || displayMode == 5 ? 1f : 0f);
    }
    private int DisplayMode
    {
        get => displayMode;
        set
        {
            displayMode = value;
            updateDisplayTxture(displayMode);
            crtUpdateNeeded |= iHaveCRT;
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
                matSimControl.SetFloat("_Lambda", lambda);
            crtUpdateNeeded |= iHaveCRT;
            if (iHaveLambdaControl && !lambdaSlider.PointerDown)
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
            if (iHavePitchControl && !pitchSlider.PointerDown)
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
            if (iHaveWidthControl && !widthSlider.PointerDown)
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
            crtUpdateNeeded |= iHaveCRT;
            if (iHaveScaleControl && !scaleSlider.PointerDown)
                scaleSlider.SetValue(value);
            RequestSerialization();
        }
    }

    public void onMode()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
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

    void UpdateSimulation()
    {
        if (displayMode >= 0)
            simCRT.Update(1);
        crtUpdateNeeded = false;
    }

    float waveTime = 0;
    float delta;

    private void Update()
    {
        if (!iHaveCRT)
            return;
        if (CRTUpdatesMovement)
        {
            if (playSim && displayMode >= 0 && displayMode < 4)
            {
                delta = Time.deltaTime;
                waveTime -= delta;
                if (waveTime < 0)
                {
                    waveTime += dt;
                    crtUpdateNeeded |= true;
                }
            }
        }
        if (crtUpdateNeeded)
        {
           UpdateSimulation();
        }
    }
    void Start()
    {
        player = Networking.LocalPlayer;
        ReviewOwnerShip();

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
        }

        configureSimControl(PanelHasVanillaMaterial);
        if (iHaveSimControl)
        {
            defaultWidth = matSimControl.GetFloat("_SlitWidth");
            defaultLambda = matSimControl.GetFloat("_Lambda");
            defaultScale = matSimControl.GetFloat("_Scale");
            defaultPitch = matSimControl.GetFloat("_SlitPitch"); 
            defaultSources = Mathf.RoundToInt(matSimControl.GetFloat("_SlitCount"));
        }
        defaultSpeed = waveSpeed;
        if (iHaveSimDisplay)
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
        {
            if (matSimDisplay.HasProperty("_DisplayMode"))
                DisplayMode = Mathf.RoundToInt(matSimDisplay.GetFloat("_DisplayMode"));
            else
            {
                int dMode = Mathf.RoundToInt(matSimDisplay.GetFloat("_ShowReal")) > 0 ? 1 : 0;
                dMode += Mathf.RoundToInt(matSimDisplay.GetFloat("_ShowImaginary")) > 0 ? 2 : 0;

                int nSq = Mathf.RoundToInt(matSimDisplay.GetFloat("_ShowSquare")) > 0 ? 1 : 0;
                switch (dMode)
                {
                    case 1:
                        DisplayMode = nSq;
                        break;
                    case 2: 
                        DisplayMode = 2 + nSq;
                        break;
                    case 3:
                        DisplayMode = 4 + nSq;
                        break;
                    default:
                        DisplayMode = -1;
                        break;
                }
            }
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
        crtUpdateNeeded |= iHaveCRT;
    }
}
