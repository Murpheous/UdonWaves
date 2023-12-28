
using UdonSharp;
using UnityEngine;
using UnityEngine.Rendering;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class HuygensMonitor : UdonSharpBehaviour
{
    [SerializeField]
    CustomRenderTexture simCRT;
    [Tooltip("Material for Custom Render Texture")] public Material matCRT = null;
    [Tooltip("DisplayPanel")] public MeshRenderer thePanel;
    [SerializeField]
    Vector2Int simDResolution = new Vector2Int(2048,1024);
    [SerializeField] float sourceOffset;
    [SerializeField] int numApertures = 2;
    [SerializeField] int aperturePitch = 100;
    // Timing
    [SerializeField, Range(0.1f, 5f)] float dt = 1f;

    // UdonSync stuff
    private VRCPlayerApi player;
    private bool iAmOwner = false;
    private bool ihaveSim = false;

    private void UpdateOwnerShip()
    {
        iAmOwner = Networking.IsOwner(this.gameObject);
        //if (iAmOwner && pointerDown)
        //    FreqPointerIsDown = true;
    }

    void UpdateWaves(int DisplayMode)
    {
        matCRT.SetFloat("_DisplayMode", DisplayMode);
        simCRT.Update(1);
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        UpdateOwnerShip();
    }

    float waveTime = 0;
    float updateTime = 10;
    float delta;
    bool animationPlay = true;
    int DisplayMode = 0;
    private void Update()
    {
        if (animationPlay)
        {
            delta = Time.deltaTime;
            waveTime += delta;

            while (updateTime < waveTime)
            {
                updateTime += 5;
                DisplayMode += 1;
                if (DisplayMode > 5)
                    DisplayMode = 0;
                if (DisplayMode < 5)
                    UpdateWaves(DisplayMode);
                else
                    simCRT.Initialize();
            }
        }
    }
    void Start()
    {
        player = Networking.LocalPlayer;
        iAmOwner = Networking.IsOwner(this.gameObject);
        if (simCRT != null)
        {
            matCRT = simCRT.material;
            simCRT.Initialize();
            ihaveSim = true;
        }
        animationPlay = ihaveSim;
    }
}
