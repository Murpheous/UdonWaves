using System.Runtime.CompilerServices;
using UdonSharp;
using Unity.Mathematics;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]

public class WavePanelControl : UdonSharpBehaviour
{
    [Tooltip("Wave Display Mesh")] public MeshRenderer thePanel;
    [Tooltip("Mesh point scale (nominally mm)"),Range(100,4096)] private float mmToPixels = 1024;
 
    [SerializeField] Slider speedSlider;
    private bool iHaveSpeedControl = false;
    bool speedPtr = false;
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(WaveSpeed))] float waveSpeed;

    [SerializeField] Toggle togPlay;
    [SerializeField,UdonSynced, FieldChangeCallback(nameof(PlaySim))] bool playSim;

    [SerializeField] Toggle togReal;
    private bool iHaveRealTog = false;
    [SerializeField] Toggle togImaginary;
    private bool iHaveImTog = false;
    [SerializeField] Toggle togRealPwr;
    private bool iHaveRePwrTog = false;
    [SerializeField] Toggle togImPwr;
    private bool iHaveImPwrTog = false;
    [SerializeField] Toggle togProbability;
    private bool iHaveProbTog = false;
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(ViewSelect))]
    public int viewSelect;

    [SerializeField] Slider lambdaSlider;
    private bool iHaveLambdaControl = false;
    bool lambdaPtr = false;
    [SerializeField, Range(1, 100)] float defaultLambda = 24;
    [SerializeField, Range(1, 100),UdonSynced,FieldChangeCallback(nameof(Lambda))] float lambda = 24;

    // Debug
    [SerializeField] 
    Vector2Int panelPixels = new Vector2Int(2048,1024);
    [SerializeField] 
    private Material matSIM = null;
    [SerializeField] 
    private bool iHaveSimMaterial = false;
    [SerializeField]
    private int displayMode = 0;

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

    public void onSpeed()
    {
        if (iHaveSpeedControl && speedPtr)
        {
          WaveSpeed = speedSlider.value;
        }
    }

    private int ViewSelect
    {
        get => viewSelect;
        set
        {
            viewSelect = value;
            //updateClientState();
            RequestSerialization();
        }
    }


    public void onView()
    {
        int udatedView = viewSelect;
        if (iHaveRealTog && togReal.isOn)
        {
            ViewSelect = 0;
            return;
        }
        if (iHaveImTog && togImaginary.isOn)
        {
            ViewSelect = 2;
            return;
        }
        if (iHaveRePwrTog && togRealPwr.isOn)
        {
            ViewSelect = 1;
            return;
        }
        if (iHaveImPwrTog && togImPwr.isOn)
        {
            ViewSelect = 3;
            return;
        }
        if (iHaveProbTog && togProbability.isOn)
        {
            ViewSelect = 4;
            return;
        }
        Debug.Log("Random View Select!");
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
        }
        iHaveRealTog = togReal != null;
        iHaveImTog = togImaginary != null;
        iHaveRePwrTog = togRealPwr !=  null;
        iHaveImPwrTog =  togImPwr != null;
        iHaveProbTog =  togProbability != null;

        iHaveSpeedControl = speedSlider != null;
        iHaveLambdaControl = lambdaSlider != null;
        Lambda = defaultLambda;
        WaveSpeed = waveSpeed;
    }
}
