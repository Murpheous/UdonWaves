
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class SyncedToggle : UdonSharpBehaviour
{
    [SerializeField]
    private Toggle toggle;
    [SerializeField]
    private UdonBehaviour toggleClient;
    public int toggleIndex = -1;
    [SerializeField]
    private bool currentState = false;
    [SerializeField]
    private bool reportedState = false;
    [SerializeField]
    private bool hasClient = false;

    public bool CurrentState
    {
        get 
        { 
            return currentState; 
        }
        set 
        {
            currentState = value;
            if (reportedState != currentState) 
            {
                if (hasClient && currentState)
                {
                    if (toggleIndex >= 0)
                        toggleClient.SendCustomEvent("toggleIndex");
                }
            }
            reportedState = value;
        }
    }
    public void setState(bool state = false)
    {
        currentState = state;
        reportedState = state;
        if (toggle != null)
        {
            if (toggle.isOn != state)
            {
                toggle.isOn = state;
            }
        }
    }
    public void onToggle()
    {
        CurrentState = toggle.isOn;
    }
    void Start()
    {
        if (toggle == null)
            toggle = GetComponent<Toggle>();
        hasClient = toggleClient != null;
        setState();
    }
}
