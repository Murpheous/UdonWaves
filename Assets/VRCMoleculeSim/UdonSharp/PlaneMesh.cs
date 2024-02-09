
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
    private Vector2Int highRes = new Vector2Int(640, 400);
    [SerializeField]
    private Vector2Int lowRes = new Vector2Int(16, 10);
    [SerializeField,FieldChangeCallback(nameof(UseHighRes))]
    public bool useHighRes = true;
    public bool UseHighRes
    {
        get => useHighRes;
        set
        {
            if (value != useHighRes)
            {
                useHighRes = value;
            }
            RequestSerialization();
            if (useHighRes)
                AssignBigMesh();
            else
                AssignSmallMesh();
        }
    }

    /*
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
    */
    /*
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
    */

    public Material material;
    
    Mesh smallMesh;
    Mesh bigMesh;
    MeshFilter mf;

    Vector3[] vertices;
    Vector2[] uvs;
    int[] triangles;

    bool AssignBigMesh()
    {
        if (mf == null)
            mf = GetComponent<MeshFilter>();
        if (mf == null)
            return false;
        if (bigMesh == null)
            ConstructBigMesh();
        if (bigMesh == null)
            return false;
        mf.mesh = bigMesh;
        if (material != null)
        {
            Vector4 ms = new Vector4(dimensions.x / highRes.x, dimensions.y / highRes.y, 1.0f / highRes.x, 1.0f / highRes.y);
            material.SetVector("_MeshSpacing", ms);
        }
        return true;
    }

    bool AssignSmallMesh()
    {
        if (mf == null)
            mf = GetComponent<MeshFilter>();
        if (mf == null)
            return false;
        if (smallMesh == null)
            ConstructSmallMesh();
        if (smallMesh == null)
            return false;
        mf.mesh = smallMesh;
        if (material != null)
        {
            Vector4 ms = new Vector4(dimensions.x / lowRes.x, dimensions.y / lowRes.y, 1.0f / lowRes.x, 1.0f / lowRes.y);
            material.SetVector("_MeshSpacing", ms);
        }
        return true;
    }

    private void ConstructSmallMesh()
    {
        if (CalculatePlaneDefinition(lowRes))
        {
            smallMesh = new Mesh();
            if (triangles.Length >= 32767)
                smallMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            smallMesh.vertices = vertices;
            smallMesh.triangles = triangles;
            smallMesh.uv = uvs;
            triangles = null;
            vertices = null;
            uvs = null;
        }
    }

    private void ConstructBigMesh()
    {
        if (CalculatePlaneDefinition(highRes))
        {
            bigMesh = new Mesh();
            if (triangles.Length >= 32767)
                bigMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            bigMesh.vertices = vertices;
            bigMesh.triangles = triangles;
            bigMesh.uv = uvs;
            triangles = null;
            vertices = null;
            uvs = null;
        }
    }

    bool CalculatePlaneDefinition(Vector2Int res)
    {
        if ((res.x <= 0) || (res.y <= 0) || (dimensions.x <= 0) || (dimensions.y <= 0))
            return false;
        int vertLen = (res.x+1) * (res.y + 1);
        vertices = new Vector3[vertLen];
        uvs = new Vector2[vertLen];
        float xPitch = dimensions.x / res.x;
        float yPitch = dimensions.y / res.y;
        float xOffset = dimensions.x / 2;
        float yOffset = dimensions.y / 2;
        int nVertex = 0;
        float xPos = 0;
        float yPos = 0;

        for (int y = 0; y < res.y + 1; y++)
        {
            yPos = y * xPitch;
            for (int x = 0; x < res.x + 1; x++)
            {
                xPos = x * xPitch;
                vertices[nVertex] = new Vector3(xPos - xOffset, 0, yOffset - yPos);
                uvs[nVertex] = new Vector2(xPos / dimensions.x, yPos / dimensions.y);
                nVertex++;
            }
        }

        triangles = new int[res.x*res.y*6];
        int nTriangle = 0;
        for (int row = 0; row < res.y; row++)
        {
            for (int col = 0; col < res.x; col++)
            {
                int i = row * res.x + row + col;
                // First Triangle
                triangles[nTriangle++] = i;
                triangles[nTriangle++] = (i + 1 + res.x);
                triangles[nTriangle++] = (i + 2 + res.x);
                // Second Triangle
                triangles[nTriangle++] = i;
                triangles[nTriangle++] = (i + 2 + res.x);
                triangles[nTriangle++] = (i + 1);
            }
        }
        return true;
    }

    private void Start()
    {
        ConstructSmallMesh();
        ConstructBigMesh();
        if (mf !=  null)
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (material != null)
                mr.material = material;
        }
        UseHighRes = useHighRes;
    }

}
