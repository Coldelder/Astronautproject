using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 关卡切换：第 7 关 → 第 8 关（lt78）
///
/// 沿用原来的过场逻辑：有 FadeManager 就用 FadeManager.Instance.FadeAndLoad(targetScene)，
/// 没有就自己临时建一层黑幕 Canvas 做淡出 → 加载场景 → 淡入。
///
/// 两种用法都支持：
/// 1. 触发器模式：挂在第 7 关出口的物体上，勾选 Collider2D 的 Is Trigger；
///    玩家（带 Player 标签 + Collider2D）走进来即切换。
/// 2. 按钮模式：挂在「跳过 / 下一关」按钮上，Start 里代码自动绑定 onClick，
///    Inspector 的 OnClick 列表可以留空。
/// </summary>
public class lt78 : MonoBehaviour
{
    [Header("目标场景")]
    [Tooltip("必须是 Build Settings 里的场景名，默认 Level8")]
    public string targetScene = "Level8";

    [Tooltip("目标场景名找不到时，是否退回到 Build Settings 的下一个场景")]
    public bool fallbackToNextBuildScene = true;

    [Header("过场")]
    public float fadeDuration = 0.5f;

    bool triggered;

    void Start()
    {
        // 按钮模式：自动绑定，不用在 Inspector 里拖
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnSkipClicked);
            Debug.Log("[lt78] 检测到 Button 组件，已自动绑定 OnClick");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (other.GetComponentInParent<Player>() == null) return;   // 只认玩家
        Begin();
    }

    /// <summary>给按钮用：Inspector 里也可以手动绑这个 public void 方法。</summary>
    public void OnSkipClicked()
    {
        Begin();
    }

    void Begin()
    {
        if (triggered) return;
        triggered = true;
        Debug.Log("[lt78] 触发切换，当前场景 = " + SceneManager.GetActiveScene().name +
                  "，目标 = " + targetScene);
        StartCoroutine(TransitionRoutine());
    }

    IEnumerator TransitionRoutine()
    {
        string sceneToLoad = ResolveSceneName();
        if (sceneToLoad == null)
        {
            Debug.LogError("[lt78] 找不到可加载的场景：\"" + targetScene +
                           "\" 不在 Build Settings 里，且没有下一个场景可退。");
            triggered = false;
            yield break;
        }

        // 优先走全局 FadeManager
        if (FadeManager.Instance != null)
        {
            Debug.Log("[lt78] 使用 FadeManager 过场 → " + sceneToLoad);
            FadeManager.Instance.FadeAndLoad(sceneToLoad);
            yield break;
        }

        // 没有 FadeManager：自己建黑幕
        Debug.Log("[lt78] 未找到 FadeManager，自建黑幕过场 → " + sceneToLoad);

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

        var op = SceneManager.LoadSceneAsync(sceneToLoad);
        if (op == null)
        {
            Debug.LogError("[lt78] LoadSceneAsync 返回 null，场景名无效：" + sceneToLoad);
            Destroy(canvasGo);
            triggered = false;
            yield break;
        }
        while (!op.isDone) yield return null;

        yield return Fade(group, 0f, fadeDuration);
        Destroy(canvasGo);
    }

    /// <summary>先按场景名找，找不到再退回下一个 Build Settings 场景。</summary>
    string ResolveSceneName()
    {
        if (!string.IsNullOrEmpty(targetScene) && Application.CanStreamedLevelBeLoaded(targetScene))
            return targetScene;

        if (!fallbackToNextBuildScene) return null;

        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next < SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("[lt78] 场景名 \"" + targetScene + "\" 不在 Build Settings 里，退回序号 " + next);
            return SceneManager.GetSceneByBuildIndex(next).name;
        }
        return null;
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
