
using UdonSharp;
using UnityEngine;
using System.Collections.Generic;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;
using TMPro;

public class WaveMonitor : UdonSharpBehaviour
{
    public CustomRenderTexture texture;
    public Vector2Int SimDimensions = new Vector2Int(2,1);

    [Header("Stimulus")]
    public Vector4 effect;
    [Range(0f, 2.5f), SerializeField]
    float frequency = 0.5f;
    public Slider frequencySlider;
    public TextMeshPro frequencyLabel;
    [Header("Wave paraemters")]

    public float CFL = 0.5f;
    float CFLSq = 0.25f;
    float AbsorbFactor = 0.25f;
    public float waveSpeedPixels = 40; // Speed

    float dt; // Time step
    float effectPeriod = 1;
    float effectTime = 0;

    float lambdaEffect = 1;
    public Material simulationMaterial = null;
    public Material surfaceMaterial = null;

    [Header("Obstacles")]
    public RenderTexture obstaclesTex;
    public Camera obstaclesCamera;

    [Header("UI Toggles")]
    public Toggle TogViewDisplacementMode;
    public Toggle TogViewAmplitudeSquare;
    public Toggle TogViewEnergy;
    public Toggle TogglePlay;
    public Toggle TogglePause;
    public Toggle ToggleReset;
    public bool showDisplacement = true;
    public bool showAmplitudeSquare = false;
    public bool showEnergy          = false;
    private bool animationPlay = true;

    private void UpdateUI()
    {
        if (TogViewDisplacementMode != null)
            TogViewDisplacementMode.isOn = showDisplacement;
        if (TogViewAmplitudeSquare != null)
            TogViewAmplitudeSquare.isOn = showAmplitudeSquare;
        if (TogViewEnergy != null)
            TogViewEnergy.isOn = showEnergy;
    }

    private void UpdateFrequencyUI()
    {
        if (frequencySlider != null)
        {
            if (frequencySlider.value != frequency)
                frequencySlider.value = frequency;
        }
        if (frequencyLabel != null)
            frequencyLabel.text = string.Format("Frequency: {0:0.00}Hz",frequency);
    }


    bool ShowDisplacement
    {
        get { return showDisplacement; }
        set
        {
            showDisplacement = value;
            if (value && showAmplitudeSquare)
                showAmplitudeSquare= false;
            if (value && showEnergy)
                showEnergy = false;
            if (surfaceMaterial != null && showDisplacement)
                surfaceMaterial.SetFloat("_ViewSelection", 0);
            if (!animationPlay)
                UpdateWaves(0);
        }
    }

    bool ShowAmplitudeSquare
    {
        get { return showAmplitudeSquare; }
        set
        {
            showAmplitudeSquare = value;
            if (value && showDisplacement) 
                showDisplacement= false;
            if (value && showEnergy)
                showEnergy = false;
            if (showAmplitudeSquare && (surfaceMaterial != null))
            {
                surfaceMaterial.SetFloat("_ViewSelection", 1);
                if (!animationPlay)
                    UpdateWaves(0);
            }
        }
    }

    bool ShowEnergy
    {
        get { return showAmplitudeSquare; }
        set
        {
            showEnergy = value;
            if (value && showDisplacement)
                showDisplacement = false;
            if (value && showAmplitudeSquare)
                showAmplitudeSquare = false;
            if (showEnergy && (surfaceMaterial != null))
            {
                surfaceMaterial.SetFloat("_ViewSelection", 2);
                if (!animationPlay)
                    UpdateWaves(0);
            }
        }
    }
    //* Slider changed
    public void FrequencyChanged()
    {
        float newFreq = frequency;
        if (frequencySlider!= null)
        {
            if (frequency != frequencySlider.value) 
            {
                newFreq = frequencySlider.value;
            }
        }
        if (newFreq != frequency)
        {
            frequency = newFreq;
            UpdateFrequencyUI();
            CalcParameters();
        }
    }
    //* UI Toggle Handlers

    public void PlayChanged()
    {
        if (TogglePlay != null)
        {
            if (TogglePlay.isOn)
            {
                animationPlay = true;
            }
        }
    }

    public void PauseChanged()
    {
        if (TogglePause != null)
        {
            if (TogglePause.isOn)
                animationPlay = false;
        }
    }

    public void ResetChanged()
    {
        if (ToggleReset != null)
        {
            if (ToggleReset.isOn)
            {
                if (texture != null)
                {
                    texture.Initialize();
                }
                ToggleReset.isOn = false;
            }
        }
    }

    public void ViewAmplitudeChanged()
    {
        if (TogViewDisplacementMode != null)
        {
            if (ShowDisplacement != TogViewDisplacementMode.isOn)
            {
                ShowDisplacement = !showDisplacement;
                UpdateUI();
            }
        }
    }

    public void ViewAmplitudeSqChanged()
    {
        if (TogViewAmplitudeSquare != null)
        {
            if (TogViewAmplitudeSquare.isOn != ShowAmplitudeSquare)
            {
                ShowAmplitudeSquare = !showAmplitudeSquare;
                UpdateUI();
            }
        }
    }

    public void ViewEnergyChanged()
    {
        if (TogViewEnergy != null)
        {
            if (TogViewEnergy.isOn != ShowEnergy)
            {
                ShowEnergy = !showEnergy;
                UpdateUI();
            }
        }
    }


    void CalcParameters()
    {
        CFLSq = CFL * CFL;
        AbsorbFactor = (CFL - 1) / (1 + CFL);
        effectPeriod = 1/frequency;
        dt = CFL / waveSpeedPixels;
        float cBar = waveSpeedPixels / (2*Mathf.PI);
        // Calculate dt using c in Pixels per second
        // CFL = cdt/dx (dt*(c/dx + c/dy));
        // c is pixels per sec and dx=dy=1 (1 pixel)
        // dt = CFL/(c/1+c/1);
        // dt = CFL/(2c);
        lambdaEffect = waveSpeedPixels * effectPeriod;
        if (simulationMaterial != null)
        {
            simulationMaterial.SetFloat("_Cdtdx^2", CFLSq);
            simulationMaterial.SetFloat("_Cbar", cBar);
            simulationMaterial.SetFloat("_C", waveSpeedPixels);
            simulationMaterial.SetFloat("_DeltaT", dt);
            simulationMaterial.SetVector("_Effect", effect);
            simulationMaterial.SetFloat("_CFAbsorb", AbsorbFactor);
        }
    }

    void Start()
    {

        CalcParameters();
        ShowDisplacement = true;
        UpdateUI();

        if (texture != null)
        {
            texture.Initialize();
            if (simulationMaterial!= null)
            {
                texture.material = simulationMaterial;
            }
        }
        if (obstaclesTex != null)
        {
            if (obstaclesCamera == null)
                obstaclesCamera = GetComponentInChildren<Camera>();
            if (obstaclesCamera != null)
            {
                obstaclesCamera.clearFlags = CameraClearFlags.SolidColor;
                obstaclesCamera.targetTexture = obstaclesTex;
                obstaclesCamera.backgroundColor = Color.black;
                obstaclesCamera.orthographic = true;
                // Camera orthographic size is image height in space/2 width is determined by aspect ratio
                //obstaclesCamera.orthographicSize = SimDimensions.y / 2f;
            }
        }

        UpdateFrequencyUI();
        
    }

    void UpdateWaves(float dt)
    {
        bool isReset = false;
        texture.ClearUpdateZones();
        effectTime += dt;
        effect.w = 0;
        if (effectTime > effectPeriod)
        {
            effectTime -= effectPeriod;
            effect.w = 1;
            isReset= true;
        }
        effect.z = Mathf.Sin(effectTime * 2 * Mathf.PI * frequency);
        simulationMaterial.SetVector("_Effect", effect);
        if (isReset)
        {
           // UpdateZones();
        }
        //else
        texture.Update(1);
        effect.w = 0;
        simulationMaterial.SetVector("_Effect", effect);
    }
    double waveTime = 0;
    double updateTime = 0;

    void Update()
    {
        if (animationPlay)
        {
            waveTime += Time.deltaTime;
            while (updateTime < waveTime)
            {
                updateTime += dt;
                UpdateWaves(dt);
            }
        }
    }

    CustomRenderTextureUpdateZone[] TheUpdateZones = new CustomRenderTextureUpdateZone[2];
    //TheUpdateZones = new CustomRenderTextureUpdateZone[2];
   void UpdateZones()
    {
        //CustomRenderTextureUpdateZone DefaultZone = new CustomRenderTextureUpdateZone();
        //TheUpdateZones[0].needSwap = true;
        /*DefaultZone.passIndex = 0;
        DefaultZone.rotation = 0f;
        DefaultZone.updateZoneCenter = new Vector2(0.5f, 0.5f);
        DefaultZone.updateZoneSize = new Vector2(1f, 1f);
        CustomRenderTextureUpdateZone ResetZone = new CustomRenderTextureUpdateZone();
        ResetZone.needSwap = true;
        ResetZone.passIndex = 1;
        ResetZone.rotation = 0f;
        ResetZone.updateZoneCenter = new Vector2(0.5f, 0.5f);
        ResetZone.updateZoneSize = new Vector2(1f, 1f);
        TheUpdateZones[0] = DefaultZone;
        TheUpdateZones[1] = ResetZone;*/
        texture.SetUpdateZones(TheUpdateZones);
    }     
}
