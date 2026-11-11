using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour
{
    [Header("动画设置")]
    public Animator animator;
    public string disappearTriggerName = "Disappear";

    [Header("动画时长控制 (关键修改)")]
    [Tooltip("把消失动画的 AnimationClip 直接拖进来，确保时间100%准确")]
    public AnimationClip disappearClip;

    [Header("设置")]
    public float extraWaitTime = 0.1f;

    public void OpenDoor()
    {
        if (animator != null)
        {
            animator.SetTrigger(disappearTriggerName);
            StartCoroutine(WaitAndDisappear());
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private IEnumerator WaitAndDisappear()
    {
        // 1. 等待一帧，确保 Trigger 被 Animator 接收并开始切换
        yield return null;

        float waitTime = 1f; // 兜底时间

        // 2. 优先使用拖进来的动画片段长度
        if (disappearClip != null)
        {
            waitTime = disappearClip.length;
        }
        else
        {
            // 3. 如果没拖，尝试动态获取
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            // 如果当前还在 DoorIdle 状态，只能强行给个保底时间
            if (stateInfo.IsName("DoorIdle"))
            {
                waitTime = 0.5f;
            }
            else
            {
                waitTime = stateInfo.length;
            }
        }

        Debug.Log($"[Door] 预计等待时间: {waitTime} 秒");
        yield return new WaitForSeconds(waitTime + extraWaitTime);

        Debug.Log("[Door] 动画播放完毕，门现在隐藏。");
        gameObject.SetActive(false);
    }
}