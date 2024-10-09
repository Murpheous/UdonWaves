
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class UdonLabel : UdonSharpBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private GameObject textPanel;
    
    public string Text
    {
        get 
        {
            if (text == null)
                return "";
            return text.text;
        }
        set 
        { 
            if (text == null) return;
            text.text = value;
        }
    }

    [SerializeField] bool isVisible = true;
    public bool Visible
    {
        get => isVisible;
        set
        {
            isVisible = value;
            if (textPanel != null)
            {
                textPanel.gameObject.SetActive(value);
            }
        }
    }

    public Vector3 LocalPostion
    {
        get => transform.localPosition;
        set
        {
            transform.localPosition = value;
        }
    }
    void Start()
    {
        if (text == null)
            text = GetComponentInChildren<TextMeshProUGUI>();
        Transform textXFRM = transform.GetChild(0);
        if (textXFRM != null)
            textPanel = textXFRM.gameObject;
    }
}
