using UnityEngine;

[ExecuteAlways]   // 编辑模式下也让 Scene 视图实时预览光线与线宽
[RequireComponent(typeof(LineRenderer))]
public class Laser2D : MonoBehaviour
{
    [Header("设置")]
    public int maxReflections = 5;        // 最大反射次数
    public float maxDistance = 100f;      // 单次射线最大距离
    public LayerMask mirrorLayer;         // 镜子所在的图层：会被反射
    public LayerMask receiverLayer;       // 接收器所在的图层：命中后终止
    public LayerMask wallLayer;           // 墙所在的图层：挡住激光，不反射
    public Transform directionPoint;      // 一个子物体，它的"up"方向决定激光的初始朝向

    [Header("外观")]
    [Tooltip("光束宽度（世界单位）。PPU 64 时 1 像素 = 0.0156；0.5 约等于 Player 一条腿的宽度")]
    public float beamWidth = 0.5f;

    private LineRenderer line;

    void OnEnable()
    {
        EnsureLine();
        ApplyBeamWidth();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        EnsureLine();
        ApplyBeamWidth();
    }
#endif

    void EnsureLine()
    {
        if (line == null) line = GetComponent<LineRenderer>();
        if (line != null) line.useWorldSpace = true;
    }

    /// <summary>
    /// 宽度只由 beamWidth 决定。
    /// LineRenderer 的 Width 曲线是按整条折线的 u(0→1) 采样的，一旦曲线不是常量，
    /// 反射后折线变短，同样的缩窄被压缩进更短的距离，就会看到明显的锥形。
    /// 这里每帧强制成常量，彻底避免该问题（编辑模式也生效）。
    /// </summary>
    void ApplyBeamWidth()
    {
        if (line == null) return;
        line.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
        line.widthMultiplier = beamWidth;

        // 2D 玩法必须用 TransformZ：View 模式会在相机空间重建网格，遇到浅角的反射拐弯时
        // miter 尖角被拉长，整条带的宽度会被带偏（表现为"反射后变细"）。
        line.alignment = LineAlignment.TransformZ;
        if (line.numCornerVertices < 2) line.numCornerVertices = 4;
    }

    void Update()
    {
        EnsureLine();
        if (line == null || directionPoint == null) return;

        ApplyBeamWidth();
        FireLaser();
    }

    void FireLaser()
    {
        // 2D 玩法下整条激光必须落在同一个平面上。
        // 射线结果（RaycastHit2D.point、Vector2 运算）都是二维的，隐式转成 Vector3 时
        // z 会变成 0；如果起点用 transform.position（带自身 z），线段就会在 Z 轴上斜掉。
        float beamZ = transform.position.z;

        line.positionCount = 1;
        line.SetPosition(0, new Vector3(transform.position.x, transform.position.y, beamZ));

        Vector2 origin = new Vector2(transform.position.x, transform.position.y);
        Vector2 direction = directionPoint.up;

        int mask = mirrorLayer | receiverLayer | wallLayer;
        int reflectCount = 0;
        LaserReceiver newHitReceiver = null;

        while (true)
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, maxDistance, mask);

            // 没打中任何东西：画到最远距离，结束
            if (hit.collider == null)
            {
                Vector2 end = origin + direction * maxDistance;
                AddPoint(new Vector3(end.x, end.y, beamZ));
                break;
            }

            AddPoint(new Vector3(hit.point.x, hit.point.y, beamZ));

            int layerBit = 1 << hit.collider.gameObject.layer;

            // 接收器：记录并终止
            if ((layerBit & receiverLayer) != 0)
            {
                newHitReceiver = hit.collider.GetComponent<LaserReceiver>();
                break;
            }

            // 墙：挡住激光，不反射
            if ((layerBit & wallLayer) != 0)
            {
                break;
            }

            // 镜子：反射次数用尽就停在镜子表面
            if (reflectCount >= maxReflections)
            {
                break;
            }

            // 沿法线偏移一点点，防止下一次射线起点卡在碰撞体内部
            origin = hit.point + hit.normal * 0.01f;
            direction = Vector2.Reflect(direction, hit.normal);
            reflectCount++;
        }

        // 每帧都通知被照射的接收器，让它自己累积/衰减充能。
        // 接收器靠"最后一次被照射的时间"判断激光是否还照着自己，所以不需要 enter/exit 事件。
        // 编辑模式下不通知，避免在编辑场景时误触发充能与开门。
        if (Application.isPlaying && newHitReceiver != null)
            newHitReceiver.OnLaserHit();
    }

    void AddPoint(Vector3 position)
    {
        line.positionCount++;
        line.SetPosition(line.positionCount - 1, position);
    }
}