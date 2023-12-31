
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
    [SerializeField] int numSourced = 2;
    [SerializeField, Range(50,500),UdonSynced, FieldChangeCallback(nameof(SlitPitch))] 
    float slitPitch = 437.5f;
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
    private int DisplayMode
    {
        get => displayMode; 
        set
        {
            displayMode = value;
            if (displayMode >= 0)
                matCRT.SetFloat("_DisplayMode", value);
            else
                simCRT.Initialize();
            updateNeeded = true;
        }
    }
    public float SlitPitch
    {
        get => slitPitch;
        set
        {
            if (slitPitch != value)
            {
                if (pitchSlider != null)
                    pitchSlider.CurrentValue = value;
            }
            slitPitch = value;
            if (vectorDrawing != null)
                vectorDrawing.SetProgramVariable<float>("slitPitch", slitPitch);
            if (matCRT != null)
                matCRT.SetFloat("_SlitPitchPx", slitPitch * pixelsPerMM);
            updateNeeded = true;
            RequestSerialization();
        }
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
                matCRT.SetFloat("_LambdaPx", lambda * pixelsPerMM);
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
    private bool ihaveSim = false;

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
        if (displayMode < 0)
            simCRT.Initialize();
        else
            simCRT.Update(1);
    }

    float waveTime = 0;
    float updateTime = 10;
    float delta;
    bool animationPlay = false;
    bool updateNeeded = false;
    float pixelsPerMM = 1;
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
        pixelsPerMM = panelWidth/simResolution.x;
        player = Networking.LocalPlayer;
        iAmOwner = Networking.IsOwner(this.gameObject);
        if (simCRT != null)
        {
            matCRT = simCRT.material;
            simCRT.Initialize();
            ihaveSim = true;
        }
        if (lambdaSlider != null)
            lambdaSlider.SetValues(lambda, 30, 80);
        if (pitchSlider != null)
            pitchSlider.SetValues(slitPitch, 50, 500);
        animationPlay = ihaveSim;
    }
}
