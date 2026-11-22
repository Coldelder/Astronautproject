using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 在 Level8 生成电梯（1×2 瓦片）：
/// 沿玩家站立高度向下找地面，检查该列是否有足够净空与空位，然后搭好门框/碰撞/视觉，
/// 挂 Elevator 脚本并保存场景。
/// 触发：菜单 Tools/月核回光/创建电梯 (Level8)，或在工程根目录放 elevator_flag.txt。
/// </summary>
[InitializeOnLoad]
public static class ElevatorSetupTool
{
    const string FlagName = "elevator_flag.txt";
    const string ReportName = "elevator_report.txt";
    const string Level8Path = "Assets/Scenes/Level8.unity";
    const string RootName = "elevator";
    const string SpritePath = "Assets/Sprites/elevator_white.png";

    const float Width = 1f;      // 1 瓦片宽
    const float Height = 2f;     // 2 瓦片高
    const float WallThickness = 0.1f;

    static bool consumed;

    static ElevatorSetupTool() { EditorApplication.update += OnUpdate; }

    [MenuItem("Tools/月核回光/创建电梯 (Level8)")]
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
        Debug.Log("[ElevatorSetup] 完成");
    }

    static void Run(StringBuilder report)
    {
        void Log(string s) { report.AppendLine(s); Debug.Log("[ElevatorSetup] " + s); }

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

            foreach (var go in level8.GetRootGameObjects())
                if (go.name == RootName) Object.DestroyImmediate(go);

            Sprite sprite = EnsureWhiteSprite();

            Transform player = null;
            foreach (var go in level8.GetRootGameObjects())
            {
                var p = go.GetComponentInChildren<Player>(true);
                if (p != null) { player = p.transform; break; }
            }
            float playerX = player != null ? player.position.x : 0f;
            float playerY = player != null ? player.position.y : 0f;
            Log($"玩家位置 ({playerX:0.00}, {playerY:0.00})");

            float spotX = float.NaN, spotY = float.NaN;
            float bestX = float.NaN, bestY = 0f, bestClear = -1f;
            int idx = 0;

            foreach (float x in CandidateXs(playerX))
            {
                idx++;
                float groundY, clear;
                bool freeSpot;
                string groundName;
                if (!Probe(x, playerY, out groundY, out clear, out freeSpot, out groundName)) continue;
                Log($"  候选 x={x:0.00} 地面 y={groundY:0.00}（{groundName}） 净空 {clear:0.00} 空位 {(freeSpot ? "空" : "占")}");

                if (clear > bestClear) { bestClear = clear; bestX = x; bestY = groundY; }
                if (freeSpot && clear >= 4.2f && Mathf.Abs(x - playerX) > 1.2f)
                {
                    spotX = x; spotY = groundY;
                    break;
                }
            }

            if (float.IsNaN(spotX) && !float.IsNaN(bestX))
            {
                spotX = bestX; spotY = bestY;
                Log($"没有 4 格净空的列，退而选净空最大的一列：x={spotX:0.00}，净空 {bestClear:0.00}");
            }
            if (float.IsNaN(spotX))
            {
                spotX = playerX + 1.8f;
                spotY = playerY - 1f;
                Log($"没探到地面，退回到 ({spotX:0.00}, {spotY:0.00})，请手动微调");
            }

            GameObject root = Build(sprite, new Vector3(spotX, spotY + Height * 0.5f, 0f));
            SceneManager.MoveGameObjectToScene(root, level8);
            Log($"电梯已生成 @ ({root.transform.position.x:0.00}, {root.transform.position.y:0.00})，底部 y={spotY:0.00}");

            if (player != null)
            {
                var col = player.GetComponent<Collider2D>();
                if (col != null)
                    Log($"玩家碰撞体世界尺寸 {col.bounds.size.x:0.00} × {col.bounds.size.y:0.00}（电梯内腔 0.94 × 1.90）");
            }

            Selection.activeGameObject = root;
            SceneView.lastActiveSceneView?.FrameSelected();

            EditorSceneManager.MarkSceneDirty(level8);
            EditorSceneManager.SaveScene(level8);
            Log("Level8 已保存");

            if (openedHere) EditorSceneManager.CloseScene(level8, true);
            AssetDatabase.Refresh();
            Log("ELEVATOR_DONE");
        }
        catch (System.Exception e)
        {
            report.AppendLine("ERROR: " + e);
            Debug.LogError("[ElevatorSetup] " + e);
        }
    }

    static IEnumerable<float> CandidateXs(float playerX)
    {
        for (float x = playerX + 1.6f; x <= playerX + 14f; x += 0.4f) yield return x;
        for (float x = playerX - 1.6f; x >= playerX - 14f; x -= 0.4f) yield return x;
    }

    /// <summary>在玩家站立高度附近向下找地面，再测电梯占位与上方净空。</summary>
    static bool Probe(float x, float playerY, out float groundY, out float clearance, out bool freeSpot, out string groundName)
    {
        groundY = float.NaN;
        clearance = 0f;
        freeSpot = false;
        groundName = "?";

        var hits = Physics2D.RaycastAll(new Vector2(x, playerY + 0.5f), Vector2.down, 30f);
        System.Array.Sort(hits, (a, b) => b.point.y.CompareTo(a.point.y));
        foreach (var h in hits)
        {
            if (h.collider == null || h.collider.isTrigger) continue;
            if (h.collider.gameObject.name == "Player") continue;
            groundY = h.point.y;
            groundName = h.collider.gameObject.name;
            break;
        }
        if (float.IsNaN(groundY)) return false;

        // 电梯自身占位（抬高一点避开地面本身）必须是空的：墙/地板瓦片都算占用
        var overl = Physics2D.OverlapBoxAll(new Vector2(x, groundY + Height * 0.5f + 0.2f),
            new Vector2(Width - 0.1f, Height - 0.5f), 0f);
        freeSpot = true;
        foreach (var c in overl)
        {
            if (c == null || c.isTrigger) continue;
            freeSpot = false;
            break;
        }

        // 上方净空：从电梯顶部往上探 4.3
        var up = Physics2D.RaycastAll(new Vector2(x, groundY + Height + 0.15f), Vector2.up, 4.5f);
        float nearest = 4.5f;
        foreach (var h in up)
        {
            if (h.collider == null || h.collider.isTrigger) continue;
            float d = h.point.y - (groundY + Height);
            if (d < nearest) nearest = d;
        }
        clearance = nearest;
        return true;
    }

    /// <summary>生成一个 1×1 世界单位的白色方形 Sprite，供电梯各部件染色。</summary>
    static Sprite EnsureWhiteSprite()
    {
        string abs = Path.Combine(Directory.GetParent(Application.dataPath).FullName, SpritePath);
        if (!File.Exists(abs))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(abs));
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    tex.SetPixel(x, y, Color.white);
            tex.Apply();
            File.WriteAllBytes(abs, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceUpdate);

            var ti = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 4f;
            ti.filterMode = FilterMode.Point;
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
            AssetDatabase.Refresh();
        }

        Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (sp == null)
        {
            AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
            AssetDatabase.Refresh();
            sp = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }
        if (sp == null) Debug.LogError("[ElevatorSetup] 读取电梯白块 sprite 失败：" + SpritePath);
        else Debug.Log($"[ElevatorSetup] sprite OK: {sp.name} rect={sp.rect.size} ppu={sp.pixelsPerUnit}");
        return sp;
    }

    static GameObject Build(Sprite sprite, Vector3 worldPos)
    {
        var root = new GameObject(RootName);
        root.transform.position = worldPos;
        Undo.RegisterCreatedObjectUndo(root, "Create Elevator");

        // ---- 视觉 ----
        var art = new GameObject("Art").transform;
        art.SetParent(root.transform, false);

        MakeSprite(art, "Body", sprite, new Vector3(0f, 0f, 0f), new Vector2(Width, Height),
            new Color(0.10f, 0.15f, 0.22f, 0.55f), 5);
        MakeSprite(art, "DoorLeft", sprite, new Vector3(-0.48f, 0f, 0f), new Vector2(0.04f, Height),
            new Color(0.55f, 0.72f, 0.92f, 1f), 6);
        MakeSprite(art, "DoorRight", sprite, new Vector3(0.48f, 0f, 0f), new Vector2(0.04f, Height),
            new Color(0.55f, 0.72f, 0.92f, 1f), 6);
        MakeSprite(art, "FrameTop", sprite, new Vector3(0f, 0.95f, 0f), new Vector2(Width, 0.1f),
            new Color(0.55f, 0.72f, 0.92f, 1f), 6);

        // ---- 碰撞 ----
        var cols = new GameObject("Colliders").transform;
        cols.SetParent(root.transform, false);

        var left = MakeSolid(cols, "Solid_Left", new Vector2(-0.5f, 0f), new Vector2(0.04f, Height));
        var right = MakeSolid(cols, "Solid_Right", new Vector2(0.5f, 0f), new Vector2(0.04f, Height));
        MakeSolid(cols, "Solid_Top", new Vector2(0f, 0.95f), new Vector2(Width, 0.1f));
        var floorCol = MakeSolid(cols, "Solid_Bottom", new Vector2(0f, -1.03f), new Vector2(Width, 0.06f));

        // 内部检测 trigger 直接挂在根物体上（回调才会送到 Elevator 脚本）
        var trig = root.AddComponent<BoxCollider2D>();
        trig.isTrigger = true;
        trig.size = new Vector2(0.94f, 1.86f);
        trig.offset = Vector2.zero;

        // 左右门与底板初始关闭：玩家能直接走进去
        left.enabled = false;
        right.enabled = false;
        floorCol.enabled = false;

        var elevator = root.AddComponent<Elevator>();
        elevator.rideHeight = 4f;
        elevator.rideDuration = 1.4f;
        elevator.enterDelay = 0.12f;
        elevator.openDoorOnArrive = true;
        elevator.oneTime = true;
        elevator.playerTag = "Player";
        elevator.doorColliders = new Collider2D[] { left, right };
        elevator.interiorTrigger = trig;
        elevator.floorCollider = floorCol;

        return root;
    }

    static void MakeSprite(Transform parent, string name, Sprite sprite, Vector3 localPos, Vector2 size, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        if (sr.sprite == null) Debug.LogWarning("[ElevatorSetup] " + name + " 没有拿到 sprite");
    }

    static BoxCollider2D MakeSolid(Transform parent, string name, Vector2 localPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(localPos.x, localPos.y, 0f);
        var c = go.AddComponent<BoxCollider2D>();
        c.size = size;
        c.offset = Vector2.zero;
        return c;
    }
}
