
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[UdonBehaviourSyncMode(BehaviourSyncMode.None)] // No networking.
public class UdonArrow : UdonSharpBehaviour
{
    [SerializeField]
    private float width = 0.05f;
    [SerializeField]
    private float length = 0.1f;
    [SerializeField]
    private Color color = Color.magenta;
    private bool needsUpdate = false;
    public float Width 
    { 
        get => width; 
        set 
        {
            if (needsUpdate)
                needsUpdate = width != value;
            width = value; 
        } 
    }
    public float Length 
    { 
        get =>length; 
        set 
        {
            if (needsUpdate)
                needsUpdate = length != value;
            length = value; 
        }
    }

    public Color Colour 
    { 
        get => color; 
        set 
        {
            if (color != value)
            {
                color = value;
                SetColour();
            }
        } 
    }

    private void SetColour()
    {
        MeshRenderer mr = this.GetComponent<MeshRenderer>();
        if (mr != null) 
        {
            mr.material.color = color;
        }
    }


    Vector3[] verticesList;
    int[] trianglesList;

    Mesh mesh;
    void Start()
    {
        mesh = new Mesh();
        this.GetComponent<MeshFilter>().mesh = mesh;
        SetColour();
        needsUpdate= true;
    }

    private void GenerateArrow()
    {
        needsUpdate = false;
        float tipLength = width * 4.25f;
        float stemLength = length - tipLength;
        float tipWidth = width * 3.75f; ;

        Vector3 Origin = Vector3.zero;
        float halfWidth = width / 2f;

        if (verticesList == null)
            verticesList = new Vector3[7];
        verticesList[0] = Origin + (halfWidth*Vector3.back);
        verticesList[1] = Origin + (halfWidth * Vector3.forward);
        verticesList[2] = verticesList[0] + (stemLength * Vector3.right);
        verticesList[3] = verticesList[1] + (stemLength * Vector3.right);
        if (trianglesList == null)
            trianglesList = new int[9];
        trianglesList[0] = 0;
        trianglesList[1] = 1;
        trianglesList[2] = 3;
        trianglesList[3] = 0;
        trianglesList[4] = 3;
        trianglesList[5] = 2;

        Origin = stemLength*Vector3.right;
        halfWidth = tipWidth / 2f;
        verticesList[4] = Origin + (halfWidth * Vector3.forward);
        verticesList[5] = Origin + (halfWidth * Vector3.back);
        verticesList[6] = Origin + (tipLength * Vector3.right);

        trianglesList[6] = 4;
        trianglesList[7] = 6;
        trianglesList[8] = 5;
        mesh.vertices = verticesList;
        mesh.triangles = trianglesList;
    }
    private void Update()
    {
        if (needsUpdate)
            GenerateArrow();
    }
}
