using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 把电梯外观换成用户提供的 Assets/Sprites/elevator.png，并移除程序生成的那套蓝色框架。
/// 触发：工程根目录放 elevatorskin_flag.txt，编译后自动执行；或菜单 Tools/月核回光/替换电梯外观。
/// </summary>
[InitializeOnLoad]
public static class ElevatorSkinTool
{
    const string FlagName = "elevatorskin_flag.txt";
    const string ReportName = "elevatorskin_report.txt";
    const string Level8Path = "Assets/Scenes/Level8.unity";
    const string RootName = "elevator";
    const string SpritePath = "Assets/Sprites/elevator.png";
    static readonly string[] FrameNames = { "Body", "DoorLeft", "DoorRight", "FrameTop", "Art" };

    static bool consumed;

    static ElevatorSkinTool() { EditorApplication.update += OnUpdate; }

    [MenuItem("Tools/月核回光/替换电梯外观")]
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
        Debug.Log("[ElevatorSkin] 完成");
    }

    static void Run(StringBuilder report)
    {
        void Log(string s) { report.AppendLine(s); Debug.Log("[ElevatorSkin] " + s); }

        try
        {
            Scene level8 = EditorSceneManager.GetSceneByPath(Level8Path);
            bool openedHere = false;
            if (!level8.IsValid() || !level8.isLoaded)
            {
                level8 = EditorSceneManager.OpenScene(Level8Path, OpenSceneMode.Additive);
                openedHere = true;
            }

            GameObject elevator = null;
            foreach (var go in level8.GetRootGameObjects())
                if (go.name == RootName) { elevator = go; break; }
            if (elevator == null) { Log("找不到 elevator 根物体"); return; }
            Log($"找到 elevator @ {elevator.transform.position}");

            // 1. 删掉自己生成的蓝色框架
            var toDelete = new List<GameObject>();
            foreach (Transform child in elevator.transform)
                if (System.Array.IndexOf(FrameNames, child.name) >= 0) toDelete.Add(child.gameObject);
            foreach (var go in toDelete)
            {
                Log("移除框架部件: " + go.name);
                Object.DestroyImmediate(go);
            }

            // 2. 取用户精灵
            Sprite sprite = LoadUserSprite(out string how);
            if (sprite == null) { Log("读取 " + SpritePath + " 失败：" + how); return; }
            Log($"使用精灵 {sprite.name}  rect={sprite.rect.size}  ppu={sprite.pixelsPerUnit}");

            // 3. 挂上去（1×2 瓦片，16×32 像素 @16ppu 天然就是 1×2 单位）
            var skin = new GameObject("Skin");
            skin.transform.SetParent(elevator.transform, false);
            skin.transform.localPosition = Vector3.zero;
            skin.transform.localRotation = Quaternion.identity;
            skin.transform.localScale = Vector3.one;

            var sr = skin.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingOrder = 5;

            Vector2 size = sprite.bounds.size;
            Log($"精灵世界尺寸 {size.x:0.00} × {size.y:0.00}（目标 1 × 2）");
            if (size.x > 0.001f && Mathf.Abs(size.x - 1f) > 0.01f)
                Log("提示：精灵尺寸不是 1×2，可在 Skin 上调 localScale 或在贴图导入里改 PPU");

            EditorSceneManager.MarkSceneDirty(level8);
            EditorSceneManager.SaveScene(level8);
            Log("Level8 已保存");

            if (openedHere) EditorSceneManager.CloseScene(level8, true);
            AssetDatabase.Refresh();
            Log("SKIN_DONE");
        }
        catch (System.Exception e)
        {
            report.AppendLine("ERROR: " + e);
            Debug.LogError("[ElevatorSkin] " + e);
        }
    }

    /// <summary>支持 Single / Multiple 两种导入模式。</summary>
    static Sprite LoadUserSprite(out string how)
    {
        how = "";
        Sprite single = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (single != null) { how = "single"; return single; }

        var all = AssetDatabase.LoadAllAssetsAtPath(SpritePath);
        foreach (var a in all)
        {
            if (a is Sprite s)
            {
                how = "multiple:" + s.name;
                return s;
            }
        }
        how = "没有可用的 Sprite 子资源";
        return null;
    }
}
