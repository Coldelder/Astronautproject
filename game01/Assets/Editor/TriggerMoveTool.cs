using System.Text;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Moves the Level2 trigger one tile unit (1 world unit) to the right.
/// Explicitly sets the new position (8.5, -4, 0) so re-running is idempotent.
/// Flag-triggered; saves Level1.
/// </summary>
[InitializeOnLoad]
public static class TriggerMoveTool
{
    const string FlagName = "trigger_move_flag.txt";
    const string ReportName = "trigger_move_report.txt";
    const string Level1Path = "Assets/Scenes/Level1.unity";
    const string TriggerName = "Level2Trigger";

    static readonly Vector3 NewPosition = new Vector3(8.5f, -4f, 0f);

    static bool consumed;

    static TriggerMoveTool()
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
            Debug.Log("[TriggerMove] " + s);
        }

        try
        {
            string original = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.SaveOpenScenes();
            var scene = EditorSceneManager.OpenScene(Level1Path, OpenSceneMode.Single);

            var trigger = FindByName(TriggerName);
            if (trigger == null)
            {
                Log("ABORT: " + TriggerName + " not found");
                File.WriteAllText(Path.Combine(root, ReportName), report.ToString());
                return;
            }

            Log("Before position=" + trigger.transform.position);
            trigger.transform.position = NewPosition;
            var box = trigger.GetComponent<BoxCollider2D>();
            if (box != null) Log("Box size unchanged=" + box.size + " isTrigger=" + box.isTrigger);
            EditorUtility.SetDirty(trigger);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Log("After position=" + trigger.transform.position + "; Level1 saved.");

            if (!string.IsNullOrEmpty(original))
                EditorSceneManager.OpenScene(original, OpenSceneMode.Single);

            report.AppendLine("TRIGGER_MOVE_DONE");
        }
        catch (System.Exception e)
        {
            report.AppendLine("FATAL: " + e);
            Debug.LogError("[TriggerMove] " + e);
        }

        File.WriteAllText(Path.Combine(root, ReportName), report.ToString());
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
