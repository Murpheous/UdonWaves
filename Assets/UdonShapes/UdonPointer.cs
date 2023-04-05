
using BestHTTP.SecureProtocol.Org.BouncyCastle.Math;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
[RequireComponent(typeof(LineRenderer))]
public class UdonPointer : UdonSharpBehaviour
{
    [SerializeField]
    private float lineLength = 0.5f;
    public float LineLength { get => lineLength; 
        set {
            if (lineLength != value)
            {
                lineLength = value;
                UpdateShaft();
                UpdateTipLocation();
            }
        } 
    }
    [SerializeField,Range(0f,0.1f)]
    private float lineWidth = 0.03f;
    
    [SerializeField]
    private float thetaDegrees = 0;
    private Vector2 startLocal = Vector2.zero;
    private Vector2 endLocal = Vector2.right;
    
    public Vector2 vector { get { return endLocal - startLocal; } }
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
    
    [SerializeField]
    private Color lineColour= Color.cyan;
    [SerializeField,Range(0f,1f)]
    private float alpha = 1;
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
    [SerializeField]
    LineRenderer[] barbLines;

    [SerializeField,Tooltip("Tip object transform")]
    Transform tip;
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
                UpdateTipLocation();
            }
        }
    }
       
    [SerializeField]
    Vector2 barbLengths = new Vector2(0.05f,0.05f);
    [SerializeField]
    Vector2 barbAngles = new Vector2(30f, -25f);

    private void RefreshColours()
    {
        currentColour = lineColour;
        currentColour.a = alpha;
        if (alpha <= 0)
        {
            //shaftLine.gameObject.SetActive(false);
            shaftLine.enabled= false;
            if (barbLines[0] != null)
                barbLines[0].enabled= false;
            if (barbLines[1] != null)
                barbLines[1].enabled = false;
            return;
        }
        shaftLine.enabled= true;
        shaftLine.startColor = currentColour;
        shaftLine.endColor = currentColour;
        if (barbLines != null)
        {
            if (barbLines[0] != null)
            {
                barbLines[0].enabled= true;
                barbLines[0].startColor = currentColour;
                barbLines[0].endColor = currentColour;
            }
            if (barbLines[1] != null)
            {
                barbLines[1].enabled = true;
                barbLines[1].startColor =currentColour;
                barbLines[1].endColor = currentColour;
            }
        }
    }
    private void UpdateTip()
    {
        if (barbLines == null) 
            return;
        if (barbLines[0] != null)
        {
            barbLines[0].SetPosition(1,Vector3.zero);
            barbLines[0].SetPosition(0, new Vector3(barbLengths.x , 0, 0));
            barbLines[0].startWidth = lineWidth;
            barbLines[0].endWidth = lineWidth;
            barbLines[0].transform.localRotation = Quaternion.Euler(0, 0, 180 - barbAngles.x);
        }
        if (barbLines[1] != null)
        {
            barbLines[1].SetPosition(1, Vector3.zero);
            barbLines[1].SetPosition(0, new Vector3(barbLengths.y, 0, 0));
            barbLines[1].startWidth = lineWidth;
            barbLines[1].endWidth = lineWidth;
            barbLines[1].transform.localRotation = Quaternion.Euler(0, 0, 180 + barbAngles.y);
        }
    }

    private void UpdateTipLocation()
    {
        if (tip != null)
        {
            tip.localPosition = Vector2.right * Mathf.Lerp(0, lineLength, tipLocation) + startLocal;
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
        if (barbLines == null)
            barbLines = GetComponentsInChildren<LineRenderer>();
        UpdateShaft();
        UpdateTipLocation();
        UpdateTip();
        RefreshColours();
        ThetaDegrees = thetaDegrees;
    }
}
