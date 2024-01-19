using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class HuygensMonitor : UdonSharpBehaviour
{
    [SerializeField] CustomRenderTexture simCRT;
    [SerializeField] bool useCRT = false;
    [Tooltip("Simulation Material")] public Material matSIM = null;
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
    [SerializeField, Range(50,500),UdonSynced, FieldChangeCallback(nameof(SourcePitch))] 
    float sourcePitch = 436.5234f;
    // Timing
    [SerializeField, Range(0.01f, 0.2f)] float dt = 0.1f;
    [Header("Controls")]
    [SerializeField]
    private SyncedSlider lambdaSlider;
    [SerializeField]
    private SyncedSlider pitchSlider;
    [SerializeField]
    private SyncedSlider scaleSlider;
    [SerializeField,Range(30,80), UdonSynced, FieldChangeCallback(nameof(Lambda))]
    float lambda = 1f;
    [SerializeField,Range(1,10),UdonSynced,FieldChangeCallback(nameof(SimScale))] float simScale = 1f;
    [SerializeField] private UdonBehaviour vectorDrawing;

    private float defaultLambda;
    private float defaultPitch;
    private float defaultScale;
    private int DisplayMode
    {
        get => displayMode; 
        set
        {
            matSIM.SetFloat("_DisplayMode", value);
            if (value >= 0)
            {
                if (displayMode < 0) 
                {
                    if (pitchSlider != null)
                        pitchSlider.IsInteractible = true;
                    if (lambdaSlider != null)
                        lambdaSlider.IsInteractible = true;
                    if (scaleSlider!= null)
                        scaleSlider.IsInteractible = true;
                }
                if (vectorDrawing != null)
                    vectorDrawing.SetProgramVariable<Vector2>("displayRect", displayRect);
            }
            else
            {
                if (pitchSlider != null)
                    pitchSlider.IsInteractible = false;
                if (lambdaSlider != null)
                    lambdaSlider.IsInteractible = false;
                if (scaleSlider != null)
                    scaleSlider.IsInteractible = false;
                if (useCRT)
                    simCRT.Initialize();
                SourcePitch = defaultPitch;
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
    public float SourcePitch
    {
        get => sourcePitch;
        set
        {
            sourcePitch = value;
            if (vectorDrawing != null)
                vectorDrawing.SetProgramVariable<float>("sourcePitch", sourcePitch);
            if (iHaveSimMaterial)
                matSIM.SetFloat("_SlitPitchPx", sourcePitch * mmToPixels);
            if (pitchSlider != null)
            {
                if ((!isStarted || pitchSlider.CurrentValue != value) && !pitchPtr)
                    pitchSlider.CurrentValue = value;
            }
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
            if (iHaveSimMaterial)
                matSIM.SetFloat("_NumSources", numSources);
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
                matSIM.SetFloat("_Scale", simScale);
            if (scaleSlider != null)
            {
                if ((!isStarted || scaleSlider.CurrentValue != value) && !scalePtr)
                    scaleSlider.CurrentValue = value;
            }
            updateNeeded = true;
            RequestSerialization();
        }
    }
    public float Lambda
    {
        get => lambda;
        set
        {
            lambda = value;
            phaseRate = 35f/value;
            if (vectorDrawing != null)
                vectorDrawing.SetProgramVariable<float>("lambda", lambda);
            if (iHaveSimMaterial)
                matSIM.SetFloat("_LambdaPx", lambda * mmToPixels);
            if (lambdaSlider != null)
            {
                if ((!isStarted || lambdaSlider.CurrentValue != value) && !lambdaPtr)
                    lambdaSlider.CurrentValue = value;
            }
            UpdatePhaseSpeed();
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

    [SerializeField, FieldChangeCallback(nameof(ScalePtr))]
    public bool scalePtr = false;

    public bool ScalePtr
    {
        get => scalePtr;
        set
        {
            if (value && !iAmOwner)
                Networking.SetOwner(player, gameObject);
            scalePtr = value;
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


    float waveTime = 0;
    float delta;
    private void UpdatePhaseSpeed()
    {
        if (iHaveSimMaterial && !useCRT)
            matSIM.SetFloat("_PhaseSpeed", playSim ? defaultLambda/lambda : 0);
    }

    [SerializeField, FieldChangeCallback(nameof(PlaySim))]
    public bool playSim = true;
    private bool PlaySim
    {
        get => playSim;
        set
        {
            playSim = value;
            UpdatePhaseSpeed();
        }
    }
    bool updateNeeded = false;
    float mmToPixels = 1;
    bool iHaveSimMaterial = false;
    float phaseTime = 0;
    float phaseRate = 0.6f;
    void UpdateWaves()
    {   
        if(useCRT && displayMode >= 0)
            simCRT.Update(1);
        updateNeeded = false;
    }

    private void Update()
    {
        if (!useCRT) return;
        if (playSim && displayMode >= 0)
        {
            delta = Time.deltaTime;
            waveTime -= delta;
            phaseTime -= delta*phaseRate;
            if (phaseTime < 0)
                phaseTime += 1;
            if ( waveTime < 0)
            {
                if (iHaveSimMaterial)
                    matSIM.SetFloat("_Phase", phaseTime);
                waveTime += dt;
                if (useCRT)
                    UpdateWaves();
            }
        }
        if (updateNeeded)
            UpdateWaves();
    }
    bool isStarted = false;
    void Start()
    {
        defaultLambda = lambda;
        defaultPitch = sourcePitch;
        defaultScale = simScale;
        mmToPixels = simResolution.x/panelWidth;
        player = Networking.LocalPlayer;
        iAmOwner = Networking.IsOwner(this.gameObject);
        if (useCRT)
        {
            if (simCRT != null)
            {
                matSIM = simCRT.material;
                simCRT.Initialize();
            }
            else
                useCRT = false;
        }
        else
        {
            matSIM = thePanel.material;
        }
        iHaveSimMaterial = matSIM != null;
        if (lambdaSlider != null)
            lambdaSlider.SetValues(lambda, 30, 80);
        if (pitchSlider != null)
            pitchSlider.SetValues(sourcePitch, 50, 500);
        Lambda = lambda;
        SourcePitch = sourcePitch;
        DisplayMode = displayMode;
        SimScale = simScale;
        
        isStarted = true;
    }
}
