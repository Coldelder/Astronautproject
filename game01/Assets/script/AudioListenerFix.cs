using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 保证每个场景都有 AudioListener。
///
/// 这个工程的 IntroScene 系列（结局/过场动画）里没有 AudioListener，
/// 而监听器又挂在摄像机上、切场景就随旧场景销毁，
/// 结果就是这些场景完全没声音——连跨场景延续的音乐也听不到。
///
/// 做法：常驻一个自己的监听器；进场景时如果场景里已经有监听器，
/// 就关掉自己的，避免同时存在两个。
/// </summary>
public class AudioListenerFix : MonoBehaviour
{
    static AudioListenerFix instance;
    static AudioListener listener;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        if (instance != null) return;

        var go = new GameObject("[AudioListenerFix]");
        Object.DontDestroyOnLoad(go);

        listener = go.AddComponent<AudioListener>();
        listener.enabled = false;

        instance = go.AddComponent<AudioListenerFix>();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (listener == null) return;

        // 场景里已有监听器（一般挂在摄像机上）就不需要自己的
        var all = Object.FindObjectsOfType<AudioListener>();
        bool sceneHas = false;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i] != listener) { sceneHas = true; break; }
        }

        listener.enabled = !sceneHas;

        if (!sceneHas)
            Debug.Log("[AudioListenerFix] " + scene.name + " 没有 AudioListener，已启用常驻监听器");
    }
}
