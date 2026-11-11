using System.Text;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu layout enlarger for MenuScene.
/// Triggered by a flag file in the project root.
/// Polls EditorApplication.update so the timing of flag creation does not matter.
/// Only adjusts RectTransform geometry and legacy UI Text font sizes.
/// Does not delete objects, does not reparent, does not touch CanvasScaler,
/// and never modifies Button/onClick/targetGraphic references.
/// Idempotent: re-running just re-applies the same values.
/// </summary>
[InitializeOnLoad]
public static class MenuEnlargeTool
{
    const string FlagName = "menu_enlarge_flag.txt";
    const string ReportName = "menu_enlarge_report.txt";

    static bool consumed;

    static MenuEnlargeTool()
    {
        EditorApplication.update += OnUpdate;
    }

    static void OnUpdate()
    {
        if (consumed) return;
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string flagPath = Path.Combine(projectRoot, FlagName);
        if (!File.Exists(flagPath)) return;

        consumed = true;
        EditorApplication.update -= OnUpdate;
        File.Delete(flagPath);
        string reportPath = Path.Combine(projectRoot, ReportName);
        var report = new StringBuilder();
        void Log(string s)
        {
            report.AppendLine(s);
            Debug.Log("[MenuEnlargeTool] " + s);
        }

        try
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.path.Replace('\\', '/').EndsWith("Scenes/MenuScene.unity"))
            {
                Log("ABORT: active scene is not MenuScene (path=" + scene.path + "). Nothing changed.");
                File.WriteAllText(reportPath, report.ToString());
                return;
            }
            Log("Active scene: " + scene.path);

            // ---------- Title ----------
            var title = Find("Text_Escape the Moon");
            if (title != null)
            {
                var rt = title.GetComponent<RectTransform>();
                Log("Title BEFORE pos=" + rt.anchoredPosition + " size=" + rt.sizeDelta +
                    " anchor=" + rt.anchorMin + " pivot=" + rt.pivot);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, 280f);
                rt.sizeDelta = new Vector2(1400f, 160f);
                var txt = title.GetComponent<Text>();
                if (txt != null)
                {
                    Log("Title font size BEFORE " + txt.fontSize + " AFTER 128");
                    txt.fontSize = 128;
                    EditorUtility.SetDirty(txt);
                }
                EditorUtility.SetDirty(rt);
                Log("Title AFTER pos=" + rt.anchoredPosition + " size=" + rt.sizeDelta);
            }
            else Log("WARN: title not found");

            // ---------- Buttons ----------
            string[] buttonNames =
            {
                "Btn_\u5F00\u59CB\u6E38\u620F", // Btn_开始游戏
                "Btn_\u8BBE\u7F6E",              // Btn_设置
                "Btn_\u9000\u51FA\u6E38\u620F"  // Btn_退出游戏
            };
            float[] buttonY = { 70f, -80f, -230f };

            for (int i = 0; i < buttonNames.Length; i++)
            {
                var btnGo = Find(buttonNames[i]);
                if (btnGo == null) { Log("WARN: button not found: " + buttonNames[i]); continue; }
                var rt = btnGo.GetComponent<RectTransform>();
                Log(buttonNames[i] + " BEFORE pos=" + rt.anchoredPosition + " size=" + rt.sizeDelta);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, buttonY[i]);
                rt.sizeDelta = new Vector2(480f, 100f);
                EditorUtility.SetDirty(rt);

                var label = FindChild(rt, "Label");
                if (label != null)
                {
                    var lrt = label.GetComponent<RectTransform>();
                    lrt.anchorMin = Vector2.zero;
                    lrt.anchorMax = Vector2.one;
                    lrt.offsetMin = Vector2.zero;
                    lrt.offsetMax = Vector2.zero;
                    var ltxt = label.GetComponent<Text>();
                    if (ltxt != null)
                    {
                        Log(buttonNames[i] + " label font BEFORE " + ltxt.fontSize + " AFTER 42");
                        ltxt.fontSize = 42;
                        EditorUtility.SetDirty(ltxt);
                    }
                    EditorUtility.SetDirty(lrt);
                }
                else Log("WARN: Label child missing on " + buttonNames[i]);

                var border = FindChild(rt, "Border");
                if (border != null)
                {
                    var brt = border.GetComponent<RectTransform>();
                    brt.anchorMin = Vector2.zero;
                    brt.anchorMax = Vector2.one;
                    brt.sizeDelta = new Vector2(8f, 8f);
                    brt.anchoredPosition = Vector2.zero;
                    EditorUtility.SetDirty(brt);
                }
                var fill = FindChild(rt, "Fill");
                if (fill != null)
                {
                    var frt = fill.GetComponent<RectTransform>();
                    frt.anchorMin = Vector2.zero;
                    frt.anchorMax = Vector2.one;
                    frt.offsetMin = Vector2.zero;
                    frt.offsetMax = Vector2.zero;
                    EditorUtility.SetDirty(frt);
                }
                Log(buttonNames[i] + " AFTER pos=" + rt.anchoredPosition + " size=" + rt.sizeDelta);
            }

            // ---------- Moon ----------
            var moon = Find("MoonDecor");
            if (moon != null)
            {
                var rt = moon.GetComponent<RectTransform>();
                Log("Moon BEFORE pos=" + rt.anchoredPosition + " size=" + rt.sizeDelta +
                    " anchor=" + rt.anchorMin + " pivot=" + rt.pivot);
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(90f, 50f);
                rt.sizeDelta = new Vector2(980f, 882f);
                EditorUtility.SetDirty(rt);
                Log("Moon AFTER pos=" + rt.anchoredPosition + " size=" + rt.sizeDelta);
            }
            else Log("WARN: MoonDecor not found");

            EditorSceneManager.MarkSceneDirty(scene);
            report.AppendLine("MENU_ENLARGE_DONE");
            Log("Scene marked dirty. Awaiting manual Ctrl+S.");
        }
        catch (System.Exception e)
        {
            report.AppendLine("FATAL: " + e);
            Debug.LogError("[MenuEnlargeTool] fatal: " + e);
        }

        File.WriteAllText(reportPath, report.ToString());
    }

    static GameObject Find(string name)
    {
        var roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
        {
            var found = FindRecursive(root.transform, name);
            if (found != null) return found.gameObject;
        }
        return null;
    }

    static Transform FindRecursive(Transform t, string name)
    {
        if (t.name == name) return t;
        for (int i = 0; i < t.childCount; i++)
        {
            var r = FindRecursive(t.GetChild(i), name);
            if (r != null) return r;
        }
        return null;
    }

    static Transform FindChild(RectTransform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var c = parent.GetChild(i);
            if (c.name == name) return c;
        }
        return null;
    }
}
