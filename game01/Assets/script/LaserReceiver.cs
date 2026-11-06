using UnityEngine;

public class LaserReceiver : MonoBehaviour
{
    public GameObject door; // 在Inspector中把门拖进来

    public void OnLaserHit()
    {
        // 激光击中时，打开门（或触发其他任务完成逻辑）
        if (door != null) door.SetActive(true);
        Debug.Log("激光命中！门已打开。");
    }
}