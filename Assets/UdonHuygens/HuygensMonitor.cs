
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class HuygensMonitor : UdonSharpBehaviour
{
    [SerializeField]
    CustomRenderTexture simCRT;
    [Tooltip("Material for Custom Render Texture")] public Material matCRT = null;
    [Tooltip("DisplayPanel")] public MeshRenderer thePanel;
    [SerializeField,FieldChangeCallback(nameof(DisplayMode))]
    public int displayMode = 1;
    [SerializeField] Vector2Int simResolution = new Vector2Int(2048,1024);
    [Tooltip("Panel Width (mm)"),SerializeField] float panelWidth = 2000;
    [SerializeField] float sourceOffset;
    [SerializeField,UdonSynced,FieldChangeCallback(nameof(NumSources))] int numSources = 2;
    [SerializeField] TextMeshProUGUI lblSourceCount;
    [SerializeField, Range(50,500),UdonSynced, FieldChangeCallback(nameof(SourcePitch))] 
    float sourcePitch = 436.5234f;
    // Timing
    [SerializeField, Range(0.1f, 5f)] float dt = 1f;
    [Header("Controls")]
    [SerializeField]
    private SyncedSlider lambdaSlider;
    [SerializeField]
    private SyncedSlider pitchSlider;
    [SerializeField,Range(30,80), UdonSynced, FieldChangeCallback(nameof(Lambda))]
    float lambda = 1f;
    [SerializeField] private UdonBehaviour vectorDrawing;

    private float defaultLambda;
    private float defaultPitch;
    private int DisplayMode
    {
        get => displayMode; 
        set
        {
            if (value >= 0)
            {
                matCRT.SetFloat("_DisplayMode", value);
                if (displayMode < 0) 
                {
                    if (pitchSlider != null)
                        pitchSlider.IsInteractible = true;
                    if (lambdaSlider != null)
                        lambdaSlider.IsInteractible = true;
                }
            }
            else
            {
                if (pitchSlider != null)
                    pitchSlider.IsInteractible = false;
                if (lambdaSlider != null)
                    lambdaSlider.IsInteractible = false;
                simCRT.Initialize();
                SourcePitch = defaultPitch;
                Lambda = defaultLambda;
                NumSources = 2;
            }
            displayMode = value;
            updateNeeded = true;
        }
    }
    public float SourcePitch
    {
        get => sourcePitch;
        set
        {
            if (sourcePitch != value)
            {
                if (pitchSlider != null)
                    pitchSlider.CurrentValue = value;
            }
            sourcePitch = value;
            if (vectorDrawing != null)
                vectorDrawing.SetProgramVariable<float>("sourcePitch", sourcePitch);
            if (matCRT != null)
                matCRT.SetFloat("_SlitPitchPx", sourcePitch * mmToPixels);
            if (pitchSlider != null && !pitchPtr && pitchSlider.CurrentValue != value)
                pitchSlider.CurrentValue = value;
            updateNeeded = true;
            RequestSerialization();
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
            updateNeeded = true;
            numSources = value;
            if (matCRT != null)
                matCRT.SetFloat("_NumSources", numSources);
            if (lblSourceCount != null)
                lblSourceCount.text = numSources.ToString();
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
    public float Lambda
    {
        get => lambda;
        set
        {
            if (lambda != value)
            {
                if (lambdaSlider != null)
                    lambdaSlider.CurrentValue = lambda;
            }
            lambda = value;
            if (vectorDrawing != null)
                vectorDrawing.SetProgramVariable<float>("lambda", lambda);
            if (matCRT != null)
                matCRT.SetFloat("_LambdaPx", lambda * mmToPixels);
            if (lambdaSlider != null && !lambdaPtr && lambdaSlider.CurrentValue != value)
                lambdaSlider.CurrentValue = lambda;
            updateNeeded = true;
            RequestSerialization();
        } 
    }

    [SerializeField, FieldChangeCallback(nameof(PitchPtr))]
    public bool pitchPtr = false;

    public bool PitchPtr
    {
        get => pitchPtr;
        set
        {
            Debug.Log("Ptr Change " + value);
            if (value && !iAmOwner)
                Networking.SetOwner(player, gameObject);
            pitchPtr = value;
        }
    }

    [SerializeField, FieldChangeCallback(nameof(LambdaPtr))]
    public bool lambdaPtr = false;

    public bool LambdaPtr
    {
        get => lambdaPtr;
        set
        {
            if (value && !iAmOwner)
                    Networking.SetOwner(player, gameObject);
            lambdaPtr = value;
        }
    }

    // UdonSync stuff
    private VRCPlayerApi player;
    private bool iAmOwner = false;

    private void UpdateOwnerShip()
    {
        iAmOwner = Networking.IsOwner(this.gameObject);
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        UpdateOwnerShip();
    }

    void UpdateWaves()
    {
        if (displayMode >= 0)
            simCRT.Update(1);
    }

    float waveTime = 0;
    float updateTime = 10;
    float delta;
    bool animationPlay = false;
    bool updateNeeded = false;
    float mmToPixels = 1;
    private void Update()
    {
        if (animationPlay)
        {
            delta = Time.deltaTime;
            waveTime += delta;

            while (updateTime < waveTime)
            {
                updateTime += dt;
                UpdateWaves();
                updateNeeded = false;
            }
        }
        if (updateNeeded)
        {
            UpdateWaves();
            updateNeeded=false;
        }
    }
    void Start()
    {
        defaultLambda = lambda;
        defaultPitch = sourcePitch;
        mmToPixels = simResolution.x/panelWidth;
        player = Networking.LocalPlayer;
        iAmOwner = Networking.IsOwner(this.gameObject);
        if (simCRT != null)
        {
            matCRT = simCRT.material;
            simCRT.Initialize();
        }
        if (lambdaSlider != null)
            lambdaSlider.SetValues(lambda, 30, 80);
        if (pitchSlider != null)
            pitchSlider.SetValues(sourcePitch, 50, 500);
        Lambda = lambda;
        SourcePitch = sourcePitch;
        DisplayMode = displayMode;
    }
}
