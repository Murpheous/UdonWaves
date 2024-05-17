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
    [SerializeField,UdonSynced,FieldChangeCallback(nameof(PointerValue))]
    private float pointerValue;
    [SerializeField]
    private float targetValue;
    [SerializeField]
    private float maxValue = 1;
    [SerializeField]
    private float minValue = 0;

    // UdonSync stuff
    private VRCPlayerApi player;
    private bool iAmOwner = false;
    private bool started = false;


    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        iAmOwner = Networking.IsOwner(this.gameObject);
    }


    private float reportedValue = 0.0f;
    private bool interactible;
    private bool isInitialized = false;
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
        minValue= min;
        maxValue= max;
        if (slider != null)
        {
            slider.minValue = minValue / sliderScale;
            slider.maxValue = maxValue / sliderScale;
        }
        if (iAmOwner || !started)
        {
            reportedValue = value;
            PointerValue = value / sliderScale;
        }
        UpdateLabel();
    }

    public void SetValue(float value)
    {
        if (iAmOwner || !started)
        {
            reportedValue = value;
            PointerValue = value / sliderScale;
        }
        UpdateLabel();
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

    private float PointerValue
    {
        get => pointerValue;
        set
        {
            pointerValue = value;
            if (slider!= null && slider.value != value && !pointerIsDown)
                slider.SetValueWithoutNotify(pointerValue);
            targetValue = pointerValue * sliderScale;
            if (smoothTime <= 0)
                CurrentValue = targetValue;
            RequestSerialization();
        }
    }
    private void UpdateLabel()
    {
        if (sliderLabel == null)
            return;
        if (!hideLabel)
        {
            float displayValue = currentValue * unitDisplayScale;
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
            UpdateLabel();
            if (reportedValue != currentValue)
            {
                reportedValue = currentValue;
                if (iHaveClientVar)
                {
                    if (unitsInteger)
                        SliderClient.SetProgramVariable<int>(clientVariableName, Mathf.RoundToInt(currentValue));
                    else
                        SliderClient.SetProgramVariable<Single>(clientVariableName, currentValue);
                }
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
            PointerValue = slider.value;
        }
    }

    [SerializeField]
    private bool pointerIsDown = false;
    public bool PointerIsDown { get => pointerIsDown; }
    public void ptrDn()
    {
        if (!iAmOwner)
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
        if (smoothTime > 0f && currentValue != targetValue)
        {
            CurrentValue = Mathf.SmoothDamp(currentValue, targetValue, ref smthVel, smoothTime);
        }
    }
    public void Start()
    {
        player = Networking.LocalPlayer;
        iAmOwner = Networking.IsOwner(this.gameObject);

        iHaveClientVar = (SliderClient != null) && (!string.IsNullOrEmpty(clientVariableName));
        iHaveClientPtr = (SliderClient != null) && (!string.IsNullOrEmpty(clientPointerState));
        if (sliderLabel == null)
            hideLabel = true;
        if (slider != null)
        {
            interactible = slider.interactable;
            if (!isInitialized)
            {
                isInitialized = true;
            }
        }
        started = true;
    }
}
