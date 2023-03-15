
using UdonSharp;
using UnityEngine;
using Random = UnityEngine.Random;
using VRC.SDKBase;
using VRC.Udon;
using Newtonsoft.Json.Linq;

public class QuantumScatter : UdonSharpBehaviour
{
    [SerializeField]
    private int numApertures = 8;
    [SerializeField]
    private float apertureWidth = 0.8f;
    [SerializeField]
    private float apertureLambda;
    
    public float ApertureWidth
    {
        get => apertureWidth;
        set 
        {
            if (apertureWidth != value)
                isInitialized = false;
            apertureWidth = value;
        }
    }

    [SerializeField]
    private float aperturePitch = 4.0f;
    private float pitchLamba;
    public float AperturePitch 
    { 
        get => aperturePitch;
        set
        {
            if (aperturePitch != value)
                isInitialized = false;
            aperturePitch = value;
        }
    }
    [Header("Number of Lookup Points")]
    [SerializeField]
    private int pointsWide = 256;
    [SerializeField]
    private bool isInitialized = false;
    private bool gotSettings = false;
    //[SerializeField]
    private float[] currentIntegral;
    //[SerializeField] 
    private int[] randomWidths;
    //[SerializeField]
    private float[] inverseDistribution;
    [SerializeField]
    int distributionSegment;
    [SerializeField]
    int distributionRange;
    [SerializeField,Range(0.001f,1f)]
    private float lambdaMin = 1;

    public float LambdaMin 
    { 
        get => lambdaMin; 
        set
        {
            if (value == 0)
                return;
            value = Mathf.Abs(value);
            if (value != lambdaMin)
                isInitialized = false;
            lambdaMin = value;
        } 
    }

    public int NumApertures
    {
        get => numApertures;
        set
        {
            if (numApertures != value)
                isInitialized = false;
            numApertures = value;
        }
    }

    public bool SetGratingByPitch(int apertureCount, float slitWidth, float slitPitch, float lambdaMinInitial)
    {
        Debug.Log(string.Format("SetGrating: lmin={0} s={1} w={2} p={3}", lambdaMinInitial, apertureCount,slitWidth,slitPitch));
        if ((lambdaMinInitial > 100) || (apertureCount <= 0) || (slitWidth <= 0))
            return false;
        LambdaMin = lambdaMinInitial;
        NumApertures = apertureCount;
        ApertureWidth = slitWidth;
        AperturePitch = slitPitch;
        gotSettings = true;
        isInitialized = false;
        return true;
    }



    /// Get integer that gives a value inside the integer (digitised) from the humongous array distribution lookups that is the indexes across the 8192 point array
    float SubsetSample(int DistributionRange)
    {
        if (!isInitialized)
            return 0;
        if (DistributionRange > pointsWide)
            DistributionRange = pointsWide;
        int min = (-DistributionRange) + 1;
        int nRand = Random.Range(min, DistributionRange);
        if (nRand >= 0)
            return inverseDistribution[nRand];
        return -(inverseDistribution[-nRand]);
    }

    
    public float RandomImpulse()
    {
        if (!isInitialized)
            return 0f;
        float randSample = SubsetSample(pointsWide);
        return randSample/pointsWide;
    }

    public float RandomImpulseFrac(float incidentSpeedFrac)
    {
        if (!isInitialized)
            return 0f;
        distributionSegment = (int)(incidentSpeedFrac*(pointsWide-1));
        distributionRange = randomWidths[distributionSegment];
        float resultIndex = SubsetSample(distributionRange);
        //float resultF = resultIndex - Mathf.Sign(resultIndex);
        return resultIndex/(distributionSegment + 1);
    }



    private void Recalc()
    {
        Debug.Log("Recalc");
        if (!gotSettings)
            return;
        isInitialized = true;
        // Calculte aperture parameters in terms of width per (min lambda)
        if (lambdaMin == 0)
            lambdaMin = apertureWidth;
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
        Debug.Log("Recalc Done");
    }
    float nextTick = 2;
    private void Update()
    {
        nextTick -= Time.deltaTime;
        if (nextTick < 0)
        {
            nextTick = 1;
            if (gotSettings && (!isInitialized))
                Recalc();
        }
    }

    private void Start()
    {
        randomWidths = new int[pointsWide+1];
        currentIntegral = new float[pointsWide+1];
        inverseDistribution = new float[pointsWide+1];
        isInitialized = false;
    }
}
