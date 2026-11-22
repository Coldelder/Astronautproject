using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 电脑终端：玩家身上有 Battery 时，站在交互范围内按 E 可以开机，
/// 精灵图从 idleSprite（computer2，黑屏）换成 activatedSprite（computer1，亮屏）。
/// 没有电池时按 E 没有任何反应。
/// </summary>
public class Computer : MonoBehaviour
{
    [Header("引用")]
    public Transform player;

    [Header("精灵图")]
    public SpriteRenderer computerRenderer;
    public Sprite idleSprite;        // computer2：未通电（黑屏）
    public Sprite activatedSprite;   // computer1：已通电（亮屏）

    [Header("交互")]
    public float interactRange = 2f;
    public bool consumeBattery = false;  // 交互后是否消耗电池
    public bool oneTime = true;          // 只响应第一次

    [Header("通电后转场")]
    public string nextScene = "IntroScene 2";   // 渐黑之后跳转的场景
    public float transitionDelay = 4f;           // 通电后等多久开始渐黑
    public float fadeOutDuration = 1.5f;         // 渐黑用时

    public bool IsActivated { get { return isActivated; } }

    private bool isActivated = false;

    void Awake()
    {
        if (computerRenderer == null) computerRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        if (player == null) player = FindPlayer();
        if (computerRenderer != null && idleSprite != null && computerRenderer.sprite == null)
            computerRenderer.sprite = idleSprite;
    }

    public void SetPlayer(Transform t) { player = t; }

    void Update()
    {
        if (player == null)
        {
            player = FindPlayer();
            if (player == null) return;
        }
        if (isActivated && oneTime) return;

        float distance = Vector2.Distance(transform.position, player.position);
        if (distance > interactRange) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;

        Player playerScript = player.GetComponent<Player>();
        if (playerScript == null) return;

        // 没有电池：无法交互
        if (!playerScript.hasBattery) return;

        isActivated = true;
        if (consumeBattery) playerScript.hasBattery = false;

        if (computerRenderer != null && activatedSprite != null)
            computerRenderer.sprite = activatedSprite;

        Debug.Log("[Computer] 通电成功，屏幕切换为亮屏");
        StartCoroutine(ScreenFadeAndLoad());
    }

    IEnumerator ScreenFadeAndLoad()
    {
        if (transitionDelay > 0f)
            yield return new WaitForSeconds(transitionDelay);

        // 造一层全屏黑幕，从全透明渐到全黑
        var canvasGo = new GameObject("__ComputerBlackout");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;

        var blackGo = new GameObject("Black");
        blackGo.transform.SetParent(canvasGo.transform, false);
        var rect = blackGo.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var img = blackGo.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = false;

        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeOutDuration);
            img.color = new Color(0f, 0f, 0f, k);
            yield return null;
        }
        img.color = Color.black;

        Debug.Log("[Computer] 屏幕已渐黑，跳转到 " + nextScene);
        if (FadeManager.Instance != null)
            FadeManager.Instance.FadeAndLoad(nextScene);
        else
            SceneManager.LoadScene(nextScene);
    }

    static Transform FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        return p != null ? p.transform : null;
    }
}
