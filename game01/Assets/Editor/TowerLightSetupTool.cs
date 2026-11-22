using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// 在 Level9 信号塔顶部生成强光源（TowerLight）：自动在瓦片里找 tower 塔身，
/// 取右下角那座的塔顶位置，初始关闭，等 Computer 被触发后延迟 0.5s 平滑变强再闪烁。
/// 触发：菜单 Tools/月核回光/信号塔光源 (Level9)，或在工程根目录放 towerlight_flag.txt。
 /// </summary>
[InitializeOnLoad]
public static class TowerLightSetupTool
{
    const string FlagName = "towerlight_flag.txt";
    const string ReportName = "towerlight_report.txt";
    const string LevelPath = "Assets/Scenes/Level9.unity";
    const string RootName = "TowerLight";

    static bool consumed;

    static TowerLightSetupTool() { EditorApplication.update += OnUpdate; }

    [MenuItem("Tools/月核回光/信号塔光源 (Level9)")]
    static void FromMenu()
    {
        var sb = new StringBuilder();
        Run(sb);
        Debug.Log(sb.ToString());
    }

    static void OnUpdate()
    {
        if (consumed) return;
        string root = Directory.GetParent(Application.dataPath).FullName;
        string flagPath = Path.Combine(root, FlagName);
        if (!File.Exists(flagPath)) return;
        if (Application.isPlaying) { EditorApplication.ExitPlaymode(); return; }

        consumed = true;
        File.Delete(flagPath);
        EditorApplication.update -= OnUpdate;

        var report = new StringBuilder();
        Run(report);
        File.WriteAllText(Path.Combine(root, ReportName), report.ToString(), new UTF8Encoding(false));
        Debug.Log("[TowerLightSetup] 完成");
    }

    static void Run(StringBuilder report)
    {
        void Log(string s) { report.AppendLine(s); Debug.Log("[TowerLightSetup] " + s); }

        try
        {
            Scene level = EditorSceneManager.GetSceneByPath(LevelPath);
            bool openedHere = false;
            if (!level.IsValid() || !level.isLoaded)
            {
                level = EditorSceneManager.OpenScene(LevelPath, OpenSceneMode.Additive);
                openedHere = true;
            }
            Log("Level9 已加载");

            foreach (var go in level.GetRootGameObjects())
                if (go.name == RootName) Object.DestroyImmediate(go);

            Vector3 pos = FindTowerTop(level, Log);
            Log($"光源位置 ({pos.x:0.00}, {pos.y:0.00})");

            var root = new GameObject(RootName);
            root.transform.position = pos;
            SceneManager.MoveGameObjectToScene(root, level);

            var light = root.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = new Color(1f, 0.94f, 0.82f, 1f); // 暖白强光
            light.intensity = 0f;
            light.pointLightInnerRadius = 0.4f;
            light.pointLightOuterRadius = 14f;

            Computer comp = null;
            foreach (var go in level.GetRootGameObjects())
            {
                comp = go.GetComponentInChildren<Computer>(true);
                if (comp != null) break;
            }

            var tl = root.AddComponent<TowerLight>();
            tl.targetLight = light;
            tl.computer = comp;
            tl.peakIntensity = 3.2f;
            tl.lowIntensity = 0.35f;
            tl.delay = 0.5f;
            tl.rampDuration = 1.2f;
            tl.blinkOn = 0.5f;
            tl.blinkOff = 0.3f;
            tl.blinkFade = 0.08f;

            if (comp != null) Log($"已绑定 Computer：{comp.gameObject.name}");
            else Log("没找到 Computer，运行时会用 FindObjectOfType 兜底");

            Selection.activeGameObject = root;
            SceneView.lastActiveSceneView?.FrameSelected();

            EditorSceneManager.MarkSceneDirty(level);
            EditorSceneManager.SaveScene(level);
            Log("Level9 已保存");
            Log("TOWERLIGHT_DONE");

            if (openedHere) EditorSceneManager.CloseScene(level, true);
            AssetDatabase.Refresh();
        }
        catch (System.Exception e)
        {
            report.AppendLine("ERROR: " + e);
            Debug.LogError("[TowerLightSetup] " + e);
        }
    }

    /// <summary>扫描所有瓦片层，找出用 tower 贴图拼出来的塔，取右下角那一座的塔顶中心。</summary>
    static Vector3 FindTowerTop(Scene level, System.Action<string> log)
    {
        int count = 0;
        int maxX = int.MinValue;
        var hits = new System.Collections.Generic.List<Vector3Int>();
        Tilemap hitMap = null;

        foreach (var go in level.GetRootGameObjects())
        {
            foreach (var tm in go.GetComponentsInChildren<Tilemap>(true))
            {
                BoundsInt b = tm.cellBounds;
                for (int x = b.xMin; x < b.xMax; x++)
                {
                    for (int y = b.yMin; y < b.yMax; y++)
                    {
                        Sprite sp = tm.GetSprite(new Vector3Int(x, y, 0));
                        if (sp == null) continue;
                        if (!sp.name.ToLower().Contains("tower")) continue;
                        hits.Add(new Vector3Int(x, y, 0));
                        count++;
                        if (x > maxX) maxX = x;
                        if (hitMap == null) hitMap = tm;
                    }
                }
            }
        }
        log($"tower 瓦片数量 {count}，x 最大 {maxX}，所在层 {(hitMap != null ? hitMap.name : "?")}");

        if (count == 0 || hitMap == null)
        {
            log("没找到 tower 瓦片，光源放在玩家上方备用位置");
            return new Vector3(-4.5f, 6f, 0f);
        }

        // 取最靠右的那一段塔身（x 在 maxX 附近 4 格内），取它的最高格作为塔顶
        int topY = int.MinValue;
        int sumX = 0;
        int n = 0;
        foreach (var c in hits)
        {
            if (Mathf.Abs(c.x - maxX) > 4) continue;
            if (c.y > topY) topY = c.y;
            sumX += c.x;
            n++;
        }
        float centerX = n > 0 ? (sumX / (float)n) + 0.5f : maxX + 0.5f;

        Vector3 cellCenter = hitMap.GetCellCenterWorld(new Vector3Int(Mathf.RoundToInt(centerX - 0.5f), topY, 0));
        Vector3 cellTop = hitMap.CellToWorld(new Vector3Int(maxX, topY, 0));

        Vector3 world = new Vector3(cellCenter.x, cellCenter.y + 1.2f, 0f);
        log($"塔顶格 x={maxX} y={topY}，格底 y={cellTop.y:0.00}，灯光高度 {world.y:0.00}");
        return world;
    }
}
