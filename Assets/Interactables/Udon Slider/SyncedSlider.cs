using UnityEngine.UI;
using UdonSharp;
using UnityEngine;
using TMPro;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Serialization.OdinSerializer.Utilities;
using System;

public class SyncedSlider : UdonSharpBehaviour
{
    [SerializeField]    
    private Slider slider;
    [SerializeField]
    private TextMeshProUGUI sliderLabel;
    [SerializeField]
    private string sliderUnit;
    [SerializeField]
    private float sliderScale = 1.0f;
    [SerializeField]
    UdonBehaviour SliderClient;
    [SerializeField]
    string clientVariableName = "SliderValueVar";
    [SerializeField]
    string clientPointerStateVar = "PointerStateVar";
    private float currentValue;
    private float reportedValue = 0.0f;
    private bool isInteractible;
    public bool IsInteractible
    {
        get => isInteractible; 
        set 
        { 
            isInteractible = value;
            if (slider!= null ) 
                slider.interactable = value; 
        }
    }
    public void SetValues(float value, float min, float max)
    {
        currentValue = value;
        reportedValue= value;
        minValue= min;
        maxValue= max;
        if (slider != null)
        {
            slider.minValue= minValue;
            slider.maxValue= maxValue;
            slider.value= value*sliderScale;
        }
    }
    public float CurrentValue { 
        get => currentValue;
        set
        {
            currentValue = value;
            if (slider != null)
            {
                slider.value = currentValue / sliderScale;
                sliderLabel.text = string.Format("{0:0.0}{1}", currentValue, sliderUnit);
            }
            if (reportedValue != value)
            {
                reportedValue = value;
                if ((SliderClient != null) && (!string.IsNullOrEmpty(clientVariableName)))
                    SliderClient.SetProgramVariable<Single>(clientVariableName, currentValue);
            }
        }
    }
    private float maxValue = 1;
    private float minValue = 0;
    public float MaxValue
    {
        get => maxValue;
        set 
        { 
            if (maxValue != value) 
            { 
                maxValue = value;
                if (slider != null)
                    slider.maxValue= maxValue;
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
                    slider.minValue = minValue;
            }
        }
    }

    public void SliderValueChange()
    {
        if (slider != null)
        {
            CurrentValue = slider.value * sliderScale;
        }
    }

    public void OnPointerDown()
    {
        if ((SliderClient != null) && (!string.IsNullOrEmpty(clientPointerStateVar)))
            SliderClient.SetProgramVariable<bool>(clientPointerStateVar, true);
    }
    public void OnPointerUp()
    {
        if ((SliderClient != null) && (!string.IsNullOrEmpty(clientPointerStateVar)))
            SliderClient.SetProgramVariable<bool>(clientPointerStateVar, false);
    }
    public void Start()
    {
        if (slider != null)
        {
            isInteractible = slider.interactable;
            maxValue = slider.maxValue;
            minValue = slider.minValue;
        }
        SliderValueChange();
    }
}
