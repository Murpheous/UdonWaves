
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Wrapper.Modules;

[RequireComponent(typeof(ParticleSystem))]
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]

public class TargetDisplay : UdonSharpBehaviour
{
    [SerializeField] private ParticleSystem displayParticles;
    [SerializeField] private int bufferMax = 20;
    [SerializeField]
    private Vector3 decalRotation3D = Vector3.zero;
    private Color[] colourBuf;
    private Vector3[] locationBuf;
    private int bufferCount = 0;
    private bool started = false;
    [SerializeField]
    float particleSize = 0.001f;
    [SerializeField]
    float markerLifetime = 10f;
    public float MarkerLifetime { 
        get => markerLifetime; 
        set => markerLifetime = value;
    } 
    Vector3 particleSize3D = Vector3.zero;
    [SerializeField]
    bool sizeUpdateRequired = false;
    [SerializeField]
    bool dissolveRequired = false;
    public float ParticleSize
    {
        get => particleSize; 
        set
        {
            if (particleSize != value)
            {
                particleSize = value;
                sizeUpdateRequired = true;
            }
            particleSize3D = Vector3.one * particleSize;
        }
    }
//    [SerializeField] bool useLocalX;
  //  Vector3 localPos = Vector3.zero;
    public void PlotParticle(Vector3 location, Color color, float lifetime = 5)
    {
        if (!started)
            return;
        if (bufferCount >= bufferMax) 
            return;
        markerLifetime = lifetime;
        locationBuf[bufferCount] = location; 
        colourBuf[bufferCount] = color;
        bufferCount++;
    }

    public void Dissolve()
    {
        dissolveRequired = true;
    }

    public void Clear()
    {
        if (displayParticles != null)
            displayParticles.Clear();
    }

    private float polltime = 2;
    [SerializeField]
    private int particleCount;

    private ParticleSystem.Particle[] particles;
    private void LateUpdate()
    {
        polltime -= Time.deltaTime;
        bool isTime = (polltime < 0);
        if (isTime)
            polltime += 0.3f;
        if (bufferCount <= 0)
            return;
        if (!isTime && (sizeUpdateRequired || !dissolveRequired) && (bufferCount < bufferMax))
            return;
        //localPos = transform.position;
        if (displayParticles == null)
        {
            bufferCount = 0;
            return;
        }
        //float myScale = displayParticles.transform.localScale.x;
        //float particleScale = myScale * displayParticles.main.startSize.constant;
        particleCount = displayParticles.particleCount;
        particles = new ParticleSystem.Particle[bufferCount + particleCount];
        particleCount = displayParticles.GetParticles(particles);
        int updateCount = 0;
        for (int i = 0; i < bufferCount; i++)
        {
            var particle = new ParticleSystem.Particle();
            particle.position = locationBuf[i];
            particle.startColor = colourBuf[i];
            //particle.startSize = locationBuf[i].w;
            particle.startSize3D = particleSize3D;
            particle.startLifetime = markerLifetime;
            particle.remainingLifetime = markerLifetime;
            particle.rotation3D = decalRotation3D;
            particles[particleCount++] = particle;
            updateCount++;
        }
        bufferCount = 0;
        if (sizeUpdateRequired || dissolveRequired)
        {
            updateCount++;
            for (int i = 0; i < particleCount; i++)
            {
                if (sizeUpdateRequired)
                    particles[i].startSize3D = particleSize3D;
                if (dissolveRequired)
                    particles[i].remainingLifetime /= 5;
            }
            sizeUpdateRequired = false;
            dissolveRequired = false;
        }
        if (updateCount > 0)
            displayParticles.SetParticles(particles,particleCount);
    }
    void Start()
    {
        if (displayParticles == null)
            displayParticles = GetComponent<ParticleSystem>();
        colourBuf = new Color[bufferMax+1];
        locationBuf = new Vector3[bufferMax+1];
        ParticleSize = particleSize;
        started = true;
    }
}
