
using Newtonsoft.Json.Linq;
using System.Runtime.Remoting.Messaging;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)] // Keeps performance up

public class UdonGrid2D : UdonSharpBehaviour
{
    [Header("Background Lattice")]
    [SerializeField] private GameObject linePrefab;
    [SerializeField,Range(1,9)] int numRows = 4;
    [SerializeField] float rowSpacingInitial = 0.12f;
    [SerializeField] Vector2 horizEndPoints = new Vector2(-0.648f, 0.561f);
    [SerializeField,Range(1,11)] int numBars = 6;
    [SerializeField] float colSpacingInitial = 0.18f;
    [SerializeField] Vector2 vertEndPoints = new Vector2(-.18f, 0.2f);
    [Header("Style")]
    [Range(0.001f, .05f)] public float lineThickness = 0.002f;
    [Range(0.5f, 2.0f)] public float fontSizeGridLabel = 0.3f;
    [ColorUsage(true, false)]
    [SerializeField] public Color gridColour = Color.gray;
    Vector2 vertExtensions = Vector2.zero;
    Vector2 horizExtensions = Vector2.zero;
    public Vector2Int plane = Vector2Int.zero;
    Vector2 gridSpacing;
    bool needsUpdate = false;
    public Vector2 GridSpacing
    {
        get => gridSpacing;
        set
        {
            needsUpdate = gridSpacing.x != value.x || gridSpacing.y != value.y;
            gridSpacing = value;
            Debug.Log("GridSpacing"+value.x+", "+value.y + "U:" + needsUpdate);
        }
    }
    public float RowSpacing { get => gridSpacing.y;}
    public float BarSpacing { get => gridSpacing.x;}
    public Vector2 HendPoints { get => horizEndPoints; set => horizEndPoints = value; }
    public Vector2 VendPoints { get => vertEndPoints; set => vertEndPoints = value; }
    public float TiltAngle
    {
        get => transform.localRotation.eulerAngles.z;
        set => transform.localRotation = Quaternion.Euler(0, 0, value);
    }
    public void SetTilt(float val)
    {
        TiltAngle = val;
    }

    [SerializeField] float alpha;
    private LineRenderer[] rowLines;
    private LineRenderer[] barLines;
    private void updateGrid()
    {
        Vector3 lineStart = Vector3.zero;
        Vector3 lineEnd = Vector3.zero;
        needsUpdate = false;
        Debug.Log(string.Format("{0}->UpdateGrid->Spacing[{1:3},{2:3}", gameObject.name, gridSpacing.x, gridSpacing.y));
        GameObject goLine;
        if (numRows > 0)
        {
            if ((rowLines == null) || rowLines.Length < numRows)
                rowLines = new LineRenderer[numRows];
            float extentHalf = ((numBars - 1) * BarSpacing)/2f;
            horizEndPoints = new Vector2(-extentHalf, extentHalf) + horizExtensions;
            lineStart.x = horizEndPoints.x;
            lineEnd.x = horizEndPoints.y;
            float rowPos = (gridSpacing.y * (numRows - 1) / 2.0f);
            for (int nRow = 0; nRow < numRows; nRow++)
            {
                if (rowLines[nRow] == null)
                {
                    goLine = Instantiate(linePrefab.gameObject);
                    if (goLine != null)
                    {
                        goLine.transform.parent = transform;
                        goLine.transform.localPosition = Vector3.zero;
                        goLine.transform.localScale = Vector3.one;
                        goLine.transform.localRotation = Quaternion.identity;
                        rowLines[nRow] = goLine.GetComponent<LineRenderer>();
                    }
                }
                lineStart.y = rowPos;
                lineEnd.y = rowPos;
                if (rowLines[nRow] != null)
                {
                    rowLines[nRow].startWidth = lineThickness;
                    rowLines[nRow].endWidth = lineThickness;
                    rowLines[nRow].startColor = gridColour;
                    rowLines[nRow].endColor = gridColour;
                    rowLines[nRow].positionCount = 2;
                    rowLines[nRow].SetPosition(0, lineStart);
                    rowLines[nRow].SetPosition(1, lineEnd);
                }
                rowPos -= gridSpacing.y;
            }
        }
        if (numBars > 0)
        {
            if ((barLines == null) || (barLines.Length < numBars))
                barLines = new LineRenderer[numBars];
            float extentHalf = ((numRows - 1) * RowSpacing)/2f;
            vertEndPoints = new Vector2(-extentHalf, extentHalf) + vertExtensions;
            lineStart.y = vertEndPoints.x;
            lineEnd.y = vertEndPoints.y;
            float barPos = (BarSpacing * (numBars - 1) / 2.0f);
            for (int nBar = 0; nBar < numBars; nBar++)
            {
                if (barLines[nBar] == null)
                {
                    goLine = Instantiate(linePrefab.gameObject, transform);
                    if (goLine != null)
                    {
                        goLine.transform.localScale = Vector3.one;
                        barLines[nBar] = goLine.GetComponent<LineRenderer>();
                    }
                }
                lineStart.x = barPos;
                lineEnd.x = barPos;
                if (barLines[nBar] != null)
                {
                    barLines[nBar].startWidth = lineThickness;
                    barLines[nBar].endWidth = lineThickness;
                    barLines[nBar].startColor = gridColour;
                    barLines[nBar].endColor = gridColour;
                    barLines[nBar].positionCount = 2;
                    barLines[nBar].SetPosition(0, lineStart);
                    barLines[nBar].SetPosition(1, lineEnd);
                }
                barPos -= gridSpacing.x;
            }
        }
    }

    void RefreshColours()
    {
        Color newColor = gridColour;
        newColor.a = alpha;
        bool enable = alpha > 0;
        if (rowLines != null)
        {
            for (int i = 0; i < rowLines.Length; i++)
            {
                if (rowLines[i] != null)
                {
                    rowLines[i].enabled = enable;
                    if (enable)
                    {
                        rowLines[i].startColor = newColor;
                        rowLines[i].endColor = newColor;
                    }
                }
            }
        }
        if (barLines != null)
        {
            for (int i = 0; i < barLines.Length; i++)
            {
                if (barLines[i] != null)
                {
                    barLines[i].enabled = enable;
                    if (enable) 
                    {
                        barLines[i].startColor = newColor;
                        barLines[i].endColor = newColor;
                    }
                }
            }
        }
    }

    public void SetAlpha(float value)
    {
        Alpha = value;
    }
    public float Alpha
    {
        get => alpha; 
        set 
        {
            if (alpha != value)
            {
                alpha = value;
                RefreshColours();
            }
        }
    }
    private void Update()
    {
        if (needsUpdate)
            updateGrid();
    }

    void Start()
    {
        gridSpacing = new Vector2(colSpacingInitial,rowSpacingInitial);
        float halfExtent = ((numRows - 1) * RowSpacing)/2f;
        vertExtensions = new Vector2(vertEndPoints.x + halfExtent, vertEndPoints.y - halfExtent);
        halfExtent = ((numBars - 1) * BarSpacing)/2f;
        horizExtensions = new Vector2(horizEndPoints.x + halfExtent, horizEndPoints.y - halfExtent);
        updateGrid();
        RefreshColours();
    }
}
