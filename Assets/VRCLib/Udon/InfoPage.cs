
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class InfoPage : UdonSharpBehaviour
{
    [SerializeField] string[] pageTitles;
    [SerializeField,TextArea] string[] pageBodies;
    //[SerializeField] 
    private int languageIndex = 0;

    
    public int LangaugeIndex
    {
        get => languageIndex; 
        set => languageIndex = value;
    }
    public string PageTitle
    {
        get 
        {
            if (pageTitles == null)
                return "";
            if (languageIndex >= pageTitles.Length || string.IsNullOrEmpty(pageTitles[languageIndex]))
                return pageTitles[0];
            return pageTitles[languageIndex];
        }
    }
    public string PageBody
    {
        get
        {
            if (pageBodies == null)
                return "";
            if (languageIndex >= pageBodies.Length || string.IsNullOrEmpty(pageBodies[languageIndex]))
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
