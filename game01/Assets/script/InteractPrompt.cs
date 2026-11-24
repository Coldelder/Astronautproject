using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 交互提示：玩家靠近可旋转的镜子 / 激光接收器时，
/// 在物体上方显示「按 E 进入旋转状态」；正在操控时显示「再按一次 E 退出状态」。
///
/// 完全不改动场景：启动时自动建一个屏幕空间的文字层，
/// 每帧找最近的、在交互范围内的对象。
/// </summary>
public class InteractPrompt : MonoBehaviour
{
    const string EnterText = "按 E 进入旋转状态";
    const string ExitText = "再按一次 E 退出状态";

    const float WorldOffsetY = 1.1f;   // 显示在物体上方多高
    const float ScreenOffsetY = 40f;   // 再往上偏多少像素
    const int FontSize = 30;

    static InteractPrompt instance;

    readonly List<MirrorController> mirrors = new List<MirrorController>();
    readonly List<LaserController> lasers = new List<LaserController>();

    Camera cam;
    Transform player;
    Text label;
    RectTransform labelRect;
    Canvas canvas;
    float refreshAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (instance == null)
        {
            var go = new GameObject("[InteractPrompt]");
            Object.DontDestroyOnLoad(go);
            instance = go.AddComponent<InteractPrompt>();
            instance.Build();
        }
        instance.Recollect();
    }

    void Build()
    {
        var canvasGo = new GameObject("Canvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(canvasGo.transform, false);

        label = textGo.AddComponent<Text>();
        label.alignment = TextAnchor.MiddleCenter;
        label.fontSize = FontSize;
        label.color = new Color(0.96f, 0.99f, 1f, 1f);
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;

        // 中文字体：工程自带的黑体
        var font = Resources.Load<Font>("Fonts/simhei");
        if (font != null) label.font = font;
        else Debug.LogWarning("[InteractPrompt] 找不到 Resources/Fonts/simhei，提示文字可能不显示");

        // 描边，保证在任何背景上都看得清
        var outline = textGo.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.sizeDelta = new Vector2(600, 60);

        label.enabled = false;
    }

    void Recollect()
    {
        mirrors.Clear();
        lasers.Clear();
        mirrors.AddRange(Object.FindObjectsOfType<MirrorController>());
        lasers.AddRange(Object.FindObjectsOfType<LaserController>());

        var p = GameObject.FindGameObjectWithTag("Player");
        player = p != null ? p.transform : null;
        cam = FindCam();
        refreshAt = Time.unscaledTime + 2f;
    }

    /// <summary>
    /// 找渲染用的相机。这个工程的相机是 Untagged，所以不能只靠 Camera.main。
    /// </summary>
    static Camera FindCam()
    {
        if (Camera.main != null) return Camera.main;

        var all = Camera.allCameras;
        if (all != null && all.Length > 0) return all[0];

        return Object.FindObjectOfType<Camera>();
    }

    void Update()
    {
        if (label == null) return;

        // 引导面板之类暂停时不要挡屏幕
        if (Time.timeScale <= 0f || GuidePanel.IsOpen)
        {
            label.enabled = false;
            return;
        }

        // 场景里动态生成的物体，隔两秒补一次
        if (Time.unscaledTime > refreshAt) Recollect();
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (player == null) { label.enabled = false; return; }
        if (cam == null) cam = FindCam();
        if (cam == null) { label.enabled = false; return; }

        Transform target = null;
        string text = null;

        // 1) 正在操控某个物体 → 显示"再按一次"
        for (int i = 0; i < mirrors.Count; i++)
        {
            if (mirrors[i] != null && mirrors[i].IsControlling) { target = mirrors[i].transform; text = ExitText; break; }
        }
        if (target == null)
        {
            for (int i = 0; i < lasers.Count; i++)
            {
                if (lasers[i] != null && lasers[i].IsControlling) { target = lasers[i].transform; text = ExitText; break; }
            }
        }

        // 2) 没在操控 → 找最近的可交互对象
        if (target == null)
        {
            float best = float.MaxValue;

            for (int i = 0; i < mirrors.Count; i++)
            {
                var m = mirrors[i];
                if (m == null || !m.isInteractable) continue;
                float d = Vector2.Distance(m.transform.position, player.position);
                if (d <= m.interactRange && d < best) { best = d; target = m.transform; }
            }
            for (int i = 0; i < lasers.Count; i++)
            {
                var l = lasers[i];
                if (l == null || !l.isInteractable) continue;
                float d = Vector2.Distance(l.transform.position, player.position);
                if (d <= l.interactRange && d < best) { best = d; target = l.transform; }
            }

            if (target != null) text = EnterText;
        }

        if (target == null)
        {
            label.enabled = false;
            return;
        }

        Vector3 world = target.position + Vector3.up * WorldOffsetY;
        Vector3 sp = cam.WorldToScreenPoint(world);
        if (sp.z <= 0f)
        {
            label.enabled = false;
            return;
        }

        label.text = text;
        label.enabled = true;
        labelRect.position = new Vector3(sp.x, sp.y + ScreenOffsetY, 0f);
    }
}
