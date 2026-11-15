using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Play-mode verification of the Level1 -> Level2 transition. Moves the real
/// player Rigidbody2D into the Level2Trigger region so the physics engine fires
/// OnTriggerEnter2D, then asserts the active scene becomes Level2.
/// Flag-triggered; writes PASS/FAIL with the scene timeline.
/// </summary>
[InitializeOnLoad]
public static class TransitionVerifyTool
{
    const string FlagName = "transition_verify_flag.txt";
    const string ReportName = "transition_verify_report.txt";

    const string KActive = "TVerify.Active";
    const string KStep = "TVerify.Step";
    const string KStart = "TVerify.Start";
    const string KLastScene = "TVerify.LastScene";
    const string KLevel2Enter = "TVerify.Level2Enter";

    static string root;
    static string reportPath;

    static TransitionVerifyTool()
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
        Log("Starting transition verification");

        if (Application.isPlaying) { EditorApplication.ExitPlaymode(); return; }

        EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity", OpenSceneMode.Single);

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
            if (scene == "Level2") EditorPrefs.SetFloat(KLevel2Enter, Elapsed());
        }

        switch (step)
        {
            case 0:
                var player = Object.FindObjectOfType<Player>();
                var trigger = GameObject.Find("Level2Trigger");
                if (player == null || trigger == null)
                {
                    Log("FAIL: player=" + (player != null) + " trigger=" + (trigger != null));
                    SetStep(4);
                    break;
                }
                var rb = player.GetComponent<Rigidbody2D>();
                Vector3 target = trigger.transform.position;
                Log("Moving player into trigger at " + target);
                if (rb != null) rb.MovePosition(target);
                else player.transform.position = target;
                SetStep(1);
                break;

            case 1:
                if (scene == "Level2")
                {
                    Log("Reached Level2 after trigger");
                    SetStep(2);
                }
                else if (Elapsed() > 8f)
                {
                    Log("FAIL: Level2 not reached (scene=" + scene + ")");
                    SetStep(3);
                }
                break;

            case 2:
                if (Elapsed() - EditorPrefs.GetFloat(KLevel2Enter, 0f) > 1.5f)
                {
                    EditorApplication.ExitPlaymode();
                    SetStep(5);
                }
                break;

            case 3:
                EditorApplication.ExitPlaymode();
                break;
        }
    }

    static void FinalizeReport()
    {
        bool reached = EditorPrefs.GetFloat(KLevel2Enter, -1f) >= 0;
        Log("Level2 reached: " + reached);
        Log(reached ? "RESULT: PASS" : "RESULT: FAIL");
    }

    static void Log(string s)
    {
        try { File.AppendAllText(reportPath, s + "\n"); } catch { }
        Debug.Log("[TransitionVerify] " + s);
    }
}
