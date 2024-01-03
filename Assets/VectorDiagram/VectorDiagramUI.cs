
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]

public class VectorDiagramUI : UdonSharpBehaviour
{
    [SerializeField] UdonBehaviour clientModule;
    [SerializeField] Toggle togOff = null;
    [SerializeField] Toggle togBragg = null;
    [SerializeField] Toggle togHuygens = null;
    [SerializeField] Toggle togK_Vectors = null;

    [SerializeField] string clientVariableName;

    [SerializeField, UdonSynced, FieldChangeCallback(nameof(OptionSelect))]
    public int optionSelect;
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(ModeSelect))]
    public bool modeSelect;
    [SerializeField]
    int clientMode;
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
        if (optionSelect != clientMode)
        {
            clientMode = optionSelect;
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


    public void togSel0()
    {
        if (togOff == null)
            return;
        if (togOff.isOn && optionSelect != 0)
            vecMode0();
    }

    public void togSel1()
    {
        if (togBragg == null)
            return;
        if (togBragg.isOn && optionSelect != 1)
            vecMode1();
    }
    public void togSel2()
    {
        if (togHuygens == null)
            return;
        if (togHuygens.isOn && optionSelect != 2)
            vecMode2();
    }
    public void togSel3()
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
