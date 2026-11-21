using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FlickerRedLight : MonoBehaviour
{
    private Light2D redLight;
    public float minIntensity = 0.2f; // 最暗
    public float maxIntensity = 1.5f; // 最亮
    public float flickerSpeed = 2f;   // 闪烁速度
    public float randomOffset = 0.5f; // 随机错开，防止多个灯同步

    private float noiseOffset;

    void Start()
    {
        redLight = GetComponent<Light2D>();
        noiseOffset = Random.Range(0f, 100f); // 生成随机种子
    }

    void Update()
    {
        // 使用 Perlin Noise 产生不规则的闪烁效果
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed + noiseOffset, 0f);
        redLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}