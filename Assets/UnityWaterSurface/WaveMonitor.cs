
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

public class WaveMonitor : UdonSharpBehaviour
{
    public CustomRenderTexture customRenderTex;
    public Vector2 tankDimensions = new Vector2(2, 2);
    public int tankResolutionX = 1536;
    public int tankResolutionY = 1536;
    [SerializeField]
    WaveSlitControls gratingControl;
    [SerializeField]
    Material matTankWalls;
    [SerializeField]
    Color TankHeightColour = Color.white;
    [SerializeField]
    Color TankVelColour = Color.blue;
    [SerializeField]
    Color TankFlowColour = Color.red;
    [Header("Stimulus")]
    public float effectX = 3;
    public float effectAmplitude = 1f;
    //[SerializeField]
    private Vector4 driveSettings;
    private float driverAmplitude;
    [Range(1f, 3f), SerializeField, UdonSynced, FieldChangeCallback(nameof(Frequency))]
    float frequency = 1f;
    //public Slider frequencySlider;
    public SyncedSlider frequencyControl;
    [SerializeField, FieldChangeCallback(nameof(FreqPointerIsDown))]
    private bool freqPointerIsDown = false;

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
                if (frequencyControl != null)
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
            }
        }
    }


    public float MaxFrequency { get => maxFrequency; }
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

    [Header("Wave Simulation Materials")]
    [Tooltip("Custom Render Texture")] public Material simulationMaterial = null;
    [Tooltip("Wave Surface Texture")] public Material surfaceMaterial = null;

    [Header("Render Texture Obstacles and Camera")]
    public RenderTexture obstaclesTex;
    public Camera obstaclesCamera;

    [Header("UI Toggles")]
    public Toggle togViewHeight;
    public Toggle togViewVelocity;
    public Toggle togViewPE;
    public Toggle togViewKE;
    public Toggle togViewEnergy;
    public Toggle TogglePlay;
    public Toggle TogglePause;
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(DisplayMode))]
    private int displayMode = 0;

    [SerializeField, UdonSynced, FieldChangeCallback(nameof(AnimationPlay))]
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
                    if (customRenderTex != null)
                    {
                        customRenderTex.Initialize();
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
            if (value && (TogglePlay != null) && (!TogglePlay.isOn))
                TogglePlay.isOn = true;
            if ((!value) && (TogglePause != null) && (!TogglePause.isOn))
                TogglePause.isOn = true;
            animationPlay = value;
            RequestSerialization();
        }
    }
    private void UpdateToggles()
    {
        switch (displayMode)
        {
            case 1:
                if (togViewPE != null && !togViewPE.isOn)
                    togViewPE.isOn = true;
                break;
            case 2:
                if (togViewVelocity != null && !togViewVelocity.isOn)
                    togViewVelocity.isOn = true;
                break;
            case 3:
                if (togViewKE != null && !togViewKE.isOn)
                    togViewKE.isOn = true;
                break;
            case 4:
                if (togViewEnergy != null && !togViewEnergy.isOn)
                    togViewEnergy.isOn = true;
                break;
            default:
                displayMode = 0;
                if (togViewHeight != null && !togViewHeight.isOn)
                    togViewHeight.isOn = true;
                break;
        }
    }

    private void UpdateTank()
    {
        if (matTankWalls == null)
            return;
        Color theColour = TankHeightColour;
        switch (displayMode)
        {
            case 2: // Velocity
            case 3: // Velocity Squared
                theColour = TankVelColour;
                break;
            case 4: // Both Squared
                theColour = TankFlowColour;
                break;
            default:
                theColour = TankHeightColour;
                break;
        }
        matTankWalls.EnableKeyword("_EMISSION");
        matTankWalls.SetColor("_EmissionColor", theColour);
    }
    private void UpdateViewControl()
    {
        if (surfaceMaterial == null)
            return;
        surfaceMaterial.SetFloat("_K", angularWaveNumber);
        switch (displayMode)
        {
            case 1: // Height Sqaured
                surfaceMaterial.SetFloat("_UseHeight", 1);
                surfaceMaterial.SetFloat("_UseVelocity", 0);
                surfaceMaterial.SetFloat("_UseSquare", 1);
                break;
            case 2: // Velocity
                surfaceMaterial.SetFloat("_UseHeight", 0);
                surfaceMaterial.SetFloat("_UseVelocity", 1);
                surfaceMaterial.SetFloat("_UseSquare", 0);
                break;
            case 3: // Velocity Squared
                surfaceMaterial.SetFloat("_UseHeight", 0);
                surfaceMaterial.SetFloat("_UseVelocity", 1);
                surfaceMaterial.SetFloat("_UseSquare", 1);
                break;
            case 4: // Both Squared
                surfaceMaterial.SetFloat("_UseHeight", 1);
                surfaceMaterial.SetFloat("_UseVelocity", 1);
                surfaceMaterial.SetFloat("_UseSquare", 1);
                break;
            default:
                surfaceMaterial.SetFloat("_UseHeight", 1);
                surfaceMaterial.SetFloat("_UseVelocity", 0);
                surfaceMaterial.SetFloat("_UseSquare", 0);
                break;
        }
    }


    private int DisplayMode
    {
        get => displayMode;
        set
        {
            displayMode = value;
            UpdateTank();
            UpdateViewControl();
            UpdateToggles();
            RequestSerialization();
            if (!animationPlay)
                UpdateWaves(0);
        }
    }

    public void Tank0()
    {
        if (iamOwner)
            DisplayMode = 0;
        else
            SendCustomNetworkEvent(toTheOwner, nameof(Tank0));
    }

    public void Tank1()
    {
        if (iamOwner)
            DisplayMode = 1;
        else
            SendCustomNetworkEvent(toTheOwner, nameof(Tank1));
    }
    public void Tank2()
    {
        if (iamOwner)
            DisplayMode = 2;
        else
            SendCustomNetworkEvent(toTheOwner, nameof(Tank2));
    }
    public void Tank3()
    {
        if (iamOwner)
            DisplayMode = 3;
        else
            SendCustomNetworkEvent(toTheOwner, nameof(Tank3));
    }
    public void Tank4()
    {
        if (iamOwner)
            DisplayMode = 4;
        else
            SendCustomNetworkEvent(toTheOwner, nameof(Tank4));
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

    public void onTogHeight()
    {
        if (togViewHeight != null && togViewHeight.isOn)
            Tank0();
    }

    public void onTogPE()
    {
        if (togViewPE != null && togViewPE.isOn)
            Tank1();
    }

    public void onTogVel()
    {
        if (togViewVelocity != null && togViewVelocity.isOn)
            Tank2();
    }

    public void onTogKE()
    {
        if (togViewKE != null && togViewKE.isOn)
            Tank3();
    }

    public void onTogEnergy()
    {
        if (togViewEnergy != null && togViewEnergy.isOn)
            Tank4();
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
        UpdateToggles();
        UpdateOwnerShip();

        if (customRenderTex != null)
        {
            if (simulationMaterial!= null)
            {
                customRenderTex.material = simulationMaterial;
            }
            customRenderTex.Initialize();
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
        DisplayMode = displayMode;
        IsStarted = true;
    }

    void UpdateWaves(float dt)
    {
        customRenderTex.Update(1);
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
