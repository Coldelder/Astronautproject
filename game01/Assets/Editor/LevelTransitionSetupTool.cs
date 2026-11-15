using System.Text;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Places an invisible trigger over the two rightmost floor tiles (cells (7,-5)
/// and (7,-4) of the "floor" tilemap). The trigger carries LevelTransitionTrigger,
/// which fades to black, loads Level2, and fades back in. Geometry is computed
/// from the real Tilemap via GetCellCenterWorld, so no hand-coded coordinates
/// are assumed. Flag-triggered; idempotent.
/// </summary>
[InitializeOnLoad]
public static class LevelTransitionSetupTool
{
    const string FlagName = "level_transition_flag.txt";
    const string ReportName = "level_transition_report.txt";
    const string Level1Path = "Assets/Scenes/Level1.unity";
    const string TriggerName = "Level2Trigger";

    static bool consumed;

    static LevelTransitionSetupTool()
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
            Debug.Log("[LevelTransitionSetup] " + s);
        }

        try
        {
            string original = EditorSceneManager.GetActiveScene().path;
            EditorSceneManager.SaveOpenScenes();
            var scene = EditorSceneManager.OpenScene(Level1Path, OpenSceneMode.Single);

            var floorGo = FindByName("floor");
            if (floorGo == null)
            {
                Log("ABORT: floor tilemap not found");
                File.WriteAllText(Path.Combine(root, ReportName), report.ToString());
                return;
            }
            var tilemap = floorGo.GetComponent<Tilemap>();
            if (tilemap == null)
            {
                Log("ABORT: Tilemap component missing on floor");
                File.WriteAllText(Path.Combine(root, ReportName), report.ToString());
                return;
            }

            var c1 = new Vector3Int(7, -5, 0);
            var c2 = new Vector3Int(7, -4, 0);
            Vector3 center1 = tilemap.GetCellCenterWorld(c1);
            Vector3 center2 = tilemap.GetCellCenterWorld(c2);
            Log("Cell (7,-5) center=" + center1);
            Log("Cell (7,-4) center=" + center2);

            // Idempotent: remove a previous trigger first.
            var previous = FindByName(TriggerName);
            if (previous != null) Object.DestroyImmediate(previous);

            var go = new GameObject(TriggerName);
            Vector3 mid = (center1 + center2) * 0.5f;
            go.transform.position = mid;

            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            // Width 2 (extends half a tile left so the player is caught before
            // the wall), height 2 to cover both stacked tiles.
            box.size = new Vector2(2f, 2f);

            var transition = go.AddComponent<LevelTransitionTrigger>();
            transition.targetScene = "Level2";
            transition.fadeDuration = 0.5f;

            Log("Trigger placed at " + mid + " box size=" + box.size);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Log("Level1 saved.");

            AssetDatabase.SaveAssets();

            if (!string.IsNullOrEmpty(original))
                EditorSceneManager.OpenScene(original, OpenSceneMode.Single);

            report.AppendLine("LEVEL_TRANSITION_DONE");
        }
        catch (System.Exception e)
        {
            report.AppendLine("FATAL: " + e);
            Debug.LogError("[LevelTransitionSetup] " + e);
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
