using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("背包状态")]
    public bool hasBattery = false;

    [Header("移动设置")]
    public float moveSpeed = 5f;

    [Header("跳跃设置")]
    public float jumpForce = 10f;
    public Transform groundCheck;
    public float checkRadius = 0.2f;
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("梯子设置")]
    public float climbSpeed = 3f;
    private bool canClimb = false;          // 玩家是否在梯子触发器范围内
    private bool isOnLadder = false;        // 玩家当前是否处于“正在爬梯子”的状态
    private float verticalInput;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private float horizontalInput;
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
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // 水平翻转
        if (horizontalInput > 0) spriteRenderer.flipX = false;
        else if (horizontalInput < 0) spriteRenderer.flipX = true;

        // 地面检测
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        // --- 核心逻辑：处理是否进入/退出爬梯状态 ---
        if (canClimb)
        {
            // 必须按了 W 或 S，才进入爬梯状态
            if (Mathf.Abs(verticalInput) > 0.01f)
            {
                isOnLadder = true;
            }
        }
        else
        {
            // 离开梯子触发器范围，强制退出爬梯
            isOnLadder = false;
        }

        // --- 动画状态逻辑 ---
        if (isOnLadder)
        {
            animator.SetBool("isClimbing", true);
            animator.SetBool("isMoving", false);
            // 动画速度跟着上下输入走
            animator.SetFloat("ClimbSpeed", Mathf.Abs(verticalInput));
        }
        else
        {
            animator.SetBool("isClimbing", false);
            animator.SetBool("isMoving", Mathf.Abs(horizontalInput) > 0.01f);
        }

        // --- 跳跃逻辑 ---
        // 只有在地面上、且不在梯子上时，才能跳跃。在梯子上按空格毫无反应。
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isOnLadder)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }

    void FixedUpdate()
    {
        if (isOnLadder)
        {
            // 在梯子上：关闭重力
            rb.gravityScale = 0f;

            // 👇 核心修改：允许 A/D (horizontalInput) 左右移动，同时保留 W/S (verticalInput) 上下移动
            rb.velocity = new Vector2(horizontalInput * moveSpeed, verticalInput * climbSpeed);
        }
        else
        {
            // 不在梯子上：恢复重力，正常移动
            rb.gravityScale = originalGravityScale;
            rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);
        }
    }

    // --- 梯子区域检测 ---
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Ladder"))
        {
            canClimb = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Ladder"))
        {
            canClimb = false;
            // 如果离开梯子，立刻取消爬梯状态
            if (isOnLadder)
            {
                isOnLadder = false;
            }
        }
    }

    void OnDisable()
    {
        if (rb != null) rb.velocity = new Vector2(0f, rb.velocity.y);
        if (animator != null)
        {
            animator.SetBool("isMoving", false);
            animator.SetBool("isClimbing", false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
    }
}