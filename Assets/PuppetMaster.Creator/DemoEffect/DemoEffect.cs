using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[PuppetMasterEffect("DemoEffect")]
public class DemoEffect : RewardEffect
{
    [Header("Show Video")]
    [FolderSelect(Description = "Relative Path to intro video folder")]
    public string IntroVideosFolder = "DemoEffect/Video/Intro";
    [FileSelect(Description = "Absolute path to intro video file")]
    public string IntroVideoFile = "";
    [NumberInput(Description = "The width resolution of the video")] public int VideoResolutionX = 640;
    [NumberInput(Description = "The height resolution of the video")] public int VideoResolutionY = 360;
    [EditorAttributes.ReadOnly][SerializeField] private RenderTexture _IntroVideoRenderTexture;
    [SerializeField] private VideoPlayer _IntroVideoPlayer;
    [SerializeField] private RawImage _IntroVideoTarget;

    [Header("Show Image")]
    [FolderSelect(Description = "Relative path to image folder")]
    public string ImageFolder = "DemoEffect/Images/Popup";
    [NumberInput(Description = "The width resolution of the image")] public int ImageResolutionX = 640;
    [NumberInput(Description = "The height resolution of the image")] public int ImageResolutionY = 360;
    [SerializeField] private Image _Image;

    [Header("Play Sound")]
    [FolderSelect(Description = "Relative path to sound folder")]
    public string SoundFolder = "DemoEffect/Sounds/Success";
    AudioSource _AudioSource;

    // Start is called before the first frame update
    protected override void OnStart()
    {
        base.OnStart();

        PuppetMasterEvents.ClickThrough = false;

        StartCoroutine(StartEffect());
    }

    private IEnumerator StartEffect()
    {
        yield return LoadAndPlayVideo();

        yield return LoadAndShowImage();
    }

    private IEnumerator LoadAndPlayVideo()
    {
        //Resize the UI canvas to the size of the incoming video
        _IntroVideoTarget.rectTransform.sizeDelta = new Vector2(VideoResolutionX, VideoResolutionY);

        //Create the render texture to the size of the expected video
        _IntroVideoRenderTexture = new RenderTexture(VideoResolutionX, VideoResolutionY, 16, RenderTextureFormat.ARGB32);
        _IntroVideoRenderTexture.Create();

        //Register the render texture to the video player and the image thats going to display it.
        _IntroVideoTarget.texture = _IntroVideoRenderTexture;
        _IntroVideoPlayer.targetTexture = _IntroVideoRenderTexture;

        //Choose between if you want to load from a file or randomly from a folder
        string chosenPath = IntroVideoFile;
        if (string.IsNullOrEmpty(chosenPath))
        {
            Debug.Log($"FilePath not set, checking folder");
            var folder = GetResourceFolder(IntroVideosFolder);
            string[] filePaths = Directory.GetFiles(folder).Where(s => !s.EndsWith(".meta")).ToArray();
            chosenPath = filePaths[UnityEngine.Random.Range(0, filePaths.Length)];
        }
        Debug.Log($"FilePath set for video irl {chosenPath}");

        _IntroVideoPlayer.url = chosenPath;
        _IntroVideoPlayer.Play();

        yield break;
    }

    private IEnumerator LoadAndShowImage()
    {
        //Set the size of the image incoming for the size of image you are going to load.
        _Image.rectTransform.sizeDelta = new Vector2(ImageResolutionX, ImageResolutionY);

        var folder = GetResourceFolder(ImageFolder);
        string[] filePaths = Directory.GetFiles(folder).Where(s => !s.EndsWith(".meta")).ToArray();
        var chosenPath = filePaths[UnityEngine.Random.Range(0, filePaths.Length)];

        yield return LoadImageSprite(chosenPath, _Image);
    }

    public void ClickToCelebrate()
    {
        StartCoroutine(LoadAndPlaySound());
    }

    private IEnumerator LoadAndPlaySound()
    {
        if (_AudioSource == null)
        {
            _AudioSource = CreateAudioSource();
        }

        var folder = GetResourceFolder(SoundFolder);
        string[] filePaths = Directory.GetFiles(folder).Where(s => !s.EndsWith(".meta")).ToArray();
        var chosenPath = filePaths[UnityEngine.Random.Range(0, filePaths.Length)];

        yield return LoadSound(chosenPath, _AudioSource, true, false);
    }

}
