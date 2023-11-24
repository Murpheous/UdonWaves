﻿
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class particleSim : UdonSharpBehaviour
{
    [SerializeField] private ParticleSystem particleEmitter;
    [SerializeField] private WaveSlitControls apertureControl;
    [SerializeField] private WaveMonitor waveControl;
    [SerializeField] private QuantumScatter quantumDistribution;

    [Header("UI Components")]
    [SerializeField]
    private Toggle togglePlay;
    [SerializeField]
    private Toggle toggleStop;
    [SerializeField]
    private Toggle toggleReset;
    [SerializeField,UdonSynced,FieldChangeCallback(nameof(ParticlesPlaying))]
    private bool particlesPlaying;

    private Transform sourceXfrm;
    private Transform apertureXfrm;

    private Color sourceColour = Color.gray;
    [Header("Frequency Parameters")]
    [SerializeField]
    private float frequency = 1.5f;
    [SerializeField]
    private float frequencyMax = 2f;
    [SerializeField]
    private float frequencyMin = 1.0f;
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
    private bool isStarted = false;
    private bool hasParticles = false;
    private bool iamOwner;
    private VRC.Udon.Common.Interfaces.NetworkEventTarget toTheOwner = VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner;
    private VRC.Udon.Common.Interfaces.NetworkEventTarget toAll = VRC.Udon.Common.Interfaces.NetworkEventTarget.All;

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
        if (!isStarted || (apertureControl == null))
            return;
        if (apertureControl.GratingWidth != currentGratingWidth)
        {
            currentGratingWidth = apertureControl.GratingWidth;
            if (!hasParticles)
                return;
            var shapeModule = particleEmitter.shape;
            if (shapeModule.shapeType == ParticleSystemShapeType.Box)
            {
                Vector3 shapeScale = shapeModule.scale;
                shapeScale.x = currentGratingWidth * 1.1f;
                shapeModule.scale = shapeScale;
            }
        }
        if (quantumDistribution != null)
        {
            float slitPitch = apertureControl.AperturePitch;
            float slitWidth = apertureControl.ApertureWidth;
            int slitCount = apertureControl.ApertureCount;
            float lmin = 0;
            if ((slitCount != currentSlitCount) ||
                (slitWidth != currentSlitWidth) ||
                (slitPitch != currentSlitPitch))
            {

                if (waveControl != null)
                {
                    lmin = waveControl.LambdaMin;
                    Debug.Log("Lmin=" + lambdaMin);
                }
                //generateSpeckleDistribution();
                if (quantumDistribution.SetGratingByPitch(slitCount, slitWidth, slitPitch,lmin))
                {
                    currentSlitPitch = slitPitch;
                    currentSlitWidth = slitWidth;
                    currentSlitCount = slitCount;
                }
            } 

        }
    }
    public bool ParticlesPlaying
    {
        get => particlesPlaying;
        set
        {
            if ((value) && (togglePlay != null) && (!togglePlay.isOn))
                togglePlay.isOn = true;
            if ((!value) && (toggleStop != null) && (!toggleStop.isOn))
                toggleStop.isOn = true;
            particlesPlaying = value;
            if (hasParticles)
            {
                if (particlesPlaying)
                    particleEmitter.Play();
                else
                    particleEmitter.Pause();
            }

            RequestSerialization();
        }
    }

    public void PlayParticles()
    {
        ParticlesPlaying = true;
    }

    public void PauseParticles()
    {
        ParticlesPlaying = false;
    }

    public void PlayChanged()
    {
        if (togglePlay != null)
        {
            if (togglePlay.isOn)
            {
                if (iamOwner)
                    PlayParticles();
                else
                    SendCustomNetworkEvent(toTheOwner,nameof(PlayParticles));
            }
        }
    }

    public void StopChanged()
    {
        if (toggleStop != null)
        {
            if (toggleStop.isOn)
            { 
                if (iamOwner)
                    PauseParticles();
                else
                    SendCustomNetworkEvent(toTheOwner,nameof(PauseParticles));
            }
        }
    }

    public void ResetChanged()
    {
        if (toggleReset != null)
        {
            if (toggleReset.isOn)
            {
                SendCustomNetworkEvent(toAll, nameof(ResetParticles));
                toggleReset.isOn = false;
            }
        }
    }

    public void ResetParticles()
    {
        if (hasParticles)
            particleEmitter.Clear();
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        UpdateOwnerShip();
    }

    private float timeRemaining = 0.5f;
    ParticleSystem.Particle[] particles;
    ParticleSystem.Particle particle;
    private void LateUpdate()
    {
        timeRemaining -= Time.deltaTime;
        if (timeRemaining > 0)
            return;
        timeRemaining += 0.1f;
        if ((apertureX <= 0) || !particlesPlaying)
            return;
        if (!hasParticles)
            return;
        var numParticles = particleEmitter.particleCount;
        var nUpdated = 0;
        if ((particles == null) || (particles.Length < numParticles))
            particles = new ParticleSystem.Particle[numParticles+200];
        numParticles = particleEmitter.GetParticles(particles);
        for (int i=0; i<numParticles; i++)
        {
            bool particleChanged = false;
            particle = particles[i];
            float startLifeTime = particle.startLifetime;
            Vector3 pos = particle.position;
            if (Mathf.Abs(pos.z) > 0.75f) 
            {
                particle.remainingLifetime = 0;
                particleChanged = true;
            }
            else if ((startLifeTime < 10) && (pos.x < apertureX))
            {// At Grating
                if (quantumDistribution !=null)
                {
                    Vector3 vUpdated;
                    Vector3 unit = Vector3.right;
                    unit.z = (quantumDistribution.RandomImpulseFrac(freqencyFrac)); // * planckValue);
                    if (Mathf.Abs(unit.z) >= 1)
                        particle.remainingLifetime = 0;
                    else
                    {
                        unit.x = -Mathf.Sqrt(1 - (unit.z * unit.z));
                        vUpdated = unit * particleSpeed;
                        particle.velocity = vUpdated;
                    }
                }
                // Set Velocity
                particleChanged = true;
                particle.startLifetime = 20f;
                particle.remainingLifetime = 60f;
            }
            if (particleChanged)
            {
                particles[i] = particle;
                nUpdated++;
            }
        }
        if (nUpdated > 0)
        {
            particleEmitter.SetParticles(particles, numParticles);
        }
    }

    private void initialize()
    {
        if ((waveControl == null) || (!waveControl.IsStarted))
            return;
        Debug.Log("Initializing");
        frequencyMax = waveControl.MaxFrequency;
        if (frequencyMax <= 0)
            return;
        frequencyMin = waveControl.MinFrequency;
        lambdaMin = waveControl.LambdaMin;
        isStarted = true;
    }
    public bool checkFrequency()
    {
        if (waveControl == null || (!waveControl.IsStarted))
            return false;
        bool changed = (frequencyMax != waveControl.MaxFrequency);
        if (!changed)
            changed = (frequencyMin != waveControl.MinFrequency);
        if (!changed)
            changed = (lambdaMin != waveControl.LambdaMin);
        if (changed)
            initialize();
        if (frequency != waveControl.Frequency) 
        {
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
        return true;
    }

    float timeLeft = 2;
    private void Update()
    {
        timeLeft -= Time.deltaTime;
        if (timeLeft < 0)
        {
            timeLeft = 2;
            if (!isStarted || frequencyMax <= 0)
               initialize();
            if (!isStarted)
                return;
            //Debug.Log("Check Dimensions & Frequency");
            if (!checkFrequency())
                return;
            if (frequencyMax <= 0)
                return;
            checkSourceDimensions();
        }
    }
    private void UpdateOwnerShip()
    {
        iamOwner = Networking.IsOwner(this.gameObject);
        if (iamOwner)
            return;
    }


    void Start()
    {
        UpdateOwnerShip();
        if (particleEmitter != null)
        {
            hasParticles = true;
            sourceXfrm = particleEmitter.transform;
            var main = particleEmitter.main;
            averageSpeed = main.startSpeed.constant;
        }
        if (apertureControl != null) 
            apertureXfrm= apertureControl.transform;
        if ((sourceXfrm != null) && (apertureXfrm != null))
            apertureX = apertureXfrm.position.x;
        ParticlesPlaying = particlesPlaying;
    }
}
