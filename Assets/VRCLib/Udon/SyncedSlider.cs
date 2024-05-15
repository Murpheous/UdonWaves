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
        reportedValue = value;
        if (slider != null)
        {
            slider.minValue = minValue / sliderScale;
            slider.maxValue = maxValue / sliderScale;
        }
        PointerValue = value / sliderScale;
    }

    public void SetValue(float value)
    {
        reportedValue = value;
        PointerValue = value / sliderScale;
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
    public float CurrentValue { 
        get => currentValue;
        set
        {
            currentValue = value;
            if (sliderLabel != null)
            {
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
            if (maxValue != value) 
            { 
                maxValue = value;
                if (slider != null)
                    slider.maxValue= maxValue/sliderScale;
            }
        }
    }

    public float MinValue
    {
        get => minValue;
        set
        {
            if (minValue != value)
            {
                minValue = value;
                if (slider != null)
                    slider.minValue = minValue/sliderScale;
            }
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
    public void onPtrDn()
    {
        if (!iAmOwner)
            Networking.SetOwner(player,gameObject);
        pointerIsDown=true;
    }
    public void onPtrUp()
    {
        pointerIsDown = false;
    }

    private bool iHaveClientVar = false;

    private float smthVel = 0;
    public void Update()
    {
        if (smoothTime <= 0f)
            return;
        if (currentValue != targetValue)
        {
            CurrentValue = Mathf.SmoothDamp(currentValue, targetValue, ref smthVel, smoothTime);
        }
    }
    public void Start()
    {
        player = Networking.LocalPlayer;
        iAmOwner = Networking.IsOwner(this.gameObject);

        iHaveClientVar = (SliderClient != null) && (!string.IsNullOrEmpty(clientVariableName));
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
    }
}
