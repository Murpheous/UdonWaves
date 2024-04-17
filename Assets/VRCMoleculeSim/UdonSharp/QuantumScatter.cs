
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
            xColor.r = gratingFourierSq[i];
            xColor.g = probIntegral[i];
            xColor.b = probabilityLookup[i];
            tex.SetPixel(i, 0, xColor);
        }
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();
        matSim.SetTexture(texName, tex);
       // Debug.Log(" Created Texture: [" + texName +"]");
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
    //[SerializeField]
    private bool settingsChanged = false;
    //[SerializeField]
    private bool settingsLoaded = false;
    public bool SettingsLoaded { get => settingsLoaded; }
    //[SerializeField]
    private bool gotSettings = false;
    //[SerializeField]
    private float[] gratingFourierSq;
   // [SerializeField]
    private float[] probIntegral;
    //[SerializeField]
    private float[] probabilityLookup;
    //[SerializeField]
    int distributionSegment;
    //[SerializeField]
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
        //Debug.Log(string.Format("{0} SetGratingByPitch: lmin={1} s={2} w={3} p={4}",gameObject.name, lambdaMin, slitCount,slitWidth,slitPitch));
 
        if ((slitCount <= 0) || (slitWidth <= 0))
            return false;

        LambdaMin = minLambda;
        NumApertures = slitCount;
        SlitWidth = slitWidth;
        SlitPitch = slitPitch;
        gotSettings = true;
        return true;
    }

    float SubsetSample(int DistributionRange)
    {
        if (!settingsLoaded)
            return 0;
        if (DistributionRange > pointsWide)
            DistributionRange = pointsWide;
        int min = (-DistributionRange) + 1;
        int nRand = Random.Range(min, DistributionRange);
        if (nRand >= 0)
            return probabilityLookup[nRand];
        return -(probabilityLookup[-nRand]);
    }


    public float RandomImpulseFrac(float incidentSpeedFrac)
    {
        if (!settingsLoaded)
            return 0f;
        distributionSegment = Mathf.RoundToInt((pointsWide - 1)*incidentSpeedFrac);
        distributionRange = (int)Mathf.Round(probIntegral[distributionSegment]);
        float resultIndex = SubsetSample(distributionRange);
        float resultFrac = (distributionScale * resultIndex )/ (pointsWide * incidentSpeedFrac);
        return Mathf.Clamp(resultFrac,-1f,1f);
    }


    private void Recalc()
    {
        //Debug.Log(gameObject.name + "Recalc");
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
        //Debug.Log(string.Format("{0} apertuere/LambdaMin={1} pitch/LambdaMin={2}",gameObject.name,widthDivLambdaMin, pitchDivLambdaMin));
        // Assume momentum spectrum is symmetrical so calculate from zero.
        float probIntegralSum = 0f;
        float scaleTheta = Mathf.PI;
        float singleSlitValueSq;
        float manySlitValue;
        float sinQd;
        float dX;
        float thisValue;
        probIntegral[0] = 0;
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
            gratingFourierSq[nPoint] = thisValue;
            probIntegral[nPoint] = probIntegralSum;
            probIntegralSum += thisValue;
        }
        probIntegral[pointsWide] = probIntegralSum;
        // Now Normalize the Integral from 0 to pointsWide;
        float normScale = (pointsWide-1) / probIntegral[pointsWide-1];
        for (int nPoint = 0; nPoint <= pointsWide; nPoint++)
            probIntegral[nPoint] *= normScale;

        // Now invert the table.
        int indexAbove = 0;
        int indexBelow;
        float vmin;
        float vmax = 0;
        float frac;
        float val;
        int lim = pointsWide - 1;
        for (int i = 0; i <= lim; i++)
        {
            // Move VMax up until > than i)
            while ((vmax <= i) && (indexAbove <= lim))
            {
                indexAbove++;
                vmax = probIntegral[indexAbove];
            }
            vmin = vmax; indexBelow = indexAbove;
            while ((indexBelow > 0) && (vmin > i))
            {
                indexBelow--;
                vmin = probIntegral[indexBelow];
            }
            if (indexBelow >= indexAbove)
                val = vmax;
            else
            {
                frac = Mathf.InverseLerp(vmin, vmax, i);
                val = Mathf.Lerp(indexBelow,indexAbove,frac);
            }
            probabilityLookup[i] = val; // * norm;
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
        gratingFourierSq = new float[pointsWide];
        probIntegral = new float[pointsWide+1];
        probabilityLookup = new float[pointsWide];
        isStarted = true;
    }
}
