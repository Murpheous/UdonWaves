
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class InfoPage : UdonSharpBehaviour
{
    [SerializeField] string pageTitle = "Title";
    [SerializeField,TextArea] string pageBody = "Text";

    public string PageTitle
    {
        get => pageTitle;
        set => pageTitle = value;   
    }
    public string PageBody
    {
        get => pageBody;
        set => pageBody = value;
    }
}
