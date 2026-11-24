using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 在每一关的右上角加一个"引导"按钮。
///
/// 做法：进入场景后找到退出键 DoorBtn，借用它的父节点（也就是 HUD 画布）
/// 和它的尺寸，把按钮贴在画布右上角，点击打开 GuidePanel。
/// 场景不用改，预制体也不用改。
/// </summary>
public static class GuideButtonBootstrap
{
    const string AnchorName = "DoorBtn";
    const string GuideButtonName = "GuideBtn";
    const float Margin = 40f;      // 距右上角的边距

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 只认 Level1..Level9；LevelScene（选关）和菜单不加
        if (!scene.name.StartsWith("Level")) return;
        if (scene.name.Length <= 5 || !char.IsDigit(scene.name[5])) return;

        if (GameObject.Find(GuideButtonName) != null) return;

        var anchor = GameObject.Find(AnchorName);
        if (anchor == null)
        {
            Debug.LogWarning("[GuideButton] 没找到 " + AnchorName + "，这关不加引导按钮");
            return;
        }

        CreateButton(anchor);
    }

    static void CreateButton(GameObject anchor)
    {
        var srcRt = anchor.GetComponent<RectTransform>();
        if (srcRt == null) return;

        var go = new GameObject(GuideButtonName, typeof(RectTransform));
        go.transform.SetParent(srcRt.parent, false);

        var rt = go.GetComponent<RectTransform>();
        // 贴住画布右上角
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.localScale = srcRt.localScale;

        // 尺寸取退出键的高度做成正方形
        float h = Mathf.Abs(srcRt.sizeDelta.y);
        rt.sizeDelta = new Vector2(h, h);
        rt.anchoredPosition = new Vector2(-Margin, -Margin);

        // 底板
        var img = go.AddComponent<Image>();
        var rounded = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        if (rounded != null)
        {
            img.sprite = rounded;
            img.type = Image.Type.Sliced;
        }
        img.color = new Color(0.18f, 0.32f, 0.36f, 1f);

        // 按钮
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = new Color(0.18f, 0.32f, 0.36f, 1f);
        colors.highlightedColor = new Color(0.30f, 0.52f, 0.56f, 1f);
        colors.pressedColor = new Color(0.09f, 0.18f, 0.21f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(GuidePanel.Show);

        // 问号
        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        var txt = textGo.AddComponent<Text>();
        txt.text = "?";
        txt.alignment = TextAnchor.MiddleCenter;
        txt.raycastTarget = false;
        txt.color = new Color(0.93f, 0.97f, 0.98f, 1f);

        Font f = null;
        try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        if (f == null)
        {
            try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
        }
        if (f != null)
        {
            txt.font = f;
            txt.fontSize = Mathf.RoundToInt(h * 0.62f);
        }
        else
        {
            Debug.LogWarning("[GuideButton] 找不到内置字体，按钮上不会显示问号");
        }

        Debug.Log("[GuideButton] 已在 " + AnchorName + " 右侧添加引导按钮");
    }
}
