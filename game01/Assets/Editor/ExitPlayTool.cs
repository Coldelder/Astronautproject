using UnityEditor;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public static class ExitPlayTool
{
    static ExitPlayTool()
    {
        EditorApplication.delayCall += () =>
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string flag = Path.Combine(root, "exit_play_flag.txt");
            if (File.Exists(flag))
            {
                File.Delete(flag);
                if (Application.isPlaying) EditorApplication.ExitPlaymode();
                Debug.Log("[ExitPlayTool] exit requested");
            }
        };
    }
}
