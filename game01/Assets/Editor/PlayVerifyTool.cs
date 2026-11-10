using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using System.Text;

[InitializeOnLoad]
public static class PlayVerifyTool
{
    const string FlagName = "play_verify_flag.txt";
    const string ReportName = "play_verify_report.txt";
    const string KActive = "MoonVerify.Active";
    const string KStep = "MoonVerify.Step";
    const string KTimer = "MoonVerify.Timer";
    const string KExpected = "MoonVerify.Expected";
    static string projectRoot;
    static string reportPath;

    static PlayVerifyTool()
    {
        EditorApplication.delayCall += OnDelay;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        // After a domain reload inside play mode, resume the update if active.
        if (EditorPrefs.GetInt(KActive, 0) == 1 && Application.isPlaying)
        {
            EditorApplication.update += VerifyUpdate;
        }
    }

    static void OnDelay()
    {
        projectRoot = Directory.GetParent(Application.dataPath).FullName;
        reportPath = Path.Combine(projectRoot, ReportName);
        if (File.Exists(Path.Combine(projectRoot, FlagName)))
        {
            File.Delete(Path.Combine(projectRoot, FlagName));
            if (File.Exists(reportPath)) File.Delete(reportPath);
            Log("Starting Play Mode verification");
            EditorPrefs.SetInt(KActive, 1);
            EditorPrefs.SetInt(KStep, 0);
            EditorPrefs.SetFloat(KTimer, 0f);
            EditorPrefs.SetString(KExpected, "LevelScene");
            EditorApplication.EnterPlaymode();
        }
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (EditorPrefs.GetInt(KActive, 0) != 1) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.update -= VerifyUpdate;
            EditorApplication.update += VerifyUpdate;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= VerifyUpdate;
            Log("Returned to Edit Mode");
            EditorPrefs.SetInt(KActive, 0);
        }
    }

    static void Log(string s)
    {
        try { File.AppendAllText(reportPath, s + "\n"); } catch { }
        Debug.Log("[PlayVerify] " + s);
    }

    static string ActiveScene() { return SceneManager.GetActiveScene().name; }

    static Button FindButton(string name)
    {
        var go = GameObject.Find(name);
        return go == null ? null : go.GetComponent<Button>();
    }

    static void Click(string name)
    {
        var btn = FindButton(name);
        if (btn == null) { Log("BUTTON NOT FOUND: " + name); return; }
        btn.onClick.Invoke();
        Log("Clicked: " + name);
    }

    static void Advance(int nextStep, string expected)
    {
        EditorPrefs.SetInt(KStep, nextStep);
        EditorPrefs.SetFloat(KTimer, 0f);
        EditorPrefs.SetString(KExpected, expected);
    }

    static void VerifyUpdate()
    {
        if (!Application.isPlaying) return;
        float timer = EditorPrefs.GetFloat(KTimer, 0f) + Time.unscaledDeltaTime;
        EditorPrefs.SetFloat(KTimer, timer);
        int step = EditorPrefs.GetInt(KStep, 0);
        string expected = EditorPrefs.GetString(KExpected, "");

        switch (step)
        {
            case 0:
                Log("Initial scene: " + ActiveScene());
                Click("Btn_开始游戏");
                Advance(1, "LevelScene");
                break;
            case 1:
                if (timer >= 1.2f)
                {
                    Log("After start, scene: " + ActiveScene() + " (expected " + expected + ")");
                    Click("Btn_1");
                    Advance(2, "LevelOne");
                }
                break;
            case 2:
                if (timer >= 1.2f)
                {
                    Log("After Btn_1, scene: " + ActiveScene() + " (expected " + expected + ")");
                    Click("DoorBtn");
                    Advance(3, "LevelScene");
                }
                break;
            case 3:
                if (timer >= 1.2f)
                {
                    Log("After LevelOne door, scene: " + ActiveScene() + " (expected " + expected + ")");
                    Click("Btn_2");
                    Advance(4, "Level2");
                }
                break;
            case 4:
                if (timer >= 1.2f)
                {
                    Log("After Btn_2, scene: " + ActiveScene() + " (expected " + expected + ")");
                    Click("DoorBtn");
                    Advance(5, "LevelScene");
                }
                break;
            case 5:
                if (timer >= 1.2f)
                {
                    Log("After Level2 door, scene: " + ActiveScene() + " (expected " + expected + ")");
                    Click("Btn_< 返回主菜单");
                    Advance(6, "MenuScene");
                }
                break;
            case 6:
                if (timer >= 1.2f)
                {
                    Log("After back-to-menu, scene: " + ActiveScene() + " (expected " + expected + ")");
                    Click("Btn_设置");
                    Advance(7, "SettingsScene");
                }
                break;
            case 7:
                if (timer >= 1.2f)
                {
                    Log("After settings, scene: " + ActiveScene() + " (expected " + expected + ")");
                    Click("Btn_< 返回主菜单");
                    Advance(8, "MenuScene");
                }
                break;
            case 8:
                if (timer >= 1.2f)
                {
                    Log("After settings back, scene: " + ActiveScene() + " (expected " + expected + ")");
                    Log("VERIFICATION_FLOW_COMPLETE");
                    Advance(9, "");
                }
                break;
            case 9:
                if (timer >= 0.5f)
                {
                    EditorApplication.ExitPlaymode();
                    Advance(10, "");
                }
                break;
        }
    }
}
