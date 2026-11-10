using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using System.Text;

[InitializeOnLoad]
public static class MoonEditTool
{
    const string FlagName = "moon_edit_flag.txt";
    const string ReportName = "moon_edit_report.txt";
    static string projectRoot;
    static string flagPath;
    static string reportPath;
    static StringBuilder report;
    static bool started;

    static MoonEditTool()
    {
        EditorApplication.delayCall += OnDelay;
    }

    static void OnDelay()
    {
        projectRoot = Directory.GetParent(Application.dataPath).FullName;
        flagPath = Path.Combine(projectRoot, FlagName);
        reportPath = Path.Combine(projectRoot, ReportName);
        if (started) return;
        if (!File.Exists(flagPath)) return;
        started = true;
        File.Delete(flagPath);
        report = new StringBuilder();
        try
        {
            DoFontAsset();
            DoMoon();
            DoLevelColors();
            DoLevelOneDoor();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            report.AppendLine("ALL_EDITS_DONE");
        }
        catch (System.Exception e)
        {
            report.AppendLine("FATAL: " + e);
            Debug.LogError("MoonEditTool fatal: " + e);
        }
        File.WriteAllText(reportPath, report.ToString());
        Debug.Log("[MoonEditTool] report written");
    }

    static void Log(string s)
    {
        report.AppendLine(s);
        Debug.Log("[MoonEditTool] " + s);
    }

    // ---------- Task 2: font asset ----------
    static void DoFontAsset()
    {
        string fontsDir = Path.Combine(Application.dataPath, "Fonts");
        if (!Directory.Exists(fontsDir)) Directory.CreateDirectory(fontsDir);
        string srcSimhei = @"C:\Windows\Fonts\simhei.ttf";
        string dstFont = Path.Combine(fontsDir, "simhei.ttf");
        if (File.Exists(srcSimhei))
        {
            File.Copy(srcSimhei, dstFont, true);
            Log("Font copied: simhei.ttf");
        }
        else
        {
            File.Copy(@"C:\Windows\Fonts\msyh.ttc", Path.Combine(fontsDir, "msyh.ttc"), true);
            Log("simhei missing, copied msyh.ttc");
        }
        AssetDatabase.Refresh();
        string relFont = "Assets/Fonts/" + Path.GetFileName(dstFont);
        AssetDatabase.ImportAsset(relFont, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(relFont) as TrueTypeFontImporter;
        if (importer != null)
        {
            // TTF fonts default to dynamic rendering in modern Unity; keep default.
            importer.SaveAndReimport();
            Log("Font importer fontRenderingMode=" + importer.fontRenderingMode);
        }
        var font = AssetDatabase.LoadAssetAtPath<Font>(relFont);
        Log("Font loaded: " + (font != null ? font.name : "NULL"));
    }

    static Font GetChineseFont()
    {
        var f = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/simhei.ttf");
        if (f == null) f = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/msyh.ttc");
        return f;
    }

    // ---------- Task 1: MoonDecor ----------
    static void DoMoon()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MenuScene.unity", OpenSceneMode.Single);
        var go = GameObject.Find("MoonDecor");
        if (go == null) { Log("MoonDecor NOT FOUND"); return; }
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = Vector2.zero;
        var img = go.GetComponent<Image>();
        var flicker = go.GetComponent<MoonBreathFlicker>();
        Log(string.Format("Moon anchorMin={0} anchorMax={1} pivot={2} pos={3} sizeDelta={4}",
            rt.anchorMin, rt.anchorMax, rt.pivot, rt.anchoredPosition, rt.sizeDelta));
        Log("Moon raycastTarget=" + img.raycastTarget + " flicker=" + (flicker != null));
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Log("MenuScene saved");
    }

    // ---------- Task 3: door colors ----------
    static ColorBlock BuildColors()
    {
        var c = ColorBlock.defaultColorBlock;
        c.normalColor = new Color(0.18f, 0.32f, 0.36f, 1f);
        c.highlightedColor = new Color(0.30f, 0.52f, 0.56f, 1f);
        c.pressedColor = new Color(0.09f, 0.18f, 0.21f, 1f);
        c.selectedColor = c.highlightedColor;
        c.disabledColor = new Color(0.09f, 0.18f, 0.21f, 0.5f);
        c.fadeDuration = 0.1f;
        c.colorMultiplier = 1f;
        return c;
    }

    static void DoLevelColors()
    {
        var cb = BuildColors();
        for (int i = 2; i <= 9; i++)
        {
            string path = "Assets/Scenes/Level" + i + ".unity";
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var btnGo = GameObject.Find("DoorBtn");
            if (btnGo == null) { Log(path + " DoorBtn NOT FOUND"); continue; }
            var btn = btnGo.GetComponent<Button>();
            btn.colors = cb;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Log(path + " colors set; normal=" + btn.colors.normalColor + " pressed=" + btn.colors.pressedColor);
        }
    }

    // ---------- LevelOne door ----------
    static void DoLevelOneDoor()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/LevelOne.unity", OpenSceneMode.Single);

        // Idempotent: remove previously created door canvas if present
        var existing = GameObject.Find("DoorCanvas");
        if (existing != null) Object.DestroyImmediate(existing);

        var canvasGo = new GameObject("DoorCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var btnGo = new GameObject("DoorBtn", typeof(RectTransform), typeof(Button),
            typeof(ButtonPressedEffect), typeof(SceneSwitcher));
        btnGo.transform.SetParent(canvasGo.transform, false);
        var btnRt = btnGo.GetComponent<RectTransform>();
        // Match Level2~9 exactly: centered anchors, pivot top-left, position (-880,500)
        btnRt.anchorMin = new Vector2(0.5f, 0.5f);
        btnRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnRt.pivot = new Vector2(0, 1);
        btnRt.anchoredPosition = new Vector2(-880, 500);
        btnRt.sizeDelta = new Vector2(70, 90);

        var btn = btnGo.GetComponent<Button>();
        btn.colors = BuildColors();

        // Frame child (white, target graphic, stretches to fill)
        var frameGo = new GameObject("Frame", typeof(RectTransform), typeof(Image));
        frameGo.transform.SetParent(btnGo.transform, false);
        var frameRt = frameGo.GetComponent<RectTransform>();
        frameRt.anchorMin = Vector2.zero; frameRt.anchorMax = Vector2.one;
        frameRt.offsetMin = Vector2.zero; frameRt.offsetMax = Vector2.zero;
        var frameImg = frameGo.GetComponent<Image>();
        frameImg.color = Color.white;
        btn.targetGraphic = frameImg;

        // Panel (black door body)
        var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(btnGo.transform, false);
        var panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero; panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = new Vector2(6, 8); panelRt.offsetMax = new Vector2(-6, -8);
        panelGo.GetComponent<Image>().color = Color.black;

        // Knob (white handle)
        var knobGo = new GameObject("Knob", typeof(RectTransform), typeof(Image));
        knobGo.transform.SetParent(btnGo.transform, false);
        var knobRt = knobGo.GetComponent<RectTransform>();
        knobRt.anchorMin = new Vector2(1, 0.5f); knobRt.anchorMax = new Vector2(1, 0.5f);
        knobRt.pivot = new Vector2(0.5f, 0.5f);
        knobRt.anchoredPosition = new Vector2(-14, 0);
        knobRt.sizeDelta = new Vector2(6, 6);
        knobGo.GetComponent<Image>().color = Color.white;

        btnGo.GetComponent<SceneSwitcher>().targetScene = "LevelScene";

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Log("LevelOne DoorCanvas+DoorBtn created; target=LevelScene; normal=" + btn.colors.normalColor);
    }
}
