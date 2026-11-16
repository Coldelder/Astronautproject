using UnityEngine;

public class LaserController : MonoBehaviour
{
    [Header("引用")]
    public Transform player;
    public Transform directionPoint;
    public MonoBehaviour playerMoveScript;

    [Header("参数")]
    public float interactRange = 2f;
    public float rotateSpeed = 90f;

    [Header("状态")]
    public bool isPowered = false;           // 是否已通电（由电闸控制）

    private bool isControlling = false;
    private LineRenderer lineRenderer;
    private Laser2D laser2D;

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (directionPoint == null)
        {
            laser2D = GetComponent<Laser2D>();
            if (laser2D != null) directionPoint = laser2D.directionPoint;
        }

        lineRenderer = GetComponent<LineRenderer>();
        if (laser2D == null) laser2D = GetComponent<Laser2D>();

        SetLaserState(false); // 游戏刚开始时，默认关闭
    }

    void Update()
    {
        if (player == null || directionPoint == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        bool inRange = distance <= interactRange;

        // 未操控状态
        if (!isControlling)
        {
            if (inRange && Input.GetKeyDown(KeyCode.E))
            {
                if (!isPowered)
                {
                    Debug.Log("发射器没有电！需要去拉电闸。");
                    return;
                }

                // 有电，进入操控
                isControlling = true;
                if (playerMoveScript != null) playerMoveScript.enabled = false;
            }
        }
        // 操控状态
        else
        {
            // 旋转激光
            float rotateInput = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) rotateInput = 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) rotateInput = -1f;

            if (rotateInput != 0f)
            {
                transform.Rotate(0f, 0f, rotateInput * rotateSpeed * Time.deltaTime);
            }

            // 按 E 退出（Q键功能已移除）
            if (Input.GetKeyDown(KeyCode.E))
            {
                isControlling = false;
                if (playerMoveScript != null) playerMoveScript.enabled = true;
            }
        }
    }

    // 👇 电闸调用这个方法时，不仅通电，而且直接开启激光
    public void PowerOn()
    {
        isPowered = true;
        SetLaserState(true); // 直接亮起！
        Debug.Log("激光已通电并自动开启。");
    }

    void SetLaserState(bool isOn)
    {
        if (lineRenderer != null) lineRenderer.enabled = isOn;
        if (laser2D != null) laser2D.enabled = isOn;
    }
}