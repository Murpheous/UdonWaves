
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]

public class UdonTween : UdonSharpBehaviour
{
    [SerializeField]
    AnimationCurve m_moveCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);
    [SerializeField]
    private Toggle stateToggle;
    [SerializeField]
    UdonBehaviour TweenClient;
    [SerializeField]
    string clientVariableName = "tweenVariable";
    [SerializeField]
    float damping = 0.1f;

    private VRCPlayerApi player;
    bool iamOwner = false;

    public float currentValue;
    private float reportedValue = 0.0f;
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(CurrentState))]
    public bool currentState = false;

    private bool CurrentState
    {
        get => currentState;
        set
        {
            if (value != currentState)
            {
                currentState = value;
                if (value)
                    animationTime = Mathf.Clamp(currentValue, 0, 1);
                else
                    animationTime = Mathf.Clamp(1 - currentValue, 0, 1);
            }
            if (stateToggle != null)
            {
                if (stateToggle.isOn != currentState)
                    stateToggle.isOn = currentState;
            }
            RequestSerialization();
        }
    }

    float animationTime = 0;
    public float CurrentValue
    {
        get => currentValue;
        set
        {
            currentValue = value;
            if (reportedValue != currentValue)
            {
                reportedValue = currentValue;
                if ((TweenClient != null) && (!string.IsNullOrEmpty(clientVariableName)))
                    TweenClient.SetProgramVariable<Single>(clientVariableName, currentValue);
            }
        }
    }

    private void ReviewOwnerShip()
    {
        iamOwner = Networking.IsOwner(this.gameObject);
    }

    public void onToggleChanged()
    {
        bool togVal = CurrentState;
        if (stateToggle != null)
            togVal = stateToggle.isOn;
        if (togVal != currentState)
        {
            if (!iamOwner)
                Networking.SetOwner(player, gameObject);
            CurrentState = togVal;
        }
    }

    private void Update()
    {
        if (animationTime < 1)
            animationTime += Time.deltaTime * damping;
        if (currentState)
        {
            if (currentValue < 1)
                CurrentValue = Mathf.Lerp(0, 1, m_moveCurve.Evaluate(animationTime));
        }
        else
        {
            if (currentValue > 0)
                CurrentValue = Mathf.Lerp(1, 0, m_moveCurve.Evaluate(animationTime));
        }
    }
/*
    public void SetValues(float value, float min, float max)
    {
        currentValue = value;
        reportedValue = value;
    }
*/
    private void Start()
    {
        player = Networking.LocalPlayer;

        ReviewOwnerShip();
        CurrentState = currentState;
    }
}
