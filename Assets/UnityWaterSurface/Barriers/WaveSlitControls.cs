
using System.Collections.Generic;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class WaveSlitControls : UdonSharpBehaviour
{
    [SerializeField] UdonBehaviour particleSim;
    bool ihaveParticleSim;
    public GameObject barPrefab;
    public Transform borderPrefab;

    public Vector2 TankDims;
    public float TankScale;
    
    public float barThickness;
    [SerializeField,UdonSynced,FieldChangeCallback(nameof (ApertureCount))]
    private int slitCount;

    [SerializeField,UdonSynced, FieldChangeCallback(nameof(ApertureWidth))]
    private float slitWidth;
    [SerializeField,UdonSynced, FieldChangeCallback(nameof(AperturePitch))]
    private float slitPitch;

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
    [SerializeField] TextMeshProUGUI labelSlits;
    [SerializeField] TextMeshProUGUI labelSlitPitch;
    [SerializeField] TextMeshProUGUI labelSlitWidth;
    [Header("Constants"), SerializeField]
    private int MAX_SLITS = 7;
    private bool gratingValid;
    private bool iamOwner;
    private VRCPlayerApi player;

    void setText(TextMeshProUGUI tmproLabel, string text)
    {
        if (tmproLabel != null)
            tmproLabel.text = text;
    }

    private void UpdateLabels()
    {
        setText(labelSlits, "Gaps\n" + slitCount.ToString());
        setText(labelSlitPitch, "Spacing\n" + Units.ToEngineeringNotation(slitPitch) + "m");
        setText(labelSlitWidth, "Gap Width\n" + Units.ToEngineeringNotation(slitWidth) + "m");
    }
    public float AperturePitch
    {
        get => slitPitch;
        private set
        {
            if (checkGratingWidth(slitWidth, value, ApertureCount))
            {
                if (value != slitPitch)
                {
                    slitPitch = value;
                    gratingValid = false;
                }
            }
            RequestSerialization();
        }
    }

    public float ApertureWidth
    {
        get => slitWidth;
        private set
        {
            if (checkGratingWidth(value,slitPitch,slitCount) && value > 0.01f)
            {
                if (value != slitWidth)
                {
                    slitWidth = value;
                    gratingValid = false;
                }
            }
            RequestSerialization();
        }
    }


    public int ApertureCount
    {
        get => slitCount;
        set
        {
            if (value < 1)
                value = 1;
            else if (value > MAX_SLITS)
                value = MAX_SLITS;
            if (value != slitCount)
            {
                slitCount = value;
                gratingValid = false;
            }
            RequestSerialization();
        }
    }

    public void OnAperturesPlus()
    {
        if ((ApertureCount < MAX_SLITS) && checkGratingWidth(ApertureWidth, AperturePitch, ApertureCount + 1))
        {
            if (!iamOwner)
                Networking.SetOwner(player, gameObject);
            ApertureCount = slitCount + 1; 
        }
    }

    public void OnAperturesMinus()
    {
        if (slitCount > 1)
        {
            if (!iamOwner)
                Networking.SetOwner(player, gameObject);
            ApertureCount = slitCount - 1;
        }
    }
    public void OnWidthPlus()
    {
        if (checkGratingWidth(ApertureWidth + 0.005f, AperturePitch, ApertureCount))
        {
            if (!iamOwner)
                Networking.SetOwner(player, gameObject);
            ApertureWidth = slitWidth + 0.005f;
        }
    }
    public void OnWidthMinus()
    {
        if (ApertureWidth <= 0.005f)
            return;
        if (checkGratingWidth(ApertureWidth - 0.005f, AperturePitch, ApertureCount))
        {
            if (!iamOwner)
                Networking.SetOwner(player,gameObject);
            ApertureWidth = slitWidth - 0.005f;
        }
    }
    public void OnPitchPlus()
    {
        if (checkGratingWidth(ApertureWidth, AperturePitch + 0.05f, ApertureCount))
        {
            if (!iamOwner)
                Networking.SetOwner(player, gameObject);
            AperturePitch = slitPitch + 0.05f;
        }
    }
    public void OnPitchMinus()
    {
        if (slitPitch <= 0.005f)
            return;
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        AperturePitch = slitPitch - 0.005f;
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

    bool checkGratingWidth(float slitWidth, float slitPitch, int numGaps)
    {
        if (numGaps <= 0)
            return true;
        if (numGaps == 1)
            return slitWidth <= tankWidth;
        return tankWidth >= (((numGaps - 1) * slitPitch) + slitWidth);
    }

    
    void setupLattice()
    {
        if (ihaveParticleSim)
        {
            particleSim.SetProgramVariable<int>("slitCount", slitCount);
            particleSim.SetProgramVariable<float>("slitWidth", slitWidth);
            particleSim.SetProgramVariable<float>("slitPitch", slitPitch);
        }
        // Set dimensons for the construction of the lattice;
        barWidth = slitPitch - slitWidth;
        gratingWidth = slitCount < 1 ? 0 : (slitPitch * (slitCount - 1)) + slitWidth;
        sideBarWidth = (tankWidth - gratingWidth) / 2.0f;
        //if (sideBarWidth < 0)
        //    sideBarWidth = gratingWidth / 4.0f;

        if (sideBarWidth < 0.05F)
            sideBarWidth = 0.05F;


        // Set up lattice parameters
        float gratingHalfWidth = (gratingWidth / 2.0f);
        float slitDelta = slitWidth + barWidth;

        // Calculate positions of side and top panels
        float sidePanelPos = gratingHalfWidth + (sideBarWidth / 2.0f);

        if (barPrefab != null)
        {
            // Set Scales for the Slit and Block Prefabs
            SetBarSizeAndPosition(panelLeft.transform, sideBarWidth, sidePanelPos, true);
            SetBarSizeAndPosition(panelRight.transform, sideBarWidth, -sidePanelPos, true);

            // Set scale and then position of the first spacer
            float StudOffset = gratingHalfWidth - (slitWidth + (barWidth / 2.0F));
            for (int nSlit = 0; nSlit < theBars.Length; nSlit++)
            {
                bool visible = (nSlit + 1) < slitCount;
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
        ihaveParticleSim = particleSim != null;
        player = Networking.LocalPlayer;
        MAX_SLITS = 7;
        gratingValid= false;
        UpdateOwnerShip();

        if (TankScale <= 0) TankScale = 1;
        if (tankWidth <= 0) tankWidth = 1.0f;
        if (tankLength <= 0) tankLength = 2.0f;
        if (barThickness <= 0) barThickness = 0.01f;
        if (slitCount < 0) slitCount = 2;
        if (slitWidth <= 0) slitWidth = 0.04f;
        if (slitPitch <= 0) slitPitch = 0.015f;
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
            //    particleScatter.SetGratingByPitch(slitCount, slitWidth, slitPitch);
        }
    }

}
