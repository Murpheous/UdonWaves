
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class RailManager : UdonSharpBehaviour
{
    [SerializeField]
    Vector3[] localScales;
    [SerializeField]
    Vector3[] localPositions;
    [SerializeField]
    GameObject thePlank;
    [SerializeField]
    bool[] plankEnables;
    [Range(0,2),SerializeField,FieldChangeCallback(nameof(ScaleIndex))]
    int scaleIndex = 1;
    
    int previousIndex = 0;
    public int ScaleIndex
    {
        get => scaleIndex;
        set
        {
            if (scaleIndex != value)
            {
                previousIndex = scaleIndex;
                scaleIndex = value;
                ScaleStarting();
            }
        }
    }

    [SerializeField, FieldChangeCallback(nameof(ScaleIsChanging))] private bool scaleIsChanging = false;
    public bool ScaleIsChanging
    {
        get => scaleIsChanging;
        set
        {
            if (scaleIsChanging != value)
            {
                scaleIsChanging = value;
            }
            if (!scaleIsChanging)
                ScaleFinished();
        }
    }

    private void ScaleStarting()
    {
        int index = Mathf.Clamp(scaleIndex, 0, plankEnables.Length);
        if (previousIndex > index)
        {
            bool requiredPlank = plankEnables[index];
            if (thePlank != null)
                thePlank.SetActive(requiredPlank);
        }
    }

    private void ScaleFinished()
    {
       int index = Mathf.Clamp(scaleIndex, 0, plankEnables.Length);
       bool requiredPlank = plankEnables[index];
        if (thePlank != null)
            thePlank.SetActive(requiredPlank);
        transform.localScale = localScales[index];
        transform.localPosition = localPositions[index];
        previousIndex = index;
        Debug.Log("PlankEnabled" + plankEnables[index].ToString());
    }

    void Start()
    {
        if ((localScales==null) || (localScales.Length < 3))
        {
            localScales = new Vector3[3];
            for (int i = 0; i < localScales.Length; i++)
                localScales[i] = transform.localScale;
        }
        if ((localPositions == null) || (localPositions.Length < 3))
        {
            localPositions = new Vector3[3];
            for (int i = 0; i < localPositions.Length; i++)
                localPositions[i] = transform.localPosition;
        }
        if ((plankEnables == null) || (plankEnables.Length < 3))
        {
            plankEnables = new bool[3];
            plankEnables[0] = false;
            plankEnables[1] = false;
            plankEnables[2] = true;
            ScaleIsChanging = false;
            if (thePlank != null)
                thePlank.SetActive(plankEnables[scaleIndex]);
        }
    }
}
