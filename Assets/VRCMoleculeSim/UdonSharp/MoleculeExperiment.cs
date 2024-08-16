using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class MoleculeExperiment : UdonSharpBehaviour
{
    [Tooltip("Particle speed at middle of range")]
    public float avgMoleculeSpeed=150;
    [SerializeField, Range(0, 70), Tooltip("% Fraction of avg velocity +-"),FieldChangeCallback(nameof(RandomRangePercent))]
    private int randomRangePercent = 50;
    [SerializeField, UdonSynced,  FieldChangeCallback(nameof(RandomizeSpeed))] bool randomizeSpeed = true;
    
    [SerializeField,FieldChangeCallback(nameof(SpeedPercent))] private float speedPercent = 0;

    [SerializeField] private UdonSlider speedSlider;

    private bool RandomizeSpeed
    {
        get => randomizeSpeed;
        set
        {
            randomizeSpeed = value;
            if (togRandomSpeed != null)
                togRandomSpeed.SetIsOnWithoutNotify(randomizeSpeed);
            if (isRunning && speedSlider != null)
                speedSlider.gameObject.SetActive(!randomizeSpeed);
            RequestSerialization();
            updateSourceElevation();
        }
    }

    [Tooltip("Slow Motion"), SerializeField, Range(0.001f, 1f)]
    private float slowMotion = 0.025f;

    public float molecularWeight = 514.5389f;
    public string moleculeName = "Pthalocyanine";
    [SerializeField] private TextMeshProUGUI moleculeText;

    [Header("Operating Settings-------")]
    [SerializeField, Tooltip("Default Particle Size"), FieldChangeCallback(nameof(ParticleStartSize))] 
    private float particleStartSize = 0.001f;
    [SerializeField, Range(0.1f, 5f), FieldChangeCallback(nameof(MarkerPointSize))] 
    private float markerPointSize = 2;
    [SerializeField] private UdonSlider markerSizeSlider;

    private VRCPlayerApi player;
    private bool iamOwner = false;

    public float MarkerPointSize
    {
        get => markerPointSize;
        set
        {
            value = Mathf.Clamp(value, 0.1f, 5.0f);
            if (markerPointSize != value)
            {
                markerPointSize = value;
                checkMarkerSizes();
            }
        }
    }

    [Tooltip("Decay time of particles at target"), SerializeField, Range(0.5f, 20f), FieldChangeCallback(nameof(MarkerLifetime))] float markerLifetime = 15;
    [Tooltip("Exaggerate/Suppress Beam Particle Size"),SerializeField, Range(0.1f, 5f), FieldChangeCallback(nameof(ParticleSize))] float particleSize = 1;
    public UdonSlider particleSizeSlider;

    private float ParticleSize
    {
        get => particleSize;
        set
        {
            value = Mathf.Clamp(value, 0.1f, 5.0f);
            particleSize = value;
            checkMarkerSizes();
        }
    }

    [SerializeField, ColorUsage(true, true)] Color defaultColour = Color.green;

    [SerializeField, UdonSynced, FieldChangeCallback(nameof(UseMonochrome))] private bool useMonochrome = false;
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(UseQuantumScatter))] private bool useQuantumScatter;
    [SerializeField, UdonSynced, FieldChangeCallback(nameof(UseGravity))]
    private bool  useGravity = true;

    [Header("Grating and Detector Distances")]
    public float L1mm = 200;
    public float L2mm = 561;

    [Header("Constants")]
    public float h = 6.62607015e-34f; // 
    public float AMU_ToKg = 1.66054e-27f;

    [Header("Gravity")]
    private bool gravityChanged = false;
    private bool settingsChanged = false;
    private bool planckChanged = false;
    private bool trajectoryChanged = false;
    public bool UseGravity
    {
        get => useGravity;
        set
        {
            if (useGravity != value)
            {
                useGravity = value;
                gravityChanged = true;
            }
            if ((togGravity != null) && (togGravity.isOn != value))
                togGravity.SetIsOnWithoutNotify(value);
            RequestSerialization();
        }
    }
    [SerializeField]
    private float gravityAcceleration; // Required because forceoverlifetime is hidden and must be copied here    ParticleSystem.MainModule mainModule;

    //[Header("Calculated Scale Values")]
    //[SerializeField]
    private float gravitySim;
    //[SerializeField]
    private float emitToGratingSim;
    //[SerializeField]
    private float gratingToTargetSim;
    //[SerializeField]
    private float maxLifetimeAfterGrating = 20f;


    [Header("Scaling Control")]

    private int gravityScale = 10;
    private int GravityScale
    {
        get => gravityScale;
        set
        {
            if (gravityScale != value)
            {
                gravityScale = value;
                gravityChanged = true;
                Debug.Log("Grav Scale=" + gravityScale.ToString());
            }
        }
    }

    private int planckScale = 10;
    private int PlanckScale
    {
        get => planckScale;
        set 
        {
            if (planckScale != value)
            {
                planckChanged = true;
                planckScale = value;
                gratingVersion = -1;
            }
        }
    }

    //[SerializeField]
    private float slowScaled = 0.025f;
    [SerializeField,FieldChangeCallback(nameof(ScaleIsChanging))] 
    private bool scaleIsChanging = true;
    private bool ScaleIsChanging
    {
        get => scaleIsChanging;
        set
        {
            scaleIsChanging = value;
            if (hasSource)
            {
                if (scaleIsChanging)
                {
                    //Debug.Log("Scale Changing");

                    particleEmitter.Pause();
                    particleEmitter.Clear();
                    if (hasTargetDecorator)
                        targetDisplay.Clear();
                }
                else
                {
                 //   Debug.Log("Scale Stopped");
                    gratingVersion = -1;
                    settingsChanged = true;
                    gravityChanged = true;
                    planckChanged = true; 
                    particleEmitter.Play();
                }
            }
        }
    }

    [SerializeField]
    private float graphicsScale = 1f;
    [Tooltip("Scale of objects at design (10x)"),SerializeField,FieldChangeCallback(nameof(NativeGraphicsRatio))]
    private int nativeGraphicsRatio = 10;
    public int NativeGraphicsRatio 
    { 
        get => nativeGraphicsRatio > 0 ? nativeGraphicsRatio : 1; 
        set
        {
            value = value > 0 ? value : 1;
            if (value != nativeGraphicsRatio)
            {
                nativeGraphicsRatio = value;
                settingsChanged = true;
            }
        }
    }

    [Tooltip("Spatial Scaling"), FieldChangeCallback(nameof(ExperimentScale))]
    public float experimentScale = 10f; 
    public float ExperimentScale
    {
        get => experimentScale;
        set
        {
            if (experimentScale != value)
            {
                experimentScale = value;
                graphicsScale = experimentScale / NativeGraphicsRatio;
                slowScaled = slowMotion * graphicsScale;
                emitToGratingSim = -(L1mm * experimentScale) / 1000;
                gratingToTargetSim = (L2mm * experimentScale) / 1000;
                settingsChanged = true;
                gravityChanged = true;
                planckChanged = true;
                gratingVersion = 0;
            }
        }
    }
    [Header("Speed Calculations")]
    [SerializeField]
    private float avgSimulationSpeed = 5f;
    [SerializeField]
    private float maxSimSpeed;
    [SerializeField]
    private float minSimSpeed;
    
    [SerializeField]
    private float userSpeedFraction = 0;
    [SerializeField]
    private float userSpeedTrim = 0.5f;
    [SerializeField]
    private float randomRange = 0.7f;

    private float SpeedPercent
    {
        get => speedPercent;
        set
        {
            int lim = RandomRangePercent;
            speedPercent = value;
            userSpeedFraction = speedPercent/100f;
            userSpeedTrim = (Mathf.Clamp(userSpeedFraction / randomRange,-1f,1f)+1f)/2f;
            if (isRunning && speedSlider != null)
                speedSlider.TitleText = string.Format("Speed\n{0}m/s", Mathf.RoundToInt((1 + userSpeedFraction) * avgMoleculeSpeed));
        }
    }

    private int RandomRangePercent
    {
        get => randomRangePercent;
        set
        {
            randomRangePercent = value;
            randomRange = (float)randomRangePercent/100f;
        }
    }

    public bool UseQuantumScatter
    {
        get => useQuantumScatter;
        set
        {
            useQuantumScatter = value;
            if ((togQuantum != null) && (togQuantum.isOn != value))
                togQuantum.SetIsOnWithoutNotify(value);
            RequestSerialization();
        }
    }

    public bool UseMonochrome
    {
        get => useMonochrome;
        set
        {
            useMonochrome = value;
            if ((togMonochrome != null) && (togMonochrome.isOn != value))
                togMonochrome.SetIsOnWithoutNotify(value);
            RequestSerialization();
        }
    }


    [Header("System Components")]
    [SerializeField]
    Transform collimatorProp;
    [SerializeField]
    ParticleSystem particleEmitter;
    Transform sourceXfrm;
    public float MarkerLifetime
    {
        get => markerLifetime;
        set
        {
            markerLifetime = value;
            if (hasTargetDecorator)
                targetDisplay.MarkerLifetime = markerLifetime;
        }
    }
    public float ParticleStartSize
    {
        get => particleStartSize;
        set
        {
            particleStartSize = value;
            checkMarkerSizes();
        }
    }

    [SerializeField]
    QuantumScatter horizontalScatter;
    [SerializeField]
    QuantumScatter verticalScatter;
    [SerializeField]
    GratingControl gratingControl;
    Transform gratingXfrm;
    float gratingThickness = 0.001f;
    [SerializeField]
    Transform targetTransform;
    [SerializeField]
    Transform floorTransform;
    [SerializeField]
    TargetDisplay targetDisplay;
    [SerializeField]
    TargetDisplay gratingDecals;
    //[SerializeField]
    Vector3 gratingPosition = Vector3.zero;
    //[SerializeField]
    Vector3 targetPosition = Vector3.zero;
    Vector3 targetRotation = Vector3.zero;
    //[SerializeField]
    bool hasFloor;
    //[SerializeField]
    bool hasTarget;
    //[SerializeField]
    bool hasTargetDecorator;
    //[SerializeField]
    bool hasGrating;
    //[SerializeField]
    bool hasGratingDecorator;
    bool hasSource;
    bool hasHorizontalScatter;
    bool hasVerticalScatter;
    bool hasTrajectoryModule = false;
    bool trajectoryValid = false;

    [SerializeField]
    TrajectoryModule trajectoryModule;
    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI gravityLabel;
    [SerializeField] TextMeshProUGUI planckLabel;
    [SerializeField] TextMeshProUGUI gravScaleLabel;
    [SerializeField] TextMeshProUGUI planckScaleLabel;
    [SerializeField] Toggle togGravity;
    [SerializeField] Toggle togQuantum;
    [SerializeField] Toggle togRandomSpeed;
    [SerializeField] Toggle togMonochrome;
    [SerializeField] Toggle togPlay;
    [SerializeField] Toggle togPause;
    [SerializeField,UdonSynced,FieldChangeCallback(nameof(PlayParticles))] bool playParticles = true;
    public bool PlayParticles 
    {  
        get => playParticles;  
        set 
        {  
            playParticles = value;
            if (togPlay != null && value && !togPlay.isOn)
                togPlay.SetIsOnWithoutNotify(true);
            if (togPause != null && !value && !togPause.isOn)
                togPause.SetIsOnWithoutNotify(true);
            if (hasSource)
            {
                if (value)
                    particleEmitter.Play();
                else
                    particleEmitter.Pause();
            }
            RequestSerialization();
        } 
    }

    //private VRC.Udon.Common.Interfaces.NetworkEventTarget toTheOwner = VRC.Udon.Common.Interfaces.NetworkEventTarget.Owner;

    [Header("Debug Stuff")]
    [SerializeField] TextMeshProUGUI debugTextField;
    [SerializeField] bool hasDebug = false;
    private void logDebug(string message)
    {
        if (!hasDebug) return;
        debugTextField.text = message;
    }
    //[SerializeField]
    private float minDeBroglieWL = 0.1f; // h/mv
    //[SerializeField]
    private bool horizReady = false;
    //[SerializeField]
    private bool vertReady = false;
    //[SerializeField]
    private bool gratingReady = false;
    [SerializeField]
    private float targetMarkerSize = 1;
    [SerializeField]
    private float gratingMarkerSize = 1;


    [Tooltip("Index of Gravity Multiplier"), UdonSynced, FieldChangeCallback(nameof(GravityIndex))]
    private int gravityIndex = 0;
    private int GravityIndex
    {
        set
        {
            gravityIndex = CheckScaleIndex(value, scaleSteps);
            GravityScale = scaleSteps[gravityIndex];
            RequestSerialization();
        }
    }

    [Tooltip("Index of Planck Multiplier"), UdonSynced, FieldChangeCallback(nameof(PlanckIndex))]
    private int planckIndex = 0;
    private int PlanckIndex
    {
        set
        {
            planckIndex = CheckScaleIndex(value, planckSteps);
            PlanckScale = planckSteps[planckIndex];
            RequestSerialization();
        }
    }


    // Internal Variables
    private ParticleSystem.MainModule mainModule;

    private ParticleSystem.Particle[] particles = null;
    private int numParticles;

    void setText(TextMeshProUGUI tmproLabel, string text)
    {
        if (tmproLabel != null)
            tmproLabel.text = text;
    }

    private void ReviewOwnerShip()
    {
        iamOwner = Networking.IsOwner(this.gameObject);
    }
    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        ReviewOwnerShip();
    }
    private void UpdateLabels()
    {
        setText(gravityLabel, string.Format("g={0:#.##}", GravityScale * gravityAcceleration));
        setText(planckLabel, string.Format("h={0:#.##e+0}", h * PlanckScale));
        setText(gravScaleLabel, string.Format("g x {0}", GravityScale));
        setText(planckScaleLabel, string.Format("h x {0}", PlanckScale));
    }

    private int[] scaleSteps = { 1, 2, 5, 10, 15, 20, 50, 100, 200, 500, 1000 };
    private int[] planckSteps = { 1, 5, 10, 50, 100, 500, 1000 };

    private int CheckScaleIndex(int newIndex , int[] steps)
    {
        return Mathf.Clamp(newIndex, 0, steps.Length - 1);
    }
    public void OnGravScaleDown()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        GravityIndex = gravityIndex-1;
    }
    public void OnGravScaleUp()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        GravityIndex = gravityIndex + 1;
    }

    public void OnPlanckScaleDown()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        PlanckIndex = planckIndex-1;
    }

    public void OnPlanckScaleUp()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        PlanckIndex =  planckIndex + 1;
    }

    // play/pause reset particle events
    public void playSim()
    {
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        if (togPlay != null) 
        {
            if (togPlay.isOn && !playParticles)
                PlayParticles = true;
        }
    }

    public void pauseSim()
    {
        if (!iamOwner)
            Networking.SetOwner(player,gameObject);
        if (togPause != null)
        {
            if (togPause.isOn && playParticles)
                PlayParticles = false;
        }
    }

    public void resetSim()
    {
        if (hasSource)
            particleEmitter.Clear(); // Restart.
    }

    public void doReset()
    {
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(resetSim));
    }

    public void OnGravityToggle()
    {
        bool newGravity = !useGravity;
        if (togGravity != null)
            newGravity  = togGravity.isOn;
        if (!iamOwner) 
            Networking.SetOwner(player, gameObject);
        UseGravity = newGravity;
    }

    public void OnQuantumToggle()
    {
        bool newQuantum = !useQuantumScatter;
        if (togQuantum != null)
            newQuantum = togQuantum.isOn;
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        UseQuantumScatter = newQuantum;
    }
    public void OnTogSpeed()
    {
        bool newRandom = !randomizeSpeed;
        if (togRandomSpeed != null)
            newRandom = togRandomSpeed.isOn;
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        RandomizeSpeed = newRandom;
    }

    public void OnTogMonochrome()
    {
        bool newMono = !useMonochrome;
        if (togMonochrome != null)
            newMono = togMonochrome.isOn;
        if (!iamOwner)
            Networking.SetOwner(player, gameObject);
        UseMonochrome = newMono;
    }

    private void LateUpdate()
    {
        if (!hasSource || !playParticles)
            return;
        int nUpdated = 0;
        Vector3 launchVelocity;
        Vector3 launchPosition;
        Vector3 particlePos;
        Vector3 particleVelocity;
        float lifeRemaining;
        int particleStage;
        numParticles = particleEmitter.particleCount;
        particles = new ParticleSystem.Particle[numParticles];
        numParticles = particleEmitter.GetParticles(particles);
            
        float spreadHigh = UnityEngine.Random.Range(-startDimensions.y, startDimensions.y);
        float spreadWide = UnityEngine.Random.Range(-startDimensions.x, startDimensions.x);

        for (int i = 0; i < numParticles; i++)
        {
            var particle = particles[i];
            particleStage = Mathf.RoundToInt(particle.startLifetime);
            particlePos = particle.position;
            particleVelocity = particle.velocity;
            lifeRemaining = particle.remainingLifetime;
            bool particleChanged = false;
            // particleStage < 10 means newborn (unlaunched)
            if (particleStage < 10)
            {
                particleChanged = true;
                particleStage = 250;
                lifeRemaining = 100;
                launchPosition = new Vector3(gratingThickness, spreadHigh, spreadWide);
                particle.axisOfRotation = launchPosition;
                launchPosition += sourceXfrm.position;
                Color launchColour = defaultColour;
                uint particleIndex = 0;
                if (trajectoryValid)
                {
                    float speedTrim = randomizeSpeed ? UnityEngine.Random.Range(0f, 1f) : userSpeedTrim;
                    int velocityIndex = (int)Mathf.Lerp(0, trajectoryModule.LookupPoints, speedTrim);
                    launchVelocity = trajectoryModule.lookupVelocity(velocityIndex);
                    if (!useMonochrome)
                        launchColour = trajectoryModule.lookupColour(velocityIndex);
                }
                else
                {
                    float speedFraction = randomizeSpeed ? UnityEngine.Random.Range(-randomRange, randomRange) : userSpeedFraction;
                    launchVelocity = new Vector3(avgSimulationSpeed * speedFraction + 1, 0, 0);
                }
                particleVelocity = launchVelocity;
                particlePos = launchPosition;
                launchVelocity.y = -launchVelocity.y;
                particle.rotation3D = launchVelocity;
                particle.startColor = launchColour;
                particle.randomSeed = particleIndex;
            }
            else // not a newborn
            {
                bool afterTarget = particlePos.x > (targetPosition.x+0.1f) && particleStage > 50;
                if (afterTarget) // Stray
                {
                    //particleStage = fadeParticle(i);
                    particleVelocity = Vector3.zero;
                    lifeRemaining = 0.5f;
                    particleStage = 43;
                    // %%%
                    particleChanged = true;
                }
                if (particleStage > 50)
                {
                    float particleGratingDelta = particlePos.x - gratingPosition.x;
                    // Any Above 50 and stopped have collided with something and need to be handled
                    float particleTargetDelta = particlePos.x - targetPosition.x;
                    bool preGratingFilter = particleStage > 240;
                    bool stopped = (particleVelocity.x < 0.01f);
                    // Handle Stopped Particle
                    if (stopped)
                    {
                        Vector3 collideVelocity = particles[i].rotation3D;
                        //
                        // Process impact of particle stopping after initial launch
                        if (preGratingFilter)
                        { // Stopped and not processed for grating
                            // Now test to see if stopped at grating
                            if (Mathf.Abs(particleGratingDelta) <= 0.01f)
                            { // Here if close to grating
                                Vector3 gratingHitPosition = particle.axisOfRotation;
                                if (hasGrating && (!gratingControl.checkLatticeCollision(gratingHitPosition)))
                                {
                                    particlePos = gratingHitPosition;
                                    particleVelocity = collideVelocity;
                                    particleStage = 240;
                                    particleChanged = true;
                                }
                                else
                                {
                                    gratingHitPosition.x = particlePos.x;
                                    if (hasGratingDecorator)
                                    {
                                        gratingDecals.FadeParticle(gratingHitPosition, particles[i].startColor, false, gratingMarkerSize, 0.5f);
                                        //particleStage = killParticle(i);
                                        particleStage = 42;
                                        particle.velocity = Vector3.zero;
                                        lifeRemaining = 0f;
                                        particleChanged = true;
                                        // %%%
                                    }
                                    else
                                    {
                                        //particleStage = fadeParticle(i);
                                        particleStage = 43;
                                        particlePos = gratingHitPosition;
                                        particleVelocity = Vector3.zero;
                                        particle.startSize = gratingMarkerSize;
                                        lifeRemaining = 0.6f;
                                        particleChanged = true;
                                        // %%%
                                    }
                                }
                            }
                            else
                            {
                                //particleStage = fadeParticle(i);
                                particleVelocity = Vector3.zero;
                                lifeRemaining = 0.5f;
                                particleStage = 43;
                                // %%%
                                particleChanged = true;
                            }
                        }
                        else
                        { // Stopped and after grating use particle for decal or erase
                            bool atTarget = hasTarget && (particleTargetDelta >= -0.01f);
                            bool atFloor = hasFloor && (!atTarget);
                            nUpdated++;
                            if (atTarget || atFloor)
                            {
                                lifeRemaining = 0f;
                                if (hasTargetDecorator)
                                {
                                    targetDisplay.PlotParticle(particlePos, particles[i].startColor, atFloor);
                                    particleStage = 42;
                                    particleVelocity = Vector3.zero;
                                }
                                else
                                {
                                    particleStage = 43;
                                    particleVelocity = Vector3.zero;
                                    lifeRemaining = 0.5f;
                                }
                                particleChanged = true;
                            }
                            else // Anywhere else
                            {
                                //particleStage = fadeParticle(i);
                                particleVelocity = Vector3.zero;
                                lifeRemaining = 0.5f;
                                particleStage = 43;
                                particleChanged = true;
                            }
                        }
                    } // Stopped
                    if (particleStage == 240)
                    {
                        float speedFraction = particleVelocity.x / maxSimSpeed;
                        float speedRestore = (maxSimSpeed * speedFraction);
                        Vector3 unitVecScatter = Vector3.right;
                        lifeRemaining = maxLifetimeAfterGrating;
                        particleChanged = true;
                        particleStage = 239;
                        if (useQuantumScatter)
                        {
                            float sY=0,sZ=0;
                            if (hasHorizontalScatter)
                            {
                                sZ = horizontalScatter.RandomImpulseFrac(speedFraction);
                                unitVecScatter.z = sZ;
                            }

                            if (hasVerticalScatter)
                            {
                                sY = verticalScatter.RandomImpulseFrac(speedFraction);
                                unitVecScatter.y = sY;
                            }
                            unitVecScatter.x = Mathf.Sqrt(1 - Mathf.Clamp01(sY * sY + sZ * sZ));
                            Vector3 updateV = unitVecScatter * speedRestore;
                            updateV.y += particleVelocity.y;
                            particleVelocity = updateV;
                        }
                    }
                }
            }
            if (particleChanged)
            {
                particle.startLifetime = particleStage;
                particle.velocity = particleVelocity;
                particle.position = particlePos;
                particle.remainingLifetime = lifeRemaining;
                nUpdated++;
                particles[i] = particle;
            }
        }

        if (nUpdated > 0)
        {
            particleEmitter.SetParticles(particles, numParticles);
        }
    }

    private void updateGravity()
    {
        if (!hasSource || scaleIsChanging)
            return;
       // Debug.Log("Update Gravity!!!");
        gravityChanged = false;
        gravitySim = useGravity ? GravityScale * gravityAcceleration * (slowScaled * slowScaled) / experimentScale : 0.0f;
        var fo = particleEmitter.forceOverLifetime;
        fo.enabled = false;
        fo.y = gravitySim;
        fo.enabled = useGravity;
        particleEmitter.Clear(); // Restart.
        particleEmitter.Play();
        if (hasTrajectoryModule)
        {
           // Debug.Log("Has Trajectory Module");
            trajectoryModule.GravitySim = gravitySim;
            trajectoryModule.UseGravity = useGravity;
        }
        logDebug(string.Format("G: Has Traj {0}, Traj Valid {1}", hasTrajectoryModule, trajectoryValid));
    }
    private void checkMarkerSizes()
    {
        float trimValue = markerPointSize / 2.0f;
        float mul = particleStartSize * experimentScale / nativeGraphicsRatio;
        targetMarkerSize = Mathf.Lerp(0.1f,1,trimValue) * mul;
        if (hasTargetDecorator)
            targetDisplay.ParticleSize = targetMarkerSize;
        if (hasSource)
            mainModule.startSize = particleStartSize * particleSize * Mathf.Sqrt(experimentScale);
    }

    private void dissolveDisplays()
    {
        if (hasSource)
            particleEmitter.Clear();
        if (hasTargetDecorator)
            targetDisplay.Dissolve();
    }

    private void updateSourceElevation()
    {
        float elevationDegrees = 0f;
        if (hasTrajectoryModule && trajectoryValid)
        {
            float speedTrim = randomizeSpeed ? 0.5f : userSpeedTrim;
            int velocityIndex = (int)Mathf.Lerp(0, trajectoryModule.LookupPoints, speedTrim);
            Vector3 launchVelocity = trajectoryModule.lookupVelocity(velocityIndex);
            elevationDegrees = Mathf.Atan2(launchVelocity.y, launchVelocity.x) * Mathf.Rad2Deg;
        }
        if (collimatorProp != null)
            collimatorProp.localRotation = Quaternion.Euler(0, 0, elevationDegrees);
    }
    private void updateSettings()
    {
        MarkerLifetime = markerLifetime;
        checkMarkerSizes();
        settingsChanged = false;
        avgSimulationSpeed = avgMoleculeSpeed * slowScaled;
        
        maxSimSpeed = avgSimulationSpeed * (1 + randomRange);
        minSimSpeed = avgSimulationSpeed * (1 - randomRange);
        maxLifetimeAfterGrating = 1.25f * gratingToTargetSim / minSimSpeed;
        minDeBroglieWL = (h * PlanckScale) / (AMU_ToKg * molecularWeight * avgMoleculeSpeed*(1+randomRange));
        if (moleculeText != null)
        {
            moleculeText.text = string.Format("Particles:<b>\n<indent=15%>{0}</b></indent>\nMolecular Weight:\n<b><indent=15%>{1:#.##}</b></indent>", moleculeName, molecularWeight);
        }
        if (hasTrajectoryModule)
        {
            trajectoryModule.loadSettings(maxSimSpeed, minSimSpeed, gravitySim, useGravity, emitToGratingSim);
            trajectoryValid = trajectoryModule.SettingsValid;
        }
        else
            trajectoryValid = false;
        updateSourceElevation();
        //logDebug(string.Format("U: Has Traj {0}, Traj Valid {1}", hasTrajectoryModule, trajectoryValid));

        trajectoryChanged = false;
        Vector3 newPosition;

        // Set position of grating
        if (hasSource)
        {
            newPosition = gratingPosition;
            newPosition.x -= emitToGratingSim;
            sourceXfrm.position = newPosition;
            if (collimatorProp != null)
                collimatorProp.localScale = new Vector3(graphicsScale, graphicsScale, graphicsScale);
        }
        if (hasTarget)
        {
            targetPosition = new Vector3(gratingToTargetSim,0f,0f);
            targetTransform.position = targetPosition;
        }
    }
    //[SerializeField]
    private int gratingVersion = -1;
    private Vector2Int apertureCounts = Vector2Int.zero;
    [SerializeField]
    private Vector2 aperturePitches = Vector2.zero;
    [SerializeField] 
    private Vector2 apertureSize = Vector2.zero;
    // Grating Dimensions in World Space
    //[SerializeField] 
    private Vector2 gratingSize = Vector2.zero;
    private Vector2 startDimensions = Vector2.zero;
    public int GratingVersion
    {
        get => gratingVersion;
        set
        {
            bool force = gratingVersion < 0 || planckChanged;
            gratingVersion = value;
            if (!hasGrating)
                return;
            planckChanged = false;
            gratingSize = gratingControl.ActiveGratingSize;
            gratingThickness = gratingControl.panelThickness*1.5f;
            startDimensions = gratingSize/1.8f;
            int rowCount = gratingControl.RowCount;
            int colCount = gratingControl.ColumnCount;
            float holeWidth = gratingControl.SlitWidthMetres;
            float holeHeight = gratingControl.SlitHeightMetres;
            float colPitch = gratingControl.SlitPitchMetres;
            float rowPitch = gratingControl.RowPitchMetres;
            bool horizChanged = force || ((colCount != apertureCounts.x) || (holeWidth != apertureSize.x) || (colPitch != aperturePitches.x));
            bool vertChanged = force || ((rowCount != apertureCounts.y) || (holeHeight != apertureSize.y) || (rowPitch != aperturePitches.y));
            apertureCounts.x = colCount; apertureCounts.y = rowCount;
            apertureSize.x = holeWidth; apertureSize.y = holeHeight; 
            aperturePitches.x = colPitch; aperturePitches.y = rowPitch;
            gratingMarkerSize = experimentScale * Mathf.Min(gratingControl.SlitHeightMetres, gratingControl.SlitWidthMetres);
            //if (hasGratingDecorator)
            //    gratingDecals.ParticleSize = gratingMarkerSize;

            if (horizChanged && horizReady && apertureCounts.x > 0)
                horizontalScatter.SetGratingByPitch(apertureCounts.x, apertureSize.x, aperturePitches.x, minDeBroglieWL);
            if (vertChanged && vertReady && apertureCounts.y > 0)
                verticalScatter.SetGratingByPitch(apertureCounts.y, apertureSize.y, aperturePitches.y, minDeBroglieWL);
            if (horizChanged || vertChanged)
                dissolveDisplays();
        }
    }

    float polltime = 1;

    private void Update()
    {
        polltime -= Time.deltaTime;
        if (polltime > 0)
            return;
        polltime += ScaleIsChanging ? 0.05f : 0.3f;
        
        if (hasVerticalScatter && !vertReady)
        {
            vertReady = verticalScatter.IsStarted;
            if (vertReady)
                gratingVersion = -1;
            else
                return;
        }
        if (hasHorizontalScatter && !horizReady)
        {
            horizReady = horizontalScatter.IsStarted;
            if (horizReady)
                gratingVersion = -1;
            else
                return;
            //Debug.Log("Got Horiz");
        }
        if (hasGrating && !gratingReady)
        {
            gratingReady = gratingControl.Started;
            if (gratingReady)
                gratingVersion = -1;
            else
                return;
            //Debug.Log("Got Grating");
        }

        bool updateUI = planckChanged || gravityChanged || settingsChanged || trajectoryChanged;
        trajectoryChanged = trajectoryChanged || gravityChanged;
        if (gravityChanged)
            updateGravity();
        if (settingsChanged || planckChanged || trajectoryChanged)
            updateSettings();
        if (hasGrating)
        {
            int gcVersion = gratingControl.GratingVersion;
            if (gcVersion != gratingVersion)
            {
                GratingVersion = gcVersion;
               // Debug.Log($"Grating version: {gcVersion}");
            }
        }
        if (updateUI)
        {
            UpdateLabels();
            if (!scaleIsChanging)
                dissolveDisplays();
        }
    }

    bool isRunning = false;
   void Start()
    {
        player = Networking.LocalPlayer;
        ReviewOwnerShip();

        hasDebug = (debugTextField != null) && debugTextField.gameObject.activeSelf;
        isRunning = true;
        if (trajectoryModule == null)
            trajectoryModule = GetComponent<TrajectoryModule>();
        hasTrajectoryModule = trajectoryModule != null;

        hasGratingDecorator = gratingDecals != null;

        if (gratingControl != null)
        {
            hasGrating = true;
            gratingXfrm = gratingControl.transform;
            gratingPosition = gratingXfrm.position;
            //gratingPosition.x -= 0.001f;
        }
        SpeedPercent = speedPercent;
        if (speedSlider != null)
        {
            speedSlider.SetLimits(-50, 50);
            speedSlider.SetValue(speedPercent);
        }
        RandomRangePercent = randomRangePercent;
        float tmp = experimentScale;
        experimentScale = 0;
        ExperimentScale = tmp;
        if (particleEmitter == null)
            particleEmitter = GetComponent<ParticleSystem>();
        hasSource = particleEmitter != null;
        if (hasSource)
        {
            sourceXfrm = particleEmitter.transform;
            mainModule = particleEmitter.main;
            mainModule.startSpeed = 0.1f;
            mainModule.playOnAwake = true;
        }

        MarkerPointSize = markerPointSize;
        if (markerSizeSlider != null)
        {
            markerSizeSlider.SetLimits(0.1f, 5f);
            markerSizeSlider.SetValue(markerPointSize);
        }
        ParticleSize = particleSize;
        if (particleSizeSlider != null)
        {
            particleSizeSlider.SetLimits(0.1f, 5f);
            particleSizeSlider.SetValue(particleSize);
        }
        RandomizeSpeed = randomizeSpeed;
        hasHorizontalScatter = (horizontalScatter != null);
        hasVerticalScatter = (verticalScatter != null);
        hasFloor = floorTransform != null;
        //hasFloorDecorator = floorDisplay != null;
        hasTarget = targetTransform != null;
        hasTargetDecorator = targetDisplay != null;
        if (hasTarget)
        {
            targetPosition = targetTransform.position;
        }
        // Forces initialise checkboxes if present.
        UseGravity = useGravity;
        UseQuantumScatter = useQuantumScatter;
        UseMonochrome = useMonochrome;
        GravityIndex = gravityIndex;
        PlanckIndex = planckIndex;
        // Forces review of settngs after start
        gravityChanged = true;
        settingsChanged = true;
        trajectoryChanged = true;
    }
}
