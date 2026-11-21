using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// 把项目里的 SpriteRenderer / TilemapRenderer 批量切换到 URP 的受光材质 Sprite-Lit-Default。
/// 只替换"默认精灵材质"（Sprites-Default 或空），不会碰自定义材质（例如 LaserGlow）。
/// 先 Scan 出清单并备份到工程根的 sprite_material_backup.json，再 Apply。
/// </summary>
public static class SpriteLitMigration
{
    const string LitMatPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";
    const string BackupPath = "sprite_material_backup.json";

    [MenuItem("Tools/Sprite Lit/1. Scan (dry run)")]
    public static void Scan() => Run(true);

    [MenuItem("Tools/Sprite Lit/2. Apply materials")]
    public static void Apply() => Run(false);

    static Material LoadLit()
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(LitMatPath);
        if (m == null)
            Debug.LogError($"[SpriteLit] 找不到受光材质: {LitMatPath}");
        return m;
    }

    static bool IsDefaultSpriteMaterial(Material m)
    {
        if (m == null) return true;
        return m.name == "Sprites-Default" || m.name == "Default-Material";
    }

    static void Run(bool dryRun)
    {
        var lit = LoadLit();
        if (lit == null) return;

        var report = new StringBuilder();
        var backup = new List<string>();
        int changed = 0, skipped = 0;

        // ---------- Prefabs ----------
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) continue;
            bool dirty = false;

            foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!IsDefaultSpriteMaterial(sr.sharedMaterial)) { skipped++; continue; }
                backup.Add($"{path}|{sr.name}|{sr.sharedMaterial?.name ?? "null"}");
                if (!dryRun) { sr.sharedMaterial = lit; dirty = true; }
                changed++;
            }
            foreach (var tr in root.GetComponentsInChildren<TilemapRenderer>(true))
            {
                if (!IsDefaultSpriteMaterial(tr.sharedMaterial)) { skipped++; continue; }
                backup.Add($"{path}|{tr.name}|{tr.sharedMaterial?.name ?? "null"}");
                if (!dryRun) { tr.sharedMaterial = lit; dirty = true; }
                changed++;
            }

            if (dirty) PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        // ---------- 场景 ----------
        var current = SceneManager.GetActiveScene();
        foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            Scene scene;
            bool opened = false;
            if (path == current.path)
            {
                scene = current;
            }
            else
            {
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                opened = true;
            }

            bool dirty = false;
            foreach (var rootGo in scene.GetRootGameObjects())
            {
                foreach (var sr in rootGo.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (!IsDefaultSpriteMaterial(sr.sharedMaterial)) { skipped++; continue; }
                    backup.Add($"{path}|{sr.name}|{sr.sharedMaterial?.name ?? "null"}");
                    if (!dryRun) { sr.sharedMaterial = lit; dirty = true; }
                    changed++;
                }
                foreach (var tr in rootGo.GetComponentsInChildren<TilemapRenderer>(true))
                {
                    if (!IsDefaultSpriteMaterial(tr.sharedMaterial)) { skipped++; continue; }
                    backup.Add($"{path}|{tr.name}|{tr.sharedMaterial?.name ?? "null"}");
                    if (!dryRun) { tr.sharedMaterial = lit; dirty = true; }
                    changed++;
                }
            }

            if (dirty) EditorSceneManager.SaveScene(scene);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }

        File.WriteAllLines(Path.Combine(Directory.GetCurrentDirectory(), BackupPath), backup);

        report.AppendLine($"[SpriteLit] 模式: {(dryRun ? "扫描（未修改）" : "已应用")}");
        report.AppendLine($"[SpriteLit] 待替换/已替换: {changed}");
        report.AppendLine($"[SpriteLit] 跳过（自定义材质）: {skipped}");
        report.AppendLine($"[SpriteLit] 原始材质清单已写入: {BackupPath}");
        report.AppendLine("  " + string.Join("\n  ", backup.GetRange(0, Mathf.Min(backup.Count, 40))));
        Debug.Log(report.ToString());
    }
}
