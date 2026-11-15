using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Automated verification of the skip button. Enters Play mode from LevelScene,
/// clicks Btn_1 to reach IntroScene, waits for the video to start, then clicks
/// the top-left Btn_Skip and asserts that Level1 is reached quickly.
/// Flag-triggered; writes a PASS/FAIL timeline.
/// </summary>
[InitializeOnLoad]
public static class SkipVerifyTool
{
    const string FlagName = "skip_verify_flag.txt";
    const string ReportName = "skip_verify_report.txt";

    const string KActive = "SkipVerify.Active";
    const string KStep = "SkipVerify.Step";
    const string KStart = "SkipVerify.Start";
    const string KLastScene = "SkipVerify.LastScene";
    const string KIntroEnter = "SkipVerify.IntroEnter";
    const string KSkipClick = "SkipVerify.SkipClick";
    const string KLevel1Enter = "SkipVerify.Level1Enter";

    static string root;
    static string reportPath;

    static SkipVerifyTool()
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
        Log("Starting skip-button verification");

        if (Application.isPlaying)
        {
            EditorApplication.ExitPlaymode();
            return;
        }

        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene("Assets/Scenes/LevelScene.unity", OpenSceneMode.Single);

        EditorPrefs.SetInt(KActive, 1);
        EditorPrefs.SetInt(KStep, 0);
        EditorPrefs.SetString(KLastScene, "");
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
            Log(string.Format("[t={0:0.00}s] {1} -> {2}", Elapsed(),
                string.IsNullOrEmpty(last) ? "<start>" : last, scene));
            EditorPrefs.SetString(KLastScene, scene);
            if (scene == "IntroScene") EditorPrefs.SetFloat(KIntroEnter, Elapsed());
            if (scene == "Level1") EditorPrefs.SetFloat(KLevel1Enter, Elapsed());
        }

        switch (step)
        {
            case 0:
                Log("Initial scene: " + scene);
                var b1 = GameObject.Find("Btn_1");
                if (b1 != null)
                {
                    var b = b1.GetComponent<Button>();
                    if (b != null) b.onClick.Invoke();
                    Log("Clicked Btn_1");
                }
                SetStep(1);
                break;

            case 1:
                if (scene == "IntroScene")
                {
                    Log("Reached IntroScene; waiting 2s so the video is playing");
                    SetStep(2);
                }
                else if (Elapsed() > 20f)
                {
                    Log("FAIL: never reached IntroScene");
                    SetStep(5);
                }
                break;

            case 2:
                if (Elapsed() - EditorPrefs.GetFloat(KIntroEnter, 0f) >= 2f)
                {
                    var skipGo = GameObject.Find("Btn_Skip");
                    if (skipGo == null)
                    {
                        Log("FAIL: Btn_Skip not found in IntroScene");
                        SetStep(5);
                        break;
                    }
                    var sb = skipGo.GetComponent<Button>();
                    if (sb != null) sb.onClick.Invoke();
                    EditorPrefs.SetFloat(KSkipClick, Elapsed());
                    Log("Clicked Btn_Skip at t=" + Elapsed().ToString("0.00"));
                    SetStep(3);
                }
                break;

            case 3:
                if (scene == "Level1")
                {
                    Log("Reached Level1 after skip");
                    SetStep(4);
                }
                else if (Elapsed() - EditorPrefs.GetFloat(KSkipClick, 0f) > 6f)
                {
                    Log("FAIL: Level1 not reached within 6s of skip");
                    SetStep(5);
                }
                break;

            case 4:
                if (Elapsed() - EditorPrefs.GetFloat(KLevel1Enter, 0f) > 1.5f)
                {
                    Log("Stable in Level1; exiting play mode");
                    EditorApplication.ExitPlaymode();
                    SetStep(6);
                }
                break;

            case 5:
                EditorApplication.ExitPlaymode();
                break;
        }
    }

    static void FinalizeReport()
    {
        float introEnter = EditorPrefs.GetFloat(KIntroEnter, -1f);
        float skipClick = EditorPrefs.GetFloat(KSkipClick, -1f);
        float level1Enter = EditorPrefs.GetFloat(KLevel1Enter, -1f);

        Log("---------------- SUMMARY ----------------");
        Log("IntroScene reached: " + (introEnter >= 0));
        Log("Skip clicked: " + (skipClick >= 0));
        Log("Level1 reached: " + (level1Enter >= 0));
        if (skipClick >= 0 && level1Enter >= 0)
            Log("Skip -> Level1 delay: " + (level1Enter - skipClick).ToString("0.00") + "s");

        bool pass = introEnter >= 0 && skipClick >= 0 && level1Enter >= 0
                    && (level1Enter - skipClick) < 6f;
        Log(pass ? "RESULT: PASS" : "RESULT: FAIL");
    }

    static void Log(string s)
    {
        try { File.AppendAllText(reportPath, s + "\n"); } catch { }
        Debug.Log("[SkipVerify] " + s);
    }
}
