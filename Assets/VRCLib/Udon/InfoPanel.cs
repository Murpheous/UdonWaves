
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
    [SerializeField] Vector2 panelSize = Vector2.one;
    [SerializeField] Vector2 shrinkSize = Vector2.one;
    [SerializeField] float textBorder = 20;
    [SerializeField] RectTransform panelXfrm;
    [SerializeField] TextMeshProUGUI infoText;
    [SerializeField] Toggle[] toggles = null;
    [SerializeField] InfoPage[] pages = null;
    int toggleCount = 0;

    bool hasTextField = false;
    private bool iamOwner;
    private VRCPlayerApi player;
    private RectTransform textRect;
//    private VRC.Udon.Common.Interfaces.NetworkEventTarget toTheOwner = VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner;
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
            if (hasTextField)
            {
                if (value >= 0)
                {
                    infoText.text = "";
                    string title = "";
                    if (pages[value] != null)
                    {
                        title = pages[value].PageTitle;
                        infoText.text = string.Format("<align=center><line-height=250%><b>{0}</b></line-height></align>\n<margin=2%>{1}</margin>", title, pages[value].PageBody);
                    }
                }
                else
                    infoText.text = defaultText;
            }
            Vector2 newSize = showPanel >= 0 ? panelSize : shrinkSize;
            Vector3 newPosition = showPanel >= 0 ? Vector3.zero : new Vector3 (0, -(panelSize.y - shrinkSize.y)/2.0f,0);
            if (panelXfrm != null) 
            {
                panelXfrm.sizeDelta = newSize;
                panelXfrm.localPosition = newPosition;
            }
            if (textRect != null)
            {
                Vector2 newTextDim = new Vector2(newSize.x - textBorder, newSize.y - textBorder);
                textRect.sizeDelta = newTextDim;
                //textRect.localPosition = newPosition;
            }
        }
    }

    public void onToggleChanged()
    {
        int newToggle = -1;
        for (int i = 0; newToggle < 0 && i < toggles.Length; i++)
        {
            if (toggles[i] != null)
            {
                if (toggles[i].isOn)
                    newToggle = i;
            }
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
        //pages = new InfoPage[toggleCount];
        //for (int i = 0; i < toggleCount; i++)
        //{
        //    if (toggles[i] != null)
        //        pages[i] = toggles[i].GetComponent<InfoPage>();
        //}
        player = Networking.LocalPlayer;
        UpdateOwnerShip();
        if (toggleGroup == null)
            toggleGroup = gameObject.GetComponent<ToggleGroup>();
        toggleGroup.allowSwitchOff = true;
        toggleGroup.SetAllTogglesOff(false);
        hasTextField = infoText != null;
        if (hasTextField)
        {
            textRect = infoText.GetComponent<RectTransform>();
        }
        if ( panelXfrm != null)
        {
            panelSize = panelXfrm.sizeDelta;
        }
        toggleGroup.EnsureValidState();
        onToggleChanged();
    }
}
