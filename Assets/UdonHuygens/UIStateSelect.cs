
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class UIStateSelect : UdonSharpBehaviour
{
    [SerializeField] UdonBehaviour clientModule;
    [SerializeField] Toggle togReset = null;
    [SerializeField] Toggle togHeight = null;
    [SerializeField] Toggle togVel = null;
    [SerializeField] Toggle togEnergy = null;
    [SerializeField] Toggle togSquare = null;
    [SerializeField] ToggleGroup selGroup = null;


    [SerializeField] Button btnIncSources = null;
    [SerializeField] Button btnDecSources = null;
    
    [SerializeField] string clientVariableName;
    [SerializeField] GameObject waveControls;

    [SerializeField, UdonSynced, FieldChangeCallback(nameof(OptionSelect))]
    public int optionSelect;
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(ModeSelect))]
    public bool modeSelect;
    [SerializeField]
    int clientMode;
    private void updateClientState()
    {
        if (optionSelect <= 0 && selGroup != null)
        {
            if (togSquare != null)
                togSquare.isOn = false;
            if (togReset != null && !togReset.isOn)
                togReset.isOn = true;
        }
        if (waveControls != null)
            waveControls.SetActive(optionSelect > 0);
        if (togHeight != null && optionSelect == 1 && !togHeight.isOn) 
            togHeight.isOn = true;
        if (togVel != null && optionSelect == 2 && !togVel.isOn)
            togVel.isOn = true;
        if (togEnergy != null && optionSelect == 3 && !togEnergy.isOn)
            togEnergy.isOn = true;
        if (togSquare != null && togSquare.isOn != modeSelect)
            togSquare.isOn = modeSelect;
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
        if (btnDecSources != null)
            btnDecSources.interactable = clientMode >= 0;
        if (btnIncSources != null)
            btnIncSources.interactable = clientMode >= 0;
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

    public void incSources()
    {
        if (!iAmOwner)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(incSources));
            return;
        }
        clientModule.SendCustomEvent("incSrc");
    }

    public void decSources()
    {
        if (!iAmOwner)
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(decSources));
            return;
        }
        clientModule.SendCustomEvent("decSrc");
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

    public void selClose()
    {
        if (iAmOwner)
            OptionSelect = 0;
        else
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(selClose));
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
        if (togSquare == null)
            return;
        if (togSquare.isOn != modeSelect)
        {
            if (modeSelect)
                modeOff();
            else
                modeOn();
        }
    }

    public void togSel0()
    {
        if (togReset == null)
            return;
        if (togReset.isOn && optionSelect > 0)
            selState0();
    }

    public void togSel1()
    {
        if (togHeight == null)
            return;
        if (togHeight.isOn && optionSelect != 1)
            selState1();
    }
    public void togSel2()
    {
        if (togVel == null)
            return;
        if (togVel.isOn && optionSelect != 2)
            selState2();
    }
    public void togSel3()
    {
        if (togEnergy == null)
            return;
        if (togEnergy.isOn && optionSelect != 3)
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
    void Start()
    {

        UpdateOwnerShip();
        iHaveClientVar = (clientModule != null) && (!string.IsNullOrEmpty(clientVariableName));
        updateClientState();
    }
}
