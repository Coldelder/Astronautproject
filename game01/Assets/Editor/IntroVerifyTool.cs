using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Automated verification of the Level1 intro flow.
/// Flag-triggered (no GUI needed). Enters Play mode from LevelScene, clicks Btn_1,
/// and logs the scene timeline LevelScene -> IntroScene -> Level1. While in
/// IntroScene it records the VideoPlayer state (playing / looping / length).
/// Writes a PASS/FAIL summary when Play mode exits.
/// </summary>
[InitializeOnLoad]
public static class IntroVerifyTool
{
    const string FlagName = "intro_verify_flag.txt";
    const string ReportName = "intro_verify_report.txt";

    const string KActive = "IntroVerify.Active";
    const string KStep = "IntroVerify.Step";
    const string KStart = "IntroVerify.Start";
    const string KLastScene = "IntroVerify.LastScene";
    const string KIntroSeen = "IntroVerify.IntroSeen";
    const string KIntroEnter = "IntroVerify.IntroEnter";
    const string KVpPlaying = "IntroVerify.VpPlaying";
    const string KVpLooping = "IntroVerify.VpLooping";
    const string KVpLength = "IntroVerify.VpLength";
    const string KLevel1Enter = "IntroVerify.Level1Enter";

    static string root;
    static string reportPath;

    static IntroVerifyTool()
    {
        EditorApplication.delayCall += OnDelay;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        if (EditorPrefs.GetInt(KActive, 0) == 1 && Application.isPlaying)
            EditorApplication.update += OnUpdate;
    }

    static void OnDelay()
    {
        root = Directory.GetParent(Application.dataPath).FullName;
        reportPath = Path.Combine(root, ReportName);
        string flag = Path.Combine(root, FlagName);
        if (!File.Exists(flag)) return;

        File.Delete(flag);
        if (File.Exists(reportPath)) File.Delete(reportPath);
        Log("Starting intro flow verification");

        if (!Application.isPlaying)
        {
            EditorSceneManager.SaveOpenScenes();
            EditorSceneManager.OpenScene("Assets/Scenes/LevelScene.unity", OpenSceneMode.Single);
        }

        EditorPrefs.SetInt(KActive, 1);
        EditorPrefs.SetInt(KStep, 0);
        EditorPrefs.SetInt(KIntroSeen, 0);
        EditorPrefs.SetString(KLastScene, "");
        EditorPrefs.SetBool(KVpPlaying, false);
        EditorPrefs.SetInt(KVpLooping, -1);
        EditorPrefs.SetFloat(KVpLength, -1f);
        EditorApplication.EnterPlaymode();
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (EditorPrefs.GetInt(KActive, 0) != 1) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
            EditorPrefs.SetFloat(KStart, (float)EditorApplication.timeSinceStartup);
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= OnUpdate;
            Log("Returned to edit mode");
            EditorPrefs.SetInt(KActive, 0);
            FinalizeReport();
        }
    }

    static float Elapsed()
    {
        return (float)EditorApplication.timeSinceStartup - EditorPrefs.GetFloat(KStart, 0f);
    }

    static void SetStep(int s) { EditorPrefs.SetInt(KStep, s); }

    static void OnUpdate()
    {
        if (!Application.isPlaying) return;

        int step = EditorPrefs.GetInt(KStep, 0);
        string scene = SceneManager.GetActiveScene().name;
        string last = EditorPrefs.GetString(KLastScene, "");

        if (scene != last)
        {
            Log(string.Format("[t={0:0.00}s] scene change: {1} -> {2}",
                Elapsed(), string.IsNullOrEmpty(last) ? "<start>" : last, scene));
            EditorPrefs.SetString(KLastScene, scene);
            if (scene == "IntroScene")
            {
                EditorPrefs.SetInt(KIntroSeen, 1);
                EditorPrefs.SetFloat(KIntroEnter, Elapsed());
            }
            if (scene == "Level1")
                EditorPrefs.SetFloat(KLevel1Enter, Elapsed());
        }

        if (scene == "IntroScene")
        {
            var vp = Object.FindObjectOfType<VideoPlayer>();
            if (vp != null)
            {
                if (vp.isPlaying) EditorPrefs.SetBool(KVpPlaying, true);
                EditorPrefs.SetInt(KVpLooping, vp.isLooping ? 1 : 0);
                if (vp.length > 0 && EditorPrefs.GetFloat(KVpLength, -1f) < 0)
                    EditorPrefs.SetFloat(KVpLength, (float)vp.length);
                Log(string.Format("  IntroScene VP: isPlaying={0} isLooping={1} time={2:0.00}s length={3:0.00}s",
                    vp.isPlaying, vp.isLooping, vp.time, vp.length));
            }
        }

        switch (step)
        {
            case 0:
                Log("Initial scene: " + scene);
                var go = GameObject.Find("Btn_1");
                if (go != null)
                {
                    var b = go.GetComponent<Button>();
                    if (b != null)
                    {
                        b.onClick.Invoke();
                        Log("Clicked Btn_1");
                    }
                    else Log("Btn_1 has no Button component");
                }
                else Log("Btn_1 NOT FOUND");
                SetStep(1);
                break;

            case 1:
                if (scene == "Level1")
                {
                    Log("Reached Level1 automatically");
                    SetStep(2);
                }
                else if (Elapsed() > 45f)
                {
                    Log("FAIL: timed out before reaching Level1 (current scene=" + scene + ")");
                    SetStep(3);
                }
                break;

            case 2:
                if (Elapsed() - EditorPrefs.GetFloat(KLevel1Enter, 0f) > 1.5f)
                {
                    Log("Stable in Level1; exiting play mode");
                    SetStep(4);
                    EditorApplication.ExitPlaymode();
                }
                break;

            case 3:
                EditorApplication.ExitPlaymode();
                break;
        }
    }

    static void FinalizeReport()
    {
        bool introSeen = EditorPrefs.GetInt(KIntroSeen, 0) == 1;
        bool vpPlaying = EditorPrefs.GetBool(KVpPlaying, false);
        int looping = EditorPrefs.GetInt(KVpLooping, -1);
        float length = EditorPrefs.GetFloat(KVpLength, -1f);
        float introEnter = EditorPrefs.GetFloat(KIntroEnter, -1f);
        float level1Enter = EditorPrefs.GetFloat(KLevel1Enter, -1f);

        Log("---------------- SUMMARY ----------------");
        Log("IntroScene reached: " + introSeen);
        Log("VideoPlayer observed playing: " + vpPlaying);
        Log("VideoPlayer isLooping (want False): " + (looping == 1 ? "True" : looping == 0 ? "False" : "unknown"));
        Log("Video clip length: " + (length >= 0 ? length.ToString("0.00") + "s" : "unknown"));
        if (introEnter >= 0 && level1Enter >= 0)
            Log("Time spent in IntroScene: " + (level1Enter - introEnter).ToString("0.00") + "s");
        Log("Reached Level1: " + (level1Enter >= 0));

        bool pass = introSeen && vpPlaying && looping == 0 && level1Enter >= 0;
        Log(pass ? "RESULT: PASS" : "RESULT: FAIL");
    }

    static void Log(string s)
    {
        try { File.AppendAllText(reportPath, s + "\n"); } catch { }
        Debug.Log("[IntroVerify] " + s);
    }
}
