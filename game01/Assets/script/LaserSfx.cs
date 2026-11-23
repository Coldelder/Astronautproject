using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 激光音效：光束亮着的时候循环播放一段电流嗡鸣。
///
/// 音频：Resources/SFX/beam_loop.mp3（1 秒无缝循环）
/// 由 LaserSfxBootstrap 自动挂到场景里每个 Laser2D 上，预制体不用改。
/// </summary>
[RequireComponent(typeof(Laser2D))]
public class LaserSfx : MonoBehaviour
{
    [Tooltip("光束存在的音量。多束激光同时存在时会叠加，所以压得比较低")]
    [Range(0f, 1f)] public float volume = 0.10f;

    [Tooltip("淡入时间，避免一亮就啪一声")]
    public float fadeIn = 0.4f;

    AudioSource source;
    float target;

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = 0f;

        var clip = Resources.Load<AudioClip>("SFX/beam_loop");
        if (clip != null) source.clip = clip;
        else Debug.LogWarning("[LaserSfx] 找不到音频: Resources/SFX/beam_loop");
    }

    void OnEnable()
    {
        if (!Application.isPlaying) return;
        if (source == null || source.clip == null) return;

        target = volume;
        if (!source.isPlaying)
        {
            source.volume = 0f;
            source.Play();
        }
    }

    void OnDisable()
    {
        target = 0f;
        if (source != null && source.isPlaying) source.Stop();
    }

    void Update()
    {
        if (!Application.isPlaying || source == null || source.clip == null) return;

        float step = (fadeIn > 0.01f) ? (volume / fadeIn) * Time.deltaTime : volume;
        source.volume = Mathf.MoveTowards(source.volume, target, Mathf.Max(step, 0.01f));
    }
}

/// <summary>
/// 场景加载后，给所有 Laser2D 挂上 LaserSfx。
/// </summary>
public static class LaserSfxBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var lasers = Object.FindObjectsOfType<Laser2D>(true);
        for (int i = 0; i < lasers.Length; i++)
        {
            if (lasers[i].GetComponent<LaserSfx>() == null)
                lasers[i].gameObject.AddComponent<LaserSfx>();
        }
    }
}
