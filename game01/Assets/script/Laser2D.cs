using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Laser2D : MonoBehaviour
{
    [Header("设置")]
    public int maxReflections = 5;        // 最大反射次数
    public float maxDistance = 100f;      // 单次射线最大距离
    public LayerMask mirrorLayer;         // 镜子所在的图层
    public LayerMask receiverLayer;       // 接收器所在的图层
    public Transform directionPoint;      // 一个子物体，它的"up"方向决定激光的初始朝向

    private LineRenderer line;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true; // 世界坐标，方便计算
    }

    void Update()
    {
        FireLaser(); // 每帧重新计算，实现实时反射效果
    }

    void FireLaser()
    {
        // 初始化线段：从自身位置开始
        line.positionCount = 1;
        line.SetPosition(0, transform.position);

        Vector2 origin = transform.position;
        Vector2 direction = directionPoint.up; // 用子物体的up轴作为激光方向

        int reflectCount = 0;

        // 开始循环：反射次数用尽，或没打中任何东西，就停止
        while (reflectCount <= maxReflections)
        {
            // 1. 发射射线，检测是否击中物体
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, maxDistance, mirrorLayer | receiverLayer);

            // 如果没有击中任何物体，画一条到最远距离的线，然后结束
            if (hit.collider == null)
            {
                line.positionCount++;
                line.SetPosition(line.positionCount - 1, origin + direction * maxDistance);
                break;
            }

            // 2. 击中了物体，先把线段延伸到碰撞点
            line.positionCount++;
            line.SetPosition(line.positionCount - 1, hit.point);

            // 3. 检查击中的是不是接收器
            if (((1 << hit.collider.gameObject.layer) & receiverLayer) != 0)
            {
                hit.collider.GetComponent<LaserReceiver>()?.OnLaserHit();
                break; // 击中接收器后，光线终止
            }

            // 4. 计算反射方向，为下一次循环做准备
            // 关键：将碰撞点沿法线方向偏移一点点，防止下一次射线起点卡在碰撞体内部导致检测失败
            Vector2 offset = hit.normal * 0.01f;
            origin = hit.point + offset;
            direction = Vector2.Reflect(direction, hit.normal);
            reflectCount++;
        }
    }
}