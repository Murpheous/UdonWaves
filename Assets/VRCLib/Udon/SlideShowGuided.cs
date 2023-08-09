using System.Collections;
using System.Collections.Generic;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDK3.Image;
using VRC.SDK3.StringLoading;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using TMPro;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]

public class SlideShowGuided : UdonSharpBehaviour
{
    [SerializeField, Tooltip("Primary URL of Slideshow JSON")]
    private VRCUrl slideShowUrl;
    
    [SerializeField, Tooltip("Urls of images")]
    private VRCUrl[] imageUrls;

    [SerializeField, Tooltip("Renderer to show downloaded images on.")]

    private new Renderer renderer;

    [SerializeField, Tooltip("Text field for current caption.")]
    private TextMeshProUGUI textField;

    [SerializeField, Tooltip("Duration in seconds until the next image is shown.")]
    private float slideDurationSeconds = 10f;

    [SerializeField]
    private int _loadedIndex = -1;
    [SerializeField,UdonSynced,FieldChangeCallback(nameof(SlideShowJSON))]
    string slideShowJSON;
    private string SlideShowJSON
    {
        get => slideShowJSON;
        set
        {
            slideShowJSON = value;
            RequestSerialization();
        }
    }

    private VRCImageDownloader _imageDownloader;
    private IUdonEventReceiver _udonEventReceiver;
    [SerializeField]
    private string[] _captions = new string[0];
    [SerializeField]
    private string[] _url_captions = new string[0];
    private Texture2D[] _downloadedTextures;
    DataList slideKeys;
    DataDictionary slideDictionary;
    [SerializeField]
    bool slidesLoaded = false;
    [SerializeField]
    int slideCount = 0;
    DataDictionary currentSlide;
    [SerializeField]
    int thisSlideIndex = -1;
    [SerializeField]
    int thisImageIndex = -1;
    [SerializeField]
    string currentCaption;
    private bool getSlide()
    {
        if (slideCount <=0)
            return false;
        if ((thisSlideIndex <= 0) || (thisSlideIndex > slideCount))
            thisSlideIndex = 1;
        DataToken slideToken;
        if(!slideDictionary.TryGetValue(slideKeys[thisSlideIndex], out slideToken))
        {
            return false;
        }

        currentSlide = slideToken.DataDictionary;
        if (currentSlide == null)
        {
            Debug.Log("CurrentSlide == null");
            return false;
        }
        thisImageIndex = thisSlideIndex;
        currentCaption = "";
        DataToken aToken;
        if (currentSlide.TryGetValue("img", out aToken))
        {

            Debug.Log("img:"+aToken.ToString()+" T=" + aToken.TokenType.ToString());
            thisImageIndex = aToken.TokenType == TokenType.String ? int.Parse(aToken.String) : int.Parse(aToken.ToString());
        }
        if (currentSlide.TryGetValue("caption", out aToken))
        {
            Debug.Log("caption:" + aToken.ToString());
            currentCaption = aToken.String;
        }
        //if (slideShowUrlField != null)
        //    slideShowUrlField. = currentSlideURL;
        return true;
    }
    private bool initSlides()
    {
        if (slideDictionary == null)
            return false;
        slideCount = slideKeys.Count - 1;
        if (slideCount <= 0)
            return false;
        thisSlideIndex = 0;
        _downloadedTextures = new Texture2D[slideCount];
        // It's important to store the VRCImageDownloader as a variable, to stop it from being garbage collected!
        _imageDownloader = new VRCImageDownloader();
        if (_imageDownloader == null)
        {
            Debug.Log("Start SlideShow:No Downloader");
            return false;
        }
        if (!getSlide())
            return false;
        slidesLoaded = true;
        return true;
    }
    void Start()
    {
        slidesLoaded = false;
        // To receive Image and String loading events, 'this' is cast into the type of the event
        _udonEventReceiver = (IUdonEventReceiver)this;
        LoadNextRecursive();
    }

    public void LoadNextRecursive()
    {
        LoadNext();
        SendCustomEventDelayedSeconds(nameof(LoadNextRecursive), slideDurationSeconds);
    }

    private void LoadNext()
    {
        if (!slidesLoaded)
        {
            Debug.Log("SlideShow Fetch URL [" + slideShowUrl + "]");
            // Captions are downloaded once. On success, OnImageLoadSuccess() will be called.
            VRCStringDownloader.LoadUrl(slideShowUrl, _udonEventReceiver);
            return;
        }
        Debug.Log("Loadnext");
        /*
        // All clients share the same server time. That's used to sync the currently displayed image.
        _loadedIndex = (int)(Networking.GetServerTimeInMilliseconds() / 1000f / slideDurationSeconds) % imageUrls.Length;

        var nextTexture = _downloadedTextures[_loadedIndex];
        renderer.sharedMaterial.EnableKeyword("_EMISSION");

        if (nextTexture != null)
        {
            // Image already downloaded! No need to download it again.
            renderer.sharedMaterial.mainTexture = nextTexture;
            renderer.sharedMaterial.SetTexture("_EmissionMap", nextTexture);
        }
        else
        {
            var rgbInfo = new TextureInfo();
            rgbInfo.GenerateMipMaps = true;
            rgbInfo.MaterialProperty = "_EmissionMap";
            Debug.Log("Load Image:" + imageUrls[_loadedIndex]);

            _imageDownloader.DownloadImage(imageUrls[_loadedIndex], renderer.material, _udonEventReceiver, rgbInfo);
        }

        UpdateCaptionText("");
        */
    }

    private void UpdateCaptionText(string caption)
    {
        if (textField == null)
            return;
        textField.text = caption;
    }

    public override void OnStringLoadSuccess(IVRCStringDownload result)
    {
        DataToken jsonResult;
        DataDictionary jsonDict;
        string json = result.Result;
        bool deSerialized = VRCJson.TryDeserializeFromJson(json,out jsonResult);

        if (!deSerialized)
        {
            Debug.Log("json parse error");
            return;
        }
        jsonDict = jsonResult.DataDictionary;
        if (jsonDict == null)
        {
            Debug.Log("json no dictionary");
            return;
        }
        Debug.Log("Has Dictionary");
        DataToken valueToken;
        DataList keys = jsonDict.GetKeys();
        if (jsonDict.TryGetValue("slides",out valueToken))
        {
            slideDictionary = jsonDict;
            slideKeys = keys;
            initSlides();
            return;
        }
        valueToken = keys[0];
        Debug.Log("Unprocessed Dictionary Key[0]=" + valueToken.String);
        return;
    }

    public override void OnStringLoadError(IVRCStringDownload result)
    {
        Debug.LogError($"Could not load string {result.Error}");
    }

    public override void OnImageLoadSuccess(IVRCImageDownload result)
    {
        Debug.Log($"Image loaded: {result.SizeInMemoryBytes} bytes.");

        _downloadedTextures[_loadedIndex] = result.Result;
    }

    public override void OnImageLoadError(IVRCImageDownload result)
    {
        Debug.Log($"Image not loaded: {result.Error.ToString()}: {result.ErrorMessage}.");
    }

    private void OnDestroy()
    {
        Debug.Log("!!!!!!Dispose");
        if (_imageDownloader != null ) 
            _imageDownloader.Dispose();
    }
}
