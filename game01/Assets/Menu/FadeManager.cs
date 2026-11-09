using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Global black-screen fade transition. Lives across scenes (DontDestroyOnLoad).
/// Fade in to black (0.25s) -> load scene -> fade out from black (0.25s).
/// </summary>
public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance { get; private set; }

    private CanvasGroup fadeGroup;
    private bool isFading;

    public const float FadeDuration = 0.25f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
    }

    void BuildOverlay()
    {
        var canvasGo = new GameObject("__FadeCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        var imgGo = new GameObject("FadePanel");
        imgGo.transform.SetParent(canvasGo.transform, false);
        var rt = imgGo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = imgGo.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;

        fadeGroup = imgGo.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
        fadeGroup.interactable = false;
    }

    /// <summary>Fade to black, load scene by name, fade back in.</summary>
    public void FadeAndLoad(string sceneName)
    {
        if (!isFading) StartCoroutine(FadeRoutine(sceneName));
    }

    /// <summary>If a scene is entered directly, fade out from black on start.</summary>
    public void FadeInFromBlack()
    {
        if (!isFading && fadeGroup != null && fadeGroup.alpha > 0.5f)
            StartCoroutine(FadeInRoutine());
    }

    IEnumerator FadeRoutine(string sceneName)
    {
        isFading = true;
        yield return FadeTo(1f, FadeDuration);
        var op = SceneManager.LoadSceneAsync(sceneName);
        yield return op;
        yield return FadeTo(0f, FadeDuration);
        isFading = false;
    }

    IEnumerator FadeInRoutine()
    {
        isFading = true;
        yield return FadeTo(0f, FadeDuration);
        isFading = false;
    }

    IEnumerator FadeTo(float target, float duration)
    {
        float start = fadeGroup.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Clamp01(Mathf.Lerp(start, target, t / duration));
            yield return null;
        }
        fadeGroup.alpha = target;
    }
}
