
using System.Xml.Linq;
using UdonSharp;
using Unity.Mathematics;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class GerstnerViaTex : UdonSharpBehaviour
{
    [SerializeField, Tooltip("Sim Shader")]
    Material matWaveSim = null;
    [SerializeField, Tooltip("Distribution Points")]
    private int pointsWide = 64;


    [SerializeField, Tooltip("steep"),Range(0f,1f)] float steep = 0.5f;


    [SerializeField]
    private Vector2[] circleMap = null;
    [SerializeField]
    private Color[]  texMap = null;

    bool hasWaveSimMat = false;

    private void GeneratePoints()
    {
        float k = Mathf.PI*2;
        float a = steep / k;
        if (circleMap == null || circleMap.Length < pointsWide)
        {
            circleMap = new Vector2[pointsWide + 1];
        }
        float delta = 2f/pointsWide;
        for (int i = 0; i < pointsWide; i++)
        {
            float x = -0.5f + i * delta;
            float q = x + a*Mathf.Cos(k*x);
            float p = Mathf.Sin(k*x);
            circleMap[i] = new Vector2(q, p);
        }
        float xBelow = 0;
        float xAbove = 0;

        int indexBelow = 0;
        int indexAbove = 1;

        for (int i = 0;i < pointsWide; i++)
        {
            
        }
    }

    void Start()
    {
        hasWaveSimMat = matWaveSim != null;
        GeneratePoints();
    }
}
