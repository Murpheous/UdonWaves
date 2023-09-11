
using Newtonsoft.Json.Linq;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)] // Keeps performance up

public class UdonGrid2D : UdonSharpBehaviour
{
    [Header("Background Lattice")]
    [SerializeField] private GameObject linePrefab;
    [SerializeField] int numRows = 4;
    [SerializeField] float rowSpacingInitial = 0.12f;
    [SerializeField] Vector2 horizEndPoints = new Vector2(-0.648f, 0.561f);
    [SerializeField] int numBars = 6;
    [SerializeField] float colSpacingInitial = 0.18f;
    [SerializeField] Vector2 vertEndPoints = new Vector2(-.18f, 0.2f);
    [Header("Style")]
    [Range(0.001f, .05f)] public float lineThickness = 0.002f;
    [Range(0.5f, 2.0f)] public float fontSizeGridLabel = 0.3f;
    [SerializeField] float labelScale;
    [ColorUsage(true, false)]
    [SerializeField] public Color gridColour = Color.gray;

    //graphicAlpha[] backDrop;

    // Miller Indices of plane 0 - freeform
    public Vector2Int plane = Vector2Int.zero;
    Vector2 gridSpacing;
    bool needsUpdate = false;
    float barSpacing;

    public Vector2 GridSpacing
    {
        get => gridSpacing;
        set
        {
            needsUpdate = gridSpacing != value;
            gridSpacing = value;
        }
    }
    public float RowSpacing { get => gridSpacing.y; set => gridSpacing.y = value; }
    public float BarSpacing { get => barSpacing; set => barSpacing = value; }
    public float BarSpaceAngstroms { get => barSpacing / labelScale; }
    public float RowSpaceAngstroms { get => gridSpacing.y / labelScale; }
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
        Vector3 vStart = Vector3.zero;
        Vector3 vEnd = Vector3.zero;
        needsUpdate = false;

        GameObject goLine = null;
        if (numRows > 0)
        {
            if ((rowLines == null) || rowLines.Length < numRows)
                rowLines = new LineRenderer[numRows];
            vStart.x = horizEndPoints.x;
            vEnd.x = horizEndPoints.y;
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
                vStart.y = rowPos;
                vEnd.y = rowPos;
                if (rowLines[nRow] != null)
                {
                    rowLines[nRow].startWidth = lineThickness;
                    rowLines[nRow].endWidth = lineThickness;
                    rowLines[nRow].startColor = gridColour;
                    rowLines[nRow].endColor = gridColour;
                    rowLines[nRow].positionCount = 2;
                    rowLines[nRow].SetPosition(0, vStart);
                    rowLines[nRow].SetPosition(1, vEnd);
                }
                rowPos -= gridSpacing.y;
            }
        }
        if (numBars > 0)
        {
            if ((barLines == null) || (barLines.Length < numBars))
                barLines = new LineRenderer[numBars];
            vStart.y = vertEndPoints.x;
            vEnd.y = vertEndPoints.y;
            float barPos = (barSpacing * (numBars - 1) / 2.0f);
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
                vStart.x = barPos;
                vEnd.x = barPos;
                if (barLines[nBar] != null)
                {
                    barLines[nBar].startWidth = lineThickness;
                    barLines[nBar].endWidth = lineThickness;
                    barLines[nBar].startColor = gridColour;
                    barLines[nBar].endColor = gridColour;
                    barLines[nBar].positionCount = 2;
                    barLines[nBar].SetPosition(0, vStart);
                    barLines[nBar].SetPosition(1, vEnd);
                }
                barPos -= barSpacing;
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
        RowSpacing = rowSpacingInitial;
        BarSpacing = colSpacingInitial;
        updateGrid();
        RefreshColours();
    }
}
