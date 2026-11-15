using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f; // 移动速度，可在Inspector中调整

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private float horizontalInput;

    void Start()
    {
        // 获取角色身上的组件
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // 获取水平输入：A/D 或 左右方向键，返回 -1、0、1
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // 水平翻转：输入向右时面向右，输入向左时面向左
        if (horizontalInput > 0)
        {
            spriteRenderer.flipX = false; // 面向右
        }
        else if (horizontalInput < 0)
        {
            spriteRenderer.flipX = true;  // 面向左
        }

        // 控制动画状态
        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            animator.SetBool("isMoving", true);  // 正在移动，播放 Walk
        }
        else
        {
            animator.SetBool("isMoving", false); // 停止移动，播放 Idle
        }
    }

    void FixedUpdate()
    {
        // 设置水平速度，同时保留原本的垂直速度（不影响下落）
        rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);
    }

    // 👇 ================== 新增的方法 ================== 👇
    // 当这个脚本被禁用（enabled = false）时，Unity 会自动调用这个方法
    void OnDisable()
    {
        // 1. 强制水平速度归零（防止惯性滑行）
        if (rb != null)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }

        // 2. 强制通知 Animator 切回待机动画（防止卡在跑步的某一帧）
        if (animator != null)
        {
            animator.SetBool("isMoving", false);
        }
    }
    // 👆 =============================================== 👆
}