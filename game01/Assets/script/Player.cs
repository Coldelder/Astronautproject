using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f; // 移动速度，可在Inspector中调整

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator; // 1. 新增：声明 Animator 变量
    private float horizontalInput;

    void Start()
    {
        // 获取角色身上的组件
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>(); // 2. 新增：获取 Animator 组件
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

        // 3. 新增：控制动画状态
        // Mathf.Abs(horizontalInput) 取绝对值，只要输入不为 0（即正在按方向键），就返回 true
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
}
