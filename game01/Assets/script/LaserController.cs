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

    // 👇 新增：用来记录激光的开启状态和组件引用
    private bool isLaserOn = false;
    private LineRenderer lineRenderer;
    private Laser2D laser2D;

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
            laser2D = GetComponent<Laser2D>();
            if (laser2D != null) directionPoint = laser2D.directionPoint;
        }

        // 👇 新增：获取组件，并默认关闭激光
        lineRenderer = GetComponent<LineRenderer>();
        if (laser2D == null) laser2D = GetComponent<Laser2D>();

        SetLaserState(false); // 游戏一开始，默认是关闭状态
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

                // 冻结玩家移动
                if (playerMoveScript != null) playerMoveScript.enabled = false;

                if (hint != null) hint.SetActive(false);
            }
        }
        // 操控状态：A/D 旋转，Q 开关激光，E 退出
        else
        {
            // --- 旋转逻辑 ---
            float rotateInput = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) rotateInput = 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) rotateInput = -1f;

            if (rotateInput != 0f)
            {
                transform.Rotate(0f, 0f, rotateInput * rotateSpeed * Time.deltaTime);
            }

            // 👇 新增：按 Q 切换激光状态
            if (Input.GetKeyDown(KeyCode.Q))
            {
                isLaserOn = !isLaserOn; // 状态反转
                SetLaserState(isLaserOn);
            }

            // --- 退出操控逻辑 ---
            if (Input.GetKeyDown(KeyCode.E))
            {
                isControlling = false;

                // 恢复玩家移动
                if (playerMoveScript != null) playerMoveScript.enabled = true;
            }
        }
    }

    // 👇 新增：专门控制激光开关的方法
    void SetLaserState(bool isOn)
    {
        // 1. 控制 LineRenderer 的显示与隐藏
        if (lineRenderer != null) lineRenderer.enabled = isOn;

        // 2. 控制 Laser2D 脚本的启用与禁用
        // 这样做不仅能隐藏线，还能让激光停止发射射线，防止关闭状态下还能触发接收器
        if (laser2D != null) laser2D.enabled = isOn;
    }
}