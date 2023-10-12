
using UdonSharp;
using UnityEngine;
using System.Collections;
using VRC.SDKBase;
using VRC.Udon;

[RequireComponent(typeof(LineRenderer))]
public class UdonCircle : UdonSharpBehaviour
{
    public float ThetaScale = 0.02f;
    [SerializeField]
    private float radius = 0.5f;
    public float Radius
    {
        get => radius;
        set
        {
            if (radius != value)
            {
                radius = value;
                UpdateCircle();
            }
        }
    }
    private int Size;
    private LineRenderer LineDrawer;
    private float Theta = 0f;
    [SerializeField,Range(0.001f,0.01f)]
    private float lineThickness = 0.005f;
    bool started = false;
    void Start()
    {
        LineDrawer = GetComponent<LineRenderer>();
        if (LineDrawer != null)
        {
            LineDrawer.startWidth = lineThickness;
            LineDrawer.endWidth = lineThickness;
        }
        else
            Debug.Log("UdonCircle !No LineRenderer");
        started = true;
        UpdateCircle();
    }

    void UpdateCircle()
    {
        if (!started)
            return;
        Theta = 0f;
        Size = (int)((1f / ThetaScale) + 1f);
        LineDrawer.positionCount = Size;
        for (int i = 0; i < Size; i++)
        {
            Theta += (2.0f * Mathf.PI * ThetaScale);
            float x = radius * Mathf.Cos(Theta);
            float y = radius * Mathf.Sin(Theta);
            LineDrawer.SetPosition(i, new Vector3(x, y, 0));
        }
    }
}
