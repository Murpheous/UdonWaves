
using UdonSharp;
using UnityEngine;
using UnityEngine.PlayerLoop;
using VRC.SDKBase;
using VRC.Udon;
[RequireComponent(typeof(ParticleSystem))]
[UdonBehaviourSyncMode(BehaviourSyncMode.None)] // No networking.

public class LaserParticles : UdonSharpBehaviour
{
    [SerializeField] ParticleSystem laserEmitter;
    [Tooltip("Particle speed at middle of range")]
    public float averageSpeed;
    [Tooltip("Value 0-1 fraction of average velocity +- e.g. 0.5 = +-50% of average")]
    public float randomRange;

    [SerializeField] bool randomSpeed = true;
    void Start()
    {
        _widthBounds = 0.025f;
        _heightBounds = 0.025f;

        laserEmitter = GetComponent<ParticleSystem>();
    }

    Color calcParticleColor(float delta)
    {
        float t;
        Color result;
        //if (!isMonochrome)
        {
            if (delta <= -0.1)
            {
                t = (delta + 0.5f) * 2.5f;
                result = new Color(Mathf.Lerp(1.0f, 0.0f, t), Mathf.Lerp(0.0f, 1.0f, t), 0);
            }
            else if (delta <= 0.2f)
            {
                t = (delta + 0.1f) * 3.33330f;
                result = new Color(0, 1, Mathf.Lerp(0.0f, 1.0f, t));
            }
            else if (delta <= 0.3f)
            {
                t = (delta - 0.1f) * 5.0f;
                result = new Color(0, Mathf.Lerp(1.0f, 0.0f, t), 1);
            }
            else
            {
                t = (delta - 0.3f) * 5.0f;
                result = new Color(Mathf.Lerp(0.0f, 0.5f, t), 0, Mathf.Lerp(1.0f, 0.5f, t));
            }
        }
        return result;
    }

    int numParticles;
    int nUpdated;
    float velocityScale_x = 1;
    float NewVel;
    float delta;
    Vector3 tmpVel = new Vector3();
    float _widthBounds;
    float _heightBounds;


    private void LateUpdate()
    {
        if (laserEmitter != null)
        {
            numParticles = laserEmitter.particleCount;
            nUpdated = 0;
            var particles = new ParticleSystem.Particle[numParticles];
            numParticles = laserEmitter.GetParticles(particles);
            for (int p = 0; p < numParticles; p++)
            {
                if (particles[p].startLifetime < 1)
                {
                    particles[p].startLifetime = 10;
                    particles[p].remainingLifetime = 5;

                    velocityScale_x = 1;
                    NewVel = averageSpeed;


                    if (randomSpeed)
                    {
                        delta = Random.Range(-randomRange, randomRange);
                        //delta = -100;
                        //while (delta < -0.5f)
                        //	delta = 0.5f*NextGaussian();
                    }
                    particles[p].startColor = calcParticleColor(delta);
                    velocityScale_x += delta;
                    NewVel *= velocityScale_x;
                    tmpVel = particles[p].velocity / particles[p].velocity.magnitude;
                    particles[p].velocity = tmpVel * NewVel;
                    nUpdated++;
                }
                else if ((particles[p].velocity.x <= 0) && (particles[p].startLifetime < 20))
                {
                    particles[p].startLifetime = 21;
                    nUpdated++;

                    Vector3 hitPos = particles[p].position;
                    if ((Mathf.Abs(hitPos.z) <= _widthBounds) && (Mathf.Abs(hitPos.y) <= _heightBounds))
                    {
                        particles[p].startColor = new Color(0, 0, 0, 0);
                        particles[p].remainingLifetime = 0;
                    }
                    else
                    {
                        particles[p].velocity = Vector3.zero;
                        particles[p].remainingLifetime = 2;
                        particles[p].startSize = particles[p].startSize * 0.5f;
                        particles[p].startLifetime = 49;
                    }
                }

            }
            if (nUpdated > 0)
            {
                laserEmitter.SetParticles(particles, numParticles);
            }
        }

    }
}
