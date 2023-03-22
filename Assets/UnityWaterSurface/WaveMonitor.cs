
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;
using TMPro;
using System.Net.Sockets;

public class WaveMonitor : UdonSharpBehaviour
{
    public CustomRenderTexture texture;
    public Vector2 tankDimensions = new Vector2(2,2);
    public int tankResolutionX = 1536;
    public int tankResolutionY = 1536;
    [SerializeField]
    WaveSlitControls gratingControl;

    [Header("Stimulus")]
    public float effectX = 3;
    //[SerializeField]
    private Vector4 effect;
    private float effectPhase;
    [Range(1f, 3f), SerializeField,UdonSynced,FieldChangeCallback(nameof(Frequency))]
    float frequency = 1f;
    //public Slider frequencySlider;
    public SyncedSlider frequencyControl;
    [UdonSynced, FieldChangeCallback(nameof(FreqPointerIsDown))]
    bool freqPointerIsDown = false;

    private float minFrequency = 1;
    private float maxFrequency = 3;
    private bool isStarted = false;
    public bool IsStarted { get => isStarted; private set => isStarted = value; }
    float LambdaPixels { get => waveSpeedPixels / Mathf.Clamp(frequency, minFrequency, maxFrequency); }

    public float Frequency 
    { 
        get
        {
            if (freqPointerIsDown)
                return requestedFrequency;
            return frequency;
        } 
        set
        {
            if (value != requestedFrequency)
            {
                if (frequencyControl!=null)
                {
                    if (frequencyControl.CurrentValue != value)
                        frequencyControl.CurrentValue = value;
                }
                requestedFrequency = value;
                if (frequencyQuenchTime <= 0)
                    CalcParameters();
            }
            RequestSerialization();
        } 
    }

    public bool FreqPointerIsDown
    {
        get => FreqPointerIsDown;
        set
        {
            if (freqPointerIsDown != value)
            {
                freqPointerIsDown = value;
                if (value)
                { 
                    if (frequencyQuenchTime <= 0)
                    {
                        frequencyQuenchTime = effectRampDuration;
                        frequencyQuenchDuration = effectRampDuration;
                        frequencyQuenchStartValue = effectPhase;
                    }
                }
                else
                { 
                    if (frequencyQuenchTime <= 0)
                        ResetEffect();
                }
                RequestSerialization();
            }
        }
    }


    public float MaxFrequency { get => maxFrequency;}
    public float MinFrequency { get => minFrequency; private set => minFrequency = value; }

    public float LambdaMin
    {
        get => WaveSpeed / maxFrequency;
    }
    [Header("Wave paraemters")]

    public float CFL = 0.5f;
    float CFLSq = 0.25f;
    float AbsorbFactor = 0.25f;
    [SerializeField]
    private float waveSpeedPixels = 40; // Speed

    float dt; // Time step
    float effectPeriod = 1;
    // Wave properties
    public float WaveSpeed
    {
        get
        {
            return (waveSpeedPixels * tankDimensions.x) / tankResolutionX;
        }
    }
    float lambdaEffect = 1;
    public Material simulationMaterial = null;
    public Material surfaceMaterial = null;

    [Header("Obstacles")]
    public RenderTexture obstaclesTex;
    public Camera obstaclesCamera;

    [Header("UI Toggles")]
    public Toggle TogViewDisplacement;
    public Toggle TogViewTension;
    public Toggle TogViewAmplitudeSquare;
    public Toggle TogViewEnergy;
    public Toggle TogglePlay;
    public Toggle TogglePause;
    public Toggle ToggleReset;
    [SerializeField, UdonSynced,FieldChangeCallback(nameof(ShowDisplacement))]
    private bool showDisplacement = true;
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(ShowTension))]
    private bool showTension = false;
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(ShowSquaredAmplitudes))]
    private bool showSquaredAmplitudes = false;
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(ShowEnergy))]
    private bool showEnergy = false;
    [SerializeField, UdonSynced,FieldChangeCallback(nameof(AnimationPlay))]
    private bool animationPlay = true;
    [UdonSynced, FieldChangeCallback(nameof(ResetTriggerState))]
    private int resetTriggerState = 0;
    private int resetDoneState = 0;

    public int ResetTriggerState
    { 
        get => resetTriggerState;
        set
        {
            if (value != resetTriggerState)
            {
                resetTriggerState = value;
                if (resetDoneState != resetTriggerState)
                {
                    resetDoneState = value;
                    if (texture != null)
                    {
                        texture.Initialize();
                    }
                    ResetEffect();
                }
                RequestSerialization();
            }
        }
    } 
    public bool AnimationPlay 
    { 
        get => animationPlay;
        set
        {
            if ((value) && (TogglePlay != null) &&  (!TogglePlay.isOn)) 
                TogglePlay.isOn = true;
            if ((!value) && (TogglePause != null) && (!TogglePause.isOn))
                TogglePause.isOn = true;
            animationPlay= value;
            RequestSerialization();
        }
    }
    private void UpdateUI()
    {
        if (TogViewDisplacement != null)
            TogViewDisplacement.isOn = ShowDisplacement;
        if (TogViewTension != null)
            TogViewTension.isOn = ShowTension;
        if (TogViewAmplitudeSquare != null)
            TogViewAmplitudeSquare.isOn = ShowSquaredAmplitudes;
        if (TogViewEnergy != null)
            TogViewEnergy.isOn = ShowEnergy;
    }

    private void UpdateViewControl()
    {
        if (surfaceMaterial == null)
            return;
        if (showDisplacement)
            surfaceMaterial.SetFloat("_ViewSelection", showSquaredAmplitudes ? 1f : 0f );
        else if (showTension)
             surfaceMaterial.SetFloat("_ViewSelection", showSquaredAmplitudes ? 3f : 2f);
        else if (ShowEnergy)
            surfaceMaterial.SetFloat("_ViewSelection", showSquaredAmplitudes ? 5f : 4f); 
    }

    /*private void UpdateFrequencyUI()
    {
        if (frequencySlider != null)
        {
            if (frequencySlider.value != requestedFrequency)
                frequencySlider.value = requestedFrequency;
        }
        if (frequencyLabel != null)
            frequencyLabel.text = string.Format("{0:0.00}Hz",requestedFrequency);
    }*/


    bool ShowDisplacement
    {
        get { return showDisplacement; }
        set
        {
            if (showDisplacement != value)
            {
                showDisplacement = value;
                UpdateViewControl();
                if (!animationPlay)
                    UpdateWaves(0);
                RequestSerialization();
            }
        }
    }

    bool ShowTension
    {
        get { return showTension; }
        set
        {
            if (value != showTension)
            {
                showTension = value;
                UpdateViewControl();
                if (!animationPlay)
                    UpdateWaves(0);
                RequestSerialization();
            }
        }
    }

    bool ShowEnergy
    {
        get { return showEnergy; }
        set
        {
            if (value != showEnergy)
            {
                showEnergy = value;
                UpdateViewControl();
                if (!animationPlay)
                    UpdateWaves(0);
                RequestSerialization();
            }
        }
    }

    bool ShowSquaredAmplitudes
    {
        get { return showSquaredAmplitudes; }
        set
        {
            if (value != showSquaredAmplitudes)
            {
                showSquaredAmplitudes = value;
                UpdateViewControl();
                if (!animationPlay)
                    UpdateWaves(0);
                RequestSerialization();
            }
        }
    }


    
    //* Slider changed
    float frequencyQuenchTime = 0;
    float frequencyQuenchDuration = 0;
    float frequencyQuenchStartValue = 0;
    float effectRampDuration = 5;
    float requestedFrequency;

    //* UI Toggle Handlers

    public void PlayChanged()
    {
        if (TogglePlay != null)
        {
            if (TogglePlay.isOn)
            {
                AnimationPlay = true;
            }
        }
    }

    public void PauseChanged()
    {
        if (TogglePause != null)
        {
            if (TogglePause.isOn)
                AnimationPlay = false;
        }
    }

    public void ResetChanged()
    {
        if (ToggleReset != null)
        {
            if (ToggleReset.isOn)
            {
                ResetTriggerState = resetTriggerState+1;
                ToggleReset.isOn = false;
            }
        }
    }

    public void ViewHeightChanged()
    {
        if (TogViewDisplacement != null)
        {
            ShowDisplacement = TogViewDisplacement.isOn;
        }
    }

    public void ViewTensionChanged()
    {
        if (TogViewTension != null)
        {
            ShowTension = TogViewTension.isOn;
        }
    }

    public void ViewSquareChanged()
    {
        if (TogViewAmplitudeSquare != null)
        {
            ShowSquaredAmplitudes = TogViewAmplitudeSquare.isOn;
        }
    }

    public void ViewEnergyChanged()
    {
        if (TogViewEnergy != null)
        {
                ShowEnergy = TogViewEnergy.isOn;
        }
    }


    void CalcParameters()
    {
        CFLSq = CFL * CFL;
        AbsorbFactor = (CFL - 1) / (1 + CFL);
        effectPeriod = 1/frequency;
        dt = CFL / waveSpeedPixels;
        effectRampDuration = effectPeriod * 9;
        // Calculate dt using c in Pixels per second
        // CFL = cdt/dx (dt*(c/dx + c/dy));
        // c is pixels per sec and dx=dy=1 (1 pixel)
        // dt = CFL/(c/1+c/1);
        // dt = CFL/(c);
        lambdaEffect = waveSpeedPixels * effectPeriod;
        requestedFrequency= frequency;
        if (simulationMaterial != null)
        {
            simulationMaterial.SetFloat("_CdTdX^2", CFLSq);
            simulationMaterial.SetFloat("_CdTdX", CFL);
            simulationMaterial.SetFloat("_T2Radians", frequency*(Mathf.PI*2));
            simulationMaterial.SetFloat("_DeltaT", dt);
            simulationMaterial.SetFloat("_Lambda2PI", lambdaEffect/(2*Mathf.PI));
            simulationMaterial.SetVector("_Effect", effect);
            simulationMaterial.SetFloat("_EffectPhase", effectPhase);
            simulationMaterial.SetFloat("_CFAbsorb", AbsorbFactor);
        }
    }

    private void UpdateOwnerShip()
    {
        bool isLocal = Networking.IsOwner(this.gameObject);
        if (frequencyControl != null)
            frequencyControl.IsInteractible = isLocal;

        if (TogViewDisplacement != null)
            TogViewDisplacement.interactable = isLocal;
        if ( TogViewTension != null)
            TogViewTension.interactable = isLocal;
        if (TogViewAmplitudeSquare != null)
            TogViewAmplitudeSquare.interactable = isLocal;
        if (TogViewEnergy != null)
            TogViewEnergy.interactable = isLocal;
        if (TogglePlay != null)
            TogglePlay.interactable = isLocal;
        if (TogglePause != null)
            TogglePause.interactable = isLocal;
        if (ToggleReset != null)
            ToggleReset.interactable = isLocal;
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        UpdateOwnerShip();
    }


    void Start()
    {
        if (frequencyControl != null)
        {
            requestedFrequency = frequency;
            frequencyControl.SetValues(frequency,minFrequency,maxFrequency);
        }
        CalcParameters();
        showDisplacement= false; // Force update of displacement visible
        ShowDisplacement = true;
        UpdateUI();
        UpdateOwnerShip();

        if (texture != null)
        {
            if (simulationMaterial!= null)
            {
                texture.material = simulationMaterial;
            }
            texture.Initialize();
        }
        ResetEffect();
        if (obstaclesTex != null)
        {
            if (obstaclesCamera == null)
                obstaclesCamera = GetComponentInChildren<Camera>();
            if (obstaclesCamera != null)
            {
                obstaclesCamera.clearFlags = CameraClearFlags.SolidColor;
                obstaclesCamera.targetTexture = obstaclesTex;
                obstaclesCamera.backgroundColor = Color.black;
                obstaclesCamera.orthographic = true;
                // Camera orthographic size is image height in space/2 width is determined by aspect ratio
                //obstaclesCamera.orthographicSize = SimDimensions.y / 2f;
            }
        }
        IsStarted = true;
    }

    void UpdateWaves(float dt)
    {
        texture.Update(1);
    }

    float waveTime = 0;
    float updateTime = 0;
    void ResetEffect()
    {
        frequency = requestedFrequency;
        CalcParameters();
        waveTime = 0;
        updateTime = 0;
    }

    float pollTime = 0;
    float currentGratingWidth = 0;
    void Update()
    {
        if (animationPlay)
        {
            float delta = Time.deltaTime;
            waveTime += delta;
            pollTime -= delta;
            if (pollTime < 0)
            {
                effect.x = effectX;
                pollTime = 0.5f;
                if (gratingControl != null)
                {
                    if (gratingControl.GratingWidth != currentGratingWidth)
                    {
                        currentGratingWidth = gratingControl.GratingWidth;
                        int effectLen = Mathf.FloorToInt(Mathf.Clamp01(currentGratingWidth / tankDimensions.y) * tankResolutionY);
                        effect.y = Mathf.Floor(Mathf.Clamp(tankResolutionY / 2 - (effectLen / 2 + (3 * LambdaPixels)), 0, tankResolutionY / 2));
                        effect.z = tankResolutionY - effect.y;

                        if (simulationMaterial != null)
                        {
                            simulationMaterial.SetVector("_Effect", effect);
                        }
                    }
                }
                else
                {
                    effect.y = 0; effect.z = tankResolutionY - 1;
                    if (simulationMaterial != null)
                    {
                        simulationMaterial.SetVector("_Effect", effect);
                    }
                }
            }
            if (frequencyQuenchTime > 0)
            {
                frequencyQuenchTime -= Time.deltaTime;
                effectPhase = Mathf.Lerp(0, frequencyQuenchStartValue, frequencyQuenchTime / frequencyQuenchDuration);
                if (simulationMaterial != null)
                    simulationMaterial.SetFloat("_EffectPhase", effectPhase);
                if (frequencyQuenchTime <= 0)
                {
                    frequency = requestedFrequency;
                    RequestSerialization();
                    CalcParameters();
                    if (!freqPointerIsDown)
                        ResetEffect();
                }
            }

            if (waveTime <= effectRampDuration)
            {
                effectPhase = Mathf.Lerp(0, 1, waveTime / effectRampDuration);
                if (simulationMaterial != null)
                    simulationMaterial.SetFloat("_EffectPhase", effectPhase);
            }
            
            while (updateTime < waveTime)
            {
                updateTime += dt;
                UpdateWaves(dt);
            }
        }
    }     
}
