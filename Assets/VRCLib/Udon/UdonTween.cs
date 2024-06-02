using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

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

    [SerializeField, FieldChangeCallback(nameof(CurrentState))]
    public bool currentState = false;
    private bool isPlaying = false;

    private bool CurrentState
    {
        get => currentState;
        set
        {
            isPlaying |= currentState != value;
            currentState = value;
        }
    }
    [SerializeField]
    float animationTime = 0;
    private void sendValue(float value)
    {
        if ((TweenClient != null) && (!string.IsNullOrEmpty(clientVariableName)))
            TweenClient.SetProgramVariable<Single>(clientVariableName, value);
    }

    public void onToggle()
    {
        bool togVal = CurrentState;
        if (stateToggle != null)
            togVal = stateToggle.isOn;
        if (togVal != currentState)
            CurrentState = togVal;
    }

    private void Update()
    {
        if (!isPlaying)
            return;
        animationTime = Mathf.Clamp01(animationTime + (Time.deltaTime * (currentState ? damping : -damping)));
        isPlaying = animationTime > 0 && animationTime < 1;
        sendValue(m_moveCurve.Evaluate(animationTime));
    }

    private void Start()
    {
        if (stateToggle != null)
            currentState = stateToggle.isOn;
        animationTime = currentState ? 0.9999f : 0.0001f;
        isPlaying = true;
    }
}
