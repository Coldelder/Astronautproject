using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FuseBox : MonoBehaviour
{
    [Header("引用")]
    public Transform player;
    public LaserController laserController;

    [Header("外观变化")]
    public SpriteRenderer fuseBoxRenderer;
    public Sprite activatedSprite;

    [Header("灯光效果")]
    public Light2D targetLight;             // 要闪烁的房间灯光
    public float finalIntensity = 2f;       // 闪烁完之后，灯光最终达到的亮度
    public float flickerDuration = 0.5f;
    public int flickerCount = 5;

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

                // 👇 删除了这里原本的激光通电代码，把它移到了协程最后

                // 触发灯光闪烁
                if (targetLight != null)
                {
                    StartCoroutine(FlickerAndBrighten());
                }
                else
                {
                    // 如果没设置闪烁灯光，那就直接给激光通电
                    if (laserController != null) laserController.PowerOn();
                }
            }
        }
    }

    // 灯光闪烁并变亮
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

        // 👇 ================== 修改点：在这里给激光通电 ================== 👇
        // 等房间的灯光完全亮起后，再激活激光
        if (laserController != null)
        {
            laserController.PowerOn();
            Debug.Log("灯光已稳定，激光设备正式启动！");
        }
        // 👆 ============================================================= 👆
    }
}