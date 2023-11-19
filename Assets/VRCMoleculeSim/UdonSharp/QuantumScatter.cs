
using UdonSharp;
using UnityEngine;
using Random = UnityEngine.Random;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)] // No networking.

public class QuantumScatter : UdonSharpBehaviour
{
    [SerializeField]
    private int numApertures = 8;
    [SerializeField]
    private float apertureWidth = 0.8f;
    [SerializeField]
    private float apertureLambda;
    private bool started = false;
    public bool Started { get => started;  }
    
    private float ApertureWidth
    {
        set 
        {
            settingsChanged |= (apertureWidth != value);
            apertureWidth = value;
        }
    }

    [SerializeField]
    private float aperturePitch = 4.0f;
    private float pitchLamba;
    private float AperturePitch 
    { 
        set
        {
            settingsChanged |= (aperturePitch != value);
            aperturePitch = value;
        }
    }
    [Header("Number of Lookup Points")]
    [SerializeField]
    private int pointsWide = 256;
    [SerializeField]
    private bool settingsChanged = false;
    [SerializeField]
    private bool settingsLoaded = false;
    [SerializeField]
    private bool gotSettings = false;
    //[SerializeField]
    private float[] currentIntegral;
    //[SerializeField] 
    private int[] randomWidths;
    //[SerializeField]
    private float[] inverseDistribution;
    //[SerializeField]
    int distributionSegment;
    [SerializeField]
    int distributionRange;
    [SerializeField,Range(0.001f,1f)]
    private float lambdaMin = 1;
    [SerializeField]
    private float simulationTheta = 1;
   // [SerializeField]
   // private float impulseScale = Mathf.PI / 1024f;
    [SerializeField]
    private float distributionScale = 1f;

    public float LambdaMin 
    { 
        get => lambdaMin;
        set
        {
            if (value == 0)
                return;
            value = Mathf.Abs(value);
            settingsChanged |= (value != lambdaMin);
            lambdaMin = value;
        } 
    }

    public int NumApertures
    {
        get => numApertures;
        set
        {
            settingsChanged |= (numApertures != value);
            numApertures = value;
        }
    }

    public bool SetGratingByPitch(int apertureCount, float slitWidth, float slitPitch, float lambdaMin)
    {
        Debug.Log(string.Format("{0} SetGratingByPitch: lmin={1} s={2} w={3} p={4}",gameObject.name, lambdaMin, apertureCount,slitWidth,slitPitch));
 
        if ((apertureCount <= 0) || (slitWidth <= 0))
            return false;

        LambdaMin = lambdaMin;
        NumApertures = apertureCount;
        ApertureWidth = slitWidth;
        AperturePitch = slitPitch;
        gotSettings = true;
        return true;
    }



    /// Get integer that gives a value inside the integer (digitised) from the humongous array distribution lookups that is the indexes across the 8192 point array
    float SubsetSample(int DistributionRange)
    {
        if (!settingsLoaded)
            return 0;
        if (DistributionRange > pointsWide)
            DistributionRange = pointsWide;
        int min = (-DistributionRange) + 1;
        int nRand = Random.Range(min, DistributionRange);
        if (nRand >= 0)
            return inverseDistribution[nRand];
        return -(inverseDistribution[-nRand]);
    }

    
    /*public float RandomImpulse()
    {
        if (!settingsUpdated)
            return 0f;
        float randSample = SubsetSample(pointsWide);
        return randSample;
    }*/

    public float RandomImpulseFrac(float incidentSpeedFrac)
    {
        if (!settingsLoaded)
            return 0f;
        distributionSegment = pointsWide - 1;
        //if ()
        //distributionSegment = (int)(incidentSpeedFrac*(pointsWide-1));
        distributionRange = randomWidths[distributionSegment];
        float resultIndex = SubsetSample(distributionRange);
        //float resultF = resultIndex - Mathf.Sign(resultIndex);
        float resultFrac = (distributionScale * resultIndex )/ (pointsWide * incidentSpeedFrac);
        return Mathf.Clamp(resultFrac,-1f,1f);
    }


    private void Recalc()
    {
        Debug.Log("Recalc");
        if (!gotSettings)
            return;
        if (apertureWidth <= 0)
            return;
        if (lambdaMin <= 0)
            return;
        settingsChanged = false;
        // Calculte aperture parameters in terms of width per (min lambda)
        apertureLambda = apertureWidth / lambdaMin;
        pitchLamba = aperturePitch / lambdaMin;
        Debug.Log(string.Format("apertureLambda={0} pitchLambda={1}",apertureLambda, pitchLamba));
        // Assume momentum spectrum is symmetrical so calculate from zero.
        float integralSum = 0f;
        float scaleTheta = Mathf.PI;
        float singleSlitValue;
        float manySlitValue;
        float dSinqd;
        float dX;
        float thisValue;
        currentIntegral[0] = 0;
        float thetaMaxSingle = Mathf.Asin((7.0f * lambdaMin/apertureWidth));
        //float scaleGrating = (float)((18.0f * Mathf.PI) / (aperturePitch * pointsWide));
        //if ((numApertures == 1) || (scaleSingle > scaleGrating))
        {
            if (thetaMaxSingle < Mathf.PI)
                scaleTheta = thetaMaxSingle;
        }
        simulationTheta = scaleTheta;
        distributionScale = simulationTheta / (Mathf.PI);

        for (int nPoint = 0; nPoint < pointsWide; nPoint++)
        {
            singleSlitValue = 1;
            dX = (scaleTheta * nPoint) / pointsWide; 
            if (nPoint != 0)
                singleSlitValue = Mathf.Sin(dX * apertureLambda) / (dX * apertureLambda);

            if (numApertures > 1)
            {
                dSinqd = Mathf.Sin(dX * pitchLamba);
                if (dSinqd == 0)
                    manySlitValue = numApertures;
                else
                    manySlitValue = Mathf.Sin(numApertures * dX * pitchLamba) / dSinqd; 
                thisValue = (singleSlitValue * singleSlitValue) * (manySlitValue * manySlitValue);
            }
            else
            {
                thisValue = (singleSlitValue * singleSlitValue);
            }
            integralSum += thisValue;
            currentIntegral[nPoint+1] = integralSum;
        }
        // Now Convert Distribution to a Normalized Distribution 0 to pointsWide;
        float normScale = pointsWide / integralSum;
        for (int nPoint = 0; nPoint <= pointsWide; nPoint++)
            currentIntegral[nPoint] = currentIntegral[nPoint] * normScale;
        Debug.Log(string.Format("integralSum={0} normScale={1}", integralSum, normScale));

        // Now invert the table.
        int indexAbove = 0;
        int indexBelow;
        float vmin;
        float vmax = currentIntegral[0];
        float frac;
        for (int i = 0; i <= pointsWide; i++)
        {
            randomWidths[i] = Mathf.Clamp(Mathf.RoundToInt(currentIntegral[i]),0,pointsWide);
            // Move VMax up until > than i)
            while ((vmax <= i) && (indexAbove < pointsWide - 1))
            {
                indexAbove++;
                vmax = currentIntegral[indexAbove];
            }
            vmin = vmax; indexBelow = indexAbove;
            while ((indexBelow > 0) && (vmin > i))
            {
                indexBelow--;
                vmin = currentIntegral[indexBelow];
            }
            if (indexBelow >= indexAbove)
                inverseDistribution[i] = vmax;
            else
            {
                frac = Mathf.InverseLerp(vmin, vmax, i);
                inverseDistribution[i] = Mathf.Lerp(indexBelow,indexAbove,frac);
            }
        }
        settingsLoaded = true;
        Debug.Log("Recalc Done");
    }
    float nextTick = 2;

    private void Update()
    {
        nextTick -= Time.deltaTime;
        if (nextTick < 0)
        {
            nextTick = 1;
            if (gotSettings && (settingsChanged || (!settingsLoaded)))
                Recalc();
            //else
            //    Debug.Log("QS NoChange");
        }
    }

    private void Start()
    {
        randomWidths = new int[pointsWide+1];
        currentIntegral = new float[pointsWide+1];
        inverseDistribution = new float[pointsWide+1];
        started = true;
    }
}
