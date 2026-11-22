using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// 在 Level9 生成一台 Computer（1×1 瓦片）：自动找地面、挂 Computer 脚本，
/// 初始精灵 computer2，通电后换成 computer1。
/// 触发：菜单 Tools/月核回光/创建电脑 (Level9)，或在工程根目录放 computer_flag.txt。
/// </summary>
[InitializeOnLoad]
public static class ComputerSetupTool
{
    const string FlagName = "computer_flag.txt";
    const string ReportName = "computer_report.txt";
    const string LevelPath = "Assets/Scenes/Level9.unity";
    const string RootName = "Computer";
    const string IdleSpritePath = "Assets/Sprites/computer2.png";      // 未通电
    const string ActiveSpritePath = "Assets/Sprites/computer1.png";    // 通电后

    static bool consumed;

    static ComputerSetupTool() { EditorApplication.update += OnUpdate; }

    [MenuItem("Tools/月核回光/创建电脑 (Level9)")]
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
        Debug.Log("[ComputerSetup] 完成");
    }

    static void Run(StringBuilder report)
    {
        void Log(string s) { report.AppendLine(s); Debug.Log("[ComputerSetup] " + s); }

        try
        {
            Sprite idle = EnsurePixelSprite(IdleSpritePath);
            Sprite active = EnsurePixelSprite(ActiveSpritePath);
            if (idle == null || active == null)
            {
                report.AppendLine("ERROR: 读不到 computer1 / computer2 精灵图");
                return;
            }
            Log($"idle={idle.name} bounds={idle.bounds.size.x:0.00}×{idle.bounds.size.y:0.00} ppu={idle.pixelsPerUnit}");
            Log($"active={active.name} bounds={active.bounds.size.x:0.00}×{active.bounds.size.y:0.00} ppu={active.pixelsPerUnit}");

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

            Transform player = null;
            foreach (var go in level.GetRootGameObjects())
            {
                var p = go.GetComponentInChildren<Player>(true);
                if (p != null) { player = p.transform; break; }
            }

            Vector3 spot = FindFloorSpot(level, player, Log);

            var root = new GameObject(RootName);
            root.transform.position = spot;
            SceneManager.MoveGameObjectToScene(root, level);

            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = idle;
            sr.sortingOrder = 6;

            // 16×16 @ PPU16 = 1×1 世界单位；若导入设置没改成功，则用缩放补到 1 格
            Vector2 spriteSize = sr.sprite.bounds.size;
            if (Mathf.Abs(spriteSize.x - 1f) > 0.01f || Mathf.Abs(spriteSize.y - 1f) > 0.01f)
            {
                float sx = 1f / Mathf.Max(0.0001f, spriteSize.x);
                float sy = 1f / Mathf.Max(0.0001f, spriteSize.y);
                root.transform.localScale = new Vector3(sx, sy, 1f);
                Log($"精灵原始尺寸 {spriteSize.x:0.00}×{spriteSize.y:0.00}，已用缩放 {sx:0.00}/{sy:0.00} 补到 1 格");
            }
            else
            {
                Log("精灵已是 1×1 世界单位，缩放保持 1");
            }

            var box = root.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.9f, 0.9f);
            box.offset = Vector2.zero;

            var comp = root.AddComponent<Computer>();
            comp.computerRenderer = sr;
            comp.idleSprite = idle;
            comp.activatedSprite = active;
            comp.interactRange = 2f;
            comp.consumeBattery = false;
            comp.oneTime = true;

            if (player != null)
            {
                comp.player = player;
                Log($"已绑定玩家 ({player.position.x:0.00}, {player.position.y:0.00})");
            }
            else
            {
                Log("场景里暂时没有玩家，脚本运行时会按 Player 标签自动查找");
            }

            Selection.activeGameObject = root;
            SceneView.lastActiveSceneView?.FrameSelected();

            EditorSceneManager.MarkSceneDirty(level);
            EditorSceneManager.SaveScene(level);
            Log("Level9 已保存");
            Log($"Computer 位置 ({root.transform.position.x:0.00}, {root.transform.position.y:0.00})");

            if (openedHere) EditorSceneManager.CloseScene(level, true);
            AssetDatabase.Refresh();
            Log("COMPUTER_DONE");
        }
        catch (System.Exception e)
        {
            report.AppendLine("ERROR: " + e);
            Debug.LogError("[ComputerSetup] " + e);
        }
    }

    /// <summary>把 16×16 的像素图按 PPU16、Point、无压缩导入，这样一帧就是 1×1 世界单位。</summary>
    static Sprite EnsurePixelSprite(string assetPath)
    {
        var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (ti != null && (ti.spritePixelsPerUnit != 16f || ti.filterMode != FilterMode.Point || ti.spriteImportMode != SpriteImportMode.Single))
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 16f;
            ti.filterMode = FilterMode.Point;
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
            AssetDatabase.Refresh();
        }
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sp == null)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
            sp = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }
        return sp;
    }

    /// <summary>以玩家站立位置为基准向下找地面，把电脑放在玩家旁边的地面上。</summary>
    static Vector3 FindFloorSpot(Scene level, Transform player, System.Action<string> log)
    {
        float px = player != null ? player.position.x : 0f;
        float py = player != null ? player.position.y : 0f;
        log($"玩家位置 ({px:0.00}, {py:0.00})");

        float[] offsets = { 1.6f, -1.6f, 2.6f, -2.6f, 0.8f, -0.8f, 3.6f, -3.6f, 4.6f, -4.6f };
        foreach (float off in offsets)
        {
            float x = px + off;
            float groundY;
            string groundName;
            if (!ProbeGround(x, py, out groundY, out groundName)) continue;

            Vector2 center = new Vector2(x, groundY + 0.5f);
            if (Occupied(center))
            {
                log($"  候选 x={x:0.00} 地面 y={groundY:0.00}（{groundName}）已有东西占用，跳过");
                continue;
            }

            log($"选定 x={x:0.00}，地面 y={groundY:0.00}（{groundName}），电脑中心 ({x:0.00}, {groundY + 0.5f:0.00})");
            return new Vector3(x, groundY + 0.5f, 0f);
        }

        // 退路：直接扫 floor 瓦片层，取离玩家最近的地面
        Tilemap floor = null;
        foreach (var go in level.GetRootGameObjects())
        {
            foreach (var tm in go.GetComponentsInChildren<Tilemap>(true))
            {
                if (tm.name.ToLower().Contains("floor")) { floor = tm; break; }
            }
            if (floor != null) break;
        }
        if (floor == null)
        {
            log("没探测到地面也没找到 floor 瓦片层，电脑放在玩家旁边");
            return new Vector3(px + 1.5f, py, 0f);
        }

        BoundsInt cells = floor.cellBounds;
        log($"floor 格子范围 x {cells.xMin}..{cells.xMax - 1}，y {cells.yMin}..{cells.yMax - 1}");
        float bestScore = float.MaxValue;
        Vector3 best = new Vector3(px + 1.5f, py, 0f);
        for (int x = cells.xMin; x < cells.xMax; x++)
        {
            if (Mathf.Abs(x + 0.5f - px) > 10f) continue;
            for (int y = cells.yMin; y < cells.yMax; y++)
            {
                if (!floor.HasTile(new Vector3Int(x, y, 0))) continue;
                if (floor.HasTile(new Vector3Int(x, y + 1, 0))) continue;
                Vector3 cw = floor.CellToWorld(new Vector3Int(x, y, 0));
                float top = cw.y + 1f;
                float score = Mathf.Abs(top - py) + Mathf.Abs(cw.x + 0.5f - px) * 0.4f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = new Vector3(cw.x + 0.5f, top + 0.5f, 0f);
                }
            }
        }
        log($"按瓦片回退选定 ({best.x:0.00}, {best.y:0.00})");
        return best;
    }

    /// <summary>从玩家站立高度向下打射线找地面。</summary>
    static bool ProbeGround(float x, float playerY, out float groundY, out string groundName)
    {
        groundY = float.NaN;
        groundName = "?";
        var hits = Physics2D.RaycastAll(new Vector2(x, playerY + 0.6f), Vector2.down, 30f);
        System.Array.Sort(hits, (a, b) => b.point.y.CompareTo(a.point.y));
        foreach (var h in hits)
        {
            if (h.collider == null || h.collider.isTrigger) continue;
            if (h.collider.gameObject.name == "Player" || h.collider.GetComponentInParent<Player>() != null) continue;
            groundY = h.point.y;
            groundName = h.collider.gameObject.name;
            return true;
        }
        return false;
    }

    static bool Occupied(Vector2 center)
    {
        var hits = Physics2D.OverlapBoxAll(center, new Vector2(0.85f, 0.85f), 0f);
        foreach (var c in hits)
        {
            if (c == null || c.isTrigger) continue;
            if (c.gameObject.name == "Player" || c.GetComponentInParent<Player>() != null) continue;
            return true;
        }
        return false;
    }
}
