using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 梯子引导灯 —— 参数取自 Level4 的 EmergencySwitch 灯光（淡黄 Point 灯）。
/// 角色靠近时逐渐亮起，远离时逐渐熄灭。
/// </summary>
[RequireComponent(typeof(Light2D))]
public class ClimberGlowLight : MonoBehaviour
{
    [Header("引用")]
    public Transform player;

    [Header("亮度")]
    public float maxIntensity = 0.5f;   // 与 Level4 emergencyIntensity 一致
    public float fadeSpeed = 6f;        // 每秒亮度变化量

    [Header("距离")]
    public float onDistance = 2.0f;     // 小于这个距离 = 全亮
    public float offDistance = 4.18f;   // 大于这个距离 = 全灭（与灯光外半径一致）

    Light2D glowLight;

    void Awake()
    {
        glowLight = GetComponent<Light2D>();
    }

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (glowLight != null) glowLight.intensity = 0f;
    }

    void Update()
    {
        if (player == null || glowLight == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        // 距离越近越亮：onDistance 内满亮，offDistance 外全黑，中间线性过渡
        float t = Mathf.Clamp01((offDistance - dist) / Mathf.Max(0.01f, offDistance - onDistance));
        float target = t * maxIntensity;

        glowLight.intensity = Mathf.MoveTowards(glowLight.intensity, target, fadeSpeed * Time.deltaTime);
    }
}
