using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 信号塔顶的强光源：初始关闭；Computer 被触发后延迟 delay 秒，
/// 平滑升到 peakIntensity，然后按固定节奏规律闪烁。
/// </summary>
public class TowerLight : MonoBehaviour
{
    [Header("触发源")]
    public Computer computer;

    [Header("灯光")]
    public Light2D targetLight;
    public float peakIntensity = 3.2f;
    public float lowIntensity = 0.35f;

    [Header("时序")]
    public float delay = 0.5f;          // Computer 触发后等多久
    public float rampDuration = 1.2f;   // 平滑变强用时
    public float blinkOn = 0.5f;        // 亮着的时间
    public float blinkOff = 0.3f;       // 熄灭的时间
    public float blinkFade = 0.08f;     // 每次切换的过渡时间

    private bool triggered = false;

    void Start()
    {
        if (targetLight == null) targetLight = GetComponent<Light2D>();
        if (targetLight != null) targetLight.intensity = 0f;

        if (computer == null)
        {
            Computer c = FindObjectOfType<Computer>();
            if (c != null) computer = c;
        }
    }

    void Update()
    {
        if (triggered || computer == null) return;
        if (!computer.IsActivated) return;

        triggered = true;
        StartCoroutine(Sequence());
    }

    IEnumerator Sequence()
    {
        yield return new WaitForSeconds(delay);

        // 平滑变强
        float elapsed = 0f;
        while (elapsed < rampDuration)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.Clamp01(elapsed / rampDuration);
            k = k * k * (3f - 2f * k); // 缓入缓出
            targetLight.intensity = Mathf.Lerp(0f, peakIntensity, k);
            yield return null;
        }
        targetLight.intensity = peakIntensity;

        // 规律闪烁
        while (true)
        {
            yield return FadeTo(peakIntensity, lowIntensity, blinkFade);
            yield return new WaitForSeconds(blinkOff);
            yield return FadeTo(lowIntensity, peakIntensity, blinkFade);
            yield return new WaitForSeconds(blinkOn);
        }
    }

    IEnumerator FadeTo(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            targetLight.intensity = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        targetLight.intensity = to;
    }
}
