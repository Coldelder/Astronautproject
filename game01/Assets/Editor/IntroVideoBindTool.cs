using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// 给 IntroScene 2 / IntroScene 3 绑定过场视频：分别是 final1.mp4、final2.mp4。
/// 触发：菜单 Tools/月核回光/绑定过场视频，或在工程根目录放 intro_video_flag.txt。
/// </summary>
[InitializeOnLoad]
public static class IntroVideoBindTool
{
    const string FlagName = "intro_video_flag.txt";
    const string ReportName = "intro_video_report.txt";

    const string Intro2Path = "Assets/Scenes/IntroScene 2.unity";
    const string Intro3Path = "Assets/Scenes/IntroScene 3.unity";
    const string Final1 = "Assets/Video/final1.mp4";
    const string Final2 = "Assets/Video/final2.mp4";

    static bool consumed;

    static IntroVideoBindTool() { EditorApplication.update += OnUpdate; }

    [MenuItem("Tools/月核回光/绑定过场视频")]
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
        Debug.Log("[IntroVideoBind] 完成");
    }

    static void Run(StringBuilder report)
    {
        void Log(string s) { report.AppendLine(s); Debug.Log("[IntroVideoBind] " + s); }

        try
        {
            EditorSceneManager.SaveOpenScenes();
            string originalScene = EditorSceneManager.GetActiveScene().path;

            Bind(Intro2Path, Final1, Log);
            Bind(Intro3Path, Final2, Log);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!string.IsNullOrEmpty(originalScene))
                EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);

            Log("INTRO_VIDEO_DONE");
        }
        catch (System.Exception e)
        {
            report.AppendLine("ERROR: " + e);
            Debug.LogError("[IntroVideoBind] " + e);
        }
    }

    static void Bind(string scenePath, string videoPath, System.Action<string> log)
    {
        VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(videoPath);
        if (clip == null)
        {
            AssetDatabase.ImportAsset(videoPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh();
            clip = AssetDatabase.LoadAssetAtPath<VideoClip>(videoPath);
        }
        if (clip == null)
        {
            log("失败：读不到视频 " + videoPath);
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        int bound = 0;
        foreach (var go in scene.GetRootGameObjects())
        {
            foreach (var vp in go.GetComponentsInChildren<VideoPlayer>(true))
            {
                vp.clip = clip;
                vp.source = VideoSource.VideoClip;
                EditorUtility.SetDirty(vp);
                bound++;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        log($"{Path.GetFileName(scenePath)} ← {Path.GetFileName(videoPath)}（{bound} 个 VideoPlayer）");
    }
}
