
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]

public class CameraZoom : UdonSharpBehaviour
{
    [SerializeField] Transform myTarget;
    [SerializeField] Camera myCamera;
    [SerializeField] Vector2 targetWidthHeight = new Vector2(1, 1);
    [SerializeField] float targetAspect = 0.75f;
    [SerializeField] int _zoom = 1;
    private float myCameraInitialFOV;
    [SerializeField] private float workingDistance = 1;

    private void UpdateScale()
    {
        targetAspect = targetWidthHeight.x / targetWidthHeight.y;
        myCamera.aspect = targetAspect;
        if ((workingDistance > 0) && (_zoom > 0))
        {
            float targetHeight = targetWidthHeight.y / _zoom;
            float theta = Mathf.Atan2(targetHeight, workingDistance);
            theta *= Mathf.Rad2Deg;
            myCamera.fieldOfView = theta;

        }
        else
            myCamera.fieldOfView = myCameraInitialFOV;
    }

    public int Zoom
    {
        get => _zoom;
        set
        {
            if (_zoom != value)
            {
                _zoom = value;
                UpdateScale();
                CheckTexture();
            }
        }
    }

    private void CheckTexture()
    {
        if (myCamera == null)
            return;
        if (camEnabled)
        {
            if (displayMaterial != null)
            {
                displayMaterial.color = Color.white;
                if (displayRT != null)
                    displayMaterial.mainTexture = displayRT;
            }
            if (displayRT != null)
                myCamera.targetTexture = displayRT;
            myCamera.enabled = true;
        }
        else
        {
            myCamera.targetTexture = null;
            myCamera.enabled = false;
        }
    }

    private RenderTexture displayRT;
    public RenderTexture DisplayRender
    {
        get => displayRT;
        set
        {
            displayRT = value;
            CheckTexture();
        }
    }

    [SerializeField]
    bool camEnabled = false;
    public bool Enabled
    {
        get => camEnabled;
        set
        {
            if (camEnabled != value)
            {
                camEnabled = value;
                CheckTexture();
            }
        }
    }


    private Material displayMaterial;

    public Material DisplayMaterial
    {
        get => displayMaterial;
        set { displayMaterial = value; }
    }

    public void SetScale(Vector2 newscale)
    {
        if (newscale == targetWidthHeight)
            return;
        targetWidthHeight = newscale;
        UpdateScale();
    }
    // Start is called before the first frame update
    void Start()
    {
        if (workingDistance <= 0)
            workingDistance = Mathf.Abs(transform.localScale.x);
        if (myCamera == null)
            myCamera = GetComponent<Camera>();
        if (myTarget == null)
            myTarget = transform.parent;
        if (myCamera != null)
        {
            myCameraInitialFOV = myCamera.fieldOfView;
            if (myTarget != null)
            {
                //myCamera.transform.LookAt(myTarget);
                myCamera.enabled = false;
            }
        }
    }
}
