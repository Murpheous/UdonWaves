using System.Runtime.CompilerServices;
using UdonSharp;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]

public class WavePanelControl : UdonSharpBehaviour
{
    [Tooltip("Wave Display Mesh")] public MeshRenderer thePanel;
    private Material matSIM = null;
    private bool iHaveSimMaterial = false;
    [SerializeField] Slider speedSlider;
    private bool iHaveSpeedControl = false;
    [SerializeField, Range(1, 100)] float defaultLambda = 24;
    [SerializeField, Range(1, 100)] float lambda = 24;

    [SerializeField,FieldChangeCallback(nameof(CurrentSpeed))] float currentSpeed;

    private void UpdatePhaseSpeed()
    {
        if (iHaveSimMaterial)
            matSIM.SetFloat("_PhaseSpeed", currentSpeed * defaultLambda / lambda);
    }

    float CurrentSpeed
    {
        get => currentSpeed;
        set
        {
            currentSpeed = Mathf.Clamp(value,0,1);
            if (iHaveSpeedControl && !speedPtrDown && speedSlider.value != currentSpeed)
                speedSlider.value = currentSpeed;
            UpdatePhaseSpeed();
        }
    }
    public void onSpeed()
    {
        if (iHaveSpeedControl && speedPtrDown)
        {
          CurrentSpeed = speedSlider.value;
        }
    }

    bool speedPtrDown = false;
    public void speedPtrDn()
    {

        speedPtrDown = true;
    }
    public void speedPtrUp()
    {
        speedPtrDown = false;
    }
    void Start()
    {
        if (thePanel != null)
            matSIM = thePanel.material;
        iHaveSimMaterial = matSIM != null;
        iHaveSpeedControl = speedSlider != null;
        CurrentSpeed = currentSpeed;
    }
}
