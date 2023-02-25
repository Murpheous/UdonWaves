
using UdonSharp;
using UnityEngine;
using System;
using Random = UnityEngine.Random;
using VRC.SDKBase;
using VRC.Udon;

public class QuantumScatter : UdonSharpBehaviour
{
    [SerializeField]
    private int _numSlits = 8;
    [SerializeField]
    private float _slitWidth = 0.8f;
    [SerializeField]
    private double _slitPitch = 4.0d;
    [SerializeField]
    private int _xformPoints = 8192;
    [SerializeField]
    private float _transformScale = 0.0016F; // The change in x per transform point for the fourier transform;
    [SerializeField]
    private float _outputScale = 1;
    [SerializeField]
    private int _xformHeight = 512;
    [SerializeField]
    private bool _IsInitialized = false;
    [SerializeField]
    private bool _IsEnabled = true;
    private double[] dCurrentDistribution;
    [SerializeField]
    private double _dCurrentMax;
    private int[] nDistributionLookup;
    [SerializeField]
    private int nDistributionSum = 0;

    public bool EnableScatter
    {
        get { return _IsEnabled; }
        set { _IsEnabled = value; }
    }
    public int NumSlits
    {
        get { return _numSlits; }
    }
    public void SetGratingBySizes(int numSlts, float slitWidth, float barWidth)
    {
        _numSlits = numSlts;
        _slitWidth = slitWidth;
        _slitPitch = slitWidth + barWidth;
        if (_numSlits > 0)
            Recalc();
    }

    public void SetGratingByRatio(int numSlts, float slitWidth, float barRatio)
    {
        Debug.Log("SetGratingByRatio");
        _numSlits = numSlts;
        _slitWidth = slitWidth;
        _slitPitch = slitWidth + (barRatio * slitWidth);
        if (_numSlits > 0)
            Recalc();
    }

    public void SetGratingByPitch(int numSlts, float slitWidth, float slitPitch)
    {
        _numSlits = numSlts;
        _slitWidth = slitWidth;
        _slitPitch = slitPitch;
        if (_numSlits > 0)
            Recalc();
    }

    /* <summary>
 * FilterSpatialScale is the spatial sampling scale used to analyze the slit pattern.
 * if it is large, the momentum distribution is narrow (quantum effect is reduced) 
 * and if small, then the distribution is wider.
 </summary> */
    public float SpatialScale
    {
        get
        {
            return _transformScale;
        }
    }
    public int PointsWide
    {
        get { return _xformPoints; }
        set
        {
            if (_xformPoints != value)
            {
                _xformPoints = value;
                if (_IsInitialized)
                    Recalc();
            }
        }
    }
    public int PointsHigh
    {
        get { return _xformHeight; }
        set
        {
            if (_xformHeight != value)
            {
                _xformHeight = value;
                if (_IsInitialized)
                    Recalc();
            }
        }
    }

    /// Get integer that gives a value inside the integer (digitised) from the humongous array distribution lookups that is the indexes across the 8192 point array
    public int RandomSample
    {
        get
        {
            if (!_IsEnabled || _numSlits <= 0)
                return 0;
            if (!_IsInitialized)
                Recalc();
            int min = (-nDistributionSum) + 1;
            int nRand = Random.Range(min, nDistributionSum);
            if (nRand >= 0)
                return nDistributionLookup[nRand];
            nRand = -nRand;
            return -(nDistributionLookup[nRand]);
        }
    }

    public float RandomImpulse()
    {
        if (!_IsEnabled || _numSlits <= 0)
            return 0.0f;
        float randSample = RandomSample;
        return randSample * _outputScale;
    }

    public Vector3 RandomReaction(float particleSpeed, Vector3 indcidentVelocity)
    {
        int maxLookup = Mathf.FloorToInt(particleSpeed);
        return indcidentVelocity;
    }
    public double[] Distribution
    {
        get
        {
            if ((!_IsInitialized) || (dCurrentDistribution.Length <= 0))
                Recalc();
            return dCurrentDistribution;
        }
    }

    public double currentMax
    {
        get
        {
            if ((!_IsInitialized) || (dCurrentDistribution.Length <= 0))
                Recalc();
            return _dCurrentMax;
        }
    }

    private void Recalc()
    {
        int[] nCurrentDistribution;
        nDistributionSum = 0;
        _IsInitialized = true;
        if (_xformPoints <= 0)
            _xformPoints = 1024;
        if (dCurrentDistribution == null)
            dCurrentDistribution = new double[_xformPoints];
        nCurrentDistribution = new int[_xformPoints];
        dCurrentDistribution.Initialize();
        // Single distribution is 7Pi wide double is 16Pi
        float scaleSingle = (float)((7.0d * Math.PI) / (_slitWidth * _xformPoints));
        float scaleGrating = (float)((18.0d * Math.PI) / (_slitPitch * _xformPoints));
        if ((_numSlits == 1) || (scaleSingle > scaleGrating))
        {
            _transformScale = scaleSingle;
        }
        else
        {
            _transformScale = scaleGrating;
        }
        _outputScale = (float)(_transformScale / Math.PI);
        // Assume momentum spectrum is symmetrical so calculate from zero.
        _dCurrentMax = 0.0;
        for (int q = 0; q < _xformPoints; q++)
        {
            double dSingleslitValue = 1;
            double dX = _transformScale * q;
            if (q != 0)
                dSingleslitValue = Math.Sin(dX * _slitWidth) / (dX * _slitWidth);

            if (_numSlits > 1)
            {
                double dManySlitValue = 1.0d;
                double dSinNqd = Math.Sin(_numSlits * dX * _slitPitch);
                double dSinqd = Math.Sin(dX * _slitPitch);
                if (dSinqd == 0)
                    dManySlitValue = _numSlits;
                else
                    dManySlitValue = dSinNqd / dSinqd;
                dCurrentDistribution[q] = (dSingleslitValue * dSingleslitValue) * (dManySlitValue * dManySlitValue);
            }
            else
            {
                dCurrentDistribution[q] = (dSingleslitValue * dSingleslitValue);
            }
            if (dCurrentDistribution[q] > _dCurrentMax)
                _dCurrentMax = dCurrentDistribution[q];
        }
        // Now Convert Distribution to Integer Distribution;
        double dScale = ((double)_xformHeight) / _dCurrentMax;
        for (int q = 0; q < _xformPoints; q++)
        {
            nCurrentDistribution[q] = (int)(dCurrentDistribution[q] * dScale);
            nDistributionSum += nCurrentDistribution[q];

        }
        // Now make a linear array with the length of each segment equal to the height of probability density function.
        if (nDistributionLookup != null)
        {
            nDistributionLookup = null;
        }
        nDistributionLookup = new int[nDistributionSum];
        int nCtr = 0;
        for (int q = 0; q < _xformPoints; q++)
        {
            for (int p = 0; p < nCurrentDistribution[q]; p++)
            {
                nDistributionLookup[nCtr] = q;
                nCtr++;
            }
        }
    }

    private void Start()
    {
        dCurrentDistribution = new double[_xformPoints];
    }
}
