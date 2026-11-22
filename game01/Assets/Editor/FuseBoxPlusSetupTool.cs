using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// 在 Level8 里生成 FuseBoxPlus：
/// 用 Assets/Prefab/FuseBox.prefab 实例化一份（Sprite / 子物体 Light 2D 全部沿用），
/// 移除 FuseBox、换成 FuseBoxPlus，并接好 player / laserControllers(最多 6 个) /
/// fuseBoxRenderer / targetLight(场景的 Global Light 2D) / 参数。
///
/// 触发方式：
/// 1. 菜单：Tools/月核回光/创建 FuseBoxPlus (Level8)
/// 2. 免点击：工程根目录建 fuseboxplus_flag.txt，脚本编译后自动执行并删除。
/// </summary>
[InitializeOnLoad]
public static class FuseBoxPlusSetupTool
{
    const string FlagName = "fuseboxplus_flag.txt";
    const string ReportName = "fuseboxplus_report.txt";
    const string Level8Path = "Assets/Scenes/Level8.unity";
    const string FuseBoxPrefabPath = "Assets/Prefab/FuseBox.prefab";
    const string NewName = "FuseBoxPlus";

    static bool consumed;

    static FuseBoxPlusSetupTool()
    {
        EditorApplication.update += OnUpdate;
    }

    [MenuItem("Tools/月核回光/创建 FuseBoxPlus (Level8)")]
    static void FromMenu()
    {
        Run(MakeReport());
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
        File.Delete(flagPath);
        EditorApplication.update -= OnUpdate;

        var report = MakeReport();
        Run(report);
        File.WriteAllText(Path.Combine(root, ReportName), report.ToString(), new UTF8Encoding(false));
        Debug.Log("[FuseBoxPlusSetup] 完成");
    }

    static StringBuilder MakeReport() => new StringBuilder();

    static void Run(StringBuilder report)
    {
        void Log(string s)
        {
            report.AppendLine(s);
            Debug.Log("[FuseBoxPlusSetup] " + s);
        }

        try
        {
            // 1. 保证 Level8 已加载
            Scene level8 = EditorSceneManager.GetSceneByPath(Level8Path);
            bool openedHere = false;
            if (!level8.IsValid() || !level8.isLoaded)
            {
                if (!File.Exists(Level8Path))
                {
                    Log("找不到场景：" + Level8Path);
                    return;
                }
                level8 = EditorSceneManager.OpenScene(Level8Path, OpenSceneMode.Additive);
                openedHere = true;
            }
            Log("Level8 已加载");

            // 2. 场景内找模板 FuseBox（取位置）、Player、Global Light 2D、LaserController
            FuseBox template = null;
            Transform player = null;
            Light2D globalLight = null;
            var lasers = new List<LaserController>();

            foreach (var rootGo in level8.GetRootGameObjects())
            {
                if (template == null)
                {
                    var f = rootGo.GetComponentInChildren<FuseBox>(true);
                    if (f != null) template = f;
                }

                if (player == null)
                {
                    var p = rootGo.GetComponentInChildren<Player>(true);
                    if (p != null) player = p.transform;
                }

                if (globalLight == null)
                {
                    foreach (var l in rootGo.GetComponentsInChildren<Light2D>(true))
                    {
                        if (l != null && l.gameObject.name == "Global Light 2D" && globalLight == null)
                            globalLight = l;
                        if (l != null && l.lightType == Light2D.LightType.Global && globalLight == null)
                            globalLight = l;
                    }
                }

                foreach (var lc in rootGo.GetComponentsInChildren<LaserController>(true))
                    if (lc != null && !lasers.Contains(lc)) lasers.Add(lc);
            }

            if (template == null)
            {
                Log("Level8 里没有找到 FuseBox 模板，取消。");
                return;
            }
            Log("模板 FuseBox 位置：" + template.transform.position);
            Log("找到 LaserController 数量：" + lasers.Count);

            // 3. 清掉上一次生成的 FuseBoxPlus
            var toRemove = new List<GameObject>();
            foreach (var rootGo in level8.GetRootGameObjects())
                if (rootGo.name == NewName) toRemove.Add(rootGo);
            foreach (var go in toRemove)
            {
                Object.DestroyImmediate(go);
                Log("已移除旧的 " + NewName);
            }

            // 4. 用 FuseBox 预制体实例化
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FuseBoxPrefabPath);
            if (prefab == null)
            {
                Log("找不到 " + FuseBoxPrefabPath);
                return;
            }

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, level8);
            if (inst == null)
            {
                Log("实例化 FuseBox 预制体失败。");
                return;
            }
            inst.name = NewName;
            inst.transform.position = template.transform.position + new Vector3(3f, 0f, 0f);
            Undo.RegisterCreatedObjectUndo(inst, "Create FuseBoxPlus");

            // 5. 换组件
            Object.DestroyImmediate(inst.GetComponent<FuseBox>());
            FuseBoxPlus plus = inst.AddComponent<FuseBoxPlus>();

            // 6. 接线：最多 6 个 LaserController
            int take = Mathf.Min(lasers.Count, FuseBoxPlus.MaxLaserCount);
            var arr = new LaserController[FuseBoxPlus.MaxLaserCount];
            for (int i = 0; i < take; i++) arr[i] = lasers[i];

            plus.player = player;
            plus.laserControllers = arr;
            plus.fuseBoxRenderer = inst.GetComponent<SpriteRenderer>();
            plus.activatedSprite = template.activatedSprite; // 沿用模板保险丝的激活外观
            plus.targetLight = globalLight;
            plus.finalIntensity = 0.8f;
            plus.flickerDuration = 0.5f;
            plus.flickerCount = 5;
            plus.interactRange = 2f;

            Log("player = " + (player != null ? player.name : "NULL"));
            Log("fuseBoxRenderer = " + (plus.fuseBoxRenderer != null ? plus.fuseBoxRenderer.name : "NULL"));
            Log("activatedSprite = " + (plus.activatedSprite != null ? plus.activatedSprite.name : "NULL"));
            Log("targetLight = " + (globalLight != null ? globalLight.name + " (intensity " + globalLight.intensity + ")" : "NULL"));
            for (int i = 0; i < FuseBoxPlus.MaxLaserCount; i++)
                Log("laserControllers[" + i + "] = " + (arr[i] != null ? arr[i].gameObject.name : "None"));

            // 7. 保存场景
            EditorSceneManager.MarkSceneDirty(level8);
            EditorSceneManager.SaveScene(level8);
            Log("Level8 已保存，实例位置 " + inst.transform.position);

            if (openedHere)
                EditorSceneManager.CloseScene(level8, true);

            AssetDatabase.Refresh();
            Log("FUSEBOXPLUS_DONE");
        }
        catch (System.Exception e)
        {
            report.AppendLine("ERROR: " + e);
            Debug.LogError("[FuseBoxPlusSetup] " + e);
        }
    }
}
