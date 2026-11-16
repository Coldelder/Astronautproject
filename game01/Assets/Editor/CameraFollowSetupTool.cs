using System.Text;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Flag-driven, idempotent setup for the Level4 horizontal camera follow.
/// Trigger: create the file "camera_follow_setup_flag.txt" in the project root.
/// - Adds CameraFollow to the Camera (defaults: deadZoneRatio 2/3, smoothTime 0.2).
/// - Restores the ground collision on "floor" to match the other levels:
///   Rigidbody2D (Static) + CompositeCollider2D + TilemapCollider2D (usedByComposite).
/// Saves the scene and writes "camera_follow_setup_report.txt".
/// </summary>
[InitializeOnLoad]
public static class CameraFollowSetupTool
{
    const string FlagName = "camera_follow_setup_flag.txt";
    const string ReportName = "camera_follow_setup_report.txt";
    const string Level4Path = "Assets/Scenes/Level4.unity";

    static bool consumed;

    static CameraFollowSetupTool()
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
            Debug.Log("[CameraFollowSetup] " + s);
        }

        try
        {
            string original = EditorSceneManager.GetActiveScene().path;
            if (original != Level4Path)
            {
                EditorSceneManager.SaveOpenScenes();
                EditorSceneManager.OpenScene(Level4Path, OpenSceneMode.Single);
            }
            var scene = EditorSceneManager.GetActiveScene();

            // --- Camera: attach CameraFollow ---
            GameObject cameraGo = FindByName("Camera");
            if (cameraGo == null)
            {
                Log("ABORT: Camera not found");
                File.WriteAllText(Path.Combine(root, ReportName), report.ToString());
                return;
            }

            var follow = cameraGo.GetComponent<CameraFollow>();
            if (follow == null)
            {
                follow = cameraGo.AddComponent<CameraFollow>();
                Log("Added CameraFollow to Camera");
            }
            else
            {
                Log("CameraFollow already present on Camera");
            }
            // Target stays null so the script auto-finds the Player-tagged object.
            Log("CameraFollow fields: deadZoneRatio=" + follow.deadZoneRatio.ToString("F4") +
                " smoothTime=" + follow.smoothTime + " useManualBounds=" + follow.useManualBounds +
                " boundsTilemap=" + (follow.boundsTilemap != null ? follow.boundsTilemap.name : "<auto union>"));

            // --- floor: restore collider trio (matches Level2/3) ---
            GameObject floorGo = FindByName("floor");
            if (floorGo == null)
            {
                Log("ABORT: floor not found");
                File.WriteAllText(Path.Combine(root, ReportName), report.ToString());
                return;
            }

            var rb = floorGo.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = floorGo.AddComponent<Rigidbody2D>();
                Log("Added Rigidbody2D to floor");
            }
            rb.bodyType = RigidbodyType2D.Static;
            Log("Rigidbody2D bodyType=" + rb.bodyType);

            var composite = floorGo.GetComponent<CompositeCollider2D>();
            if (composite == null)
            {
                composite = floorGo.AddComponent<CompositeCollider2D>();
                Log("Added CompositeCollider2D to floor");
            }
            Log("CompositeCollider2D geometryType=" + composite.geometryType +
                " generationType=" + composite.generationType);

            var tileCollider = floorGo.GetComponent<TilemapCollider2D>();
            if (tileCollider == null)
            {
                tileCollider = floorGo.AddComponent<TilemapCollider2D>();
                Log("Added TilemapCollider2D to floor");
            }
            tileCollider.usedByComposite = true;
            Log("TilemapCollider2D usedByComposite=" + tileCollider.usedByComposite);

            // --- Read the actual drawn tile bounds for the report ---
            var tm = floorGo.GetComponent<Tilemap>();
            tm.CompressBounds();
            BoundsInt cell = tm.cellBounds;
            Vector3 wMin = tm.CellToWorld(cell.min);
            Vector3 wMax = tm.CellToWorld(cell.max);
            Log("floor cellBounds x=" + cell.xMin + ".." + cell.xMax +
                " y=" + cell.yMin + ".." + cell.yMax + " tileAnchor=" + tm.tileAnchor);
            Log("floor world edges: left=" + wMin.x.ToString("F3") + " right=" + wMax.x.ToString("F3"));

            EditorUtility.SetDirty(cameraGo);
            EditorUtility.SetDirty(floorGo);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Log("Scene saved: " + scene.path);

            if (!string.IsNullOrEmpty(original) && original != Level4Path)
                EditorSceneManager.OpenScene(original, OpenSceneMode.Single);

            report.AppendLine("CAMERA_FOLLOW_SETUP_DONE");
        }
        catch (System.Exception e)
        {
            report.AppendLine("FATAL: " + e);
            Debug.LogError("[CameraFollowSetup] " + e);
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
