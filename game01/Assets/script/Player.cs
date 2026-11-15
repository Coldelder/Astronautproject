using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;

    [Header("跳跃设置")]
    public float jumpForce = 10f;           // 跳跃力度
    public Transform groundCheck;           // 脚底的检测点（空物体）
    public float checkRadius = 0.2f;        // 检测范围大小
    public LayerMask groundLayer;           // 地面所在的图层
    private bool isGrounded;                // 是否在地面上

    [Header("梯子设置")]
    public float climbSpeed = 3f;           // 爬梯子速度
    private bool isOnLadder = false;        // 是否在梯子区域
    private float verticalInput;            // 上下输入

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private float horizontalInput;

    // 记录初始重力，方便离开梯子后恢复
    private float originalGravityScale;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        originalGravityScale = rb.gravityScale;
    }

    void Update()
    {
        // 1. 获取输入
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // 2. 水平翻转
        if (horizontalInput > 0) spriteRenderer.flipX = false;
        else if (horizontalInput < 0) spriteRenderer.flipX = true;

        // 3. 动画状态（如果在梯子上移动，或者在地面移动，都算移动）
        bool isMoving = Mathf.Abs(horizontalInput) > 0.01f || (isOnLadder && Mathf.Abs(verticalInput) > 0.01f);
        animator.SetBool("isMoving", isMoving);

        // 4. 地面检测（画一个隐形的圆，看看有没有碰到Ground层）
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        // 5. 跳跃逻辑
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isOnLadder)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }

    void FixedUpdate()
    {
        // --- 梯子逻辑 ---
        if (isOnLadder)
        {
            // 在梯子上：关闭重力，允许上下移动
            rb.gravityScale = 0f;
            rb.velocity = new Vector2(horizontalInput * moveSpeed, verticalInput * climbSpeed);
        }
        // --- 地面/空中逻辑 ---
        else
        {
            // 不在梯子上：恢复重力，水平移动，保留垂直惯性（下落/跳跃）
            rb.gravityScale = originalGravityScale;
            rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);
        }
    }

    // --- 梯子区域检测 ---
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Ladder"))
        {
            isOnLadder = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Ladder"))
        {
            isOnLadder = false;
        }
    }

    // --- 禁用时的急刹车处理（保留之前的功能） ---
    void OnDisable()
    {
        if (rb != null)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }
        if (animator != null)
        {
            animator.SetBool("isMoving", false);
        }
    }

    // 在场景里画出检测范围的圈，方便调试
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
    }
}