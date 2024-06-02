
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
    private float screenDistance;

    // Set the camera field of view to match the targetwidth & height rectangle 
    [Tooltip("Spatial Scaling"), FieldChangeCallback(nameof(ExperimentScale))]

    public float experimentScale = 10;
    public float ExperimentScale
    {
        get => experimentScale;
        set
        {
            if (experimentScale != value)
            {
                experimentScale = value;
                UpdateScale();
            }
        }
    }

    private void UpdateScale()
    {
        screenDistance = Mathf.Abs(myCamera.transform.localPosition.x);//(myCamera.transform.position - myTarget.transform.position).magnitude;
        targetAspect = targetWidthHeight.x / targetWidthHeight.y;
        myCamera.aspect = targetAspect;
        if ((screenDistance > 0) && (_zoom > 0))
        {
            float targetHeight = targetWidthHeight.y / _zoom;
            float theta = Mathf.Atan2(targetHeight, screenDistance);
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
        if (_enabled)
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

    bool _enabled = false;
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled != value)
            {
                _enabled = value;
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
