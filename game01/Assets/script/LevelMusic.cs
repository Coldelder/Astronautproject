using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 全局背景音乐：主菜单 + 各关卡。
///
/// 连续性规则：
///   · 1–4 关共用 base，5–8 关共用 space_station —— 关卡之间**接着播**，不重来
///   · 第 9 关是 spaceship，换曲自然从头开始
///   · 主菜单和选关界面共用 main_theme —— 两边来回走不重来，进关卡时自动换掉
///   · 只要中间经过了菜单 / 选关 / 过场（这些场景没有配乐），下次进关就**从头放**
///   · 曲目切换（比如第 4 关进第 5 关）也从该曲目的开头开始
///
/// 音频放在 Assets/Resources/Music/ 下，按名字加载。
/// 不需要在场景里放任何东西——启动时自动创建播放器。
/// </summary>
public static class LevelMusic
{
    const float Volume = 0.60f;
    const string Folder = "Music/";

    // 场景名 -> 音频名（不带扩展名）
    static readonly Dictionary<string, string> Map = new Dictionary<string, string>
    {
        { "MenuScene", "main_theme" },
        { "LevelScene", "main_theme" },
        { "Level1", "base" },
        { "Level2", "base" },
        { "Level3", "base" },
        { "Level4", "base" },
        { "Level5", "space_station" },
        { "Level6", "space_station" },
        { "Level7", "space_station" },
        { "Level8", "space_station" },
        { "Level9", "spaceship" },
    };

    static GameObject host;
    static AudioSource source;
    static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

    /// 主菜单/选关共用的曲目
    const string MainThemeId = "main_theme";

    /// 这首曲子会在没有配乐的场景里继续播（第9关的音乐贯穿结局）
    const string PersistentTrackId = "spaceship";

    /// 离开主菜单时记住它播到哪儿了，回来接着放
    static float mainThemeTime;

    /// 当前正在播的曲目 id；进入无配乐场景时清空，这样再进关就会从头放
    static string currentId;

    /// 闪避系数：大音效响起时把音乐压下来
    static float duckFactor = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        if (host != null) return;

        host = new GameObject("[LevelMusic]");
        Object.DontDestroyOnLoad(host);

        source = host.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = Volume * duckFactor;

        host.AddComponent<MusicDuckRunner>();

        currentId = null;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (source == null) return;

        string id;
        bool hasTrack = Map.TryGetValue(scene.name, out id);

        // 离开主菜单/选关时，记住主菜单曲播到哪儿了
        if (currentId == MainThemeId && (!hasTrack || id != MainThemeId) && source.isPlaying)
            mainThemeTime = source.time;

        if (!hasTrack)
        {
            // 第9关的音乐：跨过结局/过场继续播，直到回到有配乐的场景（菜单）
            if (currentId == PersistentTrackId) return;

            // 其它情况：停掉并清空，这样下次进关会从头放
            if (source.isPlaying) source.Stop();
            source.clip = null;
            currentId = null;
            return;
        }

        AudioClip clip;
        if (!cache.TryGetValue(id, out clip))
        {
            clip = Resources.Load<AudioClip>(Folder + id);
            if (clip == null)
            {
                Debug.LogWarning("[LevelMusic] 找不到音频: Resources/" + Folder + id);
                return;
            }
            cache[id] = clip;
        }

        // 同一首还在播 —— 关卡之间接着走，不打断
        if (currentId == id && source.clip == clip && source.isPlaying) return;

        // 换曲，或者从选关界面进来：从头开始
        source.clip = clip;
        source.volume = Volume * duckFactor;

        // 回到主菜单：接着上次的位置放；其它情况从 0 开始
        if (id == MainThemeId && mainThemeTime > 0.5f && mainThemeTime < clip.length - 0.5f)
            source.time = mainThemeTime;
        else
            source.time = 0f;

        source.Play();
        currentId = id;
    }

    /// <summary>外部需要时可以手动切歌</summary>
    public static void Play(string id, bool loop = true)
    {
        AudioClip clip;
        if (!cache.TryGetValue(id, out clip))
        {
            clip = Resources.Load<AudioClip>(Folder + id);
            if (clip == null) return;
            cache[id] = clip;
        }
        if (source == null) return;
        source.clip = clip;
        source.loop = loop;
        source.volume = Volume * duckFactor;
        source.Play();
        currentId = id;
    }

    public static void Stop()
    {
        if (source != null) source.Stop();
        currentId = null;
    }

    /// <summary>供 MusicDuckRunner 每帧调用</summary>
    public static void ApplyDuck(float factor)
    {
        duckFactor = Mathf.Clamp01(factor);
        if (source != null && source.isPlaying)
            source.volume = Volume * duckFactor;
    }

    /// <summary>
    /// 压低背景音乐（大音效响起时用）。
    /// 例：LevelMusic.Duck(0.25f, 3.5f, 0.4f) —— 压到 25%，保持 3.5 秒，过渡 0.4 秒。
    /// </summary>
    public static void Duck(float amount, float holdSeconds = 2f, float fadeSeconds = 0.4f)
    {
        MusicDuckRunner.Duck(amount, holdSeconds, fadeSeconds);
    }
}
