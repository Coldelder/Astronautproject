using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// 在 Level8 里生成 ReceiverPlus：
/// 直接用 Assets/Prefab/Receiver.prefab 实例化一份（Sprite / Animator / Light2D / Collider 全部沿用），
/// 移除 LaserReceiver、换成 LaserReceiverPlus（requiredHits = 6），
/// 然后在该实例上重新接好 door / animator / chargeLight（场景内引用不丢）。
///
/// 触发方式：
/// 1. 菜单：Tools/月核回光/创建 ReceiverPlus (Level8)
/// 2. 免点击：工程根目录建 receiverplus_flag.txt，脚本编译后自动执行并删除。
/// </summary>
[InitializeOnLoad]
public static class ReceiverPlusSetupTool
{
    const string FlagName = "receiverplus_flag.txt";
    const string ReportName = "receiverplus_report.txt";
    const string Level8Path = "Assets/Scenes/Level8.unity";
    const string ReceiverPrefabPath = "Assets/Prefab/Receiver.prefab";
    const string NewName = "ReceiverPlus";

    static bool consumed;

    static ReceiverPlusSetupTool()
    {
        EditorApplication.update += OnUpdate;
    }

    [MenuItem("Tools/月核回光/创建 ReceiverPlus (Level8)")]
    static void FromMenu()
    {
        Run(MakeReport());
    }

    static void OnUpdate()
    {
        if (consumed) return;

        string root = Directory.GetParent(Application.dataPath).FullName;
        string flagPath = Path.Combine(root, FlagName);
        if (!File.Exists(flagPath)) return;

        if (Application.isPlaying)
        {
            EditorApplication.ExitPlaymode();
            return;
        }

        consumed = true;
        File.Delete(flagPath);
        EditorApplication.update -= OnUpdate;

        var report = MakeReport();
        Run(report);
        File.WriteAllText(Path.Combine(root, ReportName), report.ToString(), new UTF8Encoding(false));
        Debug.Log("[ReceiverPlusSetup] 完成");
    }

    static StringBuilder MakeReport() => new StringBuilder();

    static void Run(StringBuilder report)
    {
        void Log(string s)
        {
            report.AppendLine(s);
            Debug.Log("[ReceiverPlusSetup] " + s);
        }

        try
        {
            // 1. 保证 Level8 已加载（叠加打开，不动用户当前场景）
            Scene level8 = EditorSceneManager.GetSceneByPath(Level8Path);
            bool openedHere = false;
            if (!level8.IsValid() || !level8.isLoaded)
            {
                if (!File.Exists(Level8Path))
                {
                    Log("找不到场景：" + Level8Path);
                    return;
                }
                level8 = EditorSceneManager.OpenScene(Level8Path, OpenSceneMode.Additive);
                openedHere = true;
            }
            Log("Level8 已加载");

            // 2. 找模板 Receiver（只为取它的位置），以及场景里的 Door
            GameObject source = null;
            Door door = null;
            foreach (var rootGo in level8.GetRootGameObjects())
            {
                if (source == null)
                {
                    var r = rootGo.GetComponentInChildren<LaserReceiver>(true);
                    if (r != null) source = r.gameObject;
                }
                if (door == null)
                {
                    var d = rootGo.GetComponentInChildren<Door>(true);
                    if (d != null) door = d;
                }
            }

            if (source == null)
            {
                Log("Level8 里没有找到挂了 LaserReceiver 的 Receiver，取消。");
                return;
            }
            Log("模板位置：" + source.transform.position);

            // 3. 清掉上一次生成的 ReceiverPlus（含旧的预制体实例）
            var toRemove = new System.Collections.Generic.List<GameObject>();
            foreach (var rootGo in level8.GetRootGameObjects())
                if (rootGo.name == NewName) toRemove.Add(rootGo);
            foreach (var go in toRemove)
            {
                Object.DestroyImmediate(go);
                Log("已移除旧的 " + NewName);
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ReceiverPlus.prefab") != null)
                AssetDatabase.DeleteAsset("Assets/Prefab/ReceiverPlus.prefab");

            // 4. 用 Receiver 预制体实例化，保证子物体（Light 2D）齐全
            var receiverPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ReceiverPrefabPath);
            if (receiverPrefab == null)
            {
                Log("找不到 " + ReceiverPrefabPath);
                return;
            }

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(receiverPrefab, level8);
            if (inst == null)
            {
                Log("实例化 Receiver 预制体失败。");
                return;
            }
            inst.name = NewName;
            inst.transform.position = source.transform.position + new Vector3(4f, 0f, 0f);
            Undo.RegisterCreatedObjectUndo(inst, "Create ReceiverPlus");

            // 5. 换组件
            Object.DestroyImmediate(inst.GetComponent<LaserReceiver>());
            LaserReceiverPlus plus = inst.AddComponent<LaserReceiverPlus>();

            // 6. 接线：内部引用直接取实例自身，door 取场景里的 Door
            plus.door = door;
            plus.animator = inst.GetComponent<Animator>();
            plus.chargeLight = inst.GetComponentInChildren<Light2D>(true);
            plus.animSpeedParam = "AnimSpeed";
            plus.animLayerIndex = 0;
            plus.dischargeTime = 1.5f;
            plus.maxLightIntensity = 3f;
            plus.requiredHits = 6;

            Log("door = " + (door != null ? door.name : "NULL"));
            Log("animator = " + (plus.animator != null ? plus.animator.name : "NULL"));
            Log("chargeLight = " + (plus.chargeLight != null ? plus.chargeLight.name : "NULL"));

            // 7. 保存场景
            EditorSceneManager.MarkSceneDirty(level8);
            EditorSceneManager.SaveScene(level8);
            Log("Level8 已保存，实例位置 " + inst.transform.position);

            if (openedHere)
                EditorSceneManager.CloseScene(level8, true);

            AssetDatabase.Refresh();
            Log("RECEIVERPLUS_DONE");
        }
        catch (System.Exception e)
        {
            report.AppendLine("ERROR: " + e);
            Debug.LogError("[ReceiverPlusSetup] " + e);
        }
    }
}
