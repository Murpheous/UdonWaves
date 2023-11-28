
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using TMPro;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
[RequireComponent(typeof(ToggleGroup))]
public class InfoPanel : UdonSharpBehaviour
{
    public ToggleGroup toggleGroup;
    [SerializeField] Transform panelXfrm;
    [SerializeField] TextMeshProUGUI infoText;
    [SerializeField] Toggle[] toggles = null;
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

    public void onToggleChanged()
    {
        int newToggle = -1;
        for (int i = 0; newToggle < 0 && i < toggles.Length; i++)
        {
            if (toggles[i].isOn)
                newToggle = i;
        }
        SelectedToggle = newToggle;
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
                    toggles[selectedToggle].isOn = true;
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
        
        player = Networking.LocalPlayer;
        UpdateOwnerShip();
        if (toggleGroup == null)
            toggleGroup = gameObject.GetComponent<ToggleGroup>();
        toggleGroup.allowSwitchOff = true;
        toggleGroup.SetAllTogglesOff(false);
        hasTextField = infoText != null;
        if (hasTextField && panelXfrm == null)
        {
            panelXfrm = infoText.transform;
        }
        toggleGroup.EnsureValidState();
        onToggleChanged();
    }
}
