using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 引导面板：全屏叠加，逐页翻看说明图。
///
/// 图片来自 Resources/Guide/：page01 … page08，翻页箭头 next，关闭叉 close。
/// 完全由代码生成，不需要在场景里搭 UI，也不会被任何场景的东西挡住。
///
/// 打开时暂停游戏，关闭后恢复原来的 timeScale。
/// </summary>
public class GuidePanel : MonoBehaviour
{
    const string Folder = "Guide/";

    static GuidePanel instance;

    readonly List<Sprite> pages = new List<Sprite>();
    Sprite nextSprite;
    Sprite closeSprite;

    GameObject root;
    Image pageImage;
    Image icon;
    RectTransform pageRect;
    int index;
    bool open;
    float prevTimeScale = 1f;

    public static bool IsOpen
    {
        get { return instance != null && instance.open; }
    }

    /// <summary>打开引导第一页</summary>
    public static void Show()
    {
        if (instance == null)
        {
            var go = new GameObject("[GuidePanel]");
            Object.DontDestroyOnLoad(go);
            instance = go.AddComponent<GuidePanel>();
            instance.Build();
        }
        instance.OpenFrom(0);
    }

    void Build()
    {
        for (int i = 1; i <= 20; i++)
        {
            var s = Resources.Load<Sprite>(Folder + "page" + i.ToString("D2"));
            if (s == null) break;
            pages.Add(s);
        }
        nextSprite = Resources.Load<Sprite>(Folder + "next");
        closeSprite = Resources.Load<Sprite>(Folder + "close");

        if (pages.Count == 0)
            Debug.LogWarning("[GuidePanel] Resources/Guide 里没有找到 page01 等图片");

        // ---- Canvas ----
        var canvasGo = NewChild(transform, "Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        root = canvasGo;

        // ---- 半透明背景（点了也能关）----
        var bg = NewChild(canvasGo.transform, "Dim");
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.02f, 0.04f, 0.06f, 0.88f);
        Stretch(bgImg.rectTransform);
        var bgBtn = bg.AddComponent<Button>();
        bgBtn.transition = Selectable.Transition.None;
        bgBtn.onClick.AddListener(Close);

        // ---- 图片 ----
        var pageGo = NewChild(canvasGo.transform, "Page");
        pageImage = pageGo.AddComponent<Image>();
        pageImage.preserveAspect = true;
        pageRect = pageImage.rectTransform;
        pageRect.anchorMin = new Vector2(0.5f, 0.5f);
        pageRect.anchorMax = new Vector2(0.5f, 0.5f);
        pageRect.pivot = new Vector2(0.5f, 0.5f);
        pageRect.sizeDelta = new Vector2(1560, 878);
        pageRect.anchoredPosition = Vector2.zero;

        // ---- 翻页 / 关闭按钮 ----
        var btnGo = NewChild(canvasGo.transform, "NextBtn");
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(1f, 1f, 1f, 0.06f);
        var brt = btnImg.rectTransform;
        brt.anchorMin = new Vector2(1f, 0f);
        brt.anchorMax = new Vector2(1f, 0f);
        brt.pivot = new Vector2(1f, 0f);
        brt.sizeDelta = new Vector2(120, 120);
        brt.anchoredPosition = new Vector2(-40, 40);

        var iconGo = NewChild(btnGo.transform, "Icon");
        icon = iconGo.AddComponent<Image>();
        icon.raycastTarget = false;
        var irt = icon.rectTransform;
        Stretch(irt);
        irt.offsetMin = new Vector2(18, 18);
        irt.offsetMax = new Vector2(-18, -18);

        var btn = btnGo.AddComponent<Button>();
        btn.transition = Selectable.Transition.ColorTint;
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(Next);

        // 初始隐藏
        root.SetActive(false);

        // 切场景时强制关闭，避免 timeScale 停在 0
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneChanged;
    }

    void OnSceneChanged(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m)
    {
        if (open) Close();
    }

    void OpenFrom(int page)
    {
        index = Mathf.Clamp(page, 0, Mathf.Max(0, pages.Count - 1));
        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        open = true;
        root.SetActive(true);
        Refresh();
    }

    void Refresh()
    {
        if (pages.Count == 0) return;

        pageImage.sprite = pages[index];

        bool last = index >= pages.Count - 1;
        if (icon != null)
        {
            icon.sprite = last ? closeSprite : nextSprite;
            icon.enabled = icon.sprite != null;
        }
    }

    void Next()
    {
        if (pages.Count == 0) { Close(); return; }

        if (index < pages.Count - 1)
        {
            index++;
            Refresh();
        }
        else
        {
            Close();
        }
    }

    public void Close()
    {
        if (!open) return;
        open = false;
        if (root != null) root.SetActive(false);
        Time.timeScale = prevTimeScale;
    }

    void Update()
    {
        if (!open) return;
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
        if (Input.GetKeyDown(KeyCode.RightArrow)) Next();
        if (Input.GetKeyDown(KeyCode.LeftArrow) && index > 0) { index--; Refresh(); }
    }

    static GameObject NewChild(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
