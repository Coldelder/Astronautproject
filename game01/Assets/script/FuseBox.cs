using UnityEngine;

public class FuseBox : MonoBehaviour
{
    [Header("引用")]
    public Transform player;
    public MonoBehaviour playerMoveScript;
    public LaserController laserController;

    [Header("外观变化")]
    public SpriteRenderer fuseBoxRenderer;  // 电闸的图片组件
    public Sprite activatedSprite;          // 拉闸后要变成的新图片（在Inspector拖入）

    [Header("参数")]
    public float interactRange = 2f;

    private bool isActivated = false; // 电闸是否已经开启过

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // 如果忘了拖，尝试自动获取
        if (fuseBoxRenderer == null) fuseBoxRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (player == null || isActivated) return;

        float distance = Vector2.Distance(transform.position, player.position);

        // 玩家在范围内，并且按下了 E
        if (distance <= interactRange && Input.GetKeyDown(KeyCode.E))
        {
            Player playerScript = player.GetComponent<Player>();

            if (playerScript != null && playerScript.hasBattery)
            {
                // 成功拉闸！
                isActivated = true;

                // 消耗掉电池
                playerScript.hasBattery = false;

                // 通知激光通电并亮起
                if (laserController != null)
                {
                    laserController.PowerOn();
                }

                // 👇 切换电闸的图片
                if (fuseBoxRenderer != null && activatedSprite != null)
                {
                    fuseBoxRenderer.sprite = activatedSprite;
                }

                Debug.Log("电闸已拉下，图片已切换，激光已开启！");
            }
            else
            {
                Debug.Log("电闸没有反应...需要先找到电池！");
            }
        }
    }
}