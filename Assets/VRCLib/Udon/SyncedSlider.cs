using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRC.SDKBase;
using VRC.Udon;
using System;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)] // Keeps performance up
public class SyncedSlider : UdonSharpBehaviour
{
    [SerializeField]    
    private Slider slider;
    [SerializeField]
    private TextMeshProUGUI sliderLabel;
    [SerializeField]
    private TextMeshProUGUI sliderTitle;
    [SerializeField]
    private float smoothRate = 3.3f;
    [SerializeField]
    private bool hideLabel = false;
    [SerializeField]
    private bool unitsInteger = false;
    [SerializeField]
    private bool displayInteger = false;
    [SerializeField]
    private string sliderUnit;
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(DisplayScale))]
    private float displayScale = 1;
    [SerializeField]
    UdonBehaviour SliderClient;
    [SerializeField]
    string clientVariableName = "";
    [SerializeField]
    string clientPointerState = "";
    [SerializeField]
    private float reportedValue;
    [SerializeField,UdonSynced,FieldChangeCallback(nameof(SyncedValue))]
    private float syncedValue;
    private float targetValue;
    [SerializeField]
    private float maxValue = 1;
    [SerializeField]
    private float minValue = 0;
    const float thresholdScale = 0.002f;
    float smoothThreshold = 0.003f;

    // UdonSync stuff
    private VRCPlayerApi player;
    private bool locallyOwned = false;
    private bool started = false;


    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        locallyOwned = Networking.IsOwner(this.gameObject);
    }

    [SerializeField]
    private bool interactable=true;
    public bool Interactable
    {
        get {
            if (slider != null) 
                interactable = slider.interactable;
            return interactable;
            } 
        set 
        { 
            interactable = value;
            if (slider!= null ) 
                slider.interactable = value; 
        }
    }

    public void SetValues(float value, float min, float max)
    {
        minValue = min;
        maxValue = max;
        if (!started)
        {
            reportedValue = value;
            syncedValue = value;
            return;
        }
        updateThreshold();
        if (locallyOwned)
        {
            if (slider != null)
                slider.SetValueWithoutNotify(value);
            syncedValue = value;
            targetValue = value;
            if (reportedValue != syncedValue)
                ReportedValue = syncedValue;
            UpdateLabel();
            RequestSerialization();
        }
    }

    public void SetValue(float value)
    {
        if (!started)
        {
            reportedValue = value;
            syncedValue = value;
            return;
        }
        updateThreshold();
        if (locallyOwned)
        {
            if (slider != null)
                slider.SetValueWithoutNotify(value);
            syncedValue = value;
            targetValue = value;
            if (reportedValue != syncedValue)
                ReportedValue = syncedValue;
            UpdateLabel();
            RequestSerialization();
        }
    }

    public string TitleText
    {
        get 
        { 
            if (sliderTitle == null)
                return "";
            return sliderTitle.text;
        }
        set 
        { 
            sliderTitle.text = value; 
        }
    }

    public float DisplayScale
    {
        get => displayScale;
        set
        {
            displayScale = value;
            UpdateLabel();
            RequestSerialization();
        }
    }
    public string SliderUnit
    {
        get => sliderUnit;
        set
        {
            sliderUnit = value;
            UpdateLabel();
        }
    }

    public float SyncedValue
    {
        get => syncedValue;
        set
        {
            if (!pointerIsDown)
            {
                SetSmoothingTarget(value);
                if (slider != null)
                    slider.SetValueWithoutNotify(syncedValue);
            }
            UpdateLabel();
            RequestSerialization();
        }
    }
    private void UpdateLabel()
    {
        if (sliderLabel == null)
            return;
        if (!hideLabel)
        {
            float displayValue = syncedValue * displayScale;
            if (displayInteger)
                displayValue = Mathf.RoundToInt(displayValue);
            if (unitsInteger || displayInteger)
                sliderLabel.text = string.Format("{0}{1}", (int)displayValue, sliderUnit);
            else
                sliderLabel.text = string.Format("{0:0.0}{1}", displayValue, sliderUnit);
        }
    }
    public float ReportedValue { 
        get => reportedValue;
        set
        {
            reportedValue = value;
            if (iHaveClientVar)
            {
                if (unitsInteger)
                    SliderClient.SetProgramVariable<int>(clientVariableName, Mathf.RoundToInt(reportedValue));
                else
                    SliderClient.SetProgramVariable<Single>(clientVariableName, reportedValue);
            }
        }
    }
    private void updateThreshold()
    {
        if (slider != null)
        {
            slider.minValue = minValue;
            slider.maxValue = maxValue;
        }
        smoothThreshold = thresholdScale * Mathf.Max(0.01f, Mathf.Abs(maxValue - minValue));
    }
    public float MaxValue
    {
        get => maxValue;
    }

    public float MinValue
    {
        get => minValue;
    }

    private void SetSmoothingTarget(float value)
    {
        syncedValue = value;
        targetValue = value;
        if (smoothRate <= 0 && reportedValue != syncedValue)
            ReportedValue = syncedValue;
        UpdateLabel();
    }
    public void onValue()
    {
        if (!locallyOwned)
            Networking.SetOwner(player, gameObject);
        if (started)
        {
            SetSmoothingTarget(slider.value);
            RequestSerialization();
        }
        else
        {
            if (slider != null)
                syncedValue = slider.value;
        }
    }

    [SerializeField]
    private bool pointerIsDown = false;
    public bool PointerIsDown { get => pointerIsDown; }
    public void ptrDn()
    {
        if (!locallyOwned)
            Networking.SetOwner(player,gameObject);
        if (pointerIsDown)
            return;
        pointerIsDown=true;
        if (iHaveClientPtr)
            SliderClient.SetProgramVariable<bool>(clientPointerState, pointerIsDown);
    }
    public void ptrUp()
    {
        if (!pointerIsDown)
            return;
        pointerIsDown = false;
        if (iHaveClientPtr)
            SliderClient.SetProgramVariable<bool>(clientPointerState, pointerIsDown);
    }

    private bool iHaveClientVar = false;
    private bool iHaveClientPtr = false;

    private float smthVel = 0;
    public void Update()
    {
        if (smoothRate <= 0f)
            return;
        float delta = Mathf.Abs(targetValue - reportedValue);
        if (delta == 0)
            return;
        if (Mathf.Abs(reportedValue - targetValue) > smoothThreshold)
            ReportedValue = Mathf.SmoothDamp(reportedValue, targetValue, ref smthVel, 0.02f * smoothRate);
        else
            ReportedValue = targetValue;
    }
    public void Start()
    {
        player = Networking.LocalPlayer;
        locallyOwned = Networking.IsOwner(this.gameObject);
        
        iHaveClientVar = (SliderClient != null) && (!string.IsNullOrEmpty(clientVariableName));
        iHaveClientPtr = (SliderClient != null) && (!string.IsNullOrEmpty(clientPointerState));
        if (sliderLabel == null)
            hideLabel = true;
        if (slider != null)
        {
            slider.interactable = interactable;
            slider.minValue = minValue;
            slider.maxValue = maxValue;
           // slider.SetValueWithoutNotify(syncedValue);
        }
        reportedValue = syncedValue;
        targetValue = syncedValue;
        SyncedValue = syncedValue;
        DisplayScale = displayScale;
        updateThreshold();
        UpdateLabel();
        started = true;
    }
}
