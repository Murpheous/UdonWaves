
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class WaveMonitor : UdonSharpBehaviour
{
    public CustomRenderTexture texture;

    [Header("Stimulus")]
    public Vector4 effect;
    [Range(0f, 2.5f), SerializeField]
    float frequency = 0.5f;
    [Header("Wave paraemters")]

    public float CFL = 0.5f;
    float CFLSq = 0.25f;
    float AbsorbFactor = 0.25f;
    public float waveSpeedPixels = 40; // Speed

    float dt; // Time step
    float effectPeriod = 1;
    float effectTime = 0;

    float lambdaEffect = 1;
    public Material textMat = null;

public int iterationPerFrame = 5;

    void CalcParameters()
    {
        effectPeriod = 1/frequency;
        lambdaEffect = waveSpeedPixels * effectPeriod;
        CFLSq = CFL * CFL;
        AbsorbFactor = (CFL - 1) / (1 + CFL);
        dt = CFL / waveSpeedPixels;
    }

    void Start()
    {
        CalcParameters();
        if (texture != null)
        {
            texture.Initialize();
            if (textMat!= null)
            {
                texture.material = textMat;
            }
        }
    }

    void UpdateWaves(float dt)
    {
        texture.ClearUpdateZones();
        effectTime += dt;
        effect.w = dt;
        if (effectTime > effectPeriod)
        {
            effectTime -= effectPeriod;
            effect.w = 0;
        }
        effect.z = Mathf.Sin(effectTime * 2 * Mathf.PI * frequency);
        //waveCompute.SetFloat("dispersion", dispersion);
        if (textMat!= null)
        {
            textMat.SetFloat("_CFL^2", CFLSq);
            textMat.SetVector("_Effect", effect);
            textMat.SetFloat("_CFAbsorb", AbsorbFactor);
        }
        texture.Update(1);
    }
    double waveTime = 0;
    double updateTime = 0;

    void Update()
    {
        waveTime += Time.deltaTime;
        while (updateTime < waveTime)
        {
            updateTime += dt;
            UpdateWaves(dt);
        }
    }
    /*
   void UpdateZones()
    {
        bool leftClick = Input.GetMouseButton(0);
        bool rightClick = Input.GetMouseButton(1);
        if (!leftClick && !rightClick) return;

        RaycastHit hit;
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            var defaultZone = new CustomRenderTextureUpdateZone();
            defaultZone.needSwap = true;
            defaultZone.passIndex = 0;
            defaultZone.rotation = 0f;
            defaultZone.updateZoneCenter = new Vector2(0.5f, 0.5f);
            defaultZone.updateZoneSize = new Vector2(1f, 1f);

            var clickZone = new CustomRenderTextureUpdateZone();
            clickZone.needSwap = true;
            clickZone.passIndex = leftClick ? 1 : 2;
            clickZone.rotation = 0f;
            clickZone.updateZoneCenter = new Vector2(hit.textureCoord.x, 1f - hit.textureCoord.y);
            clickZone.updateZoneSize = new Vector2(0.01f, 0.01f);

            //texture.SetUpdateZones(new CustomRenderTextureUpdateZone[] { defaultZone, clickZone });
        }
    } */
}
