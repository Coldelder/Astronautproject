using UnityEngine;

/// <summary>
/// 背景音乐闪避（ducking）。
///
/// 某个大音效响起时，把背景音乐压到指定比例，保持一段时间，再平滑恢复。
/// 由 LevelMusic 在启动时自动挂到音乐播放器上。
/// </summary>
public class MusicDuckRunner : MonoBehaviour
{
    static MusicDuckRunner instance;

    float factor = 1f;
    float target = 1f;
    float holdUntil;
    float speed = 2f;

    void Awake()
    {
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    /// <summary>
    /// 压低背景音乐。
    /// </summary>
    /// <param name="amount">压到原来的多少（0.25 = 只剩四分之一）</param>
    /// <param name="holdSeconds">保持多久</param>
    /// <param name="fadeSeconds">压下去和恢复各用多久</param>
    public static void Duck(float amount, float holdSeconds = 2f, float fadeSeconds = 0.4f)
    {
        if (instance == null) return;
        instance.target = Mathf.Clamp01(amount);
        instance.holdUntil = Time.time + Mathf.Max(0f, holdSeconds);
        instance.speed = 1f / Mathf.Max(0.05f, fadeSeconds);
    }

    void Update()
    {
        if (Time.time >= holdUntil) target = 1f;

        factor = Mathf.MoveTowards(factor, target, speed * Time.deltaTime);
        LevelMusic.ApplyDuck(factor);
    }
}
