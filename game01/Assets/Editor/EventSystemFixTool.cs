using System.Text;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Fixes the intro scene so real mouse clicks work. IntroScene was created from
/// an empty scene and therefore lacked an EventSystem: programmatic
/// onClick.Invoke() still succeeded, but physical clicks were ignored. This tool
/// adds an EventSystem with a StandaloneInputModule, then saves the scene.
/// Flag-triggered; idempotent.
/// </summary>
[InitializeOnLoad]
public static class EventSystemFixTool
{
    const string FlagName = "eventsystem_fix_flag.txt";
    const string ReportName = "eventsystem_fix_report.txt";
    const string IntroScenePath = "Assets/Scenes/IntroScene.unity";

    static bool consumed;

    static EventSystemFixTool()
    {
        EditorApplication.update += OnUpdate;
    }

    static void OnUpdate()
    {
        if (consumed) return;
        string root = Directory.GetParent(Application.dataPath).FullName;
        string flagPath = Path.Combine(root, FlagName);
        if (!File.Exists(flagPath)) return;

        if (Application.isPlaying)
        {
            EditorApplication.ExitPlaymode();
            return;
        }

        consumed = true;
        EditorApplication.update -= OnUpdate;
        File.Delete(flagPath);
        var report = new StringBuilder();
        void Log(string s)
        {
            report.AppendLine(s);
            Debug.Log("[EventSystemFix] " + s);
        }

        try
        {
            string original = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.SaveOpenScenes();
            var scene = EditorSceneManager.OpenScene(IntroScenePath, OpenSceneMode.Single);

            var existing = Object.FindObjectOfType<EventSystem>();
            if (existing == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<StandaloneInputModule>();
                Log("Created EventSystem with StandaloneInputModule.");
            }
            else
            {
                Log("EventSystem already present: " + existing.name);
                if (existing.GetComponent<StandaloneInputModule>() == null)
                {
                    existing.gameObject.AddComponent<StandaloneInputModule>();
                    Log("Added missing StandaloneInputModule.");
                }
            }

            var skipGo = GameObject.Find("Btn_Skip");
            if (skipGo != null)
            {
                var btn = skipGo.GetComponent<Button>();
                Log("Btn_Skip present; interactable=" + (btn != null && btn.interactable));
            }
            else Log("WARN: Btn_Skip not found");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Log("IntroScene saved.");

            if (!string.IsNullOrEmpty(original))
                EditorSceneManager.OpenScene(original, OpenSceneMode.Single);

            report.AppendLine("EVENTSYSTEM_FIX_DONE");
        }
        catch (System.Exception e)
        {
            report.AppendLine("FATAL: " + e);
            Debug.LogError("[EventSystemFix] " + e);
        }

        File.WriteAllText(Path.Combine(root, ReportName), report.ToString());
    }
}
