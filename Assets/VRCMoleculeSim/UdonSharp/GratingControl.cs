
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)] // Keeps performance up

public class GratingControl : UdonSharpBehaviour
{
    [SerializeField]
    Transform gratingXfrm;
    public GameObject barPrefab;
    public GameObject frameSupport;
    [SerializeField,Range(0.0001f,0.01f)] float increment = 0.001f;
    public float panelThickness;
    [SerializeField,UdonSynced,FieldChangeCallback(nameof(BarsCollide))]
    bool barsCollide = false;
    [SerializeField,UdonSynced, FieldChangeCallback(nameof(FrameCollides))]
    bool frameCollides = false;

    public bool BarsCollide {  
        get => barsCollide; 
        set
        {
            if (value != barsCollide)
            {
                barsCollide = value;
                gratingVersionIsCurrent = false;
            }
            if (togBarsCollide != null)
            {
                if (value != togBarsCollide.isOn)
                    togBarsCollide.isOn = value;
            }
            RequestSerialization();
        }
    }

    public bool FrameCollides
    {
        get => frameCollides;
        set
        {
            if (value != frameCollides)
            {
                frameCollides = value;
                gratingVersionIsCurrent = false;
            }
            if (togFrameCollides != null)
            {
                if (value != togFrameCollides.isOn)
                    togFrameCollides.isOn = value;
            }
            RequestSerialization();
        }
    }

    private bool iamOwner;
    //private VRC.Udon.Common.Interfaces.NetworkEventTarget toTheOwner = VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner;
    private VRCPlayerApi player;

    [Header("Dimensions @ native 1:1 scale")]
    [Tooltip("Graphics Metres at Native Scaling 1/x"), SerializeField, UdonSynced, FieldChangeCallback(nameof(NativeGraphicsRatio))]
    private int nativeGraphicsRatio = 10;
    public int NativeGraphicsRatio
    {
        get => nativeGraphicsRatio > 0 ? nativeGraphicsRatio : 1;
        set
        {
            value = value > 0 ? value : 1;
            if (value != nativeGraphicsRatio)
            {
                nativeGraphicsRatio = value;
                gratingVersionIsCurrent = false;
                metricScaleFactor = 1.0f / (scaleDownFactor * nativeGraphicsRatio);
                outerDimsMetres = nativeMaxDimensions / nativeGraphicsRatio;
            }
        }
    }

    [SerializeField] private Vector2 nativeMaxDimensions;


    [SerializeField, UdonSynced, FieldChangeCallback(nameof(HoleWidthNative))]
    private float holeWidthNative = 0.008f;
    private float graphicsColWidth = 0;

    public float ApertureWidthMetres { get { return holeWidthNative * metricScaleFactor; } }

    [SerializeField, UdonSynced, FieldChangeCallback(nameof(ColumnPitchNative))]
    private float columnPitchNative = 0.01f;
    private float graphicsColPitch;
    public float ColumnPitchMetres { get { return columnPitchNative * metricScaleFactor; } }

    [SerializeField,UdonSynced,FieldChangeCallback(nameof(ColumnCount))]
    private int columnCount = 15;

    [SerializeField, UdonSynced, FieldChangeCallback(nameof(HoleHeightNative))]
    private float holeHeightNative = 0.0575f;
    public float ApertureHeightMetres { get { return holeHeightNative * metricScaleFactor; } }
    
    private float graphicsColHeight;
    
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(RowPitchNative))]
    private float rowPitchNative = 0.06f;
    //[SerializeField]
    private float graphicsRowPitch;
    public float RowPitchMetres { get { return rowPitchNative * metricScaleFactor; } }

    [SerializeField, UdonSynced, FieldChangeCallback(nameof(RowCount))]
    private int rowCount = 8;


    private int[] scaleSteps = { 1, 5, 10, 50, 100, 500, 1000};
    [SerializeField,UdonSynced,FieldChangeCallback(nameof(GratingScaleStep))]
    private int gratingScaleStep = 0;

    private int GratingScaleStep
    {
        get => gratingScaleStep;
        set
        {
            gratingScaleStep = CheckScaleIndex(value, scaleSteps);
            ScaleDownFactor = scaleSteps[GratingScaleStep];
            RequestSerialization();
        }
    }
    private int CheckScaleIndex(int newIndex, int[] steps)
    {
        return Mathf.Clamp(newIndex, 0, steps.Length - 1);
    }
    [Header("Grating Scale Factors")]

    [SerializeField] 
    private int scaleDownFactor = 1;
    [SerializeField]
    float metricScaleFactor = 1;
    [SerializeField]
    float graphicsScaleFactor = 1;
    public int ScaleDownFactor 
    {
        get => scaleDownFactor; 
        set 
        {
            value = Mathf.Clamp(value, 1, 1000);
            if (scaleDownFactor != value || !started)
            {
                scaleDownFactor = value;
                gratingVersionIsCurrent = false;
            }
        } 
    }


    [SerializeField, UdonSynced, FieldChangeCallback(nameof(ScaleIsChanging))]
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
                gratingVersionIsCurrent = false;
        }
    }

    [Tooltip("Spatial Scaling"), UdonSynced, FieldChangeCallback(nameof(ExperimentScale))]
    public float experimentScale = 10;
    public float ExperimentScale 
    { 
        get => experimentScale; 
        set 
        {
            if (experimentScale != value)
            {
                experimentScale = value;
                gratingVersionIsCurrent = false;
            }
        } 
    }
    [Header("Dimension Debug")]
    [SerializeField, Tooltip("Outer Frame at Native Res")] Vector2 outerDimsMetres;
    [SerializeField, Tooltip("Outer Frame as seen in Scaled World")] Vector2 maxDimsReduced;

    //[SerializeField] Vector2 outerDimsMetres;
    
    [SerializeField]
    private Vector2 gratingGraphicsSize = Vector2.one;
    private Vector2 gratingGraphicHalfSize = Vector2.one;
    [SerializeField,Tooltip("Grating Border Size")]
    private Vector2 gratingBorderSize = Vector2.one;

    [SerializeField]
    private Vector2 pitchSteps = Vector2.one;
    [SerializeField]
    private Vector2 sizeSteps = Vector2.one;
    [SerializeField]
    private float barWidth;
    [SerializeField]
    private float railHeight;
    [SerializeField]
    private float sideBarWidth;
    [SerializeField]
    private float upperLowerHeight;

    public Vector2 GratingGraphicsSize
    {
        get => gratingGraphicsSize;
    }

    // Grating properties
    private GameObject[] theBars;
    private GameObject[] theRails;
    private GameObject panelLeft;
    private GameObject panelRight;
    private GameObject panelTop;
    private GameObject panelBottom;

    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI labelSlits;
    [SerializeField] TextMeshProUGUI labelSlitPitch;
    [SerializeField] TextMeshProUGUI labelSlitWidth;
    [SerializeField] TextMeshProUGUI labelRows;
    [SerializeField] TextMeshProUGUI labelRowPitch;
    [SerializeField] TextMeshProUGUI labelRowHeight;
    [SerializeField] TextMeshProUGUI labelGratingScale;
    [SerializeField] Toggle togBarsCollide;
    [SerializeField] Toggle togFrameCollides;

    [Header("Constants"), SerializeField]
    private int MAX_SLITS = 16;
    [SerializeField]
    private int MAX_ROWS = 20;
    //private QuantumScatter particleScatter;
    bool gratingVersionIsCurrent;
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
        setText(labelSlits, "Slits\n" + columnCount.ToString());
        setText(labelSlitPitch, "Spacing\n" + Units.ToEngineeringNotation(ColumnPitchMetres) + "m");
        setText(labelSlitWidth, "Slit Width\n" + Units.ToEngineeringNotation(ApertureWidthMetres) + "m");
        setText(labelRows, "Rows\n" + rowCount.ToString());
        setText(labelRowPitch, "Row Spacing\n" + Units.ToEngineeringNotation(RowPitchMetres    ) + "m");
        setText(labelRowHeight, "Row Height\n" + Units.ToEngineeringNotation(ApertureHeightMetres) + "m");
        setText(labelGratingScale,"Grow/Shrink\n1:" +scaleDownFactor.ToString());
    }
    public float ColumnPitchNative
    {
        get => columnPitchNative;
        private set
        {
            if (checkGratingWidth(holeWidthNative, value, columnCount))
            {
                columnPitchNative = value;
                gratingVersionIsCurrent = false;
                RequestSerialization();
            }
        }
    }

    public float RowPitchNative
    {
        get => rowPitchNative;
        private set
        {
            if (checkGratingHeight(holeHeightNative, value, rowCount))
            {
                rowPitchNative = value;
                gratingVersionIsCurrent = false;
                RequestSerialization();
            }
        }
    }

    public float HoleWidthNative
    {
        get => holeWidthNative;
        private set
        {
            if (checkGratingWidth(value, columnPitchNative, columnCount) && value >= sizeSteps.x)
            {
                holeWidthNative = value;
                gratingVersionIsCurrent = false;
                RequestSerialization();
            }
        }
    }

    public float HoleHeightNative
    {
        get => holeHeightNative;
        private set
        {
            if (checkGratingHeight(value, rowPitchNative, rowCount) && value >= sizeSteps.y)
            {
                holeHeightNative = value;
                gratingVersionIsCurrent = false;
                RequestSerialization();
            }
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
                gratingVersionIsCurrent = false;
                RequestSerialization();
            }
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
                gratingVersionIsCurrent = false;
                RequestSerialization();
            }
        }
    }

    public void OnBarsCollide()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);

        if (togBarsCollide != null)
        {
            BarsCollide = togBarsCollide.isOn;
            Debug.Log("BarCollide Set:"+BarsCollide.ToString());
            gratingVersionIsCurrent = false;
        }
        else
            BarsCollide = !barsCollide;
    }
    public void OnGratingScaleDown()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        GratingScaleStep = gratingScaleStep - 1;
    }
    public void OnGratingScaleUp()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        GratingScaleStep = gratingScaleStep + 1;
    }
    public void OnAperturesPlus()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        if ((ColumnCount < MAX_SLITS) && checkGratingWidth(HoleWidthNative, ColumnPitchNative, ColumnCount + 1))
            ColumnCount = columnCount + 1;
    }

    public void OnAperturesMinus()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        ColumnCount = columnCount - 1;
    }
    public void OnWidthPlus()
    {

        if (!iamOwner)
        {
            Networking.SetOwner(player, gameObject);
        }
        float testVal = HoleWidthNative + sizeSteps.x;
        if (testVal >= ColumnPitchNative)
            return;
        if (checkGratingWidth(testVal, ColumnPitchNative, ColumnCount))
            HoleWidthNative = testVal;
    }
    public void OnWidthMinus()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);

        float testVal = HoleWidthNative - sizeSteps.x;
        if (testVal <= sizeSteps.x)
            return;
        if (checkGratingWidth(testVal, ColumnPitchNative, ColumnCount))
            HoleWidthNative = testVal;
    }
    public void OnPitchPlus()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        float testVal = ColumnPitchNative + pitchSteps.x;
        if (checkGratingWidth(HoleWidthNative, testVal, ColumnCount))
            ColumnPitchNative = testVal;
    }
    public void OnPitchMinus()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        float testVal = ColumnPitchNative - pitchSteps.x;
        if (testVal <= HoleWidthNative) 
            return;
        if (checkGratingWidth(HoleWidthNative, testVal, ColumnCount))
            ColumnPitchNative = testVal;
    }

    public void OnRowsPlus()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        if ((RowCount < MAX_ROWS) && checkGratingHeight(HoleHeightNative, RowPitchNative, RowCount + 1))
            RowCount = rowCount + 1;
    }

    public void OnRowsMinus()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        RowCount = rowCount - 1;
    }
    public void OnHeightPlus()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        float testVal = HoleHeightNative + sizeSteps.y;
        if (testVal >= RowPitchNative) 
            return;
        if (checkGratingHeight(testVal, RowPitchNative, RowCount))
            HoleHeightNative = testVal;
    }
    public void OnHeightMinus()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        float testVal = HoleHeightNative - sizeSteps.y;
        if (testVal <= sizeSteps.y)
            return;
        if (checkGratingHeight(testVal, RowPitchNative, RowCount))
            HoleHeightNative = testVal;
    }
    public void OnRowPitchPlus()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);

        float testVal = RowPitchNative + pitchSteps.y;
        if (checkGratingHeight(HoleHeightNative, testVal, RowCount))
            RowPitchNative = testVal;
    }
    public void OnRowPitchMinus()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        float testVal = RowPitchNative - pitchSteps.y;
        if (testVal <= HoleHeightNative)
            return;
        if (checkGratingHeight(HoleHeightNative, testVal, RowCount))
            RowPitchNative = testVal;
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
                minRowFrac = graphicsColHeight / (graphicsRowPitch * 2.0f);
                maxRowFrac = 1.0f - minRowFrac;
            }
            else
            {
                minRowFrac = (1.0f - (graphicsColHeight / graphicsRowPitch)) / 2.0f;
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
        if (Mathf.Abs(particlePosition.y) > gratingGraphicHalfSize.y)
            return true;
        if (Mathf.Abs(particlePosition.z) > gratingGraphicHalfSize.x)
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
        if (!gratingVersionIsCurrent)
            return false;
        // First calculate
        float verticalDelta = Mathf.Abs(particlePosition.y);
        if (verticalDelta > gratingGraphicHalfSize.y)
            return true;
        float horizDelta = Mathf.Abs(particlePosition.z);
        // First check if particle is outside grating overall aperture, if so return true.		
        if (horizDelta > gratingGraphicHalfSize.x)
            return true;
        if (!BarsCollide)
            return false;
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

    void SetBarSizeAndPosition(Transform bar, float targetWidth, float targetHeight, Vector2 targetPos, bool isVisible , bool barCollides = true)
    {
        if (bar == null)
            return;
        bar.localScale = new Vector3(panelThickness, targetHeight, targetWidth);
        bar.localPosition = new Vector3(0, targetPos.y, targetPos.x);
        bar.gameObject.SetActive(isVisible);
        Collider col = bar.GetComponent<Collider>();
        if (col != null)
            col.enabled = barCollides && isVisible;
    }

    bool checkGratingWidth(float holeWidthNative, float columnPitchNative, int numGaps)
    {
        if (numGaps <= 0)
            return true;
        if (numGaps == 1)
            return holeWidthNative <= nativeMaxDimensions.x;
        return nativeMaxDimensions.x >= (((numGaps - 1) * columnPitchNative) + holeWidthNative);
    }

    bool checkGratingHeight(float holeHeightNative, float rowPitchNative, int rowCount)
    {
        if (rowCount <= 0)
            return true;
        if (rowCount == 1)
            return holeHeightNative <= nativeMaxDimensions.y;
        return nativeMaxDimensions.y >= (((rowCount - 1) * rowPitchNative) + holeHeightNative);
    }

    [SerializeField]
    private int gratingVersion = -1;
    public int GratingVersion 
    { 
        get=> gratingVersion;
        set 
        {
            gratingVersion = value;
           // RequestSerialization();
        }
    }
    void setupLattice()
    {
        if (nativeGraphicsRatio <= 0)
            return;
        metricScaleFactor = 1.0f / (scaleDownFactor * nativeGraphicsRatio);
        outerDimsMetres = nativeMaxDimensions / nativeGraphicsRatio;
        graphicsScaleFactor = experimentScale * metricScaleFactor;
        // Set dimensons for the construction of the lattice;
        maxDimsReduced = nativeMaxDimensions * graphicsScaleFactor;
        graphicsRowPitch = rowPitchNative * graphicsScaleFactor;
        graphicsColPitch = columnPitchNative * graphicsScaleFactor;
        graphicsColWidth = holeWidthNative * graphicsScaleFactor;
        graphicsColHeight = holeHeightNative * graphicsScaleFactor;
        barWidth = graphicsColPitch > graphicsColWidth ? graphicsColPitch - graphicsColWidth : 0;
        railHeight = graphicsRowPitch > graphicsColHeight ? graphicsRowPitch - graphicsColHeight : 0;
        gratingGraphicsSize.x = columnCount < 1 ? 0 : ((graphicsColPitch * (columnCount - 1)) + graphicsColWidth);
        gratingGraphicsSize.y = rowCount < 1 ? (maxDimsReduced.y/scaleDownFactor) : ((graphicsRowPitch * (rowCount - 1)) + graphicsColHeight);
        gratingBorderSize.x = Mathf.Min(maxDimsReduced.x, gratingGraphicsSize.x * 1.25f);
        gratingBorderSize.y = Mathf.Min(maxDimsReduced.y, gratingGraphicsSize.y * 1.25f);
        gratingGraphicHalfSize = gratingGraphicsSize * 0.5f;

        sideBarWidth = (gratingBorderSize.x - gratingGraphicsSize.x) / 2.0f;
        upperLowerHeight = (gratingBorderSize.y - gratingGraphicsSize.y) / 2.0f;
        if (sideBarWidth < 0.001f)
            sideBarWidth = 0.001f;

        // Cache the lattice collision parameters
        setCollisionFilter();

        // Calculate positions of side and top panels
        Vector2 sidePanelPos = new Vector2 (gratingGraphicHalfSize.x + (sideBarWidth / 2.0f),0);
        Vector2 topPanelPos = new Vector2 (0,gratingGraphicHalfSize.y + (upperLowerHeight / 2.0f));
        if (frameSupport != null)
        {
            SetBarSizeAndPosition(frameSupport.transform, outerDimsMetres.x * experimentScale, outerDimsMetres.y * experimentScale, Vector2.zero, true, barsCollide);
            frameSupport.transform.localPosition = Vector3.right * (panelThickness * .5f);
        }

        if (barPrefab != null)
        {
            // Set Scales for the Slit and Block Prefabs
            SetBarSizeAndPosition(panelLeft.transform, sideBarWidth, gratingBorderSize.y, sidePanelPos, true,!barsCollide);
            SetBarSizeAndPosition(panelRight.transform, sideBarWidth, gratingBorderSize.y, -sidePanelPos, true,!barsCollide);
            if (rowCount >= 1)
            {
                SetBarSizeAndPosition(panelTop.transform, gratingBorderSize.x, upperLowerHeight, topPanelPos, true,!barsCollide);
                SetBarSizeAndPosition(panelBottom.transform, gratingBorderSize.x,upperLowerHeight, -topPanelPos, true,!barsCollide); ;
                panelTop.SetActive(true);
                panelBottom.SetActive(true);
            }
            else
            {
               panelTop.SetActive(false);
               panelBottom.SetActive(false);
            }
            // Set scale and then position of the first spacer
            float barHeight = rowCount > 0 ? gratingGraphicsSize.y : gratingBorderSize.y;
            float railWidth = columnCount > 0 ? gratingGraphicsSize.x : gratingBorderSize.x;
            Vector2 studPos = new Vector2 (gratingGraphicHalfSize.x - (graphicsColWidth + (barWidth / 2.0F)), 0);
            for (int nSlit = 0; nSlit < theBars.Length; nSlit++)
            {
                bool visible = (nSlit + 1) < columnCount;
                SetBarSizeAndPosition(theBars[nSlit].transform, barWidth, barHeight, studPos, visible, false);
                if (visible)
                    studPos.x -= graphicsColPitch;
            }
            Vector2 railPos = new Vector2(0, gratingGraphicHalfSize.y - (graphicsColHeight + (railHeight / 2.0F)));
            for (int nRail = 0; nRail < theRails.Length; nRail++)
            {
                bool visible = (nRail) < (rowCount-1);
                SetBarSizeAndPosition(theRails[nRail].transform, railWidth, railHeight, railPos, visible, false);
                if (visible)
                    railPos.y -= graphicsRowPitch;
            }
        }
        gratingVersionIsCurrent = true;
        GratingVersion = gratingVersion + 1;
        //        if (vectorDisplay != null)
        //            vectorDisplay.menuClick("grating", "updated");
    }

    void populateBars()
    {
        if (theBars == null)
            theBars = new GameObject[MAX_SLITS - 1];
        if (theRails == null)
            theRails = new GameObject[MAX_ROWS - 1];
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
        gratingVersionIsCurrent = false;
    }


    private bool started = false;
    public bool Started { get =>  started;}
    void Start()
    {
        if (gratingXfrm == null)
            gratingXfrm = transform;
        player = Networking.LocalPlayer;

        outerDimsMetres = nativeMaxDimensions / nativeGraphicsRatio;
        maxDimsReduced = outerDimsMetres * experimentScale;
        //MAX_SLITS = 15;
        if (togBarsCollide != null)
            togBarsCollide.isOn = barsCollide;
        GratingScaleStep = gratingScaleStep;
        sizeSteps = new Vector2(increment, increment);
        pitchSteps = sizeSteps * 5;

        ColumnCount = Mathf.Clamp(columnCount, 1, MAX_SLITS);
        RowCount = Mathf.Clamp(rowCount, 0, MAX_ROWS);
        gratingVersionIsCurrent = false;
        //columnPitchNative = Mathf.Clamp(columnPitchNative, pitchSteps.x, 0.1f);
        //rowPitchNative = Mathf.Clamp(rowPitchNative, pitchSteps.y, nativeMaxDimensions.y/(rowCount > 0 ? nativeMaxDimensions.y / rowCount : nativeMaxDimensions.y));
        //holeHeightNative = Mathf.Clamp(holeHeightNative, 0, maxHeight/2.0f);
        //holeWidthNative = Mathf.Clamp(holeWidthNative, 0, maxWidth/2.0f);
        if (panelThickness <= 0) panelThickness = 0.001f;
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
        if (!gratingVersionIsCurrent)
        {
           // Debug.Log("gratingVersion:"+gratingVersion);
            gratingVersionIsCurrent = true;
            setupLattice();
            UpdateLabels();
        }
    }
}
