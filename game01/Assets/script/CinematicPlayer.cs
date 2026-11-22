using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

/// <summary>
/// Fullscreen intro/cinematic player. Plays the assigned video exactly once,
/// fades in from black at the start, then automatically loads the next scene.
/// Creates its own RenderTexture and black overlay at runtime (no extra assets).
/// </summary>
public class CinematicPlayer : MonoBehaviour, ISkippableCinematic
{
    public VideoPlayer videoPlayer;
    public RawImage videoImage;
    [Tooltip("Scene loaded after the video finishes.")]
    public string nextScene = "Level1";
    [Tooltip("Black-to-clear fade-in time in seconds.")]
    [Min(0f)] public float fadeInDuration = 1.0f;
    [Tooltip("视频为空时直接跳到下一个场景（方便先搭框架、后补视频）。")]
    public bool advanceWhenNoClip = true;
    [Min(0f)] public float noClipDelay = 0.8f;

    CanvasGroup fadeGroup;
    RenderTexture rt;
    AspectRatioFitter fitter;
    bool finished;
    bool began;

    void Awake()
    {
        rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        rt.Create();

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = rt;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.loopPointReached += OnEnded;

        if (videoImage != null)
        {
            videoImage.texture = rt;
            videoImage.raycastTarget = false;
            fitter = videoImage.GetComponent<AspectRatioFitter>();
            if (fitter == null) fitter = videoImage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 16f / 9f;
        }

        BuildFade();
    }

    void Start()
    {
        fadeGroup.alpha = 1f;

        // 没挂视频时不要卡在黑屏：按设置直接进入下一个场景
        if (videoPlayer.clip == null)
        {
            if (advanceWhenNoClip) StartCoroutine(NoClipAdvance());
            return;
        }

        videoPlayer.Prepare();
        StartCoroutine(PrepareTimeout());
    }

    IEnumerator NoClipAdvance()
    {
        Debug.Log("[CinematicPlayer] 没有视频，直接跳到 " + nextScene);
        yield return new WaitForSecondsRealtime(noClipDelay);
        GoToNext();
    }

    IEnumerator PrepareTimeout()
    {
        float t = 0f;
        while (!began && t < 3f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if (!began)
        {
            Debug.LogWarning("[CinematicPlayer] prepare timeout, playing anyway");
            BeginPlay();
        }
    }

    void OnPrepared(VideoPlayer vp)
    {
        if (began) return;
        if (fitter != null && vp.width > 0 && vp.height > 0)
            fitter.aspectRatio = (float)vp.width / (float)vp.height;
        BeginPlay();
    }

    void BeginPlay()
    {
        if (began) return;
        began = true;
        videoPlayer.Play();
        if (fadeInDuration > 0f) StartCoroutine(FadeInRoutine());
        else fadeGroup.alpha = 0f;
    }

    IEnumerator FadeInRoutine()
    {
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Clamp01(1f - t / fadeInDuration);
            yield return null;
        }
        fadeGroup.alpha = 0f;
    }

    void OnEnded(VideoPlayer vp)
    {
        GoToNext();
    }

    /// <summary>Skip the cinematic immediately and proceed to the next scene.</summary>
    public void Skip()
    {
        if (finished) return;
        if (videoPlayer != null) videoPlayer.Stop();
        GoToNext();
    }

    void GoToNext()
    {
        if (finished) return;
        finished = true;
        if (FadeManager.Instance != null)
            FadeManager.Instance.FadeAndLoad(nextScene);
        else
            SceneManager.LoadScene(nextScene);
    }

    void BuildFade()
    {
        var go = new GameObject("__CinematicFade");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var sc = go.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        go.AddComponent<GraphicRaycaster>();

        var imgGo = new GameObject("Black");
        imgGo.transform.SetParent(go.transform, false);
        var irt = imgGo.AddComponent<RectTransform>();
        irt.anchorMin = Vector2.zero;
        irt.anchorMax = Vector2.one;
        irt.offsetMin = Vector2.zero;
        irt.offsetMax = Vector2.zero;
        var img = imgGo.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;

        fadeGroup = imgGo.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 1f;
        fadeGroup.blocksRaycasts = false;
        fadeGroup.interactable = false;
    }

    void OnDestroy()
    {
        if (rt != null)
        {
            rt.Release();
            Destroy(rt);
        }
    }
}
