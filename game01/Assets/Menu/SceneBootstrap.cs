using UnityEngine;

/// <summary>
/// Place one in every gameplay/empty level scene. On Start, if the screen is
/// currently covered by the fade-to-black overlay, fade back out. This guarantees
/// the 0.25s black fade-out also runs when a scene is entered directly.
/// </summary>
public class SceneBootstrap : MonoBehaviour
{
    void Start()
    {
        if (FadeManager.Instance != null)
            FadeManager.Instance.FadeInFromBlack();
    }
}
