
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class VectorDiagram : UdonSharpBehaviour
{
    [Tooltip("Source Pitch (mm)"), SerializeField,FieldChangeCallback(nameof(SourcePitch))] public float sourcePitch = 436.5234f;
    [Tooltip("Lambda (mm)"), SerializeField, FieldChangeCallback(nameof(Lambda))] public float lambda = 48.61111f;

    [SerializeField] private float arrowLambda = 18;

    [SerializeField]
    UdonPointer[] kVectors;
    [SerializeField]
    UdonPointer[] kComponents;

    Vector2[] kEndPoints;
    private bool needsUpdate = false;
    [SerializeField]
    private float arrowLength = 0.1f;
    private void recalc()
    {
        kEndPoints = new Vector2[kVectors.Length];
        arrowLength = (arrowLambda) / lambda;
        if (kVectors != null)
        {
            for (int i = 0; i< kVectors.Length;i++)
            {
                float sinTheta = i * lambda/sourcePitch;
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
    [SerializeField] TextMeshProUGUI txtAngle;
    void Start()
    {
        needsUpdate = true;
    }
}
