using UnityEngine;

public class LaserReceiver : MonoBehaviour
{
    [Header("引用")]
    public Door door;                       // 【修改点1】改成了 Door 类型

    [Header("充能参数")]
    public float dischargeTime = 1.5f;      // 决定倒放的速度。如果设为0，则移开激光后暂停不倒放。
    [Range(0f, 1f)] public float charge;    // 实时观察进度，0~1

    [Header("动画控制 (必须挂Animator)")]
    public Animator animator;
    public string animSpeedParam = "AnimSpeed"; // 刚刚在Animator里创建的Float参数名
    public int animLayerIndex = 0;              // 动画所在的层，通常是0

    public bool IsCharged { get; private set; }

    const float HitTimeout = 0.1f; // 超过这个时间没被照射，认为激光移开
    float lastHitTime = -999f;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    public void OnLaserHit()
    {
        lastHitTime = Time.time;
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;

        bool beingHit = Time.time - lastHitTime < HitTimeout;

        ControlAnimation(beingHit);
    }

    void ControlAnimation(bool beingHit)
    {
        if (animator == null) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(animLayerIndex);
        charge = Mathf.Repeat(stateInfo.normalizedTime, 1f);

        if (IsCharged)
        {
            animator.SetFloat(animSpeedParam, 0f);
            return;
        }

        if (beingHit)
        {
            if (charge >= 0.99f)
            {
                animator.SetFloat(animSpeedParam, 0f);
                Complete();
            }
            else
            {
                animator.SetFloat(animSpeedParam, 1f);
            }
        }
        else
        {
            if (charge <= 0.01f)
            {
                animator.SetFloat(animSpeedParam, 0f);
            }
            else
            {
                if (dischargeTime > 0f)
                {
                    animator.SetFloat(animSpeedParam, -1f);
                }
                else
                {
                    animator.SetFloat(animSpeedParam, 0f);
                }
            }
        }
    }

    void Complete()
    {
        if (IsCharged) return;

        charge = 1f;
        IsCharged = true;

        // 【修改点2】呼叫门的开门方法
        if (door != null)
        {
            door.OpenDoor();
        }
        else
        {
            Debug.LogWarning($"[LaserReceiver] '{name}' 充满电了，但 door 没有赋值。", this);
        }
    }
}