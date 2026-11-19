using UnityEngine;

public class MirrorController : MonoBehaviour
{
    [Header("引用")]
    public Transform player;
    public MonoBehaviour playerMoveScript;

    [Header("交互参数")]
    public bool isInteractable = true; // 该镜子是否允许被操控
    public float interactRange = 5f;
    public float rotateSpeed = 90f;

    // 全局状态锁（所有实例共享这个变量）
    public static bool isAnyObjectControlled = false;

    private bool isControlling = false;

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        bool inRange = distance <= interactRange;

        if (!isInteractable) return;

        // 按下 E 键
        if (Input.GetKeyDown(KeyCode.E) && inRange)
        {
            if (isControlling)
            {
                // 退出操控
                isControlling = false;
                isAnyObjectControlled = false;
                if (playerMoveScript != null) playerMoveScript.enabled = true;
            }
            else
            {
                // 进入操控
                if (!isAnyObjectControlled)
                {
                    isControlling = true;
                    isAnyObjectControlled = true;
                    if (playerMoveScript != null) playerMoveScript.enabled = false;
                }
            }
        }

        // 如果正在被操控，执行旋转
        if (isControlling)
        {
            // 👇 这里把 "Vertical" 改成了 "Horizontal"（对应 A/D 键）
            float rotateInput = Input.GetAxisRaw("Horizontal");

            if (Mathf.Abs(rotateInput) > 0.01f)
            {
                transform.Rotate(0f, 0f, rotateInput * rotateSpeed * Time.deltaTime);
            }
        }
    }

    void OnDisable()
    {
        if (isControlling)
        {
            isControlling = false;
            isAnyObjectControlled = false;
            if (playerMoveScript != null) playerMoveScript.enabled = true;
        }
    }
}