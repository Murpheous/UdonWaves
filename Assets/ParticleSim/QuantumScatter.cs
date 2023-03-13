
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

    [SerializeField]
    private int pointsWide = 1024;
    public int PointsWide
    {
        get => pointsWide;
        set
        {
            if (pointsWide != value)
                isInitialized = false;
            pointsWide = value;
        }
    }

    [SerializeField]
    private int pointsHigh = 512;
    public int PointsHigh
    {
        get { return pointsHigh; }
        set
        {
            if (pointsHigh != value)
            {
                if (pointsHigh != value)
                    isInitialized = false;
                pointsHigh = value;
            }
        }
    }
    [SerializeField]
    private bool isInitialized = false;
    [SerializeField]
    private bool isEnabled = true;
    private float[] currentDistribution;
    //[SerializeField] 
    private int[] randomWidths;
    [SerializeField]
    int distributionSegment;
    [SerializeField]
    int distributionRange;
    [SerializeField]
    private float currentMax;
    private int[] nDistributionLookup;
    [SerializeField]
    private int nDistributionSum = 0;
    [SerializeField,Range(0.001f,1f)]
    private float lambdaMin = 1;
    [SerializeField]
    private float spatialFrequencyMax = 1;
    [SerializeField]
    private float outputScale = 1;

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

    public bool EnableScatter
    {
        get { return isEnabled; }
        set { isEnabled = value; }
    }
    public int NumSlits
    {
        get => numApertures;
        set
        {
            if (numApertures != value)
                isInitialized = false;
            numApertures = value;
        }
    }
    /*
    public void SetGratingBySizes(int numSlts, float slitWidth, float barWidth)
    {
        numApertures = numSlts;
        apertureWidth = slitWidth;
        aperturePitch = slitWidth + barWidth;
        if (numApertures > 0)
            Recalc();
    }

    public void SetGratingByRatio(int numSlts, float slitWidth, float barRatio)
    {
        Debug.Log("SetGratingByRatio");
        numApertures = numSlts;
        apertureWidth = slitWidth;
        aperturePitch = slitWidth + (barRatio * slitWidth);
        if (numApertures > 0)
            Recalc();
    }
    */
    public void SetGratingByPitch(int numSlts, float slitWidth, float slitPitch, float lambadMin)
    {
        LambdaMin = lambdaMin;
        NumSlits = numSlts;
        ApertureWidth = slitWidth;
        AperturePitch = slitPitch;
        if (numApertures > 0)
            Recalc();
    }


    /// Get integer that gives a value inside the integer (digitised) from the humongous array distribution lookups that is the indexes across the 8192 point array
    int SubsetSample(int DistributionRange)
    {
        if (!isInitialized)
            Recalc();
        if (DistributionRange > nDistributionSum)
            DistributionRange = nDistributionSum;
        int min = (-DistributionRange) + 1;
        int nRand = Random.Range(min, DistributionRange);
        if (nRand >= 0)
            return nDistributionLookup[nRand];
        nRand = -nRand;
        return -(nDistributionLookup[nRand]);
    }

    
    public float RandomImpulse()
    {
        if (numApertures <= 0)
            return 0f;
        float randSample = SubsetSample(nDistributionSum);
        return randSample * outputScale;
    }

    public float RandomImpulseFrac(float incidentSpeedFrac)
    {
        if (numApertures <= 0)
            return 0f;
        distributionSegment = (int)(incidentSpeedFrac*(pointsWide-1));
        distributionRange = randomWidths[distributionSegment];
        float resultIndex = SubsetSample(distributionRange);
        return resultIndex/pointsWide;
    }

    public float[] Distribution
    {
        get
        {
            if (!isInitialized)
                Recalc();
            return currentDistribution;
        }
    }

    public float CurrentMax
    {
        get
        {
            if (!isInitialized)
                Recalc();
            return currentMax;
        }
    }

    private void Recalc()
    {
        int[] nCurrentDistribution;
        nDistributionSum = 0;
        isInitialized = true;
        if (pointsWide <= 0)
            pointsWide = 1024;
        if (currentDistribution == null)
            currentDistribution = new float[pointsWide];
        // Calculte aperture parameters in terms of width per (min lambda)
        if (lambdaMin == 0)
            lambdaMin = apertureWidth;
        apertureLambda = apertureWidth / lambdaMin;
        pitchLamba = aperturePitch / lambdaMin;
        nCurrentDistribution = new int[pointsWide];
        currentDistribution.Initialize();
        spatialFrequencyMax = 1 / lambdaMin;
        outputScale = spatialFrequencyMax/pointsWide;
        // Assume momentum spectrum is symmetrical so calculate from zero.
        currentMax = 0f;
        float tau = Mathf.PI * 2;
        for (int nPoint = 0; nPoint < pointsWide; nPoint++)
        {
            float singleSlitValue = 1;
            float dX = (tau * nPoint) / pointsWide; 
            if (nPoint != 0)
                singleSlitValue = Mathf.Sin(dX * apertureLambda) / (dX * apertureLambda);

            if (numApertures > 1)
            {
                float manySlitValue = 1.0f;
                float dSinqd = Mathf.Sin(dX * aperturePitch);
                if (dSinqd == 0)
                    manySlitValue = numApertures;
                else
                    manySlitValue = Mathf.Sin(numApertures * dX * aperturePitch) / dSinqd; 
                currentDistribution[nPoint] = (singleSlitValue * singleSlitValue) * (manySlitValue * manySlitValue);
            }
            else
            {
                currentDistribution[nPoint] = (singleSlitValue * singleSlitValue);
            }
            if (currentDistribution[nPoint] > currentMax)
                currentMax = currentDistribution[nPoint];
        }
        // Now Convert Distribution to Integer Distribution;
        float dScale = pointsHigh / currentMax;
        for (int nPoint = 0; nPoint < pointsWide; nPoint++)
        {
            nCurrentDistribution[nPoint] = (int)(currentDistribution[nPoint] * dScale);
            nDistributionSum += nCurrentDistribution[nPoint];

        }
        // Now make a linear array with the length of each segment equal to the height of probability density function.
        if (nDistributionLookup != null)
            nDistributionLookup = null;
        nDistributionLookup = new int[nDistributionSum];
        randomWidths = new int[pointsWide];
        int nCtr = 0;
        for (int nPoint = 0; nPoint < pointsWide; nPoint++)
        {
            randomWidths[nPoint] = nCtr;
            for (int p = 0; p < nCurrentDistribution[nPoint]; p++)
            {
                nDistributionLookup[nCtr] = nPoint;
                nCtr++;
            }
        }
    }

    private void Start()
    {
        Recalc();
    }
}
