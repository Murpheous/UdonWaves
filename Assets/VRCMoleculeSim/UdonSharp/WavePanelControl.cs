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
            defaultLambda = matSIM.GetFloat("_LambdaPx");
            float panelAspect = thePanel.transform.localScale.y/thePanel.transform.localScale.x;
            defaultScale = matSIM.GetFloat("_Scale");
            defaultSpeed = matSIM.GetFloat("_PhaseSpeed");
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
        Lambda = defaultLambda;
        WaveSpeed = defaultSpeed;
        SimScale = defaultScale;
    }
}
