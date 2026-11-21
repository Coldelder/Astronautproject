using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class CameraFollowPlus : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Horizontal Dead Zone")]
    [Range(0f, 1f)]
    public float deadZoneRatio = 0.4f;
    [Min(0f)]
    public float smoothTime = 0.2f;

    [Header("Vertical Follow")]
    public bool verticalFollow = true;
    [Range(0f, 1f)]
    public float deadZoneRatioY = 0.3f;
    [Min(0f)]
    public float smoothTimeY = 0.2f;

    [Header("Map Bounds (auto from Tilemap)")]
    public Tilemap boundsTilemap;
    public bool useManualBounds = false;
    public float manualMapLeft = 0f;
    public float manualMapRight = 100f;
    public float manualMapBottom = 0f;
    public float manualMapTop = 20f;

    [Header("Debug")]
    public bool drawDeadZoneGizmos = true;

    Camera cam;
    float initialY;
    float initialZ;

    float mapLeft;
    float mapRight;
    float mapBottom;
    float mapTop;
    bool boundsReady;

    float smoothVelocity;
    float smoothVelocityY;

    float targetVerticalAnchor = float.NaN;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("[CameraFollowPlus] 物体上没有 Camera 组件！", this);
            enabled = false;
            return;
        }
        if (!cam.orthographic)
        {
            Debug.LogWarning("[CameraFollowPlus] 相机不是正交相机！这个脚本只适合 2D Orthographic", this);
        }

        Vector3 initialPosition = transform.position;
        initialY = initialPosition.y;
        initialZ = initialPosition.z;

        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
        }

        CalculateMapBounds();
        LogBounds();
    }

    void LateUpdate()
    {
        if (cam == null) return;

        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
            else return;
        }

        if (!boundsReady) CalculateMapBounds();

        if (verticalFollow && float.IsNaN(targetVerticalAnchor))
        {
            RecordTargetVerticalAnchor();
        }

        float worldHalfWidth = cam.orthographicSize * cam.aspect;
        float worldHalfHeight = cam.orthographicSize;

        // ============== 水平跟随 ==============
        float deadHalfWidth = worldHalfWidth * deadZoneRatio;

        float minX = mapLeft + worldHalfWidth;
        float maxX = mapRight - worldHalfWidth;
        if (minX > maxX)
        {
            float mid = (mapLeft + mapRight) * 0.5f;
            minX = maxX = mid;
        }

        float offsetX = target.position.x - transform.position.x;
        float desiredX = transform.position.x;

        if (offsetX > deadHalfWidth)
            desiredX = target.position.x - deadHalfWidth;
        else if (offsetX < -deadHalfWidth)
            desiredX = target.position.x + deadHalfWidth;

        desiredX = Mathf.Clamp(desiredX, minX, maxX);

        float newX = smoothTime > 0f
            ? Mathf.SmoothDamp(transform.position.x, desiredX, ref smoothVelocity, smoothTime)
            : desiredX;
        newX = Mathf.Clamp(newX, minX, maxX);

        // ============== 上下跟随 ==============
        float newY = initialY;
        if (verticalFollow && !float.IsNaN(targetVerticalAnchor))
        {
            float deadHalfHeight = worldHalfHeight * deadZoneRatioY;

            float minY = mapBottom + worldHalfHeight;
            float maxY = mapTop - worldHalfHeight;
            if (minY > maxY)
            {
                float mid = (mapBottom + mapTop) * 0.5f;
                minY = maxY = mid;
            }

            float anchorY = CalculateAnchorCameraY(targetVerticalAnchor);
            float offsetY = target.position.y - transform.position.y;
            float desiredY = transform.position.y;

            if (offsetY > deadHalfHeight)
            {
                desiredY = target.position.y - deadHalfHeight;
            }
            else if (offsetY < -deadHalfHeight)
            {
                desiredY = target.position.y + deadHalfHeight;
            }
            else
            {
                desiredY = anchorY;
            }

            desiredY = Mathf.Clamp(desiredY, minY, maxY);
            newY = smoothTimeY > 0f
                ? Mathf.SmoothDamp(transform.position.y, desiredY, ref smoothVelocityY, smoothTimeY)
                : desiredY;
            newY = Mathf.Clamp(newY, minY, maxY);
        }

        transform.position = new Vector3(newX, newY, initialZ);
    }

    void RecordTargetVerticalAnchor()
    {
        if (cam == null || target == null) return;
        Vector3 targetViewport = cam.WorldToViewportPoint(target.position);
        targetVerticalAnchor = Mathf.Clamp01(targetViewport.y);
    }

    float CalculateAnchorCameraY(float anchor)
    {
        if (cam == null || target == null) return transform.position.y;
        float worldHalfHeight = cam.orthographicSize;
        float offsetFromCenter = (anchor - 0.5f) * worldHalfHeight * 2f;
        return target.position.y - offsetFromCenter;
    }

    void CalculateMapBounds()
    {
        if (useManualBounds)
        {
            mapLeft = manualMapLeft;
            mapRight = manualMapRight;
            mapBottom = manualMapBottom;
            mapTop = manualMapTop;
            boundsReady = true;
            return;
        }

        if (boundsTilemap != null)
        {
            GetWorldBounds(boundsTilemap, out mapLeft, out mapRight, out mapBottom, out mapTop);
            boundsReady = true;
            return;
        }

        Tilemap[] all = FindObjectsOfType<Tilemap>();
        bool any = false;
        float left = float.MaxValue;
        float right = float.MinValue;
        float bottom = float.MaxValue;
        float top = float.MinValue;

        foreach (Tilemap tm in all)
        {
            GetWorldBounds(tm, out float l, out float r, out float b, out float t);
            if (!any || l < left) left = l;
            if (!any || r > right) right = r;
            if (!any || b < bottom) bottom = b;
            if (!any || t > top) top = t;
            any = true;
        }

        if (any)
        {
            mapLeft = left;
            mapRight = right;
            mapBottom = bottom;
            mapTop = top;
            boundsReady = true;
        }
        else
        {
            Debug.LogWarning("[CameraFollowPlus] 场景内找不到任何 Tilemap！请开启 useManualBounds 手动填边界", this);
        }
    }

    static void GetWorldBounds(Tilemap tm, out float left, out float right, out float bottom, out float top)
    {
        tm.CompressBounds();
        BoundsInt cell = tm.cellBounds;
        Vector3 worldMin = tm.CellToWorld(cell.min);
        Vector3 worldMax = tm.CellToWorld(cell.max);
        left = worldMin.x;
        right = worldMax.x;
        bottom = worldMin.y;
        top = worldMax.y;
    }

    void LogBounds()
    {
        if (cam == null) return;
        float worldHalfWidth = cam.orthographicSize * cam.aspect;
        float worldHalfHeight = cam.orthographicSize;

        float minX = mapLeft + worldHalfWidth;
        float maxX = mapRight - worldHalfWidth;
        float minY = mapBottom + worldHalfHeight;
        float maxY = mapTop - worldHalfHeight;

        if (minX > maxX) { float mid = (mapLeft + mapRight) * 0.5f; minX = maxX = mid; }
        if (minY > maxY) { float mid = (mapBottom + mapTop) * 0.5f; minY = maxY = mid; }

        Debug.Log(
            $"[CameraFollowPlus] map=({mapLeft:F3},{mapBottom:F3})~({mapRight:F3},{mapTop:F3}) " +
            $"cameraMinX={minX:F3} cameraMaxX={maxY:F3} " +
            $"cameraMinY={minY:F3} cameraMaxY={maxY:F3} " +
            $"anchorY={targetVerticalAnchor:F3}");
    }

    void OnDrawGizmosSelected()
    {
        if (!drawDeadZoneGizmos || cam == null) return;
        float wHalf = cam.orthographicSize * cam.aspect * deadZoneRatio;
        float hHalf = cam.orthographicSize * deadZoneRatioY;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(wHalf * 2, hHalf * 2, 0));
    }
}
