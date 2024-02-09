
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]

public class BallisticSimulation : UdonSharpBehaviour
{
    [SerializeField, Tooltip("Custom Render texture (only if required)")] 
    private CustomRenderTexture simCRT;
    [SerializeField, Tooltip("Use Render texture mode")] bool useCRT = false;
    [Tooltip("Simulation Material")] public Material matCRT = null;
    [Tooltip("DisplayPanel")] public MeshRenderer thePanel;

    bool updateNeeded = false;

    [SerializeField]
    bool iHaveSimMaterial = false;

    void UpdateSimulation()
    {
        if (useCRT)
            simCRT.Update(1);
        updateNeeded = false;
    }

    private void Update()
    {
        if (!useCRT) return;
        if (updateNeeded)
        {
            UpdateSimulation();
        }
    }

    void Start()
    {
        if (useCRT)
        {
            if (simCRT != null)
            {
                matCRT = simCRT.material;
                simCRT.Initialize();
            }
            else
                useCRT = false;
        }
        else
        {
            matCRT = thePanel.material;
        }
        iHaveSimMaterial = matCRT != null;
    }
}
