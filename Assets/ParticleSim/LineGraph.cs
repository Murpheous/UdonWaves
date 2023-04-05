
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
[RequireComponent(typeof(LineRenderer))]

public class LineGraph : UdonSharpBehaviour
{
    public float width;
    public float height;
    public int pointsAcross;
    public int pointsHigh;
    [SerializeField, Range(0.001f, 0.01f)]
    private float lineThickness = 0.005f;

    private LineRenderer LineDrawer;

    void Start()
    {
        LineDrawer = GetComponent<LineRenderer>();
        if (LineDrawer != null)
        {
            LineDrawer.startWidth = lineThickness;
            LineDrawer.endWidth = lineThickness;
        }
    }
}
