
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class InfoPanel : UdonSharpBehaviour
{
    public ToggleGroup toggleGroup;
    [SerializeField] Transform panelXfrm;
    [SerializeField] TextMeshProUGUI infoText;
    [SerializeField] SyncedToggle[] toggles = null;
    int toggleCount = 0;

    bool hasTextField = false;
    private bool iamOwner;
    private VRCPlayerApi player;
    private VRC.Udon.Common.Interfaces.NetworkEventTarget toTheOwner = VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner;
    private VRC.Udon.Common.Interfaces.NetworkEventTarget toAll = VRC.Udon.Common.Interfaces.NetworkEventTarget.All;

    [SerializeField,UdonSynced,FieldChangeCallback(nameof(ShowPanel))] 
    private int showPanel = -1;

    [SerializeField,TextArea] string defaultText = string.Empty;
    public int ShowPanel
    {
        get => showPanel; 
        set
        {
            showPanel = value;
            Debug.Log("ShowPanel=" + value);
            bool isVisible = showPanel >= 0;
            if (value >= 0 && hasTextField)
            {
                infoText.text = defaultText;
            }
            if (panelXfrm != null) 
            { 
                panelXfrm.gameObject.SetActive(showPanel >= 0);
            }
        }
    }
     
    public void toggleState(int index, bool state)
    {
        if (!state)
        {
            Debug.Log("toggle Off=" + index);
            if (toggleGroup.AnyTogglesOn())
                return;
            Debug.Log("group off!");
            SelectedToggle = -1;
            return;
        }
        Debug.Log("toggle On=" + index);
        SelectedToggle = index;
    }

    [SerializeField,UdonSynced,FieldChangeCallback(nameof(SelectedToggle))]
    private int selectedToggle = -1;
    private bool togglePending = false;
    private int pendingToggle;
    private int SelectedToggle
    {
        get => selectedToggle;
        set
        {
            Debug.Log("Toggle Select: " +  value.ToString());
            if (!iamOwner)
            {
                togglePending = true;
                pendingToggle = value;
                Networking.SetOwner(player, gameObject);
                return;
            }
            togglePending = false;
            selectedToggle = value;
            ShowPanel = selectedToggle;
            if (value >= 0 && value < toggleCount)
            {
                if (toggles[selectedToggle] != null)
                    toggles[selectedToggle].setState(true);
            }
            if (toggleGroup != null && value < 0)
                toggleGroup.SetAllTogglesOff(false);
            RequestSerialization();
        }
    }
    
    
    public void onPanelClose()
    {
        Debug.Log(gameObject.name+": Panel Close");
        SelectedToggle = -1;
    }

    private void UpdateOwnerShip()
    {
        iamOwner = Networking.IsOwner(this.gameObject);
        if (iamOwner)
        {
            if (togglePending)
            {
                togglePending = false;
                SelectedToggle = pendingToggle;
            }
        }
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        UpdateOwnerShip();
    }

    void Start()
    {
        toggleCount = 0;
        if (toggles != null)
            toggleCount = toggles.Length;
        
        for (int i = 0; i < toggleCount; i++)
        {
            if (toggles[i] != null)
            {
                toggles[i].toggleIndex = i;
                //if (toggles[i].)
            }
        }
        player = Networking.LocalPlayer;
        UpdateOwnerShip();
        if (toggleGroup == null)
            toggleGroup = gameObject.GetComponent<ToggleGroup>();
        if (toggleGroup != null)
        {
            toggleGroup.allowSwitchOff = true;
            toggleGroup.SetAllTogglesOff(false);
        }
        hasTextField = infoText != null;
        if (hasTextField && panelXfrm == null)
        {
            panelXfrm = infoText.transform;
        }
        ShowPanel = showPanel;
    }
}
