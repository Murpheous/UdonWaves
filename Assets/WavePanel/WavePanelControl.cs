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
    private Material matSIM = null;
    private bool iHaveSimMaterial = false;
 
    [SerializeField] Slider speedSlider;
    private bool iHaveSpeedControl = false;
    bool speedPtr = false;
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(WaveSpeed))] float waveSpeed;

    [SerializeField] Toggle togPlay;
    [SerializeField,UdonSynced, FieldChangeCallback(nameof(PlaySim))] bool playSim;

    [SerializeField] Toggle togReal;
    [SerializeField] Toggle togImaginary;
    [SerializeField] Toggle togRealPwr;
    [SerializeField] Toggle togImPwr;
    [SerializeField] Toggle togProbability;

    [SerializeField] Slider lambdaSlider;
    private bool iHaveLambdaControl = false;
    bool lambdaPtr = false;
    [SerializeField, Range(1, 100)] float defaultLambda = 24;
    [SerializeField, Range(1, 100),UdonSynced,FieldChangeCallback(nameof(Lambda))] float lambda = 24;

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
        iHaveSpeedControl = speedSlider != null;
        WaveSpeed = waveSpeed;
        iHaveLambdaControl = lambdaSlider != null;
        Lambda = defaultLambda;
    }
}
