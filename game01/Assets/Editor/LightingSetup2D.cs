using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using System.IO;
using System.Text;

/// <summary>
/// 把项目切换到 2D Renderer 管线，并在当前场景加一盏 Global Light 2D。
/// 原管线资产会记录到 lighting_setup_backup.json，便于回退。
/// </summary>
public static class LightingSetup2D
{
    const string Urp2DPath = "Assets/Render/2d.asset";
    const string BackupPath = "lighting_setup_backup.json";

    /// <summary>给所有缺少 Light2D 的场景补一盏 Global Light 2D。</summary>
    [MenuItem("Tools/2D Lighting/Add Global Light To All Scenes")]
    public static void AddGlobalLightToAllScenes()
    {
        var active = SceneManager.GetActiveScene();
        int added = 0, skipped = 0;

        foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            bool opened = false;
            Scene scene;
            if (path == active.path)
            {
                scene = active;
            }
            else
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                opened = true;
            }

            bool hasLight = false;
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponentInChildren<Light2D>(true) != null) { hasLight = true; break; }

            if (hasLight)
            {
                skipped++;
            }
            else
            {
                var go = new GameObject("Global Light 2D");
                SceneManager.MoveGameObjectToScene(go, scene);
                var light = go.AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Global;
                light.color = Color.white;
                light.intensity = 1f;
                EditorSceneManager.SaveScene(scene);
                added++;
                Debug.Log($"[2DLight] 已为 {path} 添加 Global Light 2D");
            }

            if (opened) EditorSceneManager.CloseScene(scene, true);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[2DLight] 批量补光完成：新增 {added} 个场景，跳过 {skipped} 个（已有）");
    }

    [MenuItem("Tools/2D Lighting/Switch Pipeline + Global Light")]
    public static void Setup()
    {
        var urp2D = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(Urp2DPath);
        if (urp2D == null) { Debug.LogError($"[2DLight] 找不到 {Urp2DPath}"); return; }

        var sb = new StringBuilder();
        sb.AppendLine($"graphics: {(GraphicsSettings.defaultRenderPipeline ? AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline) : "null")}");

        int original = QualitySettings.GetQualityLevel();
        var names = QualitySettings.names;
        for (int i = 0; i < names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            var cur = QualitySettings.renderPipeline;
            sb.AppendLine($"quality[{i}] {names[i]}: {(cur ? AssetDatabase.GetAssetPath(cur) : "null")}");
        }
        QualitySettings.SetQualityLevel(original, false);

        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), BackupPath), sb.ToString());
        Debug.Log($"[2DLight] 原管线已记录到 {BackupPath}\n{sb}");

        // 切换
        GraphicsSettings.defaultRenderPipeline = urp2D;
        for (int i = 0; i < names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = urp2D;
        }
        QualitySettings.SetQualityLevel(original, false);

        // 当前场景加 Global Light 2D
        var scene = SceneManager.GetActiveScene();
        bool exists = false;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<Light2D>(true) != null) { exists = true; break; }
        }

        if (!exists)
        {
            var go = new GameObject("Global Light 2D");
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.color = Color.white;
            light.intensity = 1f;
            Undo.RegisterCreatedObjectUndo(go, "Add Global Light 2D");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[2DLight] 已在场景 '{scene.name}' 添加 Global Light 2D");
        }
        else
        {
            Debug.Log($"[2DLight] 场景 '{scene.name}' 已有 Light2D，跳过添加");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[2DLight] 完成：Graphics + 所有质量档已切到 2d.asset");
    }
}
