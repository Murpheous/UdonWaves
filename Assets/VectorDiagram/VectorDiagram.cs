
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class VectorDiagram : UdonSharpBehaviour
{
    public Slider stepControl;
    public float slitSpacing = 0.05f;
    public float lambda = 0.1f;
    public float arrowLambda = 17;

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
        arrowLength = arrowLambda * lambda;
        if (kVectors != null)
        {
            for (int i = 0; i< kVectors.Length;i++)
            {
                float sinTheta = i * lambda/slitSpacing;
                float thetaRadians = Mathf.Asin(sinTheta);
                if (kVectors[i] != null)
                {
                    kVectors[i].LineLength = arrowLength;
                    kVectors[i].ThetaDegrees = thetaRadians*Mathf.Rad2Deg;
                }
                kEndPoints[i].y = sinTheta * arrowLength;
                kEndPoints[i].x = Mathf.Cos(thetaRadians) * arrowLength;
            }
        }
        if (kComponents != null)
        {
            //Debug.Log("Do Components");
            int limit = kEndPoints.Length-1;
            if (kComponents.Length < limit)
                limit = kComponents.Length;
            for (int j = 0; j < limit; j++)
            {
                //Debug.Log("Do Component["+ j.ToString() + "]");
                if (kComponents[j] != null)
                {
                    kComponents[j].LineLength = kEndPoints[j + 1].y;
                    Vector3 lpos = kComponents[j].transform.localPosition;
                    lpos.x = kEndPoints[j+1].x;
                    kComponents[j].transform.localPosition = lpos;
                }
            }
        }
        needsUpdate = false;
    }
    private void showAngles()
    {
    }

    private void showImpulses()
    {

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
