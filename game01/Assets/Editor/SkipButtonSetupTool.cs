using System.Text;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Adds a "跳过" (skip) button to the intro scene. To guarantee the exact menu
/// look, it clones the real Btn_开始游戏 from MenuScene (keeping its sprite,
/// Border/Fill, font and press effect), relabels it to "跳过", saves it as a
/// prefab, and places one instance at the top-left of IntroScene under the
/// cinematic canvas. Flag-triggered; no GUI click required. Idempotent.
/// </summary>
[InitializeOnLoad]
public static class SkipButtonSetupTool
{
    const string FlagName = "skip_button_flag.txt";
    const string ReportName = "skip_button_report.txt";
    const string MenuScenePath = "Assets/Scenes/MenuScene.unity";
    const string IntroScenePath = "Assets/Scenes/IntroScene.unity";
    const string PrefabPath = "Assets/Prefab/SkipButton.prefab";
    const string SourceButtonName = "Btn_开始游戏";

    static bool consumed;

    static SkipButtonSetupTool()
    {
        EditorApplication.update += OnUpdate;
    }

    static void OnUpdate()
    {
        if (consumed) return;
        string root = Directory.GetParent(Application.dataPath).FullName;
        string flagPath = Path.Combine(root, FlagName);
        if (!File.Exists(flagPath)) return;

        // Never edit scenes during play mode; request exit and retry after
        // returning to edit mode. The flag is deliberately kept.
        if (Application.isPlaying)
        {
            EditorApplication.ExitPlaymode();
            return;
        }

        consumed = true;
        EditorApplication.update -= OnUpdate;
        File.Delete(flagPath);
        string reportPath = Path.Combine(root, ReportName);
        var report = new StringBuilder();
        void Log(string s)
        {
            report.AppendLine(s);
            Debug.Log("[SkipButtonSetup] " + s);
        }

        try
        {
            string originalScene = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.SaveOpenScenes();

            // ---------- Clone a real menu button to capture exact style ----------
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            GameObject source = FindByName(SourceButtonName);
            if (source == null)
            {
                Log("ABORT: source button not found: " + SourceButtonName);
                File.WriteAllText(reportPath, report.ToString());
                return;
            }

            GameObject clone = Object.Instantiate(source);
            clone.name = "Btn_Skip";
            clone.SetActive(true);

            // Drop scene-switching logic; keep all visuals and the press effect.
            var sw = clone.GetComponent<SceneSwitcher>();
            if (sw != null) Object.DestroyImmediate(sw);

            var labelTr = clone.transform.Find("Label");
            if (labelTr != null)
            {
                var label = labelTr.GetComponent<Text>();
                if (label != null)
                {
                    Log("Label before: " + label.text + " after: 跳过");
                    label.text = "跳过";
                    EditorUtility.SetDirty(label);
                }
            }
            else Log("WARN: Label child missing on clone");

            // Runtime skip behaviour lives on the prefab.
            clone.AddComponent<SkipButton>();

            string prefabDir = Path.Combine(root, "Assets/Prefab");
            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(clone, PrefabPath);
            Object.DestroyImmediate(clone);
            Log("Saved skip button prefab: " + PrefabPath);

            // ---------- Place one instance in IntroScene ----------
            var intro = EditorSceneManager.OpenScene(IntroScenePath, OpenSceneMode.Single);

            var canvas = FindByName("CinematicCanvas");
            if (canvas == null)
            {
                Log("ABORT: CinematicCanvas not found in IntroScene");
                File.WriteAllText(reportPath, report.ToString());
                return;
            }

            // Idempotent: remove any previous instance first.
            var previous = FindByName("Btn_Skip");
            if (previous != null) Object.DestroyImmediate(previous);

            var inst = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            inst.name = "Btn_Skip";
            inst.transform.SetParent(canvas.transform, false);

            var rt = inst.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(60f, -60f);
            EditorUtility.SetDirty(rt);

            EditorSceneManager.MarkSceneDirty(intro);
            EditorSceneManager.SaveScene(intro);
            Log("Placed Btn_Skip at top-left; size=" + rt.sizeDelta + " pos=" + rt.anchoredPosition);

            AssetDatabase.SaveAssets();

            if (!string.IsNullOrEmpty(originalScene))
                EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);

            report.AppendLine("SKIP_BUTTON_DONE");
        }
        catch (System.Exception e)
        {
            report.AppendLine("FATAL: " + e);
            Debug.LogError("[SkipButtonSetup] " + e);
        }

        File.WriteAllText(reportPath, report.ToString());
    }

    static GameObject FindByName(string name)
    {
        var roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var r in roots)
        {
            var t = FindRecursive(r.transform, name);
            if (t != null) return t.gameObject;
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
}
