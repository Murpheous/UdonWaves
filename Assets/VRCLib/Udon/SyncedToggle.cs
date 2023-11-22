
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Serialization.OdinSerializer.Utilities;

public class SyncedToggle : UdonSharpBehaviour
{
    [SerializeField]
    private Toggle toggle;
    [SerializeField]
    private UdonBehaviour toggleClient;
    [SerializeField]
    private string clientVariableName;
    [SerializeField]
    private string topicName;
    [SerializeField] 
    public int toggleIndex = -1;
    [SerializeField]
    private bool currentState = false;
    [SerializeField]
    private bool reportedState = false;
    [SerializeField]
    private bool hasToggle = false;
    [SerializeField]
    private bool hasClient = false;
    [SerializeField]
    private bool hasTopic = false;

    public bool CurrentState
    {
        set 
        {
            currentState = value;
            if (reportedState != currentState) 
            {
                if (hasClient)
                {
                    if (currentState && hasTopic)
                    {
                        toggleClient.SetProgramVariable<string>(clientVariableName, topicName);
                        if (toggleIndex >= 0)
                            toggleClient.SetProgramVariable<int>("toggleIndex", toggleIndex);
                    }
                }
            }
            reportedState = value;
        }
    }
    public void setState(bool state = false)
    {
        currentState = state;
        reportedState |= state;
        if (hasToggle)
        {
            if (toggle.isOn != state)
            {
                toggle.isOn = state;
            }
        }
    }
    public void onToggleChanged()
    {
        if (!hasToggle)
            return;
        CurrentState = toggle.isOn;
    }
    void Start()
    {
        if (toggle == null)
            toggle = GetComponent<Toggle>();
        hasToggle = toggle != null;
        if (hasToggle && string.IsNullOrEmpty(topicName))
            topicName = toggle.name;
        hasClient = toggleClient != null;
        hasTopic = !string.IsNullOrEmpty(topicName);
        setState();
    }
}
