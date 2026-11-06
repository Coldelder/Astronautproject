using UnityEngine;

public class LaserController : MonoBehaviour
{
    [Header("引用")]
    public Transform player;                 // 玩家
    public Transform directionPoint;         // Laser2D 里控制方向的子物体
    public MonoBehaviour playerMoveScript;   // 可选：玩家的移动脚本，操控时禁用

    [Header("参数")]
    public float interactRange = 2f;         // 玩家离多近可以按 E
    public float rotateSpeed = 90f;          // 旋转速度（度/秒）

    [Header("可选提示")]
    public GameObject hint;                  // 可选：靠近时显示的提示物体

    private bool isControlling = false;

    void Start()
    {
        // 自动找 Player
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // 自动从 Laser2D 拿 directionPoint
        if (directionPoint == null)
        {
            Laser2D laser = GetComponent<Laser2D>();
            if (laser != null) directionPoint = laser.directionPoint;
        }
    }

    void Update()
    {
        if (player == null || directionPoint == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        bool inRange = distance <= interactRange;

        if (hint != null) hint.SetActive(inRange && !isControlling);

        // 未操控状态：靠近按 E 进入操控
        if (!isControlling)
        {
            if (inRange && Input.GetKeyDown(KeyCode.E))
            {
                isControlling = true;

                // 冻结玩家移动，避免一边转发射器一边走路
                if (playerMoveScript != null) playerMoveScript.enabled = false;

                if (hint != null) hint.SetActive(false);
            }
        }
        // 操控状态：A/D 旋转，E 退出
        else
        {
            float rotateInput = 0f;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                rotateInput = 1f;

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                rotateInput = -1f;

            directionPoint.Rotate(0f, 0f, rotateInput * rotateSpeed * Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.E))
            {
                isControlling = false;

                // 恢复玩家移动
                if (playerMoveScript != null) playerMoveScript.enabled = true;
            }
        }
    }
}