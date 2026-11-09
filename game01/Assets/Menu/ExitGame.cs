using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to a Button. Quits the application.
/// Note: Application.Quit() is a no-op inside the Unity Editor (expected behavior).
/// </summary>
public class ExitGame : MonoBehaviour
{
    void Start()
    {
        var btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(Quit);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
