using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// When the player enters this 2D trigger, fades the screen to black, loads the
/// target scene, then fades back in. Uses the global FadeManager when present;
/// otherwise builds its own black overlay so the fade always works, even if the
/// scene is launched directly.
/// </summary>
public class Lt45 : MonoBehaviour
{
    [Tooltip("Scene loaded after the fade.")]
    public string targetScene = "IntroScene 1";

    public float fadeDuration = 0.5f;

    bool triggered;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (other.GetComponentInParent<Player>() == null) return;
        triggered = true;
        StartCoroutine(TransitionRoutine());
    }

    IEnumerator TransitionRoutine()
    {
        if (FadeManager.Instance != null)
        {
            FadeManager.Instance.FadeAndLoad(targetScene);
            yield break;
        }

        var canvasGo = new GameObject("__TransitionCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var imgGo = new GameObject("Black");
        imgGo.transform.SetParent(canvasGo.transform, false);

        var irt = imgGo.AddComponent<RectTransform>();
        irt.anchorMin = Vector2.zero;
        irt.anchorMax = Vector2.one;
        irt.offsetMin = Vector2.zero;
        irt.offsetMax = Vector2.zero;

        var img = imgGo.AddComponent<Image>();
        img.color = Color.black;

        var group = imgGo.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        yield return Fade(group, 1f, fadeDuration);

        var op = SceneManager.LoadSceneAsync(targetScene);
        while (!op.isDone) yield return null;

        yield return Fade(group, 0f, fadeDuration);
    }

    IEnumerator Fade(CanvasGroup group, float target, float duration)
    {
        float start = group.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(Mathf.Lerp(start, target, t / duration));
            yield return null;
        }
        group.alpha = target;
    }
}
