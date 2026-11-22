using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// ReceiverPlus —— 强化版接收器。
///
/// 与 LaserReceiver 的组件与表现完全一致（同样的 Sprite、Animator、Light2D、Door）。
/// 区别在于充能进度直接跟“当前照射激光条数”挂钩：
///   charge 目标值 = 当前照射条数 / requiredHits
///   1 条照射 -> 约 1/6 满，2 条 -> 2/6 …… 6 条 -> 满
///   条数减少时目标值随之降低，动画按参考 Receiver 的“空 -> 满”精灵图回退到对应帧。
/// </summary>
public class LaserReceiverPlus : MonoBehaviour
{
    [Header("引用")]
    public Door door;                       // 充满后打开的门

    [Header("照射要求")]
    [Tooltip("几条激光同时照射算“充满”（也是状态 100% 的分母）")]
    [Min(1)] public int requiredHits = 6;

    [Tooltip("超过这个时间没被同一条激光照射，就认为该激光移开了")]
    public float hitTimeout = 0.1f;

    [Header("状态进度")]
    public float chargeTime = 0.6f;         // 进度从 0 涨到 1 所需秒数（增长速度）
    public float dischargeTime = 1.5f;      // 进度从 1 掉到 0 所需秒数（回落速度）

    [Range(0f, 1f)] public float charge;    // 实时进度 0~1（等于目标进度时停住）

    [Header("动画控制 (必须挂Animator)")]
    public Animator animator;
    public string animSpeedParam = "AnimSpeed";
    public int animLayerIndex = 0;

    [Header("充能发光")]
    public Light2D chargeLight;             // 接收器子物体上的 2D 光源
    public float maxLightIntensity = 3f;

    public bool IsCharged { get; private set; }

    /// <summary>当前真正照在身上的激光条数。</summary>
    public int CurrentHits { get; private set; }

    /// <summary>当前应有的进度（= 照射条数 / requiredHits）。</summary>
    public float TargetCharge => Mathf.Clamp01((float)CurrentHits / requiredHits);

    // 记录每一条激光（按发射物体的 instanceID 区分）最后一次命中时间
    readonly Dictionary<int, float> lastHitByLaser = new Dictionary<int, float>();

    int lastLoggedHits = -1;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
    }

    void OnDisable()
    {
        lastHitByLaser.Clear();
        CurrentHits = 0;
    }

    /// <summary>由 Laser2D 每帧调用，source 是发射这条激光的物体。</summary>
    public void OnLaserHit(GameObject source)
    {
        int id = source != null ? source.GetInstanceID() : 0;
        lastHitByLaser[id] = Time.time;
    }

    /// <summary>兼容旧的无参调用（没有发射源信息时按单条激光处理）。</summary>
    public void OnLaserHit()
    {
        OnLaserHit(null);
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;

        CurrentHits = CountActiveLasers();

        if (CurrentHits != lastLoggedHits)
        {
            lastLoggedHits = CurrentHits;
            Debug.Log($"[ReceiverPlus] '{name}' 当前照射激光 {CurrentHits}/{requiredHits}，" +
                      $"目标进度 {Mathf.RoundToInt(TargetCharge * 100f)}%");
        }

        UpdateCharge();
        ApplyChargeToAnimator();

        if (chargeLight != null)
            chargeLight.intensity = Mathf.Lerp(0f, maxLightIntensity, charge);

        // 达到满状态才算完成（必须真的 6 条同时照射）
        if (!IsCharged && CurrentHits >= requiredHits && charge >= 0.99f)
            Complete();
    }

    int CountActiveLasers()
    {
        float now = Time.time;
        int count = 0;
        List<int> stale = null;

        foreach (var kv in lastHitByLaser)
        {
            if (now - kv.Value < hitTimeout)
            {
                count++;
            }
            else
            {
                if (stale == null) stale = new List<int>();
                stale.Add(kv.Key);
            }
        }

        if (stale != null)
            for (int i = 0; i < stale.Count; i++)
                lastHitByLaser.Remove(stale[i]);

        return count;
    }

    /// <summary>把进度平滑地推向“当前条数对应的状态”，条数变了就往上/往下走对应的距离。</summary>
    void UpdateCharge()
    {
        if (IsCharged) { charge = 1f; return; }

        float target = TargetCharge;

        // 上升用 chargeTime，回落用 dischargeTime；0 表示该方向瞬间到位
        float duration = target > charge ? chargeTime : dischargeTime;
        if (duration <= 0f)
        {
            charge = target;
            return;
        }

        float step = Time.deltaTime / duration;
        charge = Mathf.MoveTowards(charge, target, step);
    }

    /// <summary>把 charge 直接映射到 Receiver 的“空 -> 满”动画帧上（不改动原有 Animator）。</summary>
    void ApplyChargeToAnimator()
    {
        if (animator == null) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(animLayerIndex);

        // 停掉动画自身的推进，改由我们按 charge 手动定位帧
        if (!string.IsNullOrEmpty(animSpeedParam))
            animator.SetFloat(animSpeedParam, 0f);

        int hash = stateInfo.fullPathHash;
        if (hash == 0) return;

        // 动画是循环的：normalizedTime 传 1.0 会回绕到第 0 帧（空），所以满状态停在 0.999
        float t = Mathf.Clamp01(charge);
        if (t >= 1f) t = 0.999f;

        animator.Play(hash, animLayerIndex, t);
    }

    void Complete()
    {
        if (IsCharged) return;

        charge = 1f;
        IsCharged = true;

        Debug.Log($"[ReceiverPlus] '{name}' {requiredHits} 路激光齐射，接收已满");

        if (door != null)
            door.OpenDoor();
        else
            Debug.LogWarning($"[ReceiverPlus] '{name}' 充满电了，但 door 没有赋值。", this);
    }
}
