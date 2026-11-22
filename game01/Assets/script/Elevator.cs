using System.Collections;
using UnityEngine;

/// <summary>
/// 电梯：角色完全进入后关门困住，然后自动上升 rideHeight（默认 4 个瓦片）。
/// 上升期间角色被固定在电梯内（关闭 Player 脚本与刚体模拟），到顶后按设置决定是否开门。
/// </summary>
public class Elevator : MonoBehaviour
{
    [Header("运行参数")]
    [Tooltip("上升高度（瓦片单位，1 = 1 格）")]
    public float rideHeight = 4f;
    [Tooltip("上升耗时（秒）")]
    public float rideDuration = 1.4f;
    [Tooltip("角色站立多久后判定为“完全进入”")]
    public float enterDelay = 0.12f;

    [Header("结构")]
    [Tooltip("左右两扇门的碰撞体（初始应关闭）")]
    public Collider2D[] doorColliders;
    [Tooltip("电梯内部检测区域（Trigger）")]
    public Collider2D interiorTrigger;
    [Tooltip("底部地板碰撞体（上升前开启）")]
    public Collider2D floorCollider;

    [Header("行为")]
    [Tooltip("到顶后是否开门放人")]
    public bool openDoorOnArrive = true;
    [Tooltip("只能坐一次")]
    public bool oneTime = true;

    [Header("玩家")]
    public string playerTag = "Player";

    bool riding;
    bool used;
    float insideTimer;
    Vector3 startPosition;

    void Awake()
    {
        startPosition = transform.position;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (riding || (oneTime && used)) return;
        if (!other.CompareTag(playerTag)) return;
        if (other.attachedRigidbody == null) return;
        if (!IsFullyInside(other)) { insideTimer = 0f; return; }

        insideTimer += Time.fixedDeltaTime;
        if (insideTimer >= enterDelay) StartCoroutine(Ride(other));
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag)) insideTimer = 0f;
    }

    /// <summary>角色中心是否已经落在电梯内部区域（视为完全进入）。</summary>
    bool IsFullyInside(Collider2D col)
    {
        Bounds ib = interiorTrigger != null ? interiorTrigger.bounds : new Bounds(transform.position, new Vector3(1f, 2f, 0f));
        Vector3 c = col.bounds.center;
        return ib.Contains(new Vector3(c.x, c.y, ib.center.z));
    }

    IEnumerator Ride(Collider2D col)
    {
        riding = true;
        used = true;

        // 关门
        SetDoors(true);

        Rigidbody2D rb = col.attachedRigidbody;
        Transform pt = rb.transform;
        Player ps = rb.GetComponent<Player>();
        if (ps == null) ps = rb.GetComponentInChildren<Player>();

        rb.velocity = Vector2.zero;
        rb.simulated = false;
        pt.SetParent(transform, true);
        if (ps != null) ps.enabled = false;

        // 角色已经在里面，这时开启底板（避免上升途中掉落）
        if (floorCollider != null) floorCollider.enabled = true;

        Vector3 from = transform.position;
        Vector3 to = from + Vector3.up * rideHeight;
        float t = 0f;
        while (t < rideDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / rideDuration);
            k = k * k * (3f - 2f * k); // 缓入缓出
            transform.position = Vector3.Lerp(from, to, k);
            yield return null;
        }
        transform.position = to;

        // 放开角色
        if (ps != null) ps.enabled = true;
        pt.SetParent(null, true);
        rb.simulated = true;
        rb.velocity = Vector2.zero;

        if (openDoorOnArrive) SetDoors(false);

        riding = false;
        insideTimer = 0f;
    }

    void SetDoors(bool closed)
    {
        if (doorColliders == null) return;
        foreach (var d in doorColliders)
            if (d != null) d.enabled = closed;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 bottom = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);
        Vector3 top = bottom + Vector3.up * rideHeight;
        Vector3 right = Vector3.right * 0.5f;
        Gizmos.DrawWireCube(bottom + Vector3.up * 1f, new Vector3(1f, 2f, 0f));
        Gizmos.DrawLine(bottom + right, top + right);
        Gizmos.DrawLine(bottom - right, top - right);
        Gizmos.DrawWireCube(top + Vector3.up * 1f, new Vector3(1f, 2f, 0f));
    }
}
