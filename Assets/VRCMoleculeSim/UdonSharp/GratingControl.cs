
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)] // Keeps performance up

public class GratingControl : UdonSharpBehaviour
{
    [SerializeField]
    Transform gratingXfrm;
    [SerializeField]
    Transform frameSupport;
    public GameObject barPrefab;
    [SerializeField] private UdonBehaviour gratingDisplay;
    [SerializeField] private CameraZoom gratingCamera;
    [SerializeField] string displayZoomVar = "gratingZoomIndex";
    //public GameObject frameSupport;

    public float panelThickness;

    private bool iamOwner;
    //private VRC.Udon.Common.Interfaces.NetworkEventTarget toTheOwner = VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner;
    private VRCPlayerApi player;

    [Header("Dimensions @ native 1:1 scale")]
    [Tooltip("Graphics Metres at Native Scaling 1/x"), SerializeField, UdonSynced, FieldChangeCallback(nameof(NativeGraphicsRatio))]
    private int nativeGraphicsRatio = 10;

    private void updateScales()
    {
        metricScaleFactor = 1.0f / (scaleDownFactor * nativeGraphicsRatio);
        graphicsScaleFactor = experimentScale * metricScaleFactor;
    }
    public int NativeGraphicsRatio
    {
        get => nativeGraphicsRatio > 0 ? nativeGraphicsRatio : 1;
        set
        {
            value = value > 0 ? value : 1;
            if (value != nativeGraphicsRatio)
            {
                nativeGraphicsRatio = value;
                gratingVersionValid = false;
            }
            updateScales();
        }
    }

    [SerializeField] private Vector2 nativeMaxDimensions;

    private float graphicsColWidth = 0;


    [SerializeField]
    private float slitPitchNative = 0.8f;

    [SerializeField]
    private float rowPitchNative = 0.8f;

    private float graphicsColPitch;

    public float SlitWidthMetres { get { return slitPitchNative * slitWidthFrac * metricScaleFactor; } }
    public float SlitHeightMetres { get { return rowHeightFrac * rowPitchNative * metricScaleFactor; } }
    public float RowPitchMetres { get { return rowPitchNative * metricScaleFactor; } }
    public float SlitPitchMetres { get { return slitPitchNative * metricScaleFactor; } }

    [SerializeField,UdonSynced,FieldChangeCallback(nameof(ColumnCount))]
    private int columnCount = 15;

    [SerializeField, Range(0.25f, 1f), FieldChangeCallback(nameof(GratingWidthFrac))]
    private float gratingWidthFrac = 0.8f;


    [SerializeField, Range(0.1f, 0.9f), FieldChangeCallback(nameof(SlitWidthFrac))]
    private float slitWidthFrac = 0.8f;


    [SerializeField, Range(0.25f, 1f), FieldChangeCallback(nameof(GratingHeightFrac))]
    private float gratingHeightFrac = 0.8f;

    [SerializeField, Range(0.1f,0.9f), FieldChangeCallback(nameof(RowHeightFrac))]
    private float rowHeightFrac = 0.8f;

    [SerializeField, UdonSynced, FieldChangeCallback(nameof(RowCount))]
    private int rowCount = 8;

    private int[] scaleSteps = { 1, 5, 10, 50, 100, 500, 1000, 5000};
    [SerializeField,UdonSynced,FieldChangeCallback(nameof(GratingScaleStep))]
    private int gratingScaleStep = 0;

    private int GratingScaleStep
    {
        get => gratingScaleStep;
        set
        {
            value = CheckScaleIndex(value, scaleSteps);
            if (value != gratingScaleStep)
            {
                gratingScaleStep = value;
                updateScales();
                RequestSerialization();

            }
            if (gratingDisplay != null)
                gratingDisplay.SetProgramVariable<int>(displayZoomVar, gratingScaleStep);
            if (gratingCamera != null)
                gratingCamera.Zoom = scaleSteps[gratingScaleStep];
            ScaleDownFactor = scaleSteps[gratingScaleStep];
        }
    }
    private int CheckScaleIndex(int newIndex, int[] steps)
    {
        return Mathf.Clamp(newIndex, 0, steps.Length - 1);
    }
    [Header("Grating Scale Factors")]

    [Tooltip("Spatial Scaling"), FieldChangeCallback(nameof(ExperimentScale))]
    public float experimentScale = 10;
    public float ExperimentScale
    {
        get => experimentScale;
        set
        {
            gratingVersionValid &= experimentScale == value;
            experimentScale = value;
            if (iHaveFrame)
            {
                frameSupport.localScale = Vector3.one * experimentScale/nativeGraphicsRatio;
            }
            updateScales();
        }
    }

    //[SerializeField] 
    private int scaleDownFactor = 1;
    //[SerializeField]
    float metricScaleFactor = 1;
    //[SerializeField]
    float graphicsScaleFactor = 1;
    public int ScaleDownFactor 
    {
        get => scaleDownFactor; 
        set 
        {
            value = Mathf.Max(value, 1);
            if (scaleDownFactor != value || !started)
            {
                scaleDownFactor = value;
                gratingVersionValid = false;
            }
        } 
    }


    [SerializeField, FieldChangeCallback(nameof(ScaleIsChanging))]
    bool scaleIsChanging = true;
    public bool ScaleIsChanging
    {
        get => scaleIsChanging;
        set
        {
            if (scaleIsChanging != value)
            {
                scaleIsChanging = value;
            }
            if (!value)
                gratingVersionValid = false;
        }
    }

    [Header("Editor Debug")]
    bool iHaveFrame = false;
    [SerializeField]
    Vector2 maxDimsReduced;
    //[SerializeField]
    private float graphicsRowHeight;
    //[SerializeField]
    private float graphicsRowPitch;
    //[SerializeField]
    private Vector2 activeGratingSize = Vector2.one;
    private Vector2 activeGratingHalf = Vector2.one;
    //[SerializeField,Tooltip("Grating Border Size")]
    private Vector2 gratingBorderSize = Vector2.one;

    //[SerializeField]
    private float barWidth;
    //[SerializeField]
    private float railHeight;
    //[SerializeField]
    private float sideBarWidth;
    //[SerializeField]
    private float upperLowerHeight;

    public Vector2 ActiveGratingSize
    {
        get => activeGratingSize;
    }

    // Grating properties
    private GameObject[] theBars;
    private GameObject[] theRails;
    private GameObject panelLeft;
    private GameObject panelRight;
    private GameObject panelTop;
    private GameObject panelBottom;

    [Header("UI Elements")]
    [SerializeField] SyncedSlider gratingWidthSlider;
    [SerializeField] SyncedSlider slitWidthSlider;
    [SerializeField] SyncedSlider gratingHeightSlider;
    [SerializeField] SyncedSlider rowHeightSlider;

    [SerializeField] TextMeshProUGUI labelSlits;
    [SerializeField] TextMeshProUGUI labelRows;
    [SerializeField] TextMeshProUGUI labelGratingScale;
    [SerializeField] TextMeshProUGUI gratingDescription;

    //[Header("Constants"), SerializeField]
    const int MAX_SLITS = 16;
    //[SerializeField]
    const int MAX_ROWS = 16;

    bool gratingVersionValid;
    private void UpdateOwnerShip()
    {
        iamOwner = Networking.IsOwner(this.gameObject);
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        UpdateOwnerShip();
    }

    void setText(TextMeshProUGUI tmproLabel, string text)
    {
        if (tmproLabel != null)
            tmproLabel.text = text;
    }

    private void UpdateLabels()
    {
        setText(labelGratingScale,"Grating\nScale\n1:" +scaleDownFactor.ToString());
        if (gratingDescription == null)
            return;
        string gratingdesc = 
                string.Format("<b>Slit:</b>\n<indent=5%>w={0}m h={1}m</indent>\n<b>Spacing:</b>\n<indent=5%>x={2}m y={3}m\n</indent>", 
                Units.ToEngineeringNotation(SlitWidthMetres), Units.ToEngineeringNotation(SlitHeightMetres),
                Units.ToEngineeringNotation(SlitPitchMetres), Units.ToEngineeringNotation(RowPitchMetres));
        gratingDescription.text = gratingdesc;
    }

    private void UpdateSlitPitch()
    {
        slitPitchNative = nativeMaxDimensions.x * gratingWidthFrac / Mathf.Max(1,columnCount);
    }

    private void UpdateRowPitch()
    {
        rowPitchNative = nativeMaxDimensions.y * gratingHeightFrac / Mathf.Max(rowCount,1);
    }
    public float GratingWidthFrac
    {
        get => gratingWidthFrac; 
        set
        {
            value = Mathf.Clamp(value,0.25f,1f);
            gratingVersionValid &= value == gratingWidthFrac;
            gratingWidthFrac = value;
            UpdateSlitPitch();
        }
    }

    private float GratingHeightFrac
    {
        get => gratingHeightFrac;
        set
        {
            value = Mathf.Clamp(value, 0.25f, 1f);
            gratingVersionValid &= value == gratingHeightFrac;
            gratingHeightFrac = value;
            UpdateRowPitch();
        }
    }

    private float SlitWidthFrac
    {
        get => slitWidthFrac;
        set
        {
            gratingVersionValid &=  slitWidthFrac == value;
            slitWidthFrac = value;
        }
    }

    public float RowHeightFrac
    {
        get => rowHeightFrac;
        private set
        {
            gratingVersionValid &= rowHeightFrac == value;
            rowHeightFrac = value;
        }
    }

    public int ColumnCount
    {
        get => columnCount;
        set
        {
            if (value < 1)
                value = 1;
            else if (value > MAX_SLITS)
                value = MAX_SLITS;
            colsOdd = (value & 1) == 1;
            if (value != columnCount)
            {
                columnCount = value;
                gratingVersionValid = false;
                RequestSerialization();
            }
            UpdateSlitPitch();
            setText(labelSlits, "Slits\n" + columnCount.ToString());
        }
    }

    public int RowCount
    {
        get => rowCount;
        set
        {
            if (value < 0)
                value = 0;
            else if (value > MAX_ROWS)
                value = MAX_ROWS;
            rowsOdd = (value & 1) == 1;
            if (value != rowCount)
            {
                rowCount = value;
                gratingVersionValid = false;
                RequestSerialization();
            }
            UpdateRowPitch();
            setText(labelRows, "Rows\n" + rowCount.ToString());
        }
    }
    public void scaleDown()
    {
        if (gratingScaleStep <= 0)
            return;
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        GratingScaleStep = gratingScaleStep - 1;
    }
    public void scaleUp()
    {
        if (gratingScaleStep >= scaleSteps.Length-1)
            return;
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        GratingScaleStep = gratingScaleStep + 1;
    }
    public void onSlitsPlus()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        if (ColumnCount < MAX_SLITS)
            ColumnCount = columnCount + 1;
    }

    public void onSlitsMinus()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        ColumnCount = columnCount - 1;
    }

    public void OnRowsPlus()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        if (RowCount < MAX_ROWS)
            RowCount = rowCount + 1;
    }

    public void OnRowsMinus()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        RowCount = rowCount - 1;
    }

    private void setCollisionFilter()
    {
        if (graphicsColPitch > 0)
        {
            if (colsOdd)
            {
                minColFrac = graphicsColWidth / (graphicsColPitch * 2.0f);
                maxColFrac = 1.0f - minColFrac;
            }
            else
            {
                minColFrac = (1.0f - (graphicsColWidth / graphicsColPitch)) / 2.0f;
                maxColFrac = 1.0f - minColFrac;
            }
        }
        else
        {
            minColFrac = 0;
            maxColFrac = 0;
        }

        if (graphicsRowPitch > 0)
        {
            if (rowsOdd)
            {
                minRowFrac = graphicsRowHeight / (graphicsRowPitch * 2.0f);
                maxRowFrac = 1.0f - minRowFrac;
            }
            else
            {
                minRowFrac = (1.0f - (graphicsRowHeight / graphicsRowPitch)) / 2.0f;
                maxRowFrac = 1.0f - minRowFrac;
            }
        }
        else
        {
            minRowFrac = 0;
            maxRowFrac = 0;
        }
    }

    public bool checkBorderCollide(Vector3 particlePosition)
    {
        if (Mathf.Abs(particlePosition.y) > activeGratingHalf.y)
            return true;
        if (Mathf.Abs(particlePosition.z) > activeGratingHalf.x)
            return true;
        return false;
    }

    private bool colsOdd;
    private bool rowsOdd;
    private float minColFrac;
    private float maxColFrac;
    private float minRowFrac;
    private float maxRowFrac;
    public bool checkLatticeCollision(Vector3 particlePosition)
    {
        if (!gratingVersionValid)
            return false;
        // First calculate
        float verticalDelta = Mathf.Abs(particlePosition.y);
        if (verticalDelta > activeGratingHalf.y)
            return true;
        float horizDelta = Mathf.Abs(particlePosition.z);
        // First check if particle is outside grating overall aperture, if so return true.		
        if (horizDelta > activeGratingHalf.x)
            return true;
        // now look to see if it hits a horizontal bar
        if (rowCount > 0)
        {
            //Debug.Log("checkLatticeCollision:"+verticalDelta);
            verticalDelta /= graphicsRowPitch;
            verticalDelta -= (int)verticalDelta;
            if (!rowsOdd)
            {
                if ((verticalDelta < minRowFrac) || (verticalDelta > maxRowFrac))
                    return true;
            }
            else
            {
                if ((verticalDelta > minRowFrac) && (verticalDelta < maxRowFrac))
                    return true;
            }
        }
        if (columnCount > 0)
        {
            horizDelta /= graphicsColPitch;
            horizDelta -= (int)horizDelta;
            if (!colsOdd)
            {
                if ((horizDelta < minColFrac) || (horizDelta > maxColFrac))
                    return true;
            }
            else
            {
                if ((horizDelta > minColFrac) && (horizDelta < maxColFrac))
                    return true;
            }
        }
        return false;
    }

    GameObject LocalFromPrefab(GameObject prototype, string name, Transform xfrm)
    {
        GameObject result = Instantiate(prototype);
        if (result == null)
        {
            return result;
        }
        result.transform.SetParent(xfrm);
        result.name = name;
        result.transform.localPosition = Vector3.zero;
        result.transform.localRotation = Quaternion.identity;
        result.transform.localScale = Vector3.one;
        return result;
    }

    void SetBarSizeAndPosition(Transform bar, float targetWidth, float targetHeight, Vector2 targetPos, bool isVisible)// , bool barCollides = true)
    {
        if (bar == null)
            return;
        bar.localScale = new Vector3(panelThickness, targetHeight, targetWidth);
        bar.localPosition = new Vector3(0, targetPos.y, targetPos.x);
        bar.gameObject.SetActive(isVisible);
    }

    private int gratingVersion = -1;
    public int GratingVersion 
    { 
        get=> gratingVersion;
        set 
        {
            gratingVersion = value;
        }
    }
    void setupLattice()
    {
        if (nativeGraphicsRatio <= 0)
            return;
        // Set dimensons for the construction of the lattice;
        updateScales();
        maxDimsReduced = nativeMaxDimensions * graphicsScaleFactor;

        graphicsRowPitch = rowPitchNative * graphicsScaleFactor;
        graphicsColPitch = slitPitchNative * graphicsScaleFactor;

        graphicsColWidth = graphicsColPitch * slitWidthFrac;
        graphicsRowHeight = graphicsRowPitch * rowHeightFrac;
        //
        barWidth = graphicsColPitch > graphicsColWidth ? graphicsColPitch - graphicsColWidth : 0;
        railHeight = graphicsRowPitch > graphicsRowHeight ? graphicsRowPitch - graphicsRowHeight : 0;

        activeGratingSize.x = graphicsColPitch * columnCount;
        activeGratingSize.y = rowCount < 1 ? maxDimsReduced.y : graphicsRowPitch * rowCount;
        gratingBorderSize.x = Mathf.Max(maxDimsReduced.x, activeGratingSize.x+barWidth);
        gratingBorderSize.y = Mathf.Max(maxDimsReduced.y, activeGratingSize.y+railHeight);
        activeGratingHalf = activeGratingSize * 0.5f;

        sideBarWidth = (gratingBorderSize.x - activeGratingSize.x) / 2.0f;
        upperLowerHeight = (gratingBorderSize.y - activeGratingSize.y) / 2.0f;

        // Cache the lattice collision parameters
        setCollisionFilter();

        // Calculate positions of side and top panels
        Vector2 sidePanelPos = new Vector2 (activeGratingHalf.x + (sideBarWidth / 2.0f),0);
        Vector2 topPanelPos = new Vector2 (0,activeGratingHalf.y + (upperLowerHeight / 2.0f));

        if (barPrefab != null)
        {
            // Set Scales for the Slit and Block Prefabs
            if (sideBarWidth > 0)
            {
                SetBarSizeAndPosition(panelLeft.transform, sideBarWidth, gratingBorderSize.y, sidePanelPos, true); //,false);
                SetBarSizeAndPosition(panelRight.transform, sideBarWidth, gratingBorderSize.y, -sidePanelPos, true); //,false);
                panelLeft.SetActive(true);
                panelRight.SetActive(true);
            }
            else
            {
                panelLeft.SetActive(false);
                panelRight.SetActive(false);
            }
            if (rowCount >= 1 && upperLowerHeight > 0)
            {
                SetBarSizeAndPosition(panelTop.transform, gratingBorderSize.x, upperLowerHeight, topPanelPos, true);
                SetBarSizeAndPosition(panelBottom.transform, gratingBorderSize.x,upperLowerHeight, -topPanelPos, true);
                panelTop.SetActive(true);
                panelBottom.SetActive(true);
            }
            else
            {
               panelTop.SetActive(false);
               panelBottom.SetActive(false);
            }
            // Set scale and then position of the first spacer
            Vector2 studPos = new Vector2 (activeGratingHalf.x, 0);
            for (int nSlit = 0; nSlit < theBars.Length; nSlit++)
            {
                bool visible = (columnCount > 0) && (nSlit < columnCount + 1);
                SetBarSizeAndPosition(theBars[nSlit].transform, barWidth, activeGratingSize.y+railHeight, studPos, visible);
                if (visible)
                    studPos.x -= graphicsColPitch;
            }
            Vector2 railPos = new Vector2(0, activeGratingHalf.y);
            for (int nRail = 0; nRail < theRails.Length; nRail++)
            {
                bool visible = (rowCount > 0) && (nRail < rowCount+1);
                SetBarSizeAndPosition(theRails[nRail].transform, activeGratingSize.x, railHeight, railPos, visible);
                if (visible)
                    railPos.y -= graphicsRowPitch;
            }
        }
        gratingVersionValid = true;
        GratingVersion = gratingVersion + 1;
    }

    void populateBars()
    {
        if (theBars == null)
            theBars = new GameObject[MAX_SLITS + 1];
        if (theRails == null)
            theRails = new GameObject[MAX_ROWS + 1];
        for (int i = 0; i < theBars.Length; i++)
        {
            if (theBars[i] == null)
            {
                theBars[i] = LocalFromPrefab(barPrefab, "Bar_" + (i + 1).ToString(), gratingXfrm);
                theBars[i].transform.position = new Vector3(0, 0, 0);
                theBars[i].SetActive(false);
            }
        }
        for (int i = 0; i < theRails.Length; i++)
        {
            if (theRails[i] == null)
            {
                theRails[i] = LocalFromPrefab(barPrefab, "Rail_" + (i + 1).ToString(), gratingXfrm);
                theRails[i].transform.position = new Vector3(0, 0, 0);
                theRails[i].SetActive(false);
            }
        }
        if (panelTop == null)
            panelTop = LocalFromPrefab(barPrefab, "TopPanel", gratingXfrm);
        if (panelBottom == null)
            panelBottom = LocalFromPrefab(barPrefab, "BottomPanel", gratingXfrm);
        if (panelLeft == null)
            panelLeft = LocalFromPrefab(barPrefab, "LeftPanel", gratingXfrm);
        if (panelRight == null)
            panelRight = LocalFromPrefab(barPrefab, "RightPanel", gratingXfrm);
    }


    private bool started = false;
    private bool iHaveWidthSlider = false;
    private bool iHaveSlitWidthCtl = false;
    private bool iHaveHeightSlider = false;
    private bool iHaveRowHeightCtl = false;
    public bool Started { get =>  started;}
    void Start()
    {
        iHaveFrame = frameSupport != null;
        if (gratingXfrm == null)
            gratingXfrm = transform;
        player = Networking.LocalPlayer;
        iHaveWidthSlider = gratingWidthSlider != null;
        iHaveSlitWidthCtl = slitWidthSlider != null;
        iHaveHeightSlider = gratingHeightSlider != null;
        iHaveRowHeightCtl = rowHeightSlider != null;
        NativeGraphicsRatio = nativeGraphicsRatio;
        GratingScaleStep = gratingScaleStep;
        ColumnCount = Mathf.Clamp(columnCount, 1, MAX_SLITS);
        RowCount = Mathf.Clamp(rowCount, 0, MAX_ROWS);
        gratingVersionValid = false;
        SlitWidthFrac = slitWidthFrac;
        if (iHaveSlitWidthCtl)
            slitWidthSlider.SetValues(slitWidthFrac,0.1f, 0.9f);
        if (iHaveWidthSlider)
            gratingWidthSlider.SetValues(gratingWidthFrac,.25f, 1f);
        RowHeightFrac = rowHeightFrac;
        if (iHaveRowHeightCtl)
            rowHeightSlider.SetValues(rowHeightFrac, 0.1f, 0.9f);
        if (iHaveHeightSlider)
            gratingHeightSlider.SetValues(gratingHeightFrac, .25f, 1f);
        if (panelThickness <= 0)
            panelThickness = 0.001f;
        populateBars();
        UpdateOwnerShip();
        started = true;

    }
    float pollTick = 0.5f;
    void Update()
    {
        pollTick -= Time.deltaTime;
        if (pollTick > 0)
            return;
        pollTick += 0.1f;
        if (!started)
            return;
        if (!gratingVersionValid)
        {
           // Debug.Log("gratingVersion:"+gratingVersion);
            gratingVersionValid = true;
            setupLattice();
            UpdateLabels();
        }
    }
}
