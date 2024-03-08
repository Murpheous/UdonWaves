
using System;
using UdonSharp;
using UnityEngine;
using Random = UnityEngine.Random;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]

public class QuantumScatter : UdonSharpBehaviour
{
    [SerializeField]
    private int numApertures = 8;
    [SerializeField]
    private float slitWidth = 0.8f;
    [SerializeField]
    private float widthDivLambdaMin;
    [SerializeField]
    Material matSim = null;
    [SerializeField]
    string texName = null;
    [SerializeField]
    bool simMatIsValid = false;

    private bool isStarted = false;
    public bool IsStarted { get => isStarted; }
    public void Touch()
    {
        settingsChanged = true;
    }

    public void DefineTexture(Material theMaterial, string thePropertyName)
    {
        matSim = theMaterial;
        texName = !string.IsNullOrWhiteSpace(thePropertyName) ? thePropertyName : "_MainTex";
        simMatIsValid = (matSim != null);
        //Debug.Log(gameObject.name + " Defined Texture: [" + matSim.name + "]");
        if (simMatIsValid && IsStarted && settingsLoaded)
        {
            CreateTexture();
        }
        else
            settingsLoaded = false;
    } 

    public bool CreateTexture()
    {
        if (!simMatIsValid)
        {
            Debug.LogWarning(gameObject.name + " Create Texture: [No Material]");
            return false;
        }
        var tex = new Texture2D(pointsWide, 1, TextureFormat.RGBAFloat, false);
        Color xColor = Color.clear;
        for (int i = 0; i < pointsWide; i++)
        {
            xColor.r = gratingTransform[i];
            xColor.g = gratingTransform[i];
            xColor.b = reverseLookup[i];
            tex.SetPixel(i, 0, xColor);
        }
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();
        matSim.SetTexture(texName, tex);
        Debug.Log(" Created Texture: [" + texName +"]");
        return true;
    }
    private float SlitWidth
    {
        set 
        {
            settingsChanged |= (slitWidth != value);
            slitWidth = value;
        }
    }

    [SerializeField]
    private float slitPitch = 4.0f;
    private float pitchDivLambdaMin;
    private float SlitPitch 
    { 
        set
        {
            settingsChanged |= (slitPitch != value);
            slitPitch = value;
        }
    }
    [Header("Number of Lookup Points")]
    [SerializeField]
    private int pointsWide = 256;
    [SerializeField]
    private bool settingsChanged = false;
    [SerializeField]
    private bool settingsLoaded = false;
    public bool SettingsLoaded { get => settingsLoaded; }
    [SerializeField]
    private bool gotSettings = false;
    //[SerializeField]
    private float[] gratingTransform;
    //[SerializeField]
    private float[] transformIntegral;
    //[SerializeField] 
    private int[] randomWidths;
    //[SerializeField]
    private float[] reverseLookup;
    //[SerializeField]
    int distributionSegment;
    [SerializeField]
    int distributionRange;
    [SerializeField]
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

    public bool SetGratingByPitch(int slitCount, float slitWidth, float slitPitch, float minLambda)
    {
        Debug.Log(string.Format("{0} SetGratingByPitch: lmin={1} s={2} w={3} p={4}",gameObject.name, lambdaMin, slitCount,slitWidth,slitPitch));
 
        if ((slitCount <= 0) || (slitWidth <= 0))
            return false;

        LambdaMin = minLambda;
        NumApertures = slitCount;
        SlitWidth = slitWidth;
        SlitPitch = slitPitch;
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
            return reverseLookup[nRand];
        return -(reverseLookup[-nRand]);
    }


    public float RandomImpulseFrac(float incidentSpeedFrac)
    {
        if (!settingsLoaded)
            return 0f;
        distributionSegment = pointsWide - 1;
        distributionRange = randomWidths[distributionSegment];
        float resultIndex = SubsetSample(distributionRange);
        float resultFrac = (distributionScale * resultIndex )/ (pointsWide * incidentSpeedFrac);
        return Mathf.Clamp(resultFrac,-1f,1f);
    }


    private void Recalc()
    {
        Debug.Log(gameObject.name + "Recalc");
        if (!gotSettings)
            return;
        if (slitWidth <= 0)
            return;
        if (lambdaMin <= 0)
            return;
        settingsChanged = false;
        // Calculte aperture parameters in terms of width per (min particleSpeed)
        widthDivLambdaMin = slitWidth / lambdaMin;
        pitchDivLambdaMin = slitPitch / lambdaMin;
        Debug.Log(string.Format("{0} apertuere/LambdaMin={1} pitch/LambdaMin={2}",gameObject.name,widthDivLambdaMin, pitchDivLambdaMin));
        // Assume momentum spectrum is symmetrical so calculate from zero.
        float integralSum = 0f;
        float scaleTheta = Mathf.PI;
        float singleSlitValueSq;
        float manySlitValue;
        float sinQd;
        float dX;
        float thisValue;
        transformIntegral[0] = 0;
        float thetaMaxSingle = Mathf.Asin((7.0f * lambdaMin/slitWidth));
        if (thetaMaxSingle < Mathf.PI)
            scaleTheta = thetaMaxSingle;

        simulationTheta = scaleTheta;
        distributionScale = simulationTheta / (Mathf.PI);

        for (int nPoint = 0; nPoint < pointsWide; nPoint++)
        {
            singleSlitValueSq = 1;
            dX = (scaleTheta * nPoint) / pointsWide;
            if (nPoint != 0)
            {
                float ssTheta = dX * widthDivLambdaMin;
                singleSlitValueSq = Mathf.Sin(ssTheta) / ssTheta;
                singleSlitValueSq *= singleSlitValueSq;
            }
            thisValue = singleSlitValueSq;
            if (numApertures > 1)
            {
                sinQd = Mathf.Sin(dX * pitchDivLambdaMin);
                if (sinQd == 0)
                    manySlitValue = numApertures;
                else
                    manySlitValue = Mathf.Sin(numApertures * dX * pitchDivLambdaMin) / sinQd; 
                thisValue = singleSlitValueSq * (manySlitValue * manySlitValue);
            }
            integralSum += thisValue;
            gratingTransform[nPoint] = thisValue;
            transformIntegral[nPoint+1] = integralSum;
        }
        // Now Convert Distribution to a Normalized Distribution 0 to pointsWide;
        float normScale = pointsWide / integralSum;
        for (int nPoint = 0; nPoint <= pointsWide; nPoint++)
        {
            transformIntegral[nPoint] *= normScale;
            gratingTransform[nPoint] *= normScale;
        }

        // Now invert the table.
        int indexAbove = 0;
        int indexBelow;
        float vmin;
        float vmax = transformIntegral[0];
        float frac;
        for (int i = 0; i <= pointsWide; i++)
        {
            randomWidths[i] = Mathf.Clamp(Mathf.RoundToInt(transformIntegral[i]),0,pointsWide);
            // Move VMax up until > than i)
            while ((vmax <= i) && (indexAbove < pointsWide - 1))
            {
                indexAbove++;
                vmax = transformIntegral[indexAbove];
            }
            vmin = vmax; indexBelow = indexAbove;
            while ((indexBelow > 0) && (vmin > i))
            {
                indexBelow--;
                vmin = transformIntegral[indexBelow];
            }
            if (indexBelow >= indexAbove)
                reverseLookup[i] = vmax;
            else
            {
                frac = Mathf.InverseLerp(vmin, vmax, i);
                reverseLookup[i] = Mathf.Lerp(indexBelow,indexAbove,frac);
            }
        }
        if (simMatIsValid) 
            CreateTexture();
        settingsLoaded = true;
        //Debug.Log(gameObject.name + ": Recalc Done");
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
        }
    }

    private void Start()
    {
        randomWidths = new int[pointsWide+1];
        gratingTransform = new float[pointsWide + 1];
        transformIntegral = new float[pointsWide+1];
        reverseLookup = new float[pointsWide+1];
        isStarted = true;
    }
}
