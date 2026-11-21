using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the skip button in the intro scene. On click it skips the running
/// cinematic and proceeds to the next scene through CinematicPlayer.
/// </summary>
public class SkipButton : MonoBehaviour
{
    void Start()
    {
        var btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(OnSkip);
    }

    public void OnSkip()
    {
        var player = FindObjectOfType<CinematicPlayer>();
        if (player != null) player.Skip();
    }
}
