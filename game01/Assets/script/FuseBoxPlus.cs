using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// FuseBoxPlus：与 FuseBox 逻辑一致（玩家带电池靠近按 E -> 换图 -> 灯光闪烁后变亮 -> 给激光通电），
/// 区别在于可以一次给最多 6 个 LaserController 通电。
/// </summary>
public class FuseBoxPlus : MonoBehaviour
{
    /// <summary>最多支持的激光数量。</summary>
    public const int MaxLaserCount = 6;

    [Header("引用")]
    public Transform player;
    [Tooltip("最多 6 个，多余的在运行时会自动截断")]
    public LaserController[] laserControllers = new LaserController[MaxLaserCount];
    public SpriteRenderer fuseBoxRenderer;
    public Sprite activatedSprite;

    [Header("灯光效果")]
    public Light2D targetLight;             // 要闪烁变亮的那盏灯
    public float finalIntensity = 2f;       // 闪烁结束后的最终亮度
    public float flickerDuration = 0.5f;
    public int flickerCount = 5;

    [Header("交互")]
    public float interactRange = 2f;

    private bool isActivated = false;

    void Awake()
    {
        // 超过 6 个的引用直接截断，保证不会无限扩展
        if (laserControllers != null && laserControllers.Length > MaxLaserCount)
        {
            Debug.LogWarning("[FuseBoxPlus] laserControllers 最多支持 " + MaxLaserCount + " 个，已截断多余引用。");
            LaserController[] trimmed = new LaserController[MaxLaserCount];
            for (int i = 0; i < MaxLaserCount; i++) trimmed[i] = laserControllers[i];
            laserControllers = trimmed;
        }
    }

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (fuseBoxRenderer == null) fuseBoxRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (player == null || isActivated) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= interactRange && Input.GetKeyDown(KeyCode.E))
        {
            Player playerScript = player.GetComponent<Player>();

            if (playerScript != null && playerScript.hasBattery)
            {
                isActivated = true;
                playerScript.hasBattery = false;

                // 切换保险丝外观
                if (fuseBoxRenderer != null && activatedSprite != null)
                {
                    fuseBoxRenderer.sprite = activatedSprite;
                }

                // 灯光闪烁 + 变亮，结束后给所有激光通电
                if (targetLight != null)
                {
                    StartCoroutine(FlickerAndBrighten());
                }
                else
                {
                    PowerOnAllLasers();
                }
            }
        }
    }

    /// <summary>闪烁一段时间后平滑提亮，最后把最多 6 个激光全部通电。</summary>
    private IEnumerator FlickerAndBrighten()
    {
        float originalIntensity = targetLight.intensity;
        float flickerInterval = flickerDuration / (flickerCount * 2f);

        // 1. 闪烁阶段
        for (int i = 0; i < flickerCount; i++)
        {
            targetLight.intensity = 0f;
            yield return new WaitForSeconds(flickerInterval);

            targetLight.intensity = originalIntensity;
            yield return new WaitForSeconds(flickerInterval);
        }

        // 2. 平滑变亮阶段
        float elapsed = 0f;
        float brightenTime = 1f;
        float startIntensity = targetLight.intensity;

        while (elapsed < brightenTime)
        {
            elapsed += Time.deltaTime;
            targetLight.intensity = Mathf.Lerp(startIntensity, finalIntensity, elapsed / brightenTime);
            yield return null;
        }

        targetLight.intensity = finalIntensity;

        // 3. 灯光稳定后再统一通电
        PowerOnAllLasers();
    }

    /// <summary>给数组内所有非空 LaserController 通电（最多 MaxLaserCount 个）。</summary>
    public void PowerOnAllLasers()
    {
        if (laserControllers == null) return;

        int limit = Mathf.Min(laserControllers.Length, MaxLaserCount);
        int powered = 0;
        for (int i = 0; i < limit; i++)
        {
            LaserController lc = laserControllers[i];
            if (lc == null) continue;
            lc.PowerOn();
            powered++;
        }

        Debug.Log("[FuseBoxPlus] 保险丝已接通，本次通电激光数：" + powered);
    }
}
