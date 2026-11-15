using System.Text;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// One-click setup that wires the desktop "过场.mp4" as the Level1 intro.
/// Triggered by a flag file in the project root (same pattern as MenuEnlargeTool),
/// so no GUI click is required. Creates IntroScene (fullscreen VideoPlayer +
/// CinematicPlayer), adds it to Build Settings, and repoints ONLY the Level1
/// button in LevelScene to IntroScene. Everything else is left untouched.
/// Idempotent: re-running does not duplicate the scene or build entry.
/// </summary>
[InitializeOnLoad]
public static class IntroSetupTool
{
    const string FlagName = "intro_setup_flag.txt";
    const string ReportName = "intro_setup_report.txt";
    const string VideoRelPath = "Assets/Video/过场.mp4";
    const string IntroScenePath = "Assets/Scenes/IntroScene.unity";
    const string LevelScenePath = "Assets/Scenes/LevelScene.unity";

    static bool consumed;

    static IntroSetupTool()
    {
        EditorApplication.update += OnUpdate;
    }

    static void OnUpdate()
    {
        if (consumed) return;
        string root = Directory.GetParent(Application.dataPath).FullName;
        string flagPath = Path.Combine(root, FlagName);
        if (!File.Exists(flagPath)) return;

        var report = new StringBuilder();
        void Log(string s)
        {
            report.AppendLine(s);
            Debug.Log("[IntroSetupTool] " + s);
        }

        var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoRelPath);
        if (clip == null)
        {
            if (!File.Exists(Path.Combine(root, VideoRelPath)))
            {
                consumed = true;
                EditorApplication.update -= OnUpdate;
                File.Delete(flagPath);
                Log("ABORT: video not found at " + VideoRelPath);
                File.WriteAllText(Path.Combine(root, ReportName), report.ToString());
                return;
            }
            AssetDatabase.ImportAsset(VideoRelPath, ImportAssetOptions.ForceSynchronousImport);
            clip = AssetDatabase.LoadAssetAtPath<VideoClip>(VideoRelPath);
            if (clip == null)
            {
                // Import still in progress; keep the flag and retry on next tick.
                return;
            }
        }

        consumed = true;
        EditorApplication.update -= OnUpdate;
        File.Delete(flagPath);
        string reportPath = Path.Combine(root, ReportName);

        try
        {
            string originalScene = EditorSceneManager.GetActiveScene().path;
            Log("Original active scene: " + originalScene);

            // Persist current work first so later scene switches never show
            // an unsaved-changes modal dialog (which cannot be clicked while
            // the screenshot channel is blank).
            EditorSceneManager.SaveOpenScenes();

            // ---------- Build IntroScene ----------
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;

            var canvasGo = new GameObject("CinematicCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var imgGo = new GameObject("VideoImage");
            imgGo.transform.SetParent(canvasGo.transform, false);
            var raw = imgGo.AddComponent<RawImage>();
            var rawRt = raw.rectTransform;
            rawRt.anchorMin = Vector2.zero;
            rawRt.anchorMax = Vector2.one;
            rawRt.offsetMin = Vector2.zero;
            rawRt.offsetMax = Vector2.zero;

            var vpGo = new GameObject("VideoPlayer");
            var vp = vpGo.AddComponent<VideoPlayer>();
            vp.source = VideoSource.VideoClip;
            vp.clip = clip;
            vp.playOnAwake = false;
            vp.isLooping = false;
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.audioOutputMode = VideoAudioOutputMode.Direct;

            var cp = vpGo.AddComponent<CinematicPlayer>();
            cp.videoPlayer = vp;
            cp.videoImage = raw;
            cp.nextScene = "Level1";
            cp.fadeInDuration = 1.0f;

            EditorSceneManager.SaveScene(scene, IntroScenePath);
            Log("Saved intro scene: " + IntroScenePath);

            // ---------- Build Settings (idempotent) ----------
            var buildList = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            buildList.RemoveAll(x => x.path == IntroScenePath);
            buildList.Add(new EditorBuildSettingsScene(IntroScenePath, true));
            EditorBuildSettings.scenes = buildList.ToArray();
            Log("Added intro scene to Build Settings.");

            // ---------- Repoint only the Level1 button in LevelScene ----------
            var levelScene = EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);
            var switchers = Object.FindObjectsOfType<SceneSwitcher>();
            SceneSwitcher target = null;
            foreach (var s in switchers)
            {
                if (s.targetScene == "Level1") { target = s; break; }
            }
            if (target == null)
            {
                foreach (var s in switchers)
                {
                    if (s.targetScene == "IntroScene") { target = s; break; }
                }
            }

            if (target == null)
            {
                Log("ABORT: Level1 button not found in LevelScene; nothing repointed.");
            }
            else
            {
                Log("Level1 button '" + target.gameObject.name + "' before targetScene=" + target.targetScene);
                target.targetScene = "IntroScene";
                EditorUtility.SetDirty(target);
                EditorSceneManager.MarkSceneDirty(levelScene);
                EditorSceneManager.SaveScene(levelScene);
                Log("Level1 button targetScene now IntroScene; LevelScene saved.");
            }

            AssetDatabase.SaveAssets();

            // Restore the scene the user originally had open.
            if (!string.IsNullOrEmpty(originalScene))
                EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);

            report.AppendLine("INTRO_SETUP_DONE");
        }
        catch (System.Exception e)
        {
            report.AppendLine("FATAL: " + e);
            Debug.LogError("[IntroSetupTool] " + e);
        }

        File.WriteAllText(reportPath, report.ToString());
    }
}
