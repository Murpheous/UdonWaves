
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class UIStateSelect : UdonSharpBehaviour
{
    [SerializeField] UdonBehaviour clientModule;
    [SerializeField] Toggle tog0 = null;
    [SerializeField] Toggle tog1 = null;
    [SerializeField] Toggle tog2 = null;
    [SerializeField] Toggle tog3 = null;
    [SerializeField] Toggle togMode = null;

    [SerializeField] string clientVariableName;

    [SerializeField, UdonSynced, FieldChangeCallback(nameof(OptionSelect))]
    public int optionSelect;
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(ModeSelect))]
    public bool modeSelect;
    [SerializeField]
    int clientMode;
    private void updateClientState()
    {
        if (tog0 != null && optionSelect == 0 && !tog0.isOn) 
            tog0.isOn = true;
        if (tog1 != null && optionSelect == 1 && !tog1.isOn)
            tog1.isOn = true;
        if (tog2 != null && optionSelect == 2 && !tog2.isOn)
            tog2.isOn = true;
        if (tog3 != null && optionSelect == 3 && !tog3.isOn)
            tog3.isOn = true;
        if (togMode != null && togMode.isOn != modeSelect)
            togMode.isOn = modeSelect;
        int newOption = -1;
        switch (optionSelect)
        {
            case 0:
                newOption = -1;
                break;
            case 1:
                newOption = modeSelect ? 1 : 0;
                break;
            case 2:
                newOption = modeSelect ? 3 : 2;
                break;
            case 3:
                newOption =  4;
                break;
        }
        if (newOption != clientMode)
        {
            clientMode = newOption;
            if (iHaveClientVar)
                clientModule.SetProgramVariable<int>(clientVariableName, clientMode);
        }
    }
    private int OptionSelect 
    {  
        get => optionSelect;
        set 
        {
            optionSelect = value;
            updateClientState();
            RequestSerialization();
        }
    }

    private bool ModeSelect
    {
        get => modeSelect;
        set
        {
            modeSelect = value;
            updateClientState();
            RequestSerialization();
        }
    }
    public void selState0()
    {
        if (iAmOwner)
            OptionSelect = 0;
        else
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner,nameof(selState0));
    }

    public void selState1()
    {
        if (iAmOwner)
            OptionSelect = 1;
        else
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(selState1));
    }
    public void selState2()
    {
        if (iAmOwner)
            OptionSelect = 2;
        else
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(selState2));
    }
    public void selState3()
    {
        if (iAmOwner)
            OptionSelect = 3;
        else
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(selState3));
    }

    public void modeOn()
    {
        if (iAmOwner)
            ModeSelect = true;
        else
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(modeOn));
    }
    public void modeOff()
    {
        if (iAmOwner)
            ModeSelect = false;
        else
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(modeOff));
    }

    public void selMode()
    {
        if (togMode == null)
            return;
        if (togMode.isOn != modeSelect)
        {
            if (modeSelect)
                modeOff();
            else
                modeOn();
        }
    }

    public void togSel0()
    {
        if (tog0 == null)
            return;
        if (tog0.isOn && optionSelect != 0)
            selState0();
    }
    public void togSel1()
    {
        if (tog1 == null)
            return;
        if (tog1.isOn && optionSelect != 1)
            selState1();
    }
    public void togSel2()
    {
        if (tog2 == null)
            return;
        if (tog2.isOn && optionSelect != 2)
            selState2();
    }
    public void togSel3()
    {
        if (tog3 == null)
            return;
        if (tog3.isOn && optionSelect != 3)
            selState3();
    }
    // UdonSync stuff
    private VRCPlayerApi player;
    private bool iAmOwner = false;

    private void UpdateOwnerShip()
    {
        iAmOwner = Networking.IsOwner(this.gameObject);
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        UpdateOwnerShip();
    }

    private bool iHaveClientVar = false;
    private bool iHaveToggles = false;
    void Start()
    {

        UpdateOwnerShip();
        iHaveClientVar = (clientModule != null) && (!string.IsNullOrEmpty(clientVariableName));
        updateClientState();
    }
}
