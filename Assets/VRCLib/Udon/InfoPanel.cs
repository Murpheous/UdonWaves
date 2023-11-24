
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class InfoPanel : UdonSharpBehaviour
{
    public ToggleGroup subjectGroup;
    [SerializeField] Transform panelXfrm;
    [SerializeField] TextMeshProUGUI infoText;
    [SerializeField] SyncedToggle[] toggles = null;
    int toggleCount = 0;

    bool hasGroup = false;
    bool hasTextField = false;
    bool hasTransform = false;
    private bool iamOwner;
    private VRCPlayerApi player;
    private VRC.Udon.Common.Interfaces.NetworkEventTarget toTheOwner = VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner;
    private VRC.Udon.Common.Interfaces.NetworkEventTarget toAll = VRC.Udon.Common.Interfaces.NetworkEventTarget.All;

    [SerializeField,UdonSynced,FieldChangeCallback(nameof(ShowPanel))] 
    private int showPanel = 0;

    [SerializeField,TextArea] string defaultText = string.Empty;
    public int ShowPanel
    {
        get => showPanel; 
        set
        {
            if (showPanel != value)
            {
                if (value > 0 && hasTextField)
                {
                    infoText.text = defaultText;
                }
                showPanel = value;
            }
            if (hasTextField)
                infoText.enabled = showPanel > 0;
            if (hasTransform) 
            { 
                panelXfrm.gameObject.SetActive(showPanel > 0);
            }
            if (hasGroup && showPanel <= 0)
            {
                subjectGroup.SetAllTogglesOff();
            }
        }
    }
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(ToggleIndex))]
    public int toggleIndex;

    private bool togglePending = false;
    private int pendingToggle;
    private int ToggleIndex
    {
        get => toggleIndex;
        set
        {
            if (!iamOwner)
            {
                togglePending = true;
                pendingToggle = value;
                Networking.SetOwner(player, gameObject);
                return;
            }
            togglePending = false;
            toggleIndex = value;
            if (value < toggleCount)
            {
                toggles[toggleIndex].setState(true);
            }
            RequestSerialization();
        }
    }
    
    public void onPanelClose()
    {
        ShowPanel = 0;
    }

    private void UpdateOwnerShip()
    {
        iamOwner = Networking.IsOwner(this.gameObject);
        if (iamOwner)
        {
            if (togglePending)
                ToggleIndex = pendingToggle;
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
            toggles[i].toggleIndex = i;
        player = Networking.LocalPlayer;
        UpdateOwnerShip();
        if (subjectGroup == null)
            subjectGroup = gameObject.GetComponent<ToggleGroup>();

        hasTextField = infoText != null;
        if (hasTextField && panelXfrm == null)
        {
            panelXfrm = infoText.transform;
        }
        hasTransform = panelXfrm != null;
        ShowPanel = showPanel;
    }
}
