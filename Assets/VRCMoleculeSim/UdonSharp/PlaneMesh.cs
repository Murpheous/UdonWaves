
using System.Collections.Generic;
using System.Linq;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]

public class PlaneMesh : UdonSharpBehaviour
{
    [SerializeField]
    private Vector2 dimensions = new Vector2(1, 0.625f);
    [SerializeField]
    private Vector2Int resolution = new Vector2Int(513, 320);

    public Vector2 Dimensions
    {
        get => dimensions;
        set
        {
            if ((!isInitialized) || dimensions != value)
            {
                isInitialized = CalculatePlaneDefinition();
                if (isInitialized)
                    AssignPlane();
            }
            dimensions = value;
        }
    }

    public Vector2Int Resolution
    {
        get => resolution;
        set
        {
            if ((!isInitialized) || resolution != value)
            {
                isInitialized = CalculatePlaneDefinition();
                if (isInitialized)
                    AssignPlane();
            }
            resolution = value;
        }
    }


    public Material material;
    bool isInitialized = false;

    Mesh theMesh;
    MeshFilter mf;

    Vector3[] vertices;
    Vector2[] uvs;
    int[] triangles;

    bool AssignPlane()
    {
        if (mf == null)
            mf = GetComponent<MeshFilter>();
        if (mf == null)
            return false;

        theMesh = new Mesh();
        if (triangles.Length >= 32767)
            theMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        theMesh.vertices = vertices;
        theMesh.triangles = triangles;
        theMesh.uv = uvs;
        //theMesh.RecalculateNormals();
        mf.mesh = theMesh;
        triangles = null;
        vertices = null;
        uvs = null;
        return true;
    }
    bool CalculatePlaneDefinition()
    {
        if ((resolution.x <= 0) || (resolution.y <= 0) || (dimensions.x <= 0) || (dimensions.y <= 0))
            return false;
        int vertLen = (resolution.x+1) * (resolution.y + 1);
        vertices = new Vector3[vertLen];
        uvs = new Vector2[vertLen];
        float xPitch = dimensions.x / resolution.x;
        float yPitch = dimensions.y / resolution.y;
        float xOffset = dimensions.x / 2;
        float yOffset = dimensions.y / 2;
        int nVertex = 0;
        for (int y = 0; y < resolution.y + 1; y++)
        {
            for (int x = 0; x < resolution.x + 1; x++)
            {
                vertices[nVertex] = new Vector3(xOffset - x * xPitch, 0, yOffset - y * yPitch);
                uvs[nVertex] = new Vector2(x * xPitch / dimensions.x, y * yPitch / dimensions.y);
                nVertex++;
            }
        }

        triangles = new int[resolution.x*resolution.y*6];
        int nTriangle = 0;
        for (int row = 0; row < resolution.y; row++)
        {
            for (int col = 0; col < resolution.x; col++)
            {
                int i = row * resolution.x + row + col;
                // First Triangle
                triangles[nTriangle++] = i;
                triangles[nTriangle++] = (i + 1 + resolution.x);
                triangles[nTriangle++] = (i + 2 + resolution.x);
                // Second Triangle
                triangles[nTriangle++] = i;
                triangles[nTriangle++] = (i + 2 + resolution.x);
                triangles[nTriangle++] = (i + 1);
            }
        }
        return true;
    }

    private void Start()
    {
        CalculatePlaneDefinition();
        AssignPlane();
        if (mf !=  null)
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (material != null)
            {
                Vector4 ms = new Vector4(dimensions.x / resolution.x, dimensions.y / resolution.y, 1.0f / resolution.x, 1.0f / resolution.y);
                material.SetVector("_MeshSpacing", ms);
                mr.material = material;
            }
        }
    }

}
