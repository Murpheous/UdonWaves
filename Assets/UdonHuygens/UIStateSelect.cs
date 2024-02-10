
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class UIStateSelect : UdonSharpBehaviour
{
    [SerializeField] UdonBehaviour clientModule;
    bool iHaveTogReset = false;
    [SerializeField] Toggle togReset = null;
    bool iHaveTogReal = false;
    [SerializeField] Toggle togReal = null;
    bool iHaveTogIm = false;
    [SerializeField] Toggle togImaginary = null;
    bool iHaveTogRealPwr = false;
    [SerializeField] Toggle togRealPwr = null;
    bool iHaveToImPwr = false;
    [SerializeField] Toggle togImPwr = null;
    bool iHaveTogAmp = false;
    [SerializeField] Toggle togAmplitude = null;
    bool iHaveTogProb = false;
    [SerializeField] Toggle togProbability = null;

    [SerializeField, UdonSynced,FieldChangeCallback(nameof (PlaySim))] bool playSim = true;
    [SerializeField] Toggle togPlay = null;

    [SerializeField] Button btnIncSources = null;
    [SerializeField] Button btnDecSources = null;
    
    [SerializeField] string clientVariableName;
    [SerializeField] GameObject waveControls;

    [SerializeField, UdonSynced, FieldChangeCallback(nameof(DisplayMode))]
    public int displayMode;
    [SerializeField]
    int clientMode;


    private void updateClientState()
    {
        if (waveControls != null)
            waveControls.SetActive(displayMode >= 0);
        switch (displayMode)
        {
            case 0:
                if (iHaveTogReal && !togReal.isOn)
                    togReal.isOn = true;
                break;
            case 1:
                if (iHaveTogRealPwr && !togRealPwr.isOn)
                    togRealPwr.isOn = true;
                break;
            case 2:
                if (iHaveTogIm && !togImaginary.isOn)
                    togImaginary.isOn = true;
                break;
            case 3:
                if (iHaveToImPwr && !togImPwr.isOn)
                    togImPwr.isOn = true;
                break;
            case 4:
                if (iHaveTogAmp && !togAmplitude.isOn)
                    togAmplitude.isOn = true;
                break;
            case 5:
                if (iHaveTogProb && !togProbability.isOn)
                    togProbability.isOn = true;
                break;
            default:
                displayMode = -1;
                if (iHaveTogReset && !togReset.isOn)
                    togReset.isOn = true;
                break;
        }
        if (displayMode != clientMode)
        {
            clientMode = displayMode;
            if (iHaveClientVar)
                clientModule.SetProgramVariable<int>(clientVariableName, clientMode);
        }
        if (btnDecSources != null)
            btnDecSources.interactable = clientMode >= 0;
        if (btnIncSources != null)
            btnIncSources.interactable = clientMode >= 0;
    }
    private int DisplayMode 
    {  
        get => displayMode;
        set 
        {
            displayMode = value;
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
    private bool PlaySim
    {
        get => playSim;
        set
        {
            playSim = value;
            if (iHaveClientVar)
                clientModule.SetProgramVariable<bool>("playSim",value);
            if (togPlay != null && togPlay.isOn != value)
                togPlay.isOn = value;
            RequestSerialization();
        }
    }
    public void selState0()
    {
        if (iAmOwner)
            DisplayMode = 0;
        else
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner,nameof(selState0));
    }

    public void selState1()
    {
        if (iAmOwner)
            DisplayMode = 1;
        else
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(selState1));
    }
    public void selState2()
    {
        if (iAmOwner)
            DisplayMode = 2;
        else
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(selState2));
    }

    public void selState3()
    {
        if (iAmOwner)
            DisplayMode = 3;
        else
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(selState3));
    }

    public void selState4()
    {
        if (iAmOwner)
            DisplayMode = 4;
        else
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(selState4));
    }


    public void selState5()
    {
        if (iAmOwner)
            DisplayMode = 5;
        else
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(selState5));
    }

    public void selClose()
    {
        if (iAmOwner)
            DisplayMode = -1;
        else
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(selClose));
    }

    public void simPlay()
    {
        if (iAmOwner)
            PlaySim = true;
        else
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(simPlay));
    }

    public void simStop()
    {
        if (iAmOwner)
            PlaySim = false;
        else
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner, nameof(simStop));
    }

    public void onMode()
    {
        if (iHaveTogReal && togReal.isOn)
        {
            selState0();
            return;
        }
        if (iHaveTogIm && togImaginary.isOn)
        {
            selState2();
            return;
        }
        if (iHaveTogRealPwr && togRealPwr.isOn)
        {
            selState1();
            return;
        }
        if (iHaveToImPwr && togImPwr.isOn)
        {
            selState3();
            return;
        }
        if (iHaveTogAmp && togAmplitude.isOn)
        {
            selState4();
            return;
        }
        if (iHaveTogProb && togProbability.isOn)
        {
            selState5();
            return;
        }
        if (iHaveTogReset && togReset.isOn)
            selClose();
    }

    public void onPlayState()
    {
        if (togPlay ==  null)
            return;
        if (togPlay.isOn != playSim)
        {
            if (togPlay.isOn)
                simPlay();
            else
                simStop();
        }
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
        iHaveTogReset = togReset != null;
        iHaveTogReal =  togReal != null;
        iHaveTogIm = togImaginary != null;
        iHaveTogRealPwr = togRealPwr != null;
        iHaveToImPwr = togImPwr != null;
        iHaveTogProb = togProbability != null;
        iHaveTogAmp = togAmplitude != null;

        UpdateOwnerShip();
        iHaveClientVar = (clientModule != null) && (!string.IsNullOrEmpty(clientVariableName));
        updateClientState();
    }
}
