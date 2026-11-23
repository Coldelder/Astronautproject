using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 角色走路音效。
///
/// 判断依据直接读现有组件，不改 Player.cs：
///   · Animator 的 isMoving / isClimbing —— Player 脚本本来就在维护这两个开关
///   · Rigidbody2D 的竖直速度 —— 跳起来或掉下去时不出脚步声
///
/// 音效按高度分（只在 sandSceneName 这一关生效）：
///   · 靠近地面 → sand_walk（沙土，音量小）
///   · 站到上面 → step_walk（金属，音量正常）
///
/// "地面"是自动标定的：玩家出生时站的高度就是地面，不用手填。
/// 如果标定得不对，把 groundYOverride 填成实际地面高度即可。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("金属地板（默认）")]
    public string metalClipName = "step_walk";
    [Range(0f, 1f)] public float metalVolume = 0.32f;

    [Header("沙土地面（只在下面这个关卡生效）")]
    [Tooltip("这个场景按高度切换沙土/金属")]
    public string sandSceneName = "Level5";
    public string sandClipName = "sand_walk";
    [Range(0f, 1f)] public float sandVolume = 0.18f;

    [Tooltip("离地面多高以内算沙土（世界单位）")]
    public float sandHeight = 1.5f;

    [Tooltip("切换的过渡宽度，避免忽大忽小")]
    public float blendWidth = 0.6f;

    [Tooltip("地面高度。0 表示自动取玩家出生高度")]
    public float groundYOverride = 0f;

    [Header("手感")]
    public float fadeSpeed = 10f;
    public float airVelocityThreshold = 0.6f;

    AudioSource sandSrc;
    AudioSource metalSrc;
    Animator animator;
    Rigidbody2D body;

    float groundY;
    bool sandMode;
    float targetSand;
    float targetMetal;

    void Awake()
    {
        animator = GetComponent<Animator>();
        body = GetComponent<Rigidbody2D>();

        string scene = SceneManager.GetActiveScene().name;
        sandMode = (scene == sandSceneName);

        groundY = groundYOverride != 0f ? groundYOverride : transform.position.y;

        sandSrc = CreateSource(sandClipName);
        metalSrc = CreateSource(metalClipName);

        Debug.Log("[PlayerFootsteps] " + scene + (sandMode
            ? " 按高度切换，地面 y=" + groundY.ToString("F2")
            : " 只用金属脚步"));
    }

    AudioSource CreateSource(string id)
    {
        var src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = true;
        src.spatialBlend = 0f;
        src.volume = 0f;

        var clip = Resources.Load<AudioClip>("SFX/" + id);
        if (clip != null) src.clip = clip;
        else Debug.LogWarning("[PlayerFootsteps] 找不到音频: Resources/SFX/" + id);

        return src;
    }

    void Update()
    {
        if (animator == null || body == null) return;

        bool moving = animator.GetBool("isMoving") && !animator.GetBool("isClimbing");
        bool inAir = Mathf.Abs(body.velocity.y) > airVelocityThreshold;
        bool playing = moving && !inAir;

        float h = transform.position.y - groundY;

        // 高度权重：贴地在 sandHeight 以内算沙土，往上过渡到金属
        float sandWeight = 0f;
        if (sandMode)
        {
            float lo = sandHeight - blendWidth * 0.5f;
            float hi = sandHeight + blendWidth * 0.5f;
            sandWeight = 1f - Mathf.InverseLerp(lo, hi, h);
        }

        targetSand = playing ? sandVolume * sandWeight : 0f;
        targetMetal = playing ? metalVolume * (1f - sandWeight) : 0f;

        Fade(sandSrc, targetSand);
        Fade(metalSrc, targetMetal);
    }

    void Fade(AudioSource src, float target)
    {
        if (src == null || src.clip == null) return;

        src.volume = Mathf.MoveTowards(src.volume, target, fadeSpeed * Time.deltaTime);

        if (target > 0.001f && !src.isPlaying) src.Play();
        else if (target <= 0.001f && src.isPlaying && src.volume <= 0.001f) src.Stop();
    }
}

/// <summary>
/// 自动把 PlayerFootsteps 挂到玩家身上（按 Tag = Player 查找）。
/// 场景切换后也会重新查找，所以玩家预制体不需要改。
/// </summary>
public static class PlayerFootstepsBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        if (player.GetComponent<Rigidbody2D>() == null) return;
        if (player.GetComponent<PlayerFootsteps>() != null) return;

        player.AddComponent<PlayerFootsteps>();
    }
}
