using UnityEngine.UI;
using UdonSharp;
using UnityEngine;
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
    private float smoothTime = 0f;
    [SerializeField]
    private bool hideLabel = false;
    [SerializeField]
    private bool unitsInteger = false;
    [SerializeField]
    private bool displayInteger = false;
    [SerializeField]
    private string sliderUnit;
    [SerializeField]
    private float unitDisplayScale = 1;
    [SerializeField]
    private float sliderScale = 1.0f;
    [SerializeField]
    UdonBehaviour SliderClient;
    [SerializeField]
    string clientVariableName = "";
    [SerializeField]
    string clientPointerState = "";
    [SerializeField]
    private float currentValue;
    [SerializeField,UdonSynced,FieldChangeCallback(nameof(SyncedValue))]
    private float syncedValue;
    [SerializeField]
    private float targetValue;
    [SerializeField]
    private float maxValue = 1;
    [SerializeField]
    private float minValue = 0;

    // UdonSync stuff
    private VRCPlayerApi player;
    private bool locallyOwned = false;
    private bool started = false;
    const float smoothThreshold = 0.001f;


    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        locallyOwned = Networking.IsOwner(this.gameObject);
    }


    private bool interactible;
    public bool Interactable
    {
        get {
            if (slider != null) 
                interactible = slider.interactable;
            return interactible;
            } 
        set 
        { 
            interactible = value;
            if (slider!= null ) 
                slider.interactable = value; 
        }
    }

    public float UnitDisplayScale 
    { 
        get => unitDisplayScale; 
        set 
        {
            if (unitDisplayScale != value)
            {
                unitDisplayScale = value;
            }
        } 
    }
    public void SetValues(float value, float min, float max)
    {
        if (!started)
        {
            minValue = min;
            maxValue = max;
            syncedValue = value / sliderScale;
            return;
        }
        MinValue = min;
        MaxValue= max;
        if (locallyOwned)
            SyncedValue = value / sliderScale;
    }

    public void SetValue(float value)
    {
        if (!started)
        {
            syncedValue = value/sliderScale;
            return;
        }
        if (locallyOwned)
            SyncedValue = value / sliderScale;
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
    public string SliderUnit
    {
        get => sliderUnit;
        set
        {
            sliderUnit = value;
            CurrentValue = currentValue;
        }
    }

    private float SyncedValue
    {
        get => syncedValue;
        set
        {
            syncedValue = value;
            if (slider!= null && !pointerIsDown)
                slider.SetValueWithoutNotify(syncedValue);
            targetValue = syncedValue * sliderScale;
            if (smoothTime <= 0)
                CurrentValue = targetValue;
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
            float displayValue = targetValue * unitDisplayScale;
            if (displayInteger)
                displayValue = Mathf.RoundToInt(displayValue);
            if (unitsInteger || displayInteger)
                sliderLabel.text = string.Format("{0}{1}", (int)displayValue, sliderUnit);
            else
                sliderLabel.text = string.Format("{0:0.0}{1}", displayValue, sliderUnit);
        }
        else
        {
            sliderLabel.text = "";
        }
    }
    public float CurrentValue { 
        get => currentValue;
        set
        {
            currentValue = value;
            if (iHaveClientVar)
            {
                if (unitsInteger)
                    SliderClient.SetProgramVariable<int>(clientVariableName, Mathf.RoundToInt(currentValue));
                else
                    SliderClient.SetProgramVariable<Single>(clientVariableName, currentValue);
            }
        }
    }

    public float MaxValue
    {
        get => maxValue;
        set 
        { 
            maxValue = value;
            if (slider != null)
                slider.maxValue= maxValue/sliderScale;
        }
    }

    public float MinValue
    {
        get => minValue;
        set
        {
            minValue = value;
            if (slider != null)
                slider.minValue = minValue/sliderScale;
        }
    }

    public void onValue()
    {
        if (pointerIsDown)
        {
            SyncedValue = slider.value;
        }
    }

    [SerializeField]
    private bool pointerIsDown = false;
    public bool PointerIsDown { get => pointerIsDown; }
    public void ptrDn()
    {
        if (!locallyOwned)
            Networking.SetOwner(player,gameObject);
        pointerIsDown=true;
        if (iHaveClientPtr)
            SliderClient.SetProgramVariable<bool>(clientPointerState, pointerIsDown);
    }
    public void ptrUp()
    {
        pointerIsDown = false;
        if (iHaveClientPtr)
            SliderClient.SetProgramVariable<bool>(clientPointerState, pointerIsDown);
    }

    private bool iHaveClientVar = false;
    private bool iHaveClientPtr = false;

    private float smthVel = 0;
    public void Update()
    {
        if (smoothTime <= 0f || currentValue == targetValue)
            return;
        if (Mathf.Abs(1f - targetValue / currentValue) > smoothThreshold)
            CurrentValue = Mathf.SmoothDamp(currentValue, targetValue, ref smthVel, smoothTime);
        else
            CurrentValue = targetValue;
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
            slider.interactable = interactible;
            MinValue = minValue;
            MaxValue = maxValue;
            SyncedValue = syncedValue;
        }
        started = true;
    }
}
