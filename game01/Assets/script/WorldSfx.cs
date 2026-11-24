using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 场景里的一次性音效（不带位置，2D）。
///
/// 用法：WorldSfx.Play("breaker", 0.75f);
/// 音频放在 Assets/Resources/SFX/ 下，按名字加载。
/// 内置 6 个音源轮换，短时间内连续触发不会互相打断。
/// </summary>
public static class WorldSfx
{
    const int Voices = 6;

    static GameObject host;
    static AudioSource[] sources;
    static int next;
    static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    static readonly HashSet<string> missing = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        if (host != null) return;

        host = new GameObject("[WorldSfx]");
        Object.DontDestroyOnLoad(host);

        sources = new AudioSource[Voices];
        for (int i = 0; i < Voices; i++)
        {
            var s = host.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = false;
            s.spatialBlend = 0f;
            s.volume = 1f;
            sources[i] = s;
        }
    }

    static AudioClip Get(string id)
    {
        AudioClip c;
        if (cache.TryGetValue(id, out c)) return c;
        if (missing.Contains(id)) return null;

        c = Resources.Load<AudioClip>("SFX/" + id);
        if (c == null)
        {
            missing.Add(id);
            Debug.LogWarning("[WorldSfx] 找不到音频: Resources/SFX/" + id);
            return null;
        }
        cache[id] = c;
        return c;
    }

    public static void Play(string id, float volume = 0.8f, float pitch = 1f)
    {
        if (sources == null) Init();

        var clip = Get(id);
        if (clip == null) return;

        var s = sources[next];
        next = (next + 1) % Voices;

        s.pitch = pitch;
        s.volume = Mathf.Clamp01(volume);
        s.PlayOneShot(clip, 1f);
    }
}
