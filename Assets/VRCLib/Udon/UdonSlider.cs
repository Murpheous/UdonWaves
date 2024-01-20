
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
[RequireComponent(typeof(Slider))]
public class UdonSlider : UdonSharpBehaviour
{
    private Slider mySlider;
    [SerializeField]
    private TextMeshProUGUI sliderLabel;
    [SerializeField]
    private TextMeshProUGUI sliderTitle;
    [SerializeField]
    private bool hideLabel = false;
    [SerializeField]
    private bool unitsInteger = false;
    [SerializeField]
    private bool displayInteger = false;
    [SerializeField]
    private string sliderUnit;
    [SerializeField]
    UdonBehaviour SliderClient;
    [SerializeField]
    string clientVariableName = "SliderValueVar";
    [SerializeField]
    string clientPointerStateVar = "PointerStateVar";
    [SerializeField]
    private float currentValue;
    [SerializeField]
    private float maxValue = 1;
    [SerializeField]
    private float minValue = 0;

    private float reportedValue = 0.0f;
    private bool isInteractible;

   
    public bool IsInteractible
    {
        get
        {
            isInteractible = mySlider.interactable;
            return isInteractible;
        }
        set
        {
            isInteractible = value;
            mySlider.interactable = value;
        }
    }

    public void SetValue(float value)
    {
        if (mySlider == null)
            mySlider = GetComponent<Slider>();
        reportedValue = value;
        currentValue = value;
        mySlider.value = value;
    }
    public void SetLimits(float min, float max)
    {
        if (mySlider == null)
            mySlider = GetComponent<Slider>();
        minValue = min;
        maxValue = max;
        mySlider.minValue = minValue;
        mySlider.maxValue = maxValue;
    }
    public string TitleText
    {
        get
        {
            if (!iHaveTitle)
                return "";
            return sliderTitle.text;
        }
        set
        {
            if (!iHaveTitle)
                return;
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
    private void Updatelabel()
    {
        if (!iHaveLabel) return;
        if (hideLabel)
        {
            sliderLabel.text = "";
            return;
        }
        float displayValue = currentValue;
        if (displayInteger)
            displayValue = Mathf.RoundToInt(displayValue);
        if (unitsInteger || displayInteger)
            sliderLabel.text = string.Format("{0}{1}", (int)displayValue, sliderUnit);
        else
            sliderLabel.text = string.Format("{0:0.0}{1}", displayValue, sliderUnit);
    }
    public float CurrentValue
    {
        get => currentValue;
        set
        {
            currentValue = value;
            float sliderValue = currentValue;
            if (mySlider.value != sliderValue)
                mySlider.value = sliderValue;
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
                Updatelabel();
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
                if (mySlider != null)
                    mySlider.maxValue = maxValue;
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
                if (mySlider != null)
                    mySlider.minValue = minValue;
            }
        }
    }

    public void onValue()
    {
        CurrentValue = mySlider.value;
    }

    public void ptrDn()
    {
        if (iHaveClientPtr)
            SliderClient.SetProgramVariable<bool>(clientPointerStateVar, true);
    }
    public void ptrUp()
    {
        if (iHaveClientPtr)
            SliderClient.SetProgramVariable<bool>(clientPointerStateVar, false);
    }

    private bool iHaveLabel = false;
    private bool iHaveTitle = false;
    private bool iHaveClientVar = false;
    private bool iHaveClientPtr = false;
    public void Start()
    {
        mySlider = GetComponent<Slider>();
        iHaveTitle = sliderTitle != null;
        iHaveLabel = sliderLabel != null;
        iHaveClientVar = (SliderClient != null) && (!string.IsNullOrEmpty(clientVariableName));
        iHaveClientPtr = (SliderClient != null) && (!string.IsNullOrEmpty(clientPointerStateVar));
        if (!iHaveLabel)
            hideLabel = true;
        isInteractible = mySlider.interactable;
        onValue();
    }
}
