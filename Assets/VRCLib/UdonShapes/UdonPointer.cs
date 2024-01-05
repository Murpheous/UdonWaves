
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
[RequireComponent(typeof(LineRenderer))]
public class UdonPointer : UdonSharpBehaviour
{
    [SerializeField, FieldChangeCallback(nameof(LineLength))]
    public float lineLength = 0.5f;
    public float LineLength { 
        get => lineLength; 
        set 
        {
            if (lineLength != value)
            {
                lineLength = value;
                UpdateShaft();
                UpdateTipLocations();
            }
        } 
    }
    [SerializeField,Range(0f,0.1f), FieldChangeCallback(nameof(LineWidth))]
    public float lineWidth = 0.03f;

    public float LineWidth
    {
        get => lineWidth;
        set
        {
            lineWidth = value;
            UpdateShaft();
        }
    }


    [SerializeField,FieldChangeCallback(nameof(ThetaDegrees))]
    public float thetaDegrees = 0;
    [SerializeField]
    private Vector2 startLocal = Vector2.zero;
    [SerializeField]
    private Vector2 endLocal = Vector2.right;
    public float ThetaDegrees
    {
        get => thetaDegrees; 
        set
        {
            thetaDegrees = value;
            transform.localRotation = Quaternion.Euler(0,0,thetaDegrees);
        } 
    }

    [SerializeField]
    private bool isIncoming = false;
    
    [SerializeField,FieldChangeCallback(nameof(LineColour))]
    public Color lineColour= Color.cyan;
    [SerializeField,Range(0f,1f),FieldChangeCallback(nameof(Alpha))]
    public float alpha = 1;
    private Color currentColour= Color.white;

    public Color LineColour
    {
        get => lineColour;
        set
        {
            if (lineColour != value)
            {
                lineColour = value;
                RefreshColours();
            }
        }
    }

    public float Alpha
    {
        get => alpha;
        set
        {
            if (alpha != value)
                alpha = value;
            RefreshColours();
        }
    }

    [SerializeField]
    LineRenderer shaftLine;
    LineRenderer[] tipLines;
    LineRenderer[] mirrorTipLines;

    [SerializeField,Tooltip("Tip object transform")]
    Transform tip;
    [SerializeField]
    private bool showTip = true;
    public bool ShowTip 
    {
        get =>  showTip; 
        set 
        {
            if (showTip != value)
            {
                showTip = value;
                RefreshTips();
            }
        } 
    }
    [SerializeField, Tooltip("Mirror Tip object transform")]
    Transform mirrorTip;
    [SerializeField]
    private bool showMirrorTip;
    public bool ShowMirrorTip
    {
        get => showMirrorTip;
        set
        {
            if (showMirrorTip != value)
            {
                showMirrorTip = value;
                RefreshColours();
                RefreshTips();
            }
        }
    }

    [SerializeField, Range(0f, 1f),Tooltip("Slide pointer position along shaft")]
    private float tipLocation;
    public float TipLocation
    {
        get => tipLocation;
        set
        {
            if (tipLocation != value)
            {
                tipLocation = value;
                UpdateTipLocations();
            }
        }
    }
       
    [SerializeField]
    Vector2 barbLengths = new Vector2(0.05f,0.05f);
    [SerializeField]
    Vector2 barbAngles = new Vector2(30f, -25f);
    //[SerializeField]
    private bool tipIsPresent;
    private bool mirrorTipIsPresent;

    private void RefreshColours()
    {
        bool isVisible = Alpha > 0;
        currentColour = lineColour;
        //currentColour.a = alpha;
        currentColour *= alpha;
            //shaftLine.gameObject.SetActive(false);
        shaftLine.enabled = isVisible;
        if (tipIsPresent)
        {
            tipLines[0].enabled = isVisible;
            tipLines[1].enabled = isVisible;
        }
        if (showMirrorTip && mirrorTipIsPresent)
        {
            mirrorTipLines[0].enabled = isVisible;
            mirrorTipLines[1].enabled = isVisible;
        }
        if (!isVisible)
            return;
        shaftLine.startColor = currentColour;
        shaftLine.endColor = currentColour;
        if (tipIsPresent)
        {
            tipLines[0].startColor = currentColour;
            tipLines[0].endColor = currentColour;
            tipLines[1].startColor =currentColour;
            tipLines[1].endColor = currentColour;
        }
        if (mirrorTipIsPresent)
        {
            if (showMirrorTip)
            {
                mirrorTipLines[0].startColor = currentColour;
                mirrorTipLines[0].endColor = currentColour;
                mirrorTipLines[1].startColor = currentColour;
                mirrorTipLines[1].endColor = currentColour;
            }
        }
    }
    private void RefreshTips()
    {
        if (tipIsPresent)
        {
            tipLines[0].enabled = showTip;
            tipLines[1].enabled = showTip;
            if (showTip)
            {
                tipLines[0].SetPosition(1, Vector3.zero);
                tipLines[0].SetPosition(0, new Vector3(barbLengths.x, 0, 0));
                tipLines[0].startWidth = lineWidth;
                tipLines[0].endWidth = lineWidth;
                tipLines[0].transform.localRotation = Quaternion.Euler(0, 0, 180 - barbAngles.x);
                tipLines[1].SetPosition(1, Vector3.zero);
                tipLines[1].SetPosition(0, new Vector3(barbLengths.y, 0, 0));
                tipLines[1].startWidth = lineWidth;
                tipLines[1].endWidth = lineWidth;
                tipLines[1].transform.localRotation = Quaternion.Euler(0, 0, 180 + barbAngles.y);
            }
        }
        if (mirrorTipIsPresent)
        {
            mirrorTipLines[0].enabled = showMirrorTip;
            mirrorTipLines[1].enabled = showMirrorTip;
            if (showMirrorTip)
            {
                mirrorTipLines[0].SetPosition(1, Vector3.zero);
                mirrorTipLines[0].SetPosition(0, new Vector3(barbLengths.x, 0, 0));
                mirrorTipLines[0].startWidth = lineWidth;
                mirrorTipLines[0].endWidth = lineWidth;
                mirrorTipLines[0].transform.localRotation = Quaternion.Euler(0, 0, 0 - barbAngles.x);
                mirrorTipLines[1].SetPosition(1, Vector3.zero);
                mirrorTipLines[1].SetPosition(0, new Vector3(barbLengths.y, 0, 0));
                mirrorTipLines[1].startWidth = lineWidth;
                mirrorTipLines[1].endWidth = lineWidth;
                mirrorTipLines[1].transform.localRotation = Quaternion.Euler(0, 0, 0 + barbAngles.y);
            }
        }
    }

    private void UpdateTipLocations()
    {
        if (tip != null)
        {
            tip.localPosition = Vector2.right * Mathf.Lerp(0, lineLength, tipLocation) + startLocal;
        }
        if (showMirrorTip && mirrorTip != null)
        {
            mirrorTip.localPosition = Vector2.right * Mathf.Lerp(0, lineLength, 1-tipLocation) + startLocal;
        }
    }
    private void UpdateShaft()
    {
        if (isIncoming)
        {
            startLocal = lineLength * Vector3.left;
            endLocal = Vector3.zero;
        }
        else
        {
            startLocal = Vector3.zero;
            endLocal = Vector3.right * lineLength;
        }

        if (shaftLine != null)
        {
            shaftLine.positionCount = 2;
            shaftLine.SetPosition(0, startLocal);
            shaftLine.SetPosition(1, endLocal);
            shaftLine.startWidth= lineWidth;
            shaftLine.endWidth= lineWidth;
        }
    }
    void Start()
    {
        shaftLine = GetComponent<LineRenderer>();
        if (tip != null)
            tipLines = tip.GetComponentsInChildren<LineRenderer>();
        tipIsPresent = tipLines != null && tipLines.Length == 2;
        if (mirrorTip != null)
            mirrorTipLines = mirrorTip.GetComponentsInChildren<LineRenderer>();
        mirrorTipIsPresent = mirrorTipLines != null && mirrorTipLines.Length == 2;
        UpdateShaft();
        UpdateTipLocations();
        RefreshTips();
        RefreshColours();
        ThetaDegrees = thetaDegrees;
    }
}
