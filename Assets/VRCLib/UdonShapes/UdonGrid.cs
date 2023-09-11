
using System.Linq;
using UdonSharp;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)] // Keeps performance up

public class UdonGrid : UdonSharpBehaviour
{
    [Header("Prefabs")]
    [Tooltip("Line Prototype")]
    [SerializeField]
    GameObject linePrefab;
    public bool showGrid = true;
    [SerializeField,Tooltip("Points per quadrant")]
    private Vector2Int numPoints = Vector2Int.one;
    [SerializeField]
    Vector2 gridSpacing = Vector2.one;

    [Header("Styles")]
    [SerializeField, Range(0f, 0.1f)] private float lineThickness = 0.02f;
    [ColorUsage(true, true)]
    [SerializeField] private Color lineColor = Color.black;
    [SerializeField]
    LineRenderer[] rowLines = null;
    [SerializeField]
    LineRenderer[] colLines = null;
    bool gridSpawned = false;
    bool gridDrawn = false;
    void SpawnGrid()
    {
        if (linePrefab == null)
        {
            showGrid = false;
            Debug.Log("Grid: no Line Prefab");
            return;
        }
        int numCols = numPoints.x * 2 + 1;
        int numRows = numPoints.y * 2 + 1;
        if (rowLines == null)
            rowLines = new LineRenderer[numRows];
        bool bnew = (colLines == null) || (colLines.Length < numCols);
        if (bnew )
            colLines = new LineRenderer[numCols];
        GameObject go;
        LineRenderer lr;
        for (int i = 0; i < rowLines.Length; i++)
        {
            lr = null;
            if (rowLines[i] == null)
            {
                go = Instantiate(linePrefab);
                if (go != null)
                    lr = go.GetComponent<LineRenderer>();
                if (lr != null)
                    rowLines[i] = lr;
            }
            else
            {
                go = rowLines[i].gameObject;
                lr = rowLines[i];
            }
            if (go != null)
            {
                go.transform.parent = transform;
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one;
                go.transform.localRotation = Quaternion.identity;
            }

            if (lr != null)
            {
                lr.startWidth = lineThickness;
                lr.endWidth = lineThickness;
                lr.startColor = lineColor;
                lr.endColor = lineColor;
            }
        }
        for (int i = 0; i < colLines.Length; i++)
        {
            lr = null;
            if (colLines[i] == null)
            {
                go = Instantiate(linePrefab);
                if (go != null)
                    lr = go.GetComponent<LineRenderer>();
                if (lr != null)
                    colLines[i] = lr;
            }
            else
            {
                lr = colLines[i];
                go = colLines[i].gameObject;
            }
            if (go != null)
            {
                go.transform.parent = transform;
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one;
                go.transform.localRotation = Quaternion.identity;
            }
            if (lr != null)
            {
                lr.startWidth = lineThickness;
                lr.endWidth = lineThickness;
                lr.startColor = lineColor;
                lr.endColor = lineColor;
            }
        }
        Debug.Log("Grid Spawned");
        gridSpawned = true;
    }

    void UpdateGrid()
    {
        if (rowLines == null)
            return;
        Debug.Log("Grid Update");
        Vector3 rowCol = new Vector3(numPoints.x * gridSpacing.x, numPoints.y * gridSpacing.y, 0);
        Vector3 lineEnd;
        int numRows = rowLines.Length;
        int numCols = colLines.Length;
        LineRenderer lr;
        for (int nRow = 0; nRow < numRows; nRow++)
        {
            rowCol.x = numPoints.x * gridSpacing.x;
            lineEnd = new Vector3(rowCol.x - ((numCols - 1) * gridSpacing.x), rowCol.y, 0);
            lr = rowLines[nRow];
            if (lr != null)
            {
                lr.positionCount = 2;
                lr.SetPosition(0, rowCol);
                lr.SetPosition(1, lineEnd);
            }
            for (int nCol = 0; nCol < numCols; nCol++)
            {
                if (nRow == 0)
                {
                    lineEnd = new Vector3(rowCol.x, rowCol.y - ((numRows - 1) * gridSpacing.y), 0);
                    lr = colLines[nCol];
                    if (lr != null)
                    {
                        lr.positionCount = 2;
                        lr.SetPosition(0, rowCol);
                        lr.SetPosition(1, lineEnd);
                    }
                }
                rowCol.x -= gridSpacing.x;
            }
            rowCol.y -= gridSpacing.y;
        }
        gridDrawn = true;
        Debug.Log("Grid Drawn");

    }

    float polltime=5;
    private void Update()
    {
        polltime -= Time.deltaTime;
        if (polltime > 0)
            return;
        polltime += 1;
        if (!showGrid) 
            return;
        if (!gridSpawned)
            SpawnGrid();
        if (!gridDrawn)
            UpdateGrid();
    }

    void Start()
    {
    }
}
