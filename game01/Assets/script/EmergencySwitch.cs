using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class EmergencySwitch : MonoBehaviour
{
    [Header("引用")]
    public Transform player;
    public Light2D[] emergencyLights; // 👈 把所有应急灯的 Light2D 拖进这个数组

    [Header("外观变化")]
    public SpriteRenderer fuseBoxRenderer;
    public Sprite activatedSprite;

    [Header("参数")]
    public float interactRange = 2f;
    public float emergencyIntensity = 2f; // 应急灯最终亮度
    public float fadeDuration = 0.5f;     // 应急灯亮起的耗时

    private bool isActivated = false;

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (fuseBoxRenderer == null) fuseBoxRenderer = GetComponent<SpriteRenderer>();

        // 确保开局应急灯都是关的
        foreach (var light in emergencyLights)
        {
            if (light != null) light.intensity = 0f;
        }
    }

    void Update()
    {
        if (player == null || isActivated) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= interactRange && Input.GetKeyDown(KeyCode.E))
        {
            // 注意：这里假设你没有用到电池机制。如果需要电池，加上之前的判断即可。
            isActivated = true;

            // 切换电闸图片
            if (fuseBoxRenderer != null && activatedSprite != null)
            {
                fuseBoxRenderer.sprite = activatedSprite;
            }

            // 启动应急灯序列
            StartCoroutine(TurnOnEmergencyLights());
        }
    }

    private IEnumerator TurnOnEmergencyLights()
    {
        // 这里可以加个“嗡嗡”的音效

        // 依次点亮应急灯（有先后顺序，更有电影感）
        foreach (var light in emergencyLights)
        {
            if (light == null) continue;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                light.intensity = Mathf.Lerp(0f, emergencyIntensity, elapsed / fadeDuration);
                yield return null;
            }
            light.intensity = emergencyIntensity;

            // 每亮一盏，停顿 0.2 秒再亮下一盏
            yield return new WaitForSeconds(0.2f);
        }
    }
}