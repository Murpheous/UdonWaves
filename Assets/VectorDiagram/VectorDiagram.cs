
using UdonSharp;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using Unity.Collections;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class VectorDiagram : UdonSharpBehaviour
{
    [SerializeField] float displayWidth = 1.940f;
    [SerializeField, FieldChangeCallback(nameof(DisplayHeight))] public float displayHeight = 0.920f;
    private float halfHeight = 0.46f;
    [SerializeField] GameObject linePrefab;
    [Tooltip("Source Count"),SerializeField, Range(1,16),FieldChangeCallback(nameof(NumSources))] public int numSources = 2;
    [Tooltip("Source Width (mm)"),SerializeField, FieldChangeCallback(nameof(SourceWidth))] float sourceWidth;
    [Tooltip("Source Pitch (mm)"), SerializeField,FieldChangeCallback(nameof(SourcePitch))] public float sourcePitch = 436.5234f;
    [Tooltip("Lambda (mm)"), SerializeField, FieldChangeCallback(nameof(Lambda))] public float lambda = 48.61111f;
    [SerializeField] private float arrowLambda = 18;
    [SerializeField, FieldChangeCallback(nameof(DemoMode))] public int demoMode;
    [SerializeField] UdonPointer[] kVectors;
    [SerializeField] UdonPointer[] kComponents;
    [SerializeField] UdonLabel[] vecLabels;
    [SerializeField] UdonLine[] kLines;

    Vector2[] kEndPoints;
    Vector2[] labelPoints;
    string[] beamAngles;
    private bool needsUpdate = false;
    private float arrowLength = 0.1f;
    
    public float DisplayHeight
    {
        get => displayHeight;
        set
        {
            if (displayHeight == value)
                return;
            displayHeight = value;
            halfHeight = displayHeight/2f;
            needsUpdate = true;
        }
    }
    private int DemoMode
    {
        get => demoMode; 
        set
        {
            Debug.Log("Vec Demo Mode: "+value.ToString());
            demoMode = value;
            needsUpdate = true;
        }
    }

    private void kVectorDisplay(int demoMode)
    {
        arrowLength = (arrowLambda) / lambda;
        if (demoMode <= 0)
        {
            if (kVectors != null)
            {
                for (int i = 0; i < kVectors.Length; i++)
                {
                    if (kVectors[i] != null)
                        kVectors[i].Alpha = 0f;
                }
            }
            if (vecLabels != null)
            {
                for(int i = 0;i < vecLabels.Length; i++)
                {
                    if (vecLabels[i] != null)
                        vecLabels[i].Visible = false;
                }
            }
            return;
        }
        if (kVectors == null || kVectors.Length == 0)
            return;
        kEndPoints = new Vector2[kVectors.Length];
        beamAngles = new string[kVectors.Length];
        labelPoints = new Vector2[kVectors.Length];
        float sinTheta;
        float WidthX2 = sourceWidth * 2;
        for (int i = 0; i < kVectors.Length; i++)
        {
            float thetaRadians;
            if (numSources > 1)
                sinTheta = i * lambda / sourcePitch;
            else
                sinTheta = i == 0 ? 0 : (2 * i + 1) / WidthX2;
            float lineLength = arrowLength;
            Vector2 endPoint = Vector2.left;
            Vector2 labelPoint = Vector2.left;
            bool sinIsValid = Mathf.Abs(sinTheta) < 1f;
            thetaRadians = sinIsValid ? Mathf.Asin(sinTheta) : 0;
            beamAngles[i] = sinIsValid ? string.Format("{0:0.#}°", thetaRadians*Mathf.Rad2Deg) : "";
            if (sinIsValid)
            {
                if (demoMode < 2)
                {

                    endPoint.y = sinTheta * displayWidth;
                    if (endPoint.y <= halfHeight)
                    {
                        endPoint.x = displayWidth;
                        lineLength = endPoint.magnitude;
                    }
                    else
                    {
                        endPoint.y = halfHeight;
                        lineLength = halfHeight / sinTheta;
                        endPoint.x = lineLength * Mathf.Cos(thetaRadians);
                    }
                    labelPoint = endPoint;
                }
                else
                {
                    endPoint.y = sinTheta * arrowLength;
                    if (endPoint.y <= halfHeight)
                    {
                        endPoint.x = Mathf.Cos(thetaRadians) * arrowLength;
                        labelPoint.x = displayWidth;
                    }
                    labelPoint.y = endPoint.y;
                }
            }
            if (kVectors[i] != null && endPoint.x > 0)
            {
                kVectors[i].ShowTip = demoMode >= 2;
                kVectors[i].LineLength = lineLength;
                kVectors[i].ThetaDegrees = thetaRadians * Mathf.Rad2Deg;
                kVectors[i].Alpha = 1.0f;

            }
            else
            {
                kVectors[i].Alpha = 0f;
            }
            kEndPoints[i] = endPoint;
            labelPoints[i] = labelPoint;
        }
        if (vecLabels != null && vecLabels.Length > 0)
        {
            for (int i = 0; i < vecLabels.Length; i++)
            {
                if (vecLabels[i] != null)
                {
                    int vecIdx = i + 1;
                    string labelText;
                    //string mul = vecIdx > 1 ? string.Format("{0}*",vecIdx) : ""; 
                    switch (demoMode)
                    {
                        case 1:
                            labelText = string.Format("θ<sub>{0}</sub>={1}", vecIdx, beamAngles[vecIdx ]);
                            break;
                        case 2:
                            labelText = string.Format("K<sub>{0}</sub>={0}/D",vecIdx);
                            break;
                        default:
                            labelText = "";
                            break;
                    }
                    if (demoMode > 0 && labelPoints[vecIdx].x > 0)
                    {
                        vecLabels[i].LocalPostion = labelPoints[vecIdx];
                        vecLabels[i].Visible = true;
                    }
                    else
                        vecLabels[i].Visible = false;
                    vecLabels[i].Text = labelText;
                }
            }
        }

    }

    private void kLineDisplay(int demoMode)
    {
        if (kLines == null || kLines.Length < 1)
            return;
        if (demoMode < 2)
        {
            for (int i = 0; i < kLines.Length; i++)
            {
                if (kLines[i] != null)
                    kLines[i].Alpha = 0f;
            }
            return;
        }
        int limit = kEndPoints.Length - 1;
        if (kLines.Length < limit)
            limit = kLines.Length;
        for (int j = 0; j < limit; j++)
        {
            int vecIdx = j + 1;
            if (kLines[j] != null)
            {
                if (labelPoints[vecIdx].x < 0)
                    kLines[j].Alpha = 0f;
                else
                {
                    kLines[j].Alpha = 1f;
                    kLines[j].LineLength = displayWidth;
                    kLines[j].transform.localPosition = new Vector3(0, labelPoints[vecIdx].y , 0);
                }
            }
        }
    }
    private void componentDisplay(int demoMode)
    {
        if ( kComponents == null)
            return;            
        if (demoMode < 2)
        {
            for (int i = 0; i < kComponents.Length; i++)
            {
                if (kComponents[i] != null)
                    kComponents[i].Alpha = 0f;
            }
            return;
        }
        if (kComponents == null)
            return;
        int limit = kEndPoints.Length - 1;
        if (kComponents.Length < limit)
            limit = kComponents.Length;
        for (int j = 0; j < limit; j++)
        {
            if (kComponents[j] != null)
            {
                kComponents[j].LineLength = kEndPoints[j + 1].y;
                Vector3 lpos = kComponents[j].transform.localPosition;
                lpos.x = kEndPoints[j + 1].x;
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
    private void recalc()
    {
        kVectorDisplay(demoMode);
        componentDisplay(demoMode);
        kLineDisplay(demoMode);
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
        DisplayHeight = displayHeight;
    }
}
