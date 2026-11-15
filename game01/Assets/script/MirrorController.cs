using UnityEngine;

public class MirrorController : MonoBehaviour
{
    [Header("引用")]
    public Transform player;
    public MonoBehaviour playerMoveScript;

    [Header("交互参数")]
    public float interactRange = 5f;
    public float moveSpeed = 3f;    // （虽然注释了移动，变量先留着不影响）
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
        }
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
                if (playerMoveScript != null) playerMoveScript.enabled = false;
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