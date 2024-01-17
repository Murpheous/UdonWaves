using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]

public class WavePanelControl : UdonSharpBehaviour
{
    [Tooltip("Wave Display Mesh")] public MeshRenderer thePanel;
    [Tooltip("Mesh point simScale (nominally mm)"),Range(100,4096)] private float mmToPixels = 1024;
    private Material matSIM = null;
    private bool iHaveSimMaterial = false;
 
    [SerializeField] Slider speedSlider;
    private bool iHaveSpeedControl = false;
    bool speedPtr = false;
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(WaveSpeed))] float waveSpeed;

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

    [SerializeField] Slider lambdaSlider;
    private bool iHaveLambdaControl = false;
    bool lambdaPtr = false;
    [SerializeField, Range(1, 100)] float defaultLambda = 24;
    [SerializeField, Range(1, 100),UdonSynced,FieldChangeCallback(nameof(Lambda))] float lambda = 24;

    [SerializeField] Slider scaleSlider;
    private bool iHaveScaleControl = false;
    bool scalePtr = false;
    [SerializeField, Range(1, 10)] float defaultScale = 24;
    [SerializeField, Range(1, 10), UdonSynced, FieldChangeCallback(nameof(SimScale))] float simScale = 24;

    // Debug
    [SerializeField] Vector2Int panelPixels = new Vector2Int(2048,1024);

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
            if (!speedPtr && iHaveSpeedControl && speedSlider.value != waveSpeed)
                speedSlider.value = waveSpeed;
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

    public void onSpeed()
    {
        if (iHaveSpeedControl && speedPtr)
        {
          WaveSpeed = speedSlider.value;
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

    public void sPtrDn()
    {
        speedPtr = true;
    }
    public void sPtrUp()
    {
        speedPtr = false;
    }

    public float Lambda
    {
        get => lambda;
        set
        {
            lambda = value;
            if (iHaveSimMaterial)
                matSIM.SetFloat("_LambdaPx", lambda);
            if (!lambdaPtr && iHaveLambdaControl && (lambdaSlider.value != value))
                    lambdaSlider.value = value;
            UpdatePhaseSpeed();
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
            if (!scalePtr && iHaveScaleControl && (scaleSlider.value != value))
                scaleSlider.value = value;
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

    public void onScale()
    {
        if (iHaveScaleControl)
        {
            SimScale = scaleSlider.value;
        }
    }

    public void scPtrDn()
    {

        scalePtr = true;
    }
    public void scPtrUp()
    {
        scalePtr = false;
    }

    public void onLambda()
    {
        if (iHaveLambdaControl && lambdaPtr)
        {
            Lambda = lambdaSlider.value;
        }
    }

    public void lPtrDn()
    {

        lambdaPtr = true;
    }
    public void lPtrUp()
    {
        lambdaPtr = false;
    }

    void Start()
    {
        if (thePanel != null)
            matSIM = thePanel.material;
        iHaveSimMaterial = matSIM != null;
        if (iHaveSimMaterial)
        {
            defaultLambda = matSIM.GetFloat("_LambdaPx");
            float panelAspect = thePanel.transform.localScale.y/thePanel.transform.localScale.x;
            defaultScale = matSIM.GetFloat("_Scale");
        }
        iHaveTogReal = togReal != null;
        iHaveTogIm = togImaginary  != null;
        iHaveTogRealPwr = togRealPwr != null;
        iHaveToImPwr = togImPwr != null;
        iHaveTogProb = togProbability  != null;
        iHaveSpeedControl = speedSlider != null;
        iHaveLambdaControl = lambdaSlider != null;
        iHaveScaleControl = scaleSlider != null;
        if (iHaveSimMaterial)
        {
            DisplayMode = Mathf.RoundToInt(matSIM.GetFloat("_DisplayMode"));
        }
        WaveSpeed = waveSpeed;
        Lambda = defaultLambda;
        SimScale = defaultScale;
    }
}
