using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using System.IO;
using System.Text;

[InitializeOnLoad]
public static class FontAssignTool
{
    const string FlagName = "font_assign_flag.txt";
    const string ReportName = "font_assign_report.txt";
    static string projectRoot;
    static StringBuilder report;
    static bool started;

    static FontAssignTool()
    {
        EditorApplication.delayCall += OnDelay;
    }

    static void OnDelay()
    {
        projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string flagPath = Path.Combine(projectRoot, FlagName);
        if (started) return;
        if (!File.Exists(flagPath)) return;
        started = true;
        File.Delete(flagPath);
        report = new StringBuilder();
        try
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/simhei.ttf");
            Log("Using font: " + (font != null ? font.name : "NULL"));
            string[] scenes = { "Assets/Scenes/MenuScene.unity", "Assets/Scenes/LevelScene.unity", "Assets/Scenes/SettingsScene.unity" };
            foreach (var scenePath in scenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                int n = 0;
                foreach (var t in texts)
                {
                    t.font = font;
                    EditorUtility.SetDirty(t);
                    n++;
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Log(scenePath + " assigned font to " + n + " Text components");
            }
            AssetDatabase.SaveAssets();
            report.AppendLine("FONT_ASSIGN_DONE");
        }
        catch (System.Exception e)
        {
            report.AppendLine("FATAL: " + e);
        }
        File.WriteAllText(Path.Combine(projectRoot, ReportName), report.ToString());
        Debug.Log("[FontAssignTool] done");
    }

    static void Log(string s)
    {
        report.AppendLine(s);
        Debug.Log("[FontAssignTool] " + s);
    }
}
