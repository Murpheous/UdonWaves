
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[RequireComponent(typeof(Toggle))]
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SyncedToggle : UdonSharpBehaviour
{
    [SerializeField]
    private Toggle toggle;
    [SerializeField]
    private UdonBehaviour toggleClient;
    private string clientVariable;

    public int toggleIndex = -1;
    [SerializeField]
    private bool currentState = false;
    [SerializeField]
    private bool reportedState = false;

    public bool CurrentState
    {
        get 
        { 
            return currentState; 
        }
        set 
        {
            currentState = value;
            if (currentState && reportedState != currentState) 
            {
                if (toggleClient != null)
                {
                    toggleClient.SetProgramVariable<int>("toggleIndex",toggleIndex);
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
                toggle.SetIsOnWithoutNotify(state);
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
        reportedState = !toggle.isOn;
        CurrentState = !reportedState;
    }
}
