
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
[RequireComponent(typeof(LineRenderer))]
public class UdonLine : UdonSharpBehaviour
{
    [SerializeField, FieldChangeCallback(nameof(LineLength))]
    public float lineLength = 0.5f;
    public float LineLength
    {
        get => lineLength;
        set
        {
            if ( lineLength == value)
                return;
            lineLength = value;
            UpdateLine();
        }
    }

    [SerializeField, Range(0f, 0.1f), FieldChangeCallback(nameof(LineWidth))]
    public float lineWidth = 0.03f;

    public float LineWidth
    {
        get => lineWidth;
        set
        {
            lineWidth = value;
            UpdateLine();
        }
    }

    [SerializeField, FieldChangeCallback(nameof(LineColour))]
    public Color lineColour = Color.cyan;
    [SerializeField, Range(0f, 1f),FieldChangeCallback(nameof(Alpha))]
    public float alpha = 1;
    private Color currentColour = Color.white;

    public Color LineColour
    {
        get => lineColour;
        set
        {
            lineColour = value;
            RefreshColours();
        }
    }

    public float Alpha
    {
        get => alpha;
        set
        {
            alpha = value;
            RefreshColours();
        }
    }

    [SerializeField,FieldChangeCallback(nameof(ThetaDegrees))]
    public float thetaDegrees = 0;

    private Vector2 startLocal = Vector2.zero;
    private Vector2 endLocal = Vector2.right;
    public float ThetaDegrees
    {
        get => thetaDegrees;
        set
        {
            thetaDegrees = value;
            transform.localRotation = Quaternion.Euler(0, 0, thetaDegrees);
        }
    }

    [SerializeField]
    LineRenderer theLine;

    private void RefreshColours()
    {
        bool isVisible = Alpha > 0;
        currentColour = lineColour;
        //currentColour.a = alpha;
        currentColour *= alpha;
        //theLine.gameObject.SetActive(false);
        theLine.enabled = isVisible;
        if (!isVisible)
            return;
        theLine.startColor = currentColour;
        theLine.endColor = currentColour;
    }

    private void UpdateLine()
    {
        startLocal = Vector3.zero;
        endLocal = Vector3.right * lineLength;
        if (theLine != null)
        {
            theLine.positionCount = 2;
            theLine.SetPosition(0, startLocal);
            theLine.SetPosition(1, endLocal);
            theLine.startWidth = lineWidth;
            theLine.endWidth = lineWidth;
        }
    }

    void Start()
    {
        if (theLine == null)
            theLine = GetComponent<LineRenderer>();
        UpdateLine();
        RefreshColours();
        ThetaDegrees = thetaDegrees;
    }
}
