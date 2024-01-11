
using System.Collections.Generic;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class WaveSlitControls : UdonSharpBehaviour
{
    public GameObject barPrefab;
    public Transform borderPrefab;

    public Vector2 TankDims;
    public float TankScale;
    
    public float barThickness;
    [SerializeField,UdonSynced,FieldChangeCallback(nameof (ApertureCount))]
    private int apertureCount;

    [SerializeField,UdonSynced, FieldChangeCallback(nameof(ApertureWidth))]
    private float apertureWidth;
    [SerializeField,UdonSynced, FieldChangeCallback(nameof(AperturePitch))]
    private float aperturePitch;

    private float gratingWidth;
    private float barWidth;
    private float sideBarWidth;
    public float GratingWidth { get => gratingWidth; private set => gratingWidth = value; }
    public float SideBarWidth { get => sideBarWidth; private set => sideBarWidth = value; }
    // Grating properties
    private GameObject[] theBars;
    private GameObject panelLeft;
    private GameObject panelRight;
    // dimensions

    [Header("UI Conponents")]
    [SerializeField] GameObject UiPanel;
    [SerializeField] TextMeshProUGUI labelSlits;
    [SerializeField] TextMeshProUGUI labelSlitPitch;
    [SerializeField] TextMeshProUGUI labelSlitWidth;
    [Header("Constants"), SerializeField]
    private int MAX_SLITS = 7;
    //private QuantumScatter particleScatter;
    private bool gratingValid;
    private bool iamOwner;
    private VRC.Udon.Common.Interfaces.NetworkEventTarget toTheOwner = VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner;

    void setText(TextMeshProUGUI tmproLabel, string text)
    {
        if (tmproLabel != null)
            tmproLabel.text = text;
    }

    private void UpdateLabels()
    {
        setText(labelSlits, "Gaps\n" + apertureCount.ToString());
        setText(labelSlitPitch, "Spacing\n" + Units.ToEngineeringNotation(aperturePitch) + "m");
        setText(labelSlitWidth, "Gap Width\n" + Units.ToEngineeringNotation(apertureWidth) + "m");
    }
    public float AperturePitch
    {
        get => aperturePitch;
        private set
        {
            if (checkGratingWidth(apertureWidth, value, ApertureCount))
            {
                if (value != aperturePitch)
                {
                    aperturePitch = value;
                    gratingValid = false;
                }
            }
            RequestSerialization();
        }
    }

    public float ApertureWidth
    {
        get => apertureWidth;
        private set
        {
            if (checkGratingWidth(value,aperturePitch,apertureCount) && value > 0.01f)
            {
                if (value != apertureWidth)
                {
                    apertureWidth = value;
                    gratingValid = false;
                }
            }
            RequestSerialization();
        }
    }


    public int ApertureCount
    {
        get => apertureCount;
        set
        {
            if (value < 1)
                value = 1;
            else if (value > MAX_SLITS)
                value = MAX_SLITS;
            if (value != apertureCount)
            {
                apertureCount = value;
                gratingValid = false;
            }
            RequestSerialization();
        }
    }

    public void OnAperturesPlus()
    {
        if (!iamOwner)
        {
            SendCustomNetworkEvent(toTheOwner, nameof(OnAperturesPlus));
            return;
        }
        if ((ApertureCount < MAX_SLITS) && checkGratingWidth(ApertureWidth, AperturePitch, ApertureCount + 1))
        {
                ApertureCount = apertureCount + 1; 
        }
    }

    public void OnAperturesMinus()
    {
        if (!iamOwner)
        {
            SendCustomNetworkEvent(toTheOwner, nameof(OnAperturesMinus));
            return;
        }
        ApertureCount = apertureCount - 1;
    }
    public void OnWidthPlus()
    {
        if (!iamOwner)
        {
            SendCustomNetworkEvent(toTheOwner, nameof(OnWidthPlus));
            return;
        }
        if (checkGratingWidth(ApertureWidth + 0.005f, AperturePitch, ApertureCount))
            ApertureWidth = apertureWidth + 0.005f;
    }
    public void OnWidthMinus()
    {
        if (!iamOwner)
        {
            SendCustomNetworkEvent(toTheOwner, nameof(OnWidthMinus));
            return;
        }
        if (ApertureWidth <= 0.005f)
            return;
        if (checkGratingWidth(ApertureWidth-0.005f, AperturePitch, ApertureCount))
            ApertureWidth = apertureWidth - 0.005f;
    }
    public void OnPitchPlus()
    {
        if (!iamOwner)
        {
            SendCustomNetworkEvent(toTheOwner, nameof(OnPitchPlus));
            return;
        }
        if (checkGratingWidth(ApertureWidth,AperturePitch+0.05f, ApertureCount))
            AperturePitch = aperturePitch + 0.05f;
    }
    public void OnPitchMinus()
    {
        if (!iamOwner)
        {
            SendCustomNetworkEvent(toTheOwner, nameof(OnPitchMinus));
            return;
        }

        if (aperturePitch <= 0.005f)
            return;
        //if (checkGratingWidth(ApertureWidth, AperturePitch - 0.01f, ApertureCount))
        AperturePitch = aperturePitch - 0.005f;
    }


    float tankWidth
    {
        get => TankDims.y;
        set { TankDims.y = value; }
    }
    float tankLength
    {
        get => TankDims.x;
        set { TankDims.x = value; }
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

    void SetBarSizeAndPosition(Transform bar, float targetWidth, float targetposition, bool isVisible)
    {
        if (bar == null)
            return;
        bar.localScale = new Vector3(barThickness, 1, targetWidth);
        bar.localPosition = new Vector3(0, 0, targetposition);
        bar.gameObject.SetActive(isVisible);
    }

    bool checkGratingWidth(float apertureWidth, float aperturePitch, int numGaps)
    {
        if (numGaps <= 0)
            return true;
        if (numGaps == 1)
            return apertureWidth <= tankWidth;
        return tankWidth >= (((numGaps - 1) * aperturePitch) + apertureWidth);
    }

    
    void setupLattice()
    {
        // Set dimensons for the construction of the lattice;
        barWidth = aperturePitch - apertureWidth;
        gratingWidth = apertureCount < 1 ? 0 : (aperturePitch * (apertureCount - 1)) + apertureWidth;
        sideBarWidth = (tankWidth - gratingWidth) / 2.0f;
        //if (sideBarWidth < 0)
        //    sideBarWidth = gratingWidth / 4.0f;

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

    private void UpdateOwnerShip()
    {
        iamOwner = Networking.IsOwner(this.gameObject);
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        UpdateOwnerShip();
    }

    void Start()
    {
        MAX_SLITS = 7;
        gratingValid= false;
        UpdateOwnerShip();

        if (TankScale <= 0) TankScale = 1;
        if (tankWidth <= 0) tankWidth = 1.0f;
        if (tankLength <= 0) tankLength = 2.0f;
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
            UpdateLabels();
            // if (particleScatter != null)
            //    particleScatter.SetGratingByPitch(apertureCount, apertureWidth, aperturePitch);
        }
    }

}
