using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to a Button. On press, shifts the button's own RectTransform down ~3px
/// to simulate a pressed/depressed feel; restores on release.
/// </summary>
public class ButtonPressedEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public float downOffset = -3f;
    private Vector2 anchoredPos;
    private bool hasInit;

    void Start()
    {
        anchoredPos = ((RectTransform)transform).anchoredPosition;
        hasInit = true;
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!hasInit) return;
        ((RectTransform)transform).anchoredPosition = anchoredPos + new Vector2(0, downOffset);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!hasInit) return;
        ((RectTransform)transform).anchoredPosition = anchoredPos;
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (!hasInit) return;
        ((RectTransform)transform).anchoredPosition = anchoredPos;
    }
}
