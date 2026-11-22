using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// 在 Level8 的 climber（Ladder 瓦片）上均匀生成同款淡黄色 Point 灯：
/// 颜色/半径/亮度完全沿用 Level4 的 EmergencySwitch 灯（0.88,1,0 / 外半径 4.18 / 峰值 0.5）。
/// 每盏灯挂 ClimberGlowLight，玩家靠近亮、远离灭。
///
/// 触发方式：
/// 1. 菜单：Tools/月核回光/创建梯子引导灯 (Level8)
/// 2. 免点击：工程根目录建 climberlights_flag.txt，脚本编译后自动执行并删除。
/// </summary>
[InitializeOnLoad]
public static class ClimberLightsSetupTool
{
    const string FlagName = "climberlights_flag.txt";
    const string ReportName = "climberlights_report.txt";
    const string Level8Path = "Assets/Scenes/Level8.unity";
    const string RootName = "ClimberLights";

    // 与 Level4 EmergencySwitch 灯光一致的参数
    static readonly Color PaleYellow = new Color(0.8798195f, 1f, 0f, 1f);
    const float OuterRadius = 4.18f;
    const float PeakIntensity = 0.5f;

    static bool consumed;

    static ClimberLightsSetupTool()
    {
        EditorApplication.update += OnUpdate;
    }

    [MenuItem("Tools/月核回光/创建梯子引导灯 (Level8)")]
    static void FromMenu()
    {
        Run(new StringBuilder());
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

        var report = new StringBuilder();
        Run(report);
        File.WriteAllText(Path.Combine(root, ReportName), report.ToString(), new UTF8Encoding(false));
        Debug.Log("[ClimberLightsSetup] 完成");
    }

    static void Run(StringBuilder report)
    {
        void Log(string s)
        {
            report.AppendLine(s);
            Debug.Log("[ClimberLightsSetup] " + s);
        }

        try
        {
            Scene level8 = EditorSceneManager.GetSceneByPath(Level8Path);
            bool openedHere = false;
            if (!level8.IsValid() || !level8.isLoaded)
            {
                level8 = EditorSceneManager.OpenScene(Level8Path, OpenSceneMode.Additive);
                openedHere = true;
            }
            Log("Level8 已加载");

            // 1. 找 climber 与 Player
            GameObject climber = null;
            Transform player = null;
            foreach (var go in level8.GetRootGameObjects())
            {
                if (climber == null)
                {
                    // Grid 底下有 floor / building / glass / climber 多个 Tilemap，必须全部检查
                    foreach (var t in go.GetComponentsInChildren<Tilemap>(true))
                    {
                        if (t == null) continue;
                        // 用名字判断，避免 Tag 未在 TagManager 中注册时 CompareTag 抛异常
                        if (t.gameObject.name.ToLowerInvariant().Contains("climber"))
                        {
                            climber = t.gameObject;
                            break;
                        }
                    }
                }
                if (player == null)
                {
                    var p = go.GetComponentInChildren<Player>(true);
                    if (p != null) player = p.transform;
                }
            }
            if (climber == null)
            {
                Log("没有找到 Tag=Ladder 的 Tilemap（climber）");
                return;
            }
            if (player == null) Log("警告：没有找到 Player，灯会运行时自动按 Tag 查找");

            Tilemap tm = climber.GetComponent<Tilemap>();

            // 2. 统计每一列瓦片（可能有两条梯子）
            var columns = new SortedDictionary<int, List<int>>();
            BoundsInt bounds = tm.cellBounds;
            foreach (var pos in bounds.allPositionsWithin)
            {
                if (!tm.HasTile(pos)) continue;
                if (!columns.ContainsKey(pos.x)) columns[pos.x] = new List<int>();
                columns[pos.x].Add(pos.y);
            }

            // 3. 清掉旧的
            var old = new List<GameObject>();
            foreach (var go in level8.GetRootGameObjects())
                if (go.name == RootName) old.Add(go);
            foreach (var go in old) Object.DestroyImmediate(go);

            GameObject root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, level8);
            root.transform.position = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(root, "Create ClimberLights");

            // 4. 每一列梯子均匀放灯
            int index = 0;
            foreach (var kv in columns)
            {
                int x = kv.Key;
                kv.Value.Sort();
                int minY = kv.Value[0];
                int maxY = kv.Value[kv.Value.Count - 1];
                int count = Mathf.Clamp(Mathf.CeilToInt((maxY - minY + 1) / 3f) + 1, 2, 6);

                Log($"梯子列 x={x}：y {minY}~{maxY}，放置 {count} 盏灯");

                for (int i = 0; i < count; i++)
                {
                    int y = Mathf.RoundToInt(Mathf.Lerp(minY, maxY, count == 1 ? 0f : (float)i / (count - 1)));
                    Vector3 world = tm.GetCellCenterWorld(new Vector3Int(x, y, 0));

                    var go = new GameObject($"ClimberLight_{x}_{i + 1}");
                    go.transform.SetParent(root.transform, true);
                    go.transform.position = new Vector3(world.x, world.y, 0f);

                    Light2D light = go.AddComponent<Light2D>();
                    light.lightType = Light2D.LightType.Point;
                    light.color = PaleYellow;
                    light.intensity = 0f;
                    light.pointLightInnerRadius = 0f;
                    light.pointLightOuterRadius = OuterRadius;

                    var glow = go.AddComponent<ClimberGlowLight>();
                    glow.player = player;
                    glow.maxIntensity = PeakIntensity;
                    glow.onDistance = 2f;
                    glow.offDistance = OuterRadius;
                    glow.fadeSpeed = 6f;

                    index++;
                    Log($"  {go.name} @ ({world.x:0.00}, {world.y:0.00}) 瓦片 y={y}");
                }
            }

            // 5. 保存
            EditorSceneManager.MarkSceneDirty(level8);
            EditorSceneManager.SaveScene(level8);
            Log($"共放置 {index} 盏灯，Level8 已保存");

            if (openedHere) EditorSceneManager.CloseScene(level8, true);
            AssetDatabase.Refresh();
            Log("CLIMBERLIGHTS_DONE");
        }
        catch (System.Exception e)
        {
            report.AppendLine("ERROR: " + e);
            Debug.LogError("[ClimberLightsSetup] " + e);
        }
    }
}
