
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class InfoPage : UdonSharpBehaviour
{
    [SerializeField] string[] pageTitles;
    [SerializeField,TextArea] string[] pageBodies;
    [SerializeField] int languageIndex = 0;

    
    public int LangaugeIndex
    {
        get => languageIndex; 
        set => languageIndex = value;
    }
    public string PageTitle
    {
        get 
        {
            if (languageIndex > pageTitles.Length || pageTitles[languageIndex] == null)
                return pageTitles[0];
            return pageTitles[languageIndex];
        }
    }
    public string PageBody
    {
        get
        {
            if (languageIndex > pageBodies.Length || pageBodies[languageIndex] == null)
                return pageBodies[0];
            return pageBodies[languageIndex];
        }
    }

    private void Start()
    {
        if (pageTitles == null || pageTitles.Length < 1)
        {
            pageTitles = new string[1];
            pageTitles[0] = gameObject.name;
        }
        if (pageBodies == null || pageBodies.Length < 1)
        {
            pageBodies = new string[1];
            pageBodies[0] = "Description for: " + gameObject.name;
        }
    }
}
