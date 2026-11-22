using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the skip button in the intro scene. On click it skips the running
/// cinematic and proceeds to the next scene through CinematicPlayer.
///
/// 修改说明：
/// 原来只 FindObjectOfType&lt;CinematicPlayer&gt;()，而 IntroScene 1 挂的是
/// CinematicPlayer1，所以查不到、点了没反应。现在改为查找任意实现了
/// ISkippableCinematic 的过场播放器，两种过场脚本都能被跳过。
/// </summary>
public class SkipButton : MonoBehaviour
{
    void Start()
    {
        var btn = GetComponent<Button>();
        if (btn == null)
        {
            Debug.LogWarning("[SkipButton] 身上没有 Button 组件，无法绑定点击");
            return;
        }

        btn.onClick.RemoveListener(OnSkip);
        btn.onClick.AddListener(OnSkip);
        Debug.Log("[SkipButton] 已绑定 OnClick，按钮 = " + name);
    }

    /// <summary>点击跳过：找到当前场景里的过场播放器并让它立即结束。</summary>
    public void OnSkip()
    {
        Debug.Log("[SkipButton] 点击触发，开始查找过场播放器");

        MonoBehaviour[] all = FindObjectsOfType<MonoBehaviour>();
        foreach (var mb in all)
        {
            if (mb is ISkippableCinematic skipper)
            {
                Debug.Log("[SkipButton] 命中过场播放器：" + mb.GetType().Name + "，执行 Skip()");
                skipper.Skip();
                return;
            }
        }

        Debug.LogError("[SkipButton] 当前场景里没有任何实现 ISkippableCinematic 的过场播放器");
    }
}
