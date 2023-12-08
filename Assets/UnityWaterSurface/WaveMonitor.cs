
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
    public float effectAmplitude = 1f;
    //[SerializeField]
    private Vector4 driveSettings;
    private float driverAmplitude;
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
    private float LambdaPixels { get => waveSpeedPixels / Mathf.Clamp(frequency, minFrequency, maxFrequency); }
    private bool iamOwner;
    private VRCPlayerApi player;
    private VRC.Udon.Common.Interfaces.NetworkEventTarget toTheOwner = VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner;
    private VRC.Udon.Common.Interfaces.NetworkEventTarget toAll = VRC.Udon.Common.Interfaces.NetworkEventTarget.All;

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
                        frequencyQuenchStartValue = driverAmplitude;
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
    [Header("Wave parameters")]
    [SerializeField] private float waveSpeedPixels = 40; // Speed
    [SerializeField] private float CFL = 0.5f;

    [Header("Calculated-Debug")]
    [SerializeField] private float CFLSq = 0.25f;
    [SerializeField] private float effectPeriod = 1;
    [SerializeField] private float absorbFactor = 0.25f;
    [SerializeField] private float dt; // Time step
    [SerializeField] private float angularWaveNumber = 40; // Speed
    [SerializeField] private float lambdaEffect = 1;

    // Wave properties
    public float WaveSpeed
    {
        get
        {
            return (waveSpeedPixels * tankDimensions.x) / tankResolutionX;
        }
    }
    [Header("Wave Sim Materials")]
    [Tooltip("Custom Render Texture")] public Material simulationMaterial = null;
    [Tooltip("Wave Surface Texture")] public Material surfaceMaterial = null;

    [Header("Render Texture Obstacles and Camera")]
    public RenderTexture obstaclesTex;
    public Camera obstaclesCamera;

    [Header("UI Toggles")]
    public Toggle TogViewDisplacement;
    public Toggle TogViewForce;
    public Toggle TogViewVelocity;
    public Toggle TogViewSquare;
    public Toggle TogViewEnergy;
    public Toggle TogglePlay;
    public Toggle TogglePause;
    [SerializeField, UdonSynced,FieldChangeCallback(nameof(ShowDisplacement))]
    private bool showDisplacement = true;
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(ShowVelocity))]
    private bool showVelocity = false;
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(ShowForce))]
    private bool showForce = false;
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(ShowSquareAmplitudes))]
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
            if (value && (TogglePlay != null) &&  (!TogglePlay.isOn)) 
                TogglePlay.isOn = true;
            if ((!value) && (TogglePause != null) && (!TogglePause.isOn))
                TogglePause.isOn = true;
            animationPlay= value;
            RequestSerialization();
        }
    }
    private void UpdateToggles()
    {
        if (TogViewDisplacement != null && showDisplacement)
            TogViewDisplacement.isOn = true;
        if (TogViewForce != null && showForce)
            TogViewForce.isOn = true;
        if (TogViewVelocity != null && showVelocity)
            TogViewVelocity.isOn = true;
        if (TogViewEnergy != null && showEnergy)
            TogViewEnergy.isOn = true;
        if (TogViewSquare != null && TogViewSquare.isOn != showSquaredAmplitudes)
            TogViewSquare.isOn = showSquaredAmplitudes;
    }

    private void UpdateViewControl()
    {
        if (surfaceMaterial == null)
            return;
        surfaceMaterial.SetFloat("_K", angularWaveNumber);
        if (showDisplacement)
            surfaceMaterial.SetFloat("_ViewSelection", showSquaredAmplitudes ? 1f : 0f );
        else if (showVelocity)
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
        get => showDisplacement;
        set
        {
            if (showDisplacement != value)
            {
                showDisplacement = value;
                UpdateViewControl();
                if (!animationPlay)
                    UpdateWaves(0);
            }
            if (showDisplacement)
                UpdateToggles();
            RequestSerialization();
        }
    }

    bool ShowForce
    {
        get { return showForce; }
        set
        {
            if (value != showForce)
            {
                showForce = value;
                UpdateViewControl();
                if (!animationPlay)
                    UpdateWaves(0);
            }
            if (showForce)
                UpdateToggles();
            RequestSerialization();
        }
    }

    bool ShowVelocity
    {
        get { return showVelocity; }
        set
        {
            if (value != showVelocity)
            {
                showVelocity = value;
                UpdateViewControl();
                if (!animationPlay)
                    UpdateWaves(0);
            }
            if (showVelocity)
                UpdateToggles();
            RequestSerialization();
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
            }
            if (showEnergy)
                UpdateToggles();
            RequestSerialization();
        }
    }

    bool ShowSquareAmplitudes
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
                UpdateToggles();
            }
            RequestSerialization();
        }
    }


    
    //* Slider changed
    float frequencyQuenchTime = 0;
    float frequencyQuenchDuration = 0;
    float frequencyQuenchStartValue = 0;
    float effectRampDuration = 5;
    float requestedFrequency;

    //* UI Toggle Handlers

    public void PlayWaves()
    {
        AnimationPlay = true;
    }

    public void PlayChanged()
    {
        if (TogglePlay != null)
        {
            if (TogglePlay.isOn)
            {
                if (iamOwner)
                    AnimationPlay = true;
                else
                    SendCustomNetworkEvent(toTheOwner,nameof(PlayWaves));
            }
        }
    }

    bool pointerDown = false;
    public void OnFreqPointerDown()
    {
        pointerDown = true;
        if (iamOwner)
        {
            FreqPointerIsDown = true;
        }
        else
        {
            Networking.SetOwner(player, gameObject);
        }
    }

    public void OnFreqPointerUp()
    {
        pointerDown = false;
        FreqPointerIsDown = false;
    }


    public void PauseWaves()
    {
        AnimationPlay = false;
    }
    public void PauseChanged()
    {
        if (TogglePause != null)
        {
            if (TogglePause.isOn)
            {
                if (iamOwner)
                    AnimationPlay = false;
                else
                    SendCustomNetworkEvent(toTheOwner,nameof(PauseWaves));
            }
        }
    }

    public void ResetWaves()
    {
        ResetTriggerState = resetTriggerState + 1;
    }
    public void onReset()
    {
        SendCustomNetworkEvent(toAll, nameof(ResetWaves));
    }

    public void ViewHeightChanged()
    {
        if (TogViewDisplacement != null)
        {
            ShowDisplacement = TogViewDisplacement.isOn;
        }
    }

    public void ViewForceChanged()
    {
        if (TogViewForce != null)
        {
            ShowForce = TogViewForce.isOn;
        }
    }

    public void ViewVelocityChanged()
    {
        if (TogViewVelocity != null)
        {
            ShowVelocity = TogViewVelocity.isOn;
        }
    }

    public void ViewSquareChanged()
    {
        if (TogViewSquare != null)
        {
            ShowSquareAmplitudes = TogViewSquare.isOn;
        }
        ShowSquareAmplitudes = TogViewSquare.isOn;
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
        absorbFactor = (CFL - 1) / (1 + CFL);
        effectPeriod = 1/frequency;
        dt = CFL / waveSpeedPixels;
        effectRampDuration = effectPeriod * 5;
        lambdaEffect = waveSpeedPixels * effectPeriod;
        angularWaveNumber = Mathf.PI * 2/lambdaEffect;
        // Calculate dt using c in Pixels per second
        // CFL = cdt/dx (dt*(c/dx + c/dy));
        // c is pixels per sec and dx=dy=1 (1 pixel)
        // dt = CFL/(c/1+c/1);
        // dt = CFL/(c);
        requestedFrequency = frequency;
        if (simulationMaterial != null)
        {
            simulationMaterial.SetFloat("_CdTdX^2", CFLSq);
            simulationMaterial.SetFloat("_CdTdX", CFL);
            simulationMaterial.SetFloat("_KSquared", angularWaveNumber * angularWaveNumber);
            simulationMaterial.SetFloat("_K", angularWaveNumber);
            simulationMaterial.SetFloat("_DeltaT", dt);
            simulationMaterial.SetFloat("_T2Radians", frequency * (Mathf.PI * 2));
            simulationMaterial.SetVector("_DriveSettings", driveSettings);
            simulationMaterial.SetFloat("_DriveAmplitude", driverAmplitude);
            simulationMaterial.SetFloat("_CFAbsorb", absorbFactor);
        }
    }

    private void UpdateOwnerShip()
    {
        iamOwner = Networking.IsOwner(this.gameObject);
        if (iamOwner && pointerDown)
            FreqPointerIsDown = true;
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        UpdateOwnerShip();
    }


    void Start()
    {
        player = Networking.LocalPlayer;
        if (frequencyControl != null)
        {
            requestedFrequency = frequency;
            frequencyControl.SetValues(frequency,minFrequency,maxFrequency);
        }
        CalcParameters();
        showDisplacement= false; // Force update of displacement visible
        ShowDisplacement = true;
        UpdateToggles();
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
                driveSettings.x = effectX;
                pollTime = 0.5f;
                if (gratingControl != null)
                {
                    if (gratingControl.GratingWidth != currentGratingWidth)
                    {
                        currentGratingWidth = gratingControl.GratingWidth;
                        int effectLen = Mathf.FloorToInt(Mathf.Clamp01(currentGratingWidth / tankDimensions.y) * tankResolutionY);
                        driveSettings.y = Mathf.Floor(Mathf.Clamp(tankResolutionY / 2 - (effectLen / 2 + (1.5f * LambdaPixels)), 0, tankResolutionY / 2));
                        driveSettings.z = tankResolutionY - driveSettings.y;

                        if (simulationMaterial != null)
                        {
                            simulationMaterial.SetVector("_DriveSettings", driveSettings);
                        }
                    }
                }
                else
                {
                    driveSettings.y = 0; driveSettings.z = tankResolutionY - 1;
                    if (simulationMaterial != null)
                    {
                        simulationMaterial.SetVector("_DriveSettings", driveSettings);
                    }
                }
            }
            if (frequencyQuenchTime > 0)
            {
                frequencyQuenchTime -= Time.deltaTime;
                driverAmplitude = effectAmplitude *  Mathf.Lerp(0, frequencyQuenchStartValue, frequencyQuenchTime / frequencyQuenchDuration);
                if (simulationMaterial != null)
                    simulationMaterial.SetFloat("_DriveAmplitude", driverAmplitude);
                if (frequencyQuenchTime <= 0)
                {
                    frequency = requestedFrequency;
                    RequestSerialization();
                    CalcParameters();
                    UpdateViewControl();
                    if (!freqPointerIsDown)
                        ResetEffect();
                }
            }

            if (waveTime <= effectRampDuration)
            {
                driverAmplitude = effectAmplitude * Mathf.Lerp(0, 1, waveTime / effectRampDuration);
                if (simulationMaterial != null)
                    simulationMaterial.SetFloat("_DriveAmplitude", driverAmplitude);
            }
            
            while (updateTime < waveTime)
            {
                updateTime += dt;
                UpdateWaves(dt);
            }
        }
    }     
}
