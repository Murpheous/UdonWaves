
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]

public class DiagramUI : UdonSharpBehaviour
{
    [SerializeField] UdonBehaviour clientModule;
    [SerializeField] Toggle togOff = null;
    [SerializeField] Toggle togBragg = null;
    [SerializeField] Toggle togHuygens = null;
    [SerializeField] Toggle togK_Vectors = null;

    [SerializeField] string clientVariableName;

    [SerializeField, UdonSynced, FieldChangeCallback(nameof(OptionSelect))]
    public int optionSelect;

    [SerializeField]
    int clientOption;
    private void updateClientState()
    {
        if (togOff != null && optionSelect <= 0 && !togOff.isOn)
            togOff.isOn = true;
        if (togBragg != null && optionSelect == 1 && !togBragg.isOn)
            togBragg.isOn = true;
        if (togHuygens != null && optionSelect == 2 && !togHuygens.isOn)
            togHuygens.isOn = true;
        if (togK_Vectors != null && optionSelect == 3 && !togK_Vectors.isOn)
            togK_Vectors.isOn = true;
        if (optionSelect != clientOption)
        {
            clientOption = optionSelect;
            if (iHaveClientVar)
                clientModule.SetProgramVariable<int>(clientVariableName, clientOption);
        }
    }
    private int OptionSelect
    {
        get => optionSelect;
        set
        {
            Debug.Log("Vec OptionSelect: " + value.ToString());
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

    public void vecMode0()
    {
        if (iAmOwner)
            OptionSelect = 0;
        else
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(vecMode0));
    }

    public void vecMode1()
    {
        if (iAmOwner)
            OptionSelect = 1;
        else
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(vecMode1));
    }
    public void vecMode2()
    {
        if (iAmOwner)
            OptionSelect = 2;
        else
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(vecMode2));
    }

    public void vecMode3()
    {
        if (iAmOwner)
            OptionSelect = 3;
        else
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(vecMode3));
    }


    public void onSel0()
    {
        Debug.Log("onSel0");
        if (togOff == null)
            return;
        if (togOff.isOn && optionSelect != 0)
            vecMode0();
    }

    public void onSel1()
    {
        Debug.Log("onSel1");
        if (togBragg == null)
            return;
        if (togBragg.isOn && optionSelect != 1)
            vecMode1();
    }
    public void onSel2()
    {
        if (togHuygens == null)
            return;
        if (togHuygens.isOn && optionSelect != 2)
            vecMode2();
    }
    public void onSel3()
    {
        if (togK_Vectors == null)
            return;
        if (togK_Vectors.isOn && optionSelect != 3)
            vecMode3();
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
