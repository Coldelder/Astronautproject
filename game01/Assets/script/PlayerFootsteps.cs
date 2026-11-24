using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 角色走路音效。
///
/// 是否在走路：优先看刚体水平速度，其次才看 Animator 的 isMoving。
/// （原来的写法只认 Animator，某些关卡不设置那个开关就永远没声音。）
///
/// 音效按高度分（只在 sandSceneNames 里列出的关卡生效）：
///   · 靠近地图最低点 → sand_walk（沙土，音量小）
///   · 往上           → step_walk（金属，音量正常）
///
/// "最低点"是自动标定的：记录玩家在该场景里到过的最低 y，不用手填。
/// 想固定成某个高度，填 groundYOverride。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("金属地板（默认）")]
    public string metalClipName = "step_walk";
    [Range(0f, 1f)] public float metalVolume = 0.32f;

    [Header("沙土地面")]
    [Tooltip("这些关卡启用沙地/金属的高度切换")]
    public string[] sandSceneNames = new string[] { "Level5", "Level9" };
    public string sandClipName = "sand_walk";
    [Range(0f, 1f)] public float sandVolume = 0.18f;

    [Tooltip("离地图最低点多高以内算沙土（世界单位）")]
    public float sandHeight = 1.5f;

    [Tooltip("切换的过渡宽度，避免忽大忽小")]
    public float blendWidth = 0.6f;

    [Tooltip("地面高度。0 表示自动取玩家到过的最低点")]
    public float groundYOverride = 0f;

    [Header("手感")]
    public float fadeSpeed = 10f;

    [Tooltip("竖直速度超过这个值就当作在空中，不播脚步")]
    public float airVelocityThreshold = 0.6f;

    [Tooltip("水平速度超过这个值就算在走")]
    public float moveSpeedThreshold = 0.15f;

    AudioSource sandSrc;
    AudioSource metalSrc;
    Animator animator;
    Rigidbody2D body;

    float lowestY;
    bool sandMode;
    bool logged;

    void Awake()
    {
        animator = GetComponent<Animator>();
        body = GetComponent<Rigidbody2D>();

        string scene = SceneManager.GetActiveScene().name;

        sandMode = false;
        if (sandSceneNames != null)
        {
            for (int i = 0; i < sandSceneNames.Length; i++)
            {
                if (sandSceneNames[i] == scene) { sandMode = true; break; }
            }
        }

        lowestY = transform.position.y;

        sandSrc = CreateSource(sandClipName);
        metalSrc = CreateSource(metalClipName);

        Debug.Log("[PlayerFootsteps] " + scene
                  + (sandMode ? " 启用沙地/金属切换" : " 只用金属脚步")
                  + "  起始 y=" + transform.position.y.ToString("F2"));
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
        if (body == null) return;

        Vector2 v = body.velocity;
        bool climbing = animator != null && animator.GetBool("isClimbing");

        // 走路判断：刚体速度优先，Animator 作为补充
        bool speedMoving = Mathf.Abs(v.x) > moveSpeedThreshold;
        bool animMoving = animator != null && animator.GetBool("isMoving");
        bool moving = (speedMoving || animMoving) && !climbing;

        // 空中不播：竖直速度大、或者明显在下落
        bool inAir = Mathf.Abs(v.y) > airVelocityThreshold;
        bool playing = moving && !inAir;

        // 记录到过的最低点，用来判断"地图最底层"
        if (transform.position.y < lowestY) lowestY = transform.position.y;
        float baseY = groundYOverride != 0f ? groundYOverride : lowestY;

        float h = transform.position.y - baseY;
        float sandWeight = 0f;
        if (sandMode)
        {
            float lo = sandHeight - blendWidth * 0.5f;
            float hi = sandHeight + blendWidth * 0.5f;
            sandWeight = 1f - Mathf.InverseLerp(lo, hi, h);
        }

        float targetSand = playing ? sandVolume * sandWeight : 0f;
        float targetMetal = playing ? metalVolume * (1f - sandWeight) : 0f;

        Fade(sandSrc, targetSand);
        Fade(metalSrc, targetMetal);

        // 只打一次，方便排查"为什么没声音"
        if (!logged && playing)
        {
            logged = true;
            Debug.Log("[PlayerFootsteps] 开始播放  v=(" + v.x.ToString("F2") + "," + v.y.ToString("F2")
                      + ")  y=" + transform.position.y.ToString("F2")
                      + "  最低=" + lowestY.ToString("F2")
                      + "  沙=" + targetSand.ToString("F3")
                      + "  金属=" + targetMetal.ToString("F3")
                      + "  监听音量=" + AudioListener.volume.ToString("F2"));
        }
    }

    void Fade(AudioSource src, float target)
    {
        if (src == null || src.clip == null) return;

        src.volume = Mathf.MoveTowards(src.volume, target, fadeSpeed * Time.unscaledDeltaTime);

        if (target > 0.001f && !src.isPlaying) src.Play();
        else if (target <= 0.001f && src.isPlaying && src.volume <= 0.001f) src.Stop();
    }
}

/// <summary>
/// 自动把 PlayerFootsteps 挂到玩家身上（按 Tag = Player 查找）。
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
        if (player == null)
        {
            Debug.LogWarning("[PlayerFootsteps] 场景 " + scene.name + " 里没找到 Tag=Player 的物体");
            return;
        }

        if (player.GetComponent<Rigidbody2D>() == null) return;
        if (player.GetComponent<PlayerFootsteps>() != null) return;

        player.AddComponent<PlayerFootsteps>();
    }
}
