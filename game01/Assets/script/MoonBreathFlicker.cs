using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class MoonBreathFlicker : MonoBehaviour
{
    public float minAlpha = 0.4f;
    public float maxAlpha = 0.9f;
    public float cycleTime = 2.5f;
    private Image img;
    private Color originColor;
    void Start()
    {
        img = GetComponent<Image>();
        originColor = img.color;
    }
    void Update()
    {
        float t = Mathf.PingPong(Time.time / cycleTime, 1f);
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
        img.color = new Color(originColor.r, originColor.g, originColor.b, alpha);
    }
}
