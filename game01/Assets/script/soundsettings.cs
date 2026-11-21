using UnityEngine;
using UnityEngine.UI;

public class SettingsAudio : MonoBehaviour
{
    [Tooltip("音量滑动条，留空则自动查找")]
    public Slider volumeSlider;

    const string VOL_KEY = "MasterVolume";

    void Start()
    {
        // 读取上次保存的音量，没有记录时默认 1（最大）
        float vol = PlayerPrefs.GetFloat(VOL_KEY, 1f);
        AudioListener.volume = vol;

        if (volumeSlider == null)
            volumeSlider = FindObjectOfType<Slider>();

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.value = vol;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
    }

    // 滑条拖动时调用：实时改音量并保存
    public void SetVolume(float value)
    {
        float v = Mathf.Clamp01(value);
        AudioListener.volume = v;
        PlayerPrefs.SetFloat(VOL_KEY, v);
        PlayerPrefs.Save();
    }

    void OnDestroy()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(SetVolume);
    }
}
