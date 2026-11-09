using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to a Button. Target scene name is configured in the Inspector.
/// Loads the target scene via the global FadeManager (black fade transition).
/// Falls back to direct LoadScene if FadeManager is not present.
/// </summary>
public class SceneSwitcher : MonoBehaviour
{
    [Tooltip("Exact scene name as shown in Build Settings, e.g. LevelScene")]
    public string targetScene;

    void Start()
    {
        var btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(OnClick);
    }

    public void OnClick()
    {
        if (string.IsNullOrEmpty(targetScene)) return;
        if (FadeManager.Instance != null)
            FadeManager.Instance.FadeAndLoad(targetScene);
        else
            SceneManager.LoadScene(targetScene);
    }
}
