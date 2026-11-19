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
    public bool isPowered = false;
    public bool isInteractable = true; // 👈 激光也可以加个开关

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

        SetLaserState(false);
    }

    void Update()
    {
        if (player == null || directionPoint == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        bool inRange = distance <= interactRange;

        if (!isInteractable) return;

        if (!isControlling)
        {
            if (inRange && Input.GetKeyDown(KeyCode.E))
            {
                if (!isPowered)
                {
                    Debug.Log("发射器没有电！需要去拉电闸。");
                    return;
                }

                // 使用和镜子相同的静态锁
                if (!MirrorController.isAnyObjectControlled)
                {
                    isControlling = true;
                    MirrorController.isAnyObjectControlled = true; // 👈 占用全局锁
                    if (playerMoveScript != null) playerMoveScript.enabled = false;
                }
            }
        }
        else
        {
            float rotateInput = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) rotateInput = 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) rotateInput = -1f;

            if (rotateInput != 0f)
            {
                transform.Rotate(0f, 0f, rotateInput * rotateSpeed * Time.deltaTime);
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                isControlling = false;
                MirrorController.isAnyObjectControlled = false; // 👈 释放全局锁
                if (playerMoveScript != null) playerMoveScript.enabled = true;
            }
        }
    }

    public void PowerOn()
    {
        isPowered = true;
        SetLaserState(true);
    }

    void SetLaserState(bool isOn)
    {
        if (lineRenderer != null) lineRenderer.enabled = isOn;
        if (laser2D != null) laser2D.enabled = isOn;
    }

    // 👇 同样加上防卡死保护
    void OnDisable()
    {
        if (isControlling)
        {
            isControlling = false;
            MirrorController.isAnyObjectControlled = false;
            if (playerMoveScript != null) playerMoveScript.enabled = true;
        }
    }
}