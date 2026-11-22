using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// 按 IntroScene 1 复刻出 IntroScene 2 / IntroScene 3：
/// 场景资产原样复制（VideoPlayer 清空、其余对象/组件全部保留），
/// 跳转关系设为 IntroScene 1 → IntroScene 2 → MenuScene，并把两个新场景加进 Build Settings。
/// 触发：菜单 Tools/月核回光/复刻过场场景，或在工程根目录放 intro_clone_flag.txt。
/// </summary>
[InitializeOnLoad]
public static class IntroSceneCloneTool
{
    const string FlagName = "intro_clone_flag.txt";
    const string ReportName = "intro_clone_report.txt";

    const string SourcePath = "Assets/Scenes/IntroScene 1.unity";
    const string Intro1Path = "Assets/Scenes/IntroScene 1.unity";
    const string Intro2Path = "Assets/Scenes/IntroScene 2.unity";
    const string Intro3Path = "Assets/Scenes/IntroScene 3.unity";

    static bool consumed;

    static IntroSceneCloneTool() { EditorApplication.update += OnUpdate; }

    [MenuItem("Tools/月核回光/复刻过场场景")]
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
        Debug.Log("[IntroSceneClone] 完成");
    }

    static void Run(StringBuilder report)
    {
        void Log(string s) { report.AppendLine(s); Debug.Log("[IntroSceneClone] " + s); }

        try
        {
            if (!File.Exists(Path.Combine(Directory.GetParent(Application.dataPath).FullName, Intro1Path)) &&
                AssetDatabase.LoadAssetAtPath<SceneAsset>(Intro1Path) == null)
            {
                Log("ABORT: 找不到源场景 " + Intro1Path);
                return;
            }

            EditorSceneManager.SaveOpenScenes();
            string originalScene = EditorSceneManager.GetActiveScene().path;

            Clone(Intro2Path, Log);
            Clone(Intro3Path, Log);

            Configure(Intro1Path, "IntroScene 2", "Assets/Video/sceneswitch.mp4", Log);  // 1 → 2，并补回原视频
            Configure(Intro2Path, "MenuScene", null, Log);                                // 2 → MenuScene，视频清空
            Configure(Intro3Path, "MenuScene", null, Log);                                // 3 → MenuScene，视频清空

            UpdateBuildSettings(Log);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!string.IsNullOrEmpty(originalScene))
                EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);

            Log("INTRO_CLONE_DONE");
        }
        catch (System.Exception e)
        {
            report.AppendLine("ERROR: " + e);
            Debug.LogError("[IntroSceneClone] " + e);
        }
    }

    static void Clone(string destPath, System.Action<string> log)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(destPath) != null)
        {
            log("已存在，跳过复制：" + destPath);
            return;
        }
        bool ok = AssetDatabase.CopyAsset(SourcePath, destPath);
        AssetDatabase.Refresh();
        log((ok ? "已复制 → " : "复制失败 → ") + destPath);
    }

    /// <summary>打开场景，设置跳转目标；clipPath 为空时把 VideoPlayer 的 clip 清空，否则恢复成指定视频。</summary>
    static void Configure(string scenePath, string nextScene, string clipPath, System.Action<string> log)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        string shortName = Path.GetFileName(scenePath);

        // 场景里用的是 CinematicPlayer 或 CinematicPlayer1，两个都要照顾到
        int found = 0;
        foreach (var go in scene.GetRootGameObjects())
        {
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                string typeName = mb.GetType().Name;
                if (typeName != "CinematicPlayer" && typeName != "CinematicPlayer1") continue;

                var so = new SerializedObject(mb);
                var prop = so.FindProperty("nextScene");
                if (prop != null) prop.stringValue = nextScene;
                var adv = so.FindProperty("advanceWhenNoClip");
                if (adv != null) adv.boolValue = true;
                so.ApplyModifiedProperties();
                found++;
            }
        }
        log(found > 0
            ? $"{shortName} nextScene = {nextScene}（{found} 个过场脚本）"
            : $"{shortName} 里没有过场脚本，跳转未设置");

        int changed = 0;
        VideoClip clip = null;
        if (!string.IsNullOrEmpty(clipPath)) clip = AssetDatabase.LoadAssetAtPath<VideoClip>(clipPath);

        foreach (var go in scene.GetRootGameObjects())
        {
            foreach (var vp in go.GetComponentsInChildren<VideoPlayer>(true))
            {
                if (vp.clip == clip) continue;
                vp.clip = clip;
                EditorUtility.SetDirty(vp);
                changed++;
            }
        }
        log(clip != null
            ? $"{shortName} 视频恢复为 {Path.GetFileName(clipPath)}（{changed} 个）"
            : $"{shortName} 视频已清空（{changed} 个）");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        log(shortName + " 已保存");

        if (found == 0)
            log("提示：如果这条场景本来就不需要跳转，可以忽略上面的警告");
    }

    static void UpdateBuildSettings(System.Action<string> log)
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        list.RemoveAll(x => x.path == Intro2Path || x.path == Intro3Path);

        int insertAt = 0;
        for (int i = 0; i < list.Count; i++)
            if (list[i].path == Intro1Path) insertAt = i + 1;

        list.Insert(insertAt, new EditorBuildSettingsScene(Intro2Path, true));
        list.Insert(insertAt + 1, new EditorBuildSettingsScene(Intro3Path, true));
        EditorBuildSettings.scenes = list.ToArray();

        var names = new List<string>();
        foreach (var s in EditorBuildSettings.scenes) names.Add(Path.GetFileNameWithoutExtension(s.path));
        log("Build Settings 顺序：" + string.Join(" | ", names.ToArray()));
    }
}
