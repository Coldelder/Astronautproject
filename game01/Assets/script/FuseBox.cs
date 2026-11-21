using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal; // 👈 必须引入这个，才能使用 Light2D

public class FuseBox : MonoBehaviour
{
    [Header("引用")]
    public Transform player;
    public LaserController laserController;

    [Header("外观变化")]
    public SpriteRenderer fuseBoxRenderer;
    public Sprite activatedSprite;

    [Header("灯光效果 (新增)")]
    public Light2D targetLight;             // 要闪烁的灯光（拖入场景里的灯）
    public float finalIntensity = 2f;       // 闪烁完之后，灯光最终达到的亮度
    public float flickerDuration = 0.5f;    // 闪烁持续的时间
    public int flickerCount = 5;            // 闪烁的次数

    [Header("参数")]
    public float interactRange = 2f;

    private bool isActivated = false;

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

                // 切换电闸图片
                if (fuseBoxRenderer != null && activatedSprite != null)
                {
                    fuseBoxRenderer.sprite = activatedSprite;
                }

                // 激光通电
                if (laserController != null) laserController.PowerOn();

                // 👇 触发灯光闪烁
                if (targetLight != null)
                {
                    StartCoroutine(FlickerAndBrighten());
                }
                else
                {
                    Debug.LogWarning("没有指定要闪烁的灯光！");
                }
            }
        }
    }

    // 👇 核心逻辑：灯光闪烁协程
    private IEnumerator FlickerAndBrighten()
    {
        float originalIntensity = targetLight.intensity;
        float flickerInterval = flickerDuration / (flickerCount * 2f);

        // 1. 闪烁阶段
        for (int i = 0; i < flickerCount; i++)
        {
            targetLight.intensity = 0f; // 关灯
            yield return new WaitForSeconds(flickerInterval);

            targetLight.intensity = originalIntensity; // 开灯
            yield return new WaitForSeconds(flickerInterval);
        }

        // 2. 平滑变亮阶段
        float elapsed = 0f;
        float brightenTime = 1f; // 变亮过程耗时1秒
        float startIntensity = targetLight.intensity;

        while (elapsed < brightenTime)
        {
            elapsed += Time.deltaTime;
            targetLight.intensity = Mathf.Lerp(startIntensity, finalIntensity, elapsed / brightenTime);
            yield return null; // 等待下一帧
        }

        targetLight.intensity = finalIntensity; // 确保最后达到目标亮度
    }
}