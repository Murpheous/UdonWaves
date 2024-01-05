
using UdonSharp;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class VectorDiagram : UdonSharpBehaviour
{
    [SerializeField] GameObject linePrefab;
    [Tooltip("Source Count"),SerializeField, Range(1,16),FieldChangeCallback(nameof(NumSources))] public int numSources = 2;
    [Tooltip("Source Width (mm)"),SerializeField, FieldChangeCallback(nameof(SourceWidth))] float sourceWidth;
    [Tooltip("Source Pitch (mm)"), SerializeField,FieldChangeCallback(nameof(SourcePitch))] public float sourcePitch = 436.5234f;
    [Tooltip("Lambda (mm)"), SerializeField, FieldChangeCallback(nameof(Lambda))] public float lambda = 48.61111f;
    [SerializeField] private float arrowLambda = 18;
    [SerializeField, FieldChangeCallback(nameof(DemoMode))] public int demoMode;
    [SerializeField] UdonPointer[] kVectors;
    [SerializeField] UdonPointer[] kComponents;
    [SerializeField] TextMeshProUGUI[] vecLabels;
    [SerializeField] UdonLine[] kLines;


    Vector2[] kEndPoints;
    private bool needsUpdate = false;
    private float arrowLength = 0.1f;
    
    private int DemoMode
    {
        get => demoMode; 
        set
        {
            Debug.Log("Vec Demo Mode: "+value.ToString());
            demoMode = value;
        }
    }
    
    private void recalc()
    {
        kEndPoints = new Vector2[kVectors.Length];
        arrowLength = (arrowLambda) / lambda;
        float sinTheta;
        float WidthX2 = sourceWidth * 2;
        if (kVectors != null)
        {
            for (int i = 0; i< kVectors.Length;i++)
            {
                if (numSources > 1)
                {
                    sinTheta = i * lambda / sourcePitch;
                }
                else
                {
                    sinTheta = i == 0 ? 0 : (2 * i + 1) / WidthX2;
                }
                if (Mathf.Abs(sinTheta) <= 1)
                {
                    float thetaRadians = Mathf.Asin(sinTheta);
                    if (kVectors[i] != null)
                    {
                        kVectors[i].LineLength = arrowLength;
                        kVectors[i].ThetaDegrees = thetaRadians * Mathf.Rad2Deg;
                        kVectors[i].Alpha = 1;
                    }
                    kEndPoints[i].y = sinTheta * arrowLength;
                    kEndPoints[i].x = Mathf.Cos(thetaRadians) * arrowLength;
                }
                else
                {
                    if (kVectors[i] != null)
                    {
                        kVectors[i].LineLength = arrowLength;
                        kVectors[i].ThetaDegrees = 90;
                        kVectors[i].Alpha = 0;
                    }
                    kEndPoints[i].x = -1;
                    kEndPoints[i].y = arrowLength;
                }
            }
        }
        if (kComponents != null)
        {
            Debug.Log("Do Components");
            int limit = kEndPoints.Length-1;
            if (kComponents.Length < limit)
                limit = kComponents.Length;
            for (int j = 0; j < limit; j++)
            {
                Debug.Log("Do Component["+ j.ToString() + "]");
                if (kComponents[j] != null)
                {
                    kComponents[j].LineLength = kEndPoints[j + 1].y;
                    Vector3 lpos = kComponents[j].transform.localPosition;
                    lpos.x = kEndPoints[j+1].x;
                    if (lpos.x >= 0)
                    {
                        kComponents[j].transform.localPosition = lpos;
                        kComponents[j].Alpha = 1;
                    }
                    else
                    {
                        lpos.x = 0;
                        kComponents[j].transform.localPosition = lpos;
                        kComponents[j].Alpha = 0;
                    }    
                }
            }
        }
        needsUpdate = false;
    }

    public int NumSources
    {
        get => numSources;
        set
        {
            value = Mathf.Max(1,value);
            numSources = value;
            needsUpdate = true;
        }
    }

    public float SourceWidth
    {
        get => sourceWidth;
        set
        {
            sourceWidth = Mathf.Max(1.0f,value);
            needsUpdate = true;
        }
    }
    public float SourcePitch
    {
        get => sourcePitch; 
        set
        {
            sourcePitch = value;
            needsUpdate = true;
            //Debug.Log("Vect Gap: " +  sourcePitch);
        } 
    }
    public float Lambda
    {
        get => lambda;
        set
        {
            lambda = value;
            needsUpdate = true;

           // Debug.Log("Vect Lamby: " + value);
        }
    }

    private void Update()
    {
        if (needsUpdate)
        {
            needsUpdate = false;
            recalc();
        }
    }

    void Start()
    {
        needsUpdate = true;
    }
}
