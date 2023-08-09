
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]

public class TrajectoryModule : UdonSharpBehaviour
{
    [Header("Number of Lookup Points")]
    [SerializeField, Range(128, 4096)]
    private int lookupPoints = 256;
    public int LookupPoints { get => lookupPoints; }
    //[SerializeField]
    Vector3[] launchVelocities;
    Color[] launchColours;

    private bool settingsValid = false;
    public bool SettingsValid { get => settingsValid; }
    bool settingsLoaded = false;
    public bool SettingsLoaded { get => settingsLoaded;}

    [SerializeField] 
    float gratingDistance;
    public float GratingDistance
    {
        get => gratingDistance;
        set
        {
            if (gratingDistance != value)
            {
                gratingDistance = value;
                settingsValid = false;
            }
        }
    }

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


    public Color lookupColour(int index)
    {
        if (index >= LookupPoints)
            index = lookupPoints - 1;
        return launchColours[index];
    }

    public Vector3 lookupVelocity(int  index)
    {
        if (index >= LookupPoints)
            index = lookupPoints-1;
        return launchVelocities[index];
    }
    public Vector3 getVelocityRandom()
    {
        int index = Random.Range(0, lookupPoints);
        return launchVelocities[index];
    }

    public Vector3 getVelocityFixed(float range)
    {
        int index = (int)Mathf.Clamp(lookupPoints * range,0,lookupPoints);
        return launchVelocities[index];
    }

    [SerializeField]
    private Vector2 speedMinMax;
    public Vector2 SpeedMinMax
    {
        get => speedMinMax;
        set
        {
            if (speedMinMax != value)
            {
                speedMinMax = value;
                settingsValid = false;
            }
        }
    }

    [SerializeField] float gravitySim;
    public float GravitySim
    {
        get => gravitySim; 
        set
        {
            if (value != gravitySim)
            {
                gravitySim = value;
                settingsValid = false;
            }
        } 
    }

    [SerializeField] bool useGravity;
    public bool UseGravity
    {
        get => useGravity;
        set
        {
            if (value != useGravity)
            {
                useGravity = value;
                settingsValid = false;
            }
        }
    }

    public void loadSettings(float speedMax, float speedMin, float gravitySimArg, bool hasGravityArg, float gratingDistanceArg)
    {
        UseGravity = hasGravityArg;
        GravitySim = gravitySimArg;
        SpeedMinMax = new Vector2(speedMin, speedMax);
        GratingDistance = gratingDistanceArg;
        settingsLoaded = true;
        if (!settingsValid)
            calculateTrajectories();
    }

    public void calculateTrajectories()
    {
        settingsValid = true;
        double deltaT;
        double vY;
        if ((launchVelocities == null) || (launchVelocities.Length < lookupPoints))
        {
            launchVelocities = new Vector3[lookupPoints];
            launchColours = new Color[lookupPoints];
        }
        for (int i = 0; i < lookupPoints; i++)
        {
            float frac = (float)i / (lookupPoints-1);
            launchVelocities[i] = new Vector3(Mathf.Lerp(speedMinMax.x, speedMinMax.y, frac), 0, 0);
            deltaT = gratingDistance/ launchVelocities[i].x ;
            vY = useGravity ? -0.5 * deltaT*gravitySim : 0.0;
            launchVelocities[i].y = (float)vY;
            Color l = lerpColour(frac);
            launchColours[i] = l;
        }
    }
}
