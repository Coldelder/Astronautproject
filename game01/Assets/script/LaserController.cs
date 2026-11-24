using UnityEngine;
using UnityEngine.Rendering.Universal; // 👈 必须引入，用于 Light2D

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
    public bool isInteractable = true;

    [Header("通电发光 (新增)")]
    public Light2D laserLight;               // 👈 拖入激光身上的 2D 光源
    public float targetLightIntensity = 2f;  // 👈 通电后的目标亮度

    private bool isControlling = false;
    public bool IsControlling { get { return isControlling; } }
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

        // 默认关闭激光和光源
        SetLaserState(false);
        if (laserLight != null)
        {
            laserLight.intensity = 0f;
        }
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

                if (!MirrorController.isAnyObjectControlled)
                {
                    isControlling = true;
                    MirrorController.isAnyObjectControlled = true;
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
                MirrorController.isAnyObjectControlled = false;
                if (playerMoveScript != null) playerMoveScript.enabled = true;
            }
        }
    }

    // 👇 电闸调用这个方法
    public void PowerOn()
    {
        isPowered = true;
        SetLaserState(true);

        // 👇 通电时，把激光的光源点亮
        if (laserLight != null)
        {
            laserLight.intensity = targetLightIntensity;
        }

        Debug.Log("激光已通电并自动开启，光源已亮起。");
    }

    void SetLaserState(bool isOn)
    {
        if (lineRenderer != null) lineRenderer.enabled = isOn;
        if (laser2D != null) laser2D.enabled = isOn;
    }

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