
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class WaveSlitControls : UdonSharpBehaviour
{
    public GameObject barPrefab;
    public Transform borderPrefab;

    public Vector2 TankDims;
    public float TankScale;
    public float barHeight;
    
    public float barThickness;
    
    public int apertureCount;

    public float apertureWidth;
    public float aperturePitch;

    // Private stuff
    //bool borderValid = true;
    bool gratingValid;
    int MAX_SLITS;

    float tankWidth
    {
        get => TankDims.x;
        set { TankDims.x = value; }
    }
    float tankLength
    {
        get => TankDims.y;
        set { TankDims.y = value; }
    }
    
    GameObject LocalFromPrefab(GameObject prototype, string name, Transform xfrm)
    {
        GameObject result = VRCInstantiate(prototype);
        if (result != null)
        {
            result.transform.SetParent(xfrm);
            result.name = name;
            result.transform.localPosition = Vector3.zero;
            result.transform.localRotation = Quaternion.identity;
            result.transform.localScale = Vector3.one;
        }
        return result;
    }
    
    void SetBarSizeAndPosition(Transform bar, float targetWidth, float targetposition, bool isVisible)
    {
        if (bar == null)
            return;
        bar.localScale = new Vector3(barThickness, barHeight, targetWidth);
        bar.localPosition = new Vector3(0, 0, targetposition);
        bar.gameObject.SetActive(isVisible);
    }


    int MaxSlits()
    {
        float usableWidth = tankWidth - apertureWidth;
        int units = Mathf.FloorToInt(usableWidth / (aperturePitch + apertureWidth));
        if (units > MAX_SLITS - 1)
            return MAX_SLITS;
        return units + 1;
    }

    float MaxPitch()
    {
        float usableWidth = (tankWidth - apertureWidth * MAX_SLITS);
        return usableWidth / (MAX_SLITS - 1);
    }
    
    public float AperturePitch
    {
        get => aperturePitch;
        set
        {
            if (value <= MaxPitch() && (value > 0.01f))
            {
                if (value != aperturePitch)
                {
                    aperturePitch = value;
                    gratingValid = false;
                }
            }
        }
    }

    public float ApertureWidth
    {
        get => apertureWidth;
        set
        {
            if (value <= tankWidth / MAX_SLITS && value > 0.01f)
            {
                if (value != apertureWidth)
                {
                    apertureWidth = value;
                    gratingValid = false;
                }
            }
        }
    }

    public int ApertureCount
    {
        get => apertureCount;
        set
        {
            if (value < 1)
                value = 1;
            else
            {
                int Max = MaxSlits();
                if (value > Max)
                    value = Max;
            }
            if (value != apertureCount)
            {
                apertureCount = value;
                gratingValid = false;
            }
        }
    }
    
    
    // Grating properties
    private GameObject[] theBars;
    private GameObject panelLeft;
    private GameObject panelRight;
    // dimensions
    private float gratingWidth;
    private float barWidth;
    private float sideBarWidth;
    
    void setupLattice()
    {
        // Set dimensons for the construction of the lattice;
        barWidth = aperturePitch - apertureWidth;

        gratingWidth = (aperturePitch * (apertureCount - 1)) + apertureWidth;
        sideBarWidth = (tankWidth - gratingWidth) / 2.0f;
        if (sideBarWidth < 0)
            sideBarWidth = gratingWidth / 4.0f;

        if (sideBarWidth < 0.05F)
            sideBarWidth = 0.05F;


        // Set up lattice parameters
        float gratingHalfWidth = (gratingWidth / 2.0f);
        float slitDelta = apertureWidth + barWidth;

        // Calculate positions of side and top panels
        float sidePanelPos = gratingHalfWidth + (sideBarWidth / 2.0f);

        if (barPrefab != null)
        {
            // Set Scales for the Slit and Block Prefabs
            SetBarSizeAndPosition(panelLeft.transform, sideBarWidth, sidePanelPos, true);
            SetBarSizeAndPosition(panelRight.transform, sideBarWidth, -sidePanelPos, true);

            // Set scale and then position of the first spacer
            float StudOffset = gratingHalfWidth - (apertureWidth + (barWidth / 2.0F));
            for (int nSlit = 0; nSlit < theBars.Length; nSlit++)
            {
                bool visible = (nSlit + 1) < apertureCount;
                SetBarSizeAndPosition(theBars[nSlit].transform, barWidth, StudOffset, visible);
                if (visible)
                    StudOffset -= slitDelta;
            }
        }
        gratingValid = true;
//        if (vectorDisplay != null)
//            vectorDisplay.menuClick("grating", "updated");
    }
    
    void populateBars()
    {
        if (theBars == null)
            theBars = new GameObject[MAX_SLITS - 1];
        for (int i = 0; i < theBars.Length; i++)
        {
            if (theBars[i] == null)
            {
                theBars[i] = LocalFromPrefab(barPrefab, "Bar_" + (i + 1).ToString(), transform);
                theBars[i].transform.position = new Vector3(0, 0, 0);
                theBars[i].gameObject.SetActive(false);
            }
        }
        if (panelLeft == null)
            panelLeft = LocalFromPrefab(barPrefab, "LeftPanel", transform);
        if (panelRight == null)
            panelRight = LocalFromPrefab(barPrefab, "RightPanel", transform);
        gratingValid = false;
    }

    
    void Start()
    {
        MAX_SLITS = 7;
        gratingValid= false;
        if (TankScale <= 0) TankScale = 1;
        if (tankWidth <= 0) tankWidth = 1.0f;
        if (tankLength <= 0) tankLength = 2.0f;
        if (barHeight <= 0) barHeight = 0.125f;
        if (barThickness <= 0) barThickness = 0.01f;
        if (apertureCount < 0) apertureCount = 2;
        if (apertureWidth <= 0) apertureWidth = 0.04f;
        if (aperturePitch <= 0) aperturePitch = 0.015f;
        //vectorDisplay = transform.parent.GetComponentInChildren<DisplayWaveVectors>() as IMenuClick;
        populateBars();
        //setBorders();

    }
    void Update()
    {
        //if (!borderValid)
        //    setBorders();
        if (!gratingValid)
        {
            gratingValid = true;
            setupLattice();
         //   UpdateLabels();
        }
    }

}
