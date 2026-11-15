using UnityEngine;

public class MirrorController : MonoBehaviour
{
    [Header("引用")]
    public Transform player;
    public MonoBehaviour playerMoveScript;

    [Header("交互参数")]
    public float interactRange = 2f;
    public float moveSpeed = 3f;
    public float rotateSpeed = 90f;

    [Header("移动边界限制")]
    public float minX = -5f;
    public float maxX = 5f;

    private bool isControlling = false;

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else Debug.LogError("[Mirror] 找不到 Player！请检查 Player 的 Tag！");
        }
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        bool inRange = distance <= interactRange;

        // 打印当前距离（为了不刷屏，只在按E的时候打印）
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log($"[Mirror] 按下了E键！当前距离是 {distance}，交互范围是 {interactRange}。是否在范围内：{inRange}");

            if (!inRange)
            {
                Debug.Log("[Mirror] 按了E，但是距离太远了！");
                return;
            }

            if (isControlling)
            {
                // 退出操控
                isControlling = false;
                if (playerMoveScript != null) playerMoveScript.enabled = true;
                Debug.Log("[Mirror] 退出操控状态，玩家移动恢复。");
            }
            else
            {
                // 进入操控
                isControlling = true;
                if (playerMoveScript != null)
                {
                    playerMoveScript.enabled = false;
                    Debug.Log("[Mirror] 进入操控状态，玩家移动已禁用。");
                }
                else
                {
                    Debug.LogError("[Mirror] 进入操控了，但是 Player Move Script 没赋值，无法冻结玩家！");
                }
            }
        }

        if (isControlling)
        {
            float moveInput = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(moveInput) > 0.01f)
            {
                float newX = transform.position.x + moveInput * moveSpeed * Time.deltaTime;
                newX = Mathf.Clamp(newX, minX, maxX);
                transform.position = new Vector3(newX, transform.position.y, transform.position.z);
            }

            float rotateInput = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(rotateInput) > 0.01f)
            {
                transform.Rotate(0f, 0f, rotateInput * rotateSpeed * Time.deltaTime);
            }
        }
    }
}