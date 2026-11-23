using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 门音效：气闸门开启。
///
/// 音频：Resources/SFX/door_airlock_open.mp3（1.8 秒，纯气流声）
/// 由 Door.cs 的 OpenDoor() 直接调用，不需要在场景里挂任何东西。
/// </summary>
public static class DoorSfx
{
    const string ClipName = "door_airlock_open";
    const float Volume = 0.85f;

    static AudioSource source;
    static AudioClip clip;
    static bool triedLoad;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        if (source != null) return;

        var host = new GameObject("[DoorSfx]");
        Object.DontDestroyOnLoad(host);

        source = host.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = Volume;
    }

    static AudioClip Load()
    {
        if (clip != null || triedLoad) return clip;
        triedLoad = true;
        clip = Resources.Load<AudioClip>("SFX/" + ClipName);
        if (clip == null)
            Debug.LogWarning("[DoorSfx] 找不到音频: Resources/SFX/" + ClipName);
        return clip;
    }

    /// <summary>门开始打开时调用</summary>
    public static void PlayOpen()
    {
        if (source == null) Init();

        var c = Load();
        if (c == null) return;

        // 连开两扇门时不会互相打断
        source.PlayOneShot(c, 1f);
    }
}
