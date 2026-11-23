using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// UI 音效：进入场景时自动把声音挂到所有 Button 上，不需要改场景。
///
/// 音频文件放在 Assets/Resources/SFX/ 下，按名字加载：
///   ui_hover / ui_click / ui_denied / ui_back
///
/// 音量跟随 AudioListener.volume，所以设置里那个音量滑条（SettingsAudio）会一并生效。
/// </summary>
public static class UiSfx
{
    const string ResourcePath = "SFX/";

    static GameObject host;
    static AudioSource[] voices;
    static int next;
    static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    static readonly HashSet<string> missing = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        if (host != null) return;

        host = new GameObject("[UiSfx]");
        Object.DontDestroyOnLoad(host);

        voices = new AudioSource[4];
        for (int i = 0; i < voices.Length; i++)
        {
            var src = host.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f;      // 2D
            src.volume = 1f;
            voices[i] = src;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 场景里所有按钮（包括未激活的）都挂上
        var buttons = Object.FindObjectsOfType<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].GetComponent<UiSfxButton>() == null)
                buttons[i].gameObject.AddComponent<UiSfxButton>();
        }
    }

    static AudioClip Get(string id)
    {
        if (clips.TryGetValue(id, out var c)) return c;
        if (missing.Contains(id)) return null;

        c = Resources.Load<AudioClip>(ResourcePath + id);
        if (c == null)
        {
            missing.Add(id);
            Debug.LogWarning("[UiSfx] 找不到音频: Resources/" + ResourcePath + id);
            return null;
        }
        clips[id] = c;
        return c;
    }

    /// <summary>播放一个界面音效。id 不带扩展名。</summary>
    public static void Play(string id, float volume = 0.8f, float pitch = 1f)
    {
        if (voices == null) return;
        var clip = Get(id);
        if (clip == null) return;

        var src = voices[next];
        next = (next + 1) % voices.Length;

        src.pitch = pitch;
        src.volume = Mathf.Clamp01(volume);
        src.PlayOneShot(clip, 1f);     // 音量已经放在 src.volume 上
    }

    /// <summary>带随机音高，避免连点时声音完全一样</summary>
    public static void PlayJitter(string id, float volume = 0.8f, float amount = 0.04f)
    {
        Play(id, volume, Random.Range(1f - amount, 1f + amount));
    }
}

/// <summary>
/// 挂在按钮上的监听：点击、不可用。
/// 由 UiSfx 在场景加载时自动添加，不需要手动挂。
/// 鼠标悬停不出声（已按需求去掉）。
/// </summary>
[RequireComponent(typeof(Selectable))]
public class UiSfxButton : MonoBehaviour, IPointerClickHandler
{
    Selectable selectable;

    [Header("音量")]
    [Range(0f, 1f)] public float clickVolume = 0.75f;
    [Range(0f, 1f)] public float deniedVolume = 0.6f;

    void Awake()
    {
        selectable = GetComponent<Selectable>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (selectable != null && !selectable.IsInteractable())
        {
            UiSfx.Play("ui_denied", deniedVolume);
            return;
        }
        // 点击加一点点随机音高，连点时不会像复读机
        UiSfx.PlayJitter("ui_click", clickVolume, 0.03f);
    }
}
