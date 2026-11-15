using UnityEngine;

public class MirrorController : MonoBehaviour
{
    [Header("引用")]
    public Transform player;
    public MonoBehaviour playerMoveScript;

    [Header("交互参数")]
    public float interactRange = 5f;
    public float moveSpeed = 3f;    // 保留，以后想恢复左右移动还能用
    public float rotateSpeed = 90f;

    [Header("移动边界限制")]
    public float minX = -5f;
    public float maxX = 5f;

    private bool isControlling = false;
    // 👈 删除了 private Rigidbody2D playerRb;

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        // 👈 删除了获取 playerRb 的代码
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        bool inRange = distance <= interactRange;

        if (Input.GetKeyDown(KeyCode.E) && inRange)
        {
            if (isControlling)
            {
                // 退出操控
                isControlling = false;
                if (playerMoveScript != null) playerMoveScript.enabled = true;
            }
            else
            {
                // 进入操控
                isControlling = true;

                // 只要这一行禁用了脚本，Player脚本里的 OnDisable 就会自动清零速度和播待机动画
                if (playerMoveScript != null) playerMoveScript.enabled = false;

                // 👈 删除了强行刹车的那段 playerRb.velocity 代码
            }
        }

        if (isControlling)
        {
            /* 
            // ========== 镜子左右 A/D 移动功能（已注释） ==========
            float moveInput = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(moveInput) > 0.01f)
            {
                float newX = transform.position.x + moveInput * moveSpeed * Time.deltaTime;
                newX = Mathf.Clamp(newX, minX, maxX);
                transform.position = new Vector3(newX, transform.position.y, transform.position.z);
            }
            // =======================================================
            */

            // 镜子上下 W/S 旋转功能
            float rotateInput = Input.GetAxisRaw("Vertical");
            if (Mathf.Abs(rotateInput) > 0.01f)
            {
                transform.Rotate(0f, 0f, rotateInput * rotateSpeed * Time.deltaTime);
            }
        }
    }
}