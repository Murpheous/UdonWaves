
using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class particleSim : UdonSharpBehaviour
{
    [SerializeField] private ParticleSystem particleEmitter;
    [SerializeField] private WaveSlitControls apertureControl;
    [SerializeField] private WaveMonitor waveControl;
    [SerializeField] private QuantumScatter quantumDistribution;

    private Transform sourceXfrm;
    private Transform apertureXfrm;

    private Color sourceColour = Color.gray;
    [Header("Frequency")]
    private float frequency = 1.5f;
    [SerializeField]
    private float frequencyMax = 2f;
    [SerializeField]
    private float frequencyMin = 1.0f;
    [SerializeField]
    private float waveSpeed = 1.0f;
    [SerializeField]
    private float lambdaMin = 1.0f;
    [SerializeField]
    private float freqencyFrac = 1f;
    // Only works if particles travel in decreasing X direction
    [Header("Particle Start Conditions")]
    public float apertureX = -100;
    [SerializeField]
    public float averageSpeed = 0.5f;
    [SerializeField]
    private float particleSpeed = 1;

    /* Functions from Duane VR for getting QM speckle dots */
    /*
    float randomSample()
    {
        if (cumulativeDistribution == null)
            return 0;
        if (cumulativeDistribution.Length <= 0)
            return 0;
        int nRand = UnityEngine.Random.Range(0, cumulativeDistribution.Length);
        if (UnityEngine.Random.Range(0, 2) == 1)
            return cumulativeDistribution[nRand];
        else
            return -cumulativeDistribution[nRand];
    }

    */
    public Color lerpColour(float frac)
    {
        return spectrumColour(Mathf.Lerp(700, 400, frac));
    }

    public Color spectrumColour(float wavelength, float gamma = 0.8f)
    {
        Color result = Color.white;
        if (wavelength >= 380 & wavelength <= 440)
        {
            float attenuation = 0.3f + 0.7f * (wavelength - 380.0f) / (440.0f - 380.0f);
            result.r = Mathf.Pow(((-(wavelength - 440) / (440 - 380)) * attenuation), gamma);
            result.g = 0.0f;
            result.b = Mathf.Pow((1.0f * attenuation), gamma);
        }

        else if (wavelength >= 440 & wavelength <= 490)
        {
            result.r = 0.0f;
            result.g = Mathf.Pow((wavelength - 440f) / (490f - 440f), gamma);
            result.b = 1.0f;
        }
        else if (wavelength >= 490 & wavelength <= 510)
        {
            result.r = 0.0f;
            result.g = 1.0f;
            result.b = Mathf.Pow(-(wavelength - 510f) / (510f - 490f), gamma);
        }
        else if (wavelength >= 510 & wavelength <= 580)
        {
            result.r = Mathf.Pow((wavelength - 510f) / (580f - 510f), gamma);
            result.g = 1.0f;
            result.b = 0.0f;
        }
        else if (wavelength >= 580f & wavelength <= 645f)
        {
            result.r = 1.0f;
            result.g = Mathf.Pow(-(wavelength - 645f) / (645f - 580f), gamma);
            result.b = 0.0f;
        }
        else if (wavelength >= 645 & wavelength <= 750)
        {
            float attenuation = 0.3f + 0.7f * (750 - wavelength) / (750 - 645);
            result.r = Mathf.Pow(1.0f * attenuation, gamma);
            result.g = 0.0f;
            result.b = 0.0f;
        }
        else
        {
            result.r = 0.0f;
            result.g = 0.0f;
            result.b = 0.0f;
            result.a = 0.1f;
        }
        return result;
    }

    private int currentSlitCount = 0;
    private float currentSlitWidth = 0;
    private float currentSlitPitch = 0;
    private float currentGratingWidth = -1;
    private void checkSourceDimensions()
    {
        if (apertureControl == null)
            return;
        if (apertureControl.GratingWidth != currentGratingWidth)
        {
            currentGratingWidth = apertureControl.GratingWidth;
            if (particleEmitter == null)
                return;
            var shapeModule = particleEmitter.shape;
            if (shapeModule.shapeType == ParticleSystemShapeType.Box)
            {
                Vector3 shapeScale = shapeModule.scale;
                shapeScale.x = currentGratingWidth * 1.1f;
                shapeModule.scale = shapeScale;
            }
        }
        if ((apertureControl.ApertureCount != currentSlitCount) ||
            (apertureControl.ApertureWidth != currentSlitWidth) ||
            (apertureControl.AperturePitch != currentSlitPitch))
        {
            currentSlitPitch = apertureControl.AperturePitch;
            currentSlitWidth = apertureControl.ApertureWidth;
            currentSlitCount = apertureControl.ApertureCount;
            //generateSpeckleDistribution();
            
            if (quantumDistribution != null)
            {
                quantumDistribution.SetGratingByPitch(currentSlitCount, currentSlitWidth, currentSlitPitch,lambdaMin);
                quantumDistribution.EnableScatter = true;
            } 

        }
    }
    /*
    private float[] cumulativeDistribution;
    float[] fExpectPlot;
    long[] nExpectPlot;
    private void generateSpeckleDistribution()
    {
        if (apertureControl == null)
            return;

        // float lambdaNanoMetres = 1; // (QM.nmRatioToEv / photonEV);
        int resolutionAcross = 512; // _speckleDisplay.PixelsAcross;
        int xFormHeight = 256;

        // Fist calculate the horizontal distribution
        // Horizontal Distruibution note that because of symmetry, only half of the display width is required
        int numPointsSpeckleX = (resolutionAcross / 2) + 1;
        if (fExpectPlot == null)
            fExpectPlot = new float[numPointsSpeckleX];
        fExpectPlot[0] = 1;
        if (nExpectPlot == null)
            nExpectPlot = new long[numPointsSpeckleX];
        long nExpectPlotSum = 0;
        int nCumulativeIndex = 0;

        //calcExpectFourierP(ref fExpectPlot, lambda, screenDistance, _speckleDisplay.targetAreaWidth, currentSlitCount, currentSlitPitch, currentSlitWidth);

            // Now Convert Distribution to Integer Distribution;
        for (int q = 0; q < numPointsSpeckleX; q++)
        {
            //try
            {
                nExpectPlot[q] = Convert.ToInt64(fExpectPlot[q] * xFormHeight);
                nExpectPlotSum += nExpectPlot[q];
            }
            //catch
            //{
            //    nExpectPlot[q] = 0;
            //}

        }
        cumulativeDistribution = new float[nExpectPlotSum];
        for (int q = 0; q < numPointsSpeckleX; q++)
        {
            for (int p = 0; p < nExpectPlot[q]; p++)
            {
            cumulativeDistribution[nCumulativeIndex] = q; // / pixelsPerMetreDisplay;
                nCumulativeIndex++;
            }
        }
    }
    */

    private void LateUpdate()
    {
        if (apertureX <= 0)
            return;
        if (particleEmitter != null)
        {
            var numParticles = particleEmitter.particleCount;
            var nUpdated = 0;
            var particles = new ParticleSystem.Particle[numParticles];
            numParticles = particleEmitter.GetParticles(particles);
            for (int i=0; i<numParticles; i++)
            {
                Vector3 pos = particles[i].position;
                if (Mathf.Abs(pos.z) > 1) 
                {
                    particles[i].remainingLifetime = 0;
                    nUpdated++;
                }
                else if ((particles[i].startLifetime < 10) && (particles[i].position.x < apertureX))
                {// At Grating
                    nUpdated++;
                    particles[i].startLifetime = 10f;
                    particles[i].remainingLifetime = 40f;
                    float freq = particles[i].rotation;
                    if (quantumDistribution !=null)
                    {
                        
						Vector3 vUpdated;
						vUpdated = particles[i].velocity;

                        if (quantumDistribution.EnableScatter)
                        {
                            Vector3 unit = Vector3.right;
                            unit.z = (quantumDistribution.RandomImpulseFrac(freqencyFrac)); // * planckValue);
                            unit.x = -Mathf.Sqrt(1 - (unit.z * unit.z));
                            vUpdated = unit * particleSpeed;
                        }
                        particles[i].velocity = vUpdated;   
                    }
                    // Set Velocity
                    nUpdated++;
                }
            }
            if (nUpdated > 0)
            {
                particleEmitter.SetParticles(particles, numParticles);
            }
        }
    }

    private void initialize()
    {
        if (waveControl == null)
            return;
        frequencyMax = waveControl.MaxFrequency;
        frequencyMin = waveControl.MinFrequency;
        waveSpeed = waveControl.WaveSpeed;
        lambdaMin = waveSpeed / frequencyMax;
        if (quantumDistribution != null)
        {
            quantumDistribution.LambdaMin = lambdaMin;
        }
    }
    public void checkFrequency()
    {
        if (waveControl == null)
            return;
        if (frequency != waveControl.Frequency) 
        {
            waveSpeed = waveControl.WaveSpeed;
            frequency = waveControl.Frequency;
            freqencyFrac = Mathf.Clamp(frequency /frequencyMax,0f,1f);
            float rangeFrac = (frequency - frequencyMin) / (frequencyMax - frequencyMin);
            sourceColour = lerpColour(rangeFrac);
            float speedFrac = 2*frequency/frequencyMax;
            var main = particleEmitter.main;
            if (particleEmitter != null)
            {
                main.startColor = sourceColour * 1.2f;
                particleSpeed = averageSpeed * speedFrac;
                main.startSpeed = particleSpeed;
            }
        }
    }

    private void Update()
    {
        checkSourceDimensions();
        checkFrequency();
    }
    void Start()
    {
        if (particleEmitter != null)
        {
            sourceXfrm = particleEmitter.transform;
            var main = particleEmitter.main;
            averageSpeed = main.startSpeed.constant;
        }
        if (apertureControl != null) 
            apertureXfrm= apertureControl.transform;
        if ((sourceXfrm != null) && (apertureXfrm != null))
            apertureX = apertureXfrm.position.x;
        initialize();
    }
}
