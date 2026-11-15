using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Verifies the REAL click path through the EventSystem (not onClick.Invoke).
/// Enters Play mode, reaches IntroScene, then performs an EventSystem raycast at
/// the skip button's screen position and dispatches a pointerClick via
/// ExecuteEvents. This proves the button is raycastable (not blocked) and that
/// the EventSystem wired by the fix drives the skip into Level1.
/// Flag-triggered; writes PASS/FAIL.
/// </summary>
[InitializeOnLoad]
public static class RealClickVerifyTool
{
    const string FlagName = "realclick_verify_flag.txt";
    const string ReportName = "realclick_verify_report.txt";

    const string KActive = "RealClick.Active";
    const string KStep = "RealClick.Step";
    const string KStart = "RealClick.Start";
    const string KLastScene = "RealClick.LastScene";
    const string KIntroEnter = "RealClick.IntroEnter";
    const string KLevel1Enter = "RealClick.Level1Enter";

    static string root;
    static string reportPath;

    static RealClickVerifyTool()
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
        Log("Starting real-click verification");

        if (Application.isPlaying) { EditorApplication.ExitPlaymode(); return; }

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
                var b1 = GameObject.Find("Btn_1");
                if (b1 != null)
                {
                    b1.GetComponent<Button>().onClick.Invoke();
                    Log("Clicked Btn_1");
                }
                SetStep(1);
                break;

            case 1:
                if (scene == "IntroScene") SetStep(2);
                else if (Elapsed() > 20f) { Log("FAIL: no IntroScene"); SetStep(5); }
                break;

            case 2:
                if (Elapsed() - EditorPrefs.GetFloat(KIntroEnter, 0f) >= 2f)
                {
                    DoRealClick();
                    SetStep(3);
                }
                break;

            case 3:
                if (scene == "Level1") { Log("Reached Level1 via real click"); SetStep(4); }
                else if (Elapsed() > 12f) { Log("FAIL: Level1 not reached after real click"); SetStep(5); }
                break;

            case 4:
                if (Elapsed() - EditorPrefs.GetFloat(KLevel1Enter, 0f) > 1.5f)
                {
                    EditorApplication.ExitPlaymode();
                    SetStep(6);
                }
                break;

            case 5:
                EditorApplication.ExitPlaymode();
                break;
        }
    }

    static void DoRealClick()
    {
        var es = EventSystem.current;
        var skip = GameObject.Find("Btn_Skip");
        if (es == null || skip == null)
        {
            Log("FAIL: EventSystem=" + (es != null) + " skip=" + (skip != null));
            return;
        }

        var btnRt = skip.GetComponent<RectTransform>();
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, btnRt.position);

        var pointer = new PointerEventData(es) { position = screenPos };
        var results = new List<RaycastResult>();
        es.RaycastAll(pointer, results);

        bool hit = false;
        foreach (var r in results)
        {
            if (r.gameObject == skip || r.gameObject.transform.IsChildOf(skip.transform))
            {
                hit = true;
                break;
            }
        }

        Log("Raycast at " + screenPos + " hit skip: " + hit + " (total targets: " + results.Count + ")");
        if (hit)
        {
            ExecuteEvents.Execute(skip, pointer, ExecuteEvents.pointerClickHandler);
            Log("Dispatched pointerClick through EventSystem");
        }
    }

    static void FinalizeReport()
    {
        Log("Level1 reached: " + (EditorPrefs.GetFloat(KLevel1Enter, -1f) >= 0));
        bool pass = EditorPrefs.GetFloat(KLevel1Enter, -1f) >= 0;
        Log(pass ? "RESULT: PASS" : "RESULT: FAIL");
    }

    static void Log(string s)
    {
        try { File.AppendAllText(reportPath, s + "\n"); } catch { }
        Debug.Log("[RealClickVerify] " + s);
    }
}
