
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class VectorDiagram : UdonSharpBehaviour
{
    public Slider stepControl;
    [SerializeField] TextMeshProUGUI txtAngle;
    void Start()
    {
        
    }
}
