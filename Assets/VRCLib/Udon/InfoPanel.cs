
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
    [SerializeField] private ToggleGroup toggleGroup;
    [SerializeField] private Button closeButton;
    [SerializeField, FieldChangeCallback(nameof(LanguageIndex))] int languageIndex = 0;
    [SerializeField,Tooltip("Added object made visible when panle is shown")] 
    private GameObject supportPanel;
    [SerializeField] private bool growShrink = true;
    [SerializeField] Vector2 panelSize = Vector2.one;
    [SerializeField] Vector2 shrinkSize = Vector2.one;
    [SerializeField] bool showHideContentPanel= true;
    //   [SerializeField] float textBorder = 20;
    [SerializeField] RectTransform contentPanelRect;
    [SerializeField] TextMeshProUGUI contentText;
    [SerializeField] Toggle[] toggles = null;
    [SerializeField] InfoPage[] pages = null;

    int toggleCount = 0;

    bool hasTextField = false;
    bool hasClose = false;
    private bool iamOwner;
    private VRCPlayerApi player;
//    private RectTransform textRect;
//    private VRC.Udon.Common.Interfaces.NetworkEventTarget toTheOwner = VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner;
//    private VRC.Udon.Common.Interfaces.NetworkEventTarget toAll = VRC.Udon.Common.Interfaces.NetworkEventTarget.All;

    [SerializeField,UdonSynced,FieldChangeCallback(nameof(ActiveInfoPage))] 
    private int activeInfoPage = -1;

    [SerializeField] string[] defaultTexts;
    
    public int LanguageIndex
    {
        get => languageIndex;
        set
        {
            languageIndex = value;
            if (pages != null && pages.Length > 0)
            {
                foreach (var page in pages)
                {
                    if (page != null)
                        page.LangaugeIndex = languageIndex;
                }
            }
            ActiveInfoPage = activeInfoPage;
        }
    }

    private string defaultText
    {
        get
        {
            if (defaultTexts == null || defaultTexts.Length <= 0)
                return "";
            if (languageIndex >= defaultTexts.Length || defaultTexts[languageIndex] == null)
                return defaultTexts[0];
            return defaultTexts[languageIndex];
        }
    }


    public int ActiveInfoPage
    {
        get => activeInfoPage;
        set
        {
            activeInfoPage = value;
            //Debug.Log("ActiveInfoPage=" + value);
            if (hasTextField)
            {
                if (value >= 0)
                {
                    contentText.text = "";
                    string title = "";
                    if (pages[value] != null)
                    {
                        title = pages[value].PageTitle;
                        contentText.text = string.Format("<align=center><b>{0}</b></align>\n{1}", title, pages[value].PageBody);
                    }
                }
                else
                    contentText.text = defaultText;
            }
            if (showHideContentPanel && contentPanelRect != null)
            {
                if (hasClose)
                    closeButton.gameObject.SetActive(activeInfoPage >= 0);
                if (supportPanel != null)
                    supportPanel.SetActive(activeInfoPage >= 0);
                if (growShrink)
                {
                    Vector2 newSize = activeInfoPage >= 0 ? panelSize : shrinkSize;
                    bool validSize = (newSize.x > 0) && (newSize.y > 0);
                    Vector3 newPosition = activeInfoPage >= 0 ? Vector3.zero : new Vector3(0, -(panelSize.y - shrinkSize.y) / 2.0f, 0);
                    if (validSize)
                    {
                        contentPanelRect.sizeDelta = newSize;
                        contentPanelRect.localPosition = newPosition;
                    }
                    else
                        Debug.Log(gameObject.name + "ActiveInfoPage: ValidSize=false");
                    contentPanelRect.gameObject.SetActive(validSize);
                }
                else
                {
                    contentPanelRect.gameObject.SetActive(activeInfoPage >= 0);
                }
            }
        }
    }
    public void onBtnClose()
    {
        if (selectedToggle >= 0)
        {
            SelectedToggle = -1;
        }
    }
    public void onToggle()
    {
        int toggleIdx = -1;

        for (int i = 0; toggleIdx < 0 && i < toggles.Length; i++)
        {
            if (toggles[i] != null)
            {
                if (toggles[i].isOn)
                    toggleIdx = i;
            }
        }
        //Debug.Log("Toggle Changed: " + toggleIdx.ToString());
        SelectedToggle = toggleIdx;
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
            //Debug.Log("Toggle Select: " +  value.ToString());
            if (!iamOwner)
            {
                togglePending = true;
                pendingToggle = value;
                Networking.SetOwner(player, gameObject);
                return;
            }
            togglePending = false;
            selectedToggle = value;
            ActiveInfoPage = selectedToggle;
            if (value >= 0 && value < toggleCount)
            {
                if (toggles[selectedToggle] != null)
                    toggles[selectedToggle].SetIsOnWithoutNotify(true);
            }
            if (toggleGroup != null && value < 0)
                toggleGroup.SetAllTogglesOff(false);
            RequestSerialization();
        }
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
        hasTextField = contentText != null;
        hasClose = closeButton != null && closeButton.gameObject.activeSelf;
        if (contentPanelRect != null)
            panelSize = contentPanelRect.sizeDelta;
        if (growShrink)
            growShrink = (shrinkSize.x * shrinkSize.y) > 0;
        toggleGroup.EnsureValidState();
        onToggle();
    }
}
