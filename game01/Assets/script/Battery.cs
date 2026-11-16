using UnityEngine;

public class Battery : MonoBehaviour
{
    // 当有东西进入触发器时
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 获取玩家脚本并设置拥有电池
            Player player = collision.GetComponent<Player>();
            if (player != null)
            {
                player.hasBattery = true;
                Debug.Log("玩家捡起了电池！");
                Destroy(gameObject); // 捡起后销毁电池物体
            }
        }
    }
}