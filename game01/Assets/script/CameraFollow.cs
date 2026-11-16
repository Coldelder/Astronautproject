using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Horizontal-only camera follow.
/// - Only position.x is changed every frame; y and z keep the Start values.
/// - Horizontal dead zone centered on the camera. Half width =
///   (orthographicSize * aspect) * deadZoneRatio. The camera stays still while
///   the target stays inside the dead zone and keeps the target on the edge
///   once the target leaves it.
/// - Movement is smoothed with Mathf.SmoothDamp and is hard-clamped to the
///   Tilemap bounds read at runtime.
/// </summary>
[DisallowMultipleComponent]
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Transform to follow. If empty, the object tagged Player is used.")]
    public Transform target;

    [Header("Dead Zone")]
    [Tooltip("Dead-zone half width as a fraction of the visible world half width. 2/3 = 0.6667.")]
    [Range(0f, 1f)]
    public float deadZoneRatio = 2f / 3f;

    [Header("Smoothing")]
    [Tooltip("SmoothDamp time in seconds. Smaller is snappier, larger is smoother.")]
    [Min(0f)]
    public float smoothTime = 0.2f;

    [Header("Map Bounds (auto from Tilemap)")]
    [Tooltip("Tilemap that defines the map edges. If empty, the union of every Tilemap in the scene is used.")]
    public Tilemap boundsTilemap;

    [Tooltip("When enabled, manualMapLeft/Right are used instead of the Tilemap bounds.")]
    public bool useManualBounds = false;

    [Tooltip("Manual left edge of the map in world units (half-width correction still applies).")]
    public float manualMapLeft = 0f;

    [Tooltip("Manual right edge of the map in world units (half-width correction still applies).")]
    public float manualMapRight = 0f;

    Camera cam;
    float initialY;
    float initialZ;
    float mapLeft;
    float mapRight;
    bool boundsReady;
    float smoothVelocity;

    void Start()
    {
        cam = GetComponent<Camera>();
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
        if (target == null)
        {
            // Try to recover if the player spawns after the camera.
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
            else return;
        }

        if (!boundsReady) CalculateMapBounds();

        float worldHalfWidth = cam != null ? cam.orthographicSize * cam.aspect : 0f;
        float deadHalfWidth = worldHalfWidth * deadZoneRatio;

        // Resolve camera X limits from the map edges and the current half width.
        float minX = mapLeft + worldHalfWidth;
        float maxX = mapRight - worldHalfWidth;
        if (minX > maxX)
        {
            // Map is narrower than one screen: center the camera on the map.
            float mid = (mapLeft + mapRight) * 0.5f;
            minX = maxX = mid;
        }

        // Dead-zone follow.
        float offset = target.position.x - transform.position.x;
        float desiredX = transform.position.x;
        if (offset > deadHalfWidth)
            desiredX = target.position.x - deadHalfWidth;
        else if (offset < -deadHalfWidth)
            desiredX = target.position.x + deadHalfWidth;

        desiredX = Mathf.Clamp(desiredX, minX, maxX);

        float newX = smoothTime > 0f
            ? Mathf.SmoothDamp(transform.position.x, desiredX, ref smoothVelocity, smoothTime)
            : desiredX;

        // Hard guarantee: never leave the map.
        newX = Mathf.Clamp(newX, minX, maxX);

        transform.position = new Vector3(newX, initialY, initialZ);
    }

    void CalculateMapBounds()
    {
        if (useManualBounds)
        {
            mapLeft = manualMapLeft;
            mapRight = manualMapRight;
            boundsReady = true;
            return;
        }

        if (boundsTilemap != null)
        {
            GetWorldBounds(boundsTilemap, out mapLeft, out mapRight);
            boundsReady = true;
            return;
        }

        // Auto: union of every Tilemap in the scene.
        Tilemap[] all = FindObjectsOfType<Tilemap>();
        bool any = false;
        float left = float.MaxValue;
        float right = float.MinValue;
        foreach (Tilemap tm in all)
        {
            GetWorldBounds(tm, out float l, out float r);
            if (!any || l < left) left = l;
            if (!any || r > right) right = r;
            any = true;
        }

        if (any)
        {
            mapLeft = left;
            mapRight = right;
            boundsReady = true;
        }
    }

    // World-space horizontal edges of the tiles actually drawn on a Tilemap.
    // CompressBounds() + cellBounds, converted with CellToWorld (which applies
    // the Tilemap transform, cell layout and tileAnchor).
    static void GetWorldBounds(Tilemap tm, out float left, out float right)
    {
        tm.CompressBounds();
        BoundsInt cell = tm.cellBounds;
        Vector3 worldMin = tm.CellToWorld(cell.min);
        Vector3 worldMax = tm.CellToWorld(cell.max);
        left = worldMin.x;
        right = worldMax.x;
    }

    void LogBounds()
    {
        float worldHalfWidth = cam != null ? cam.orthographicSize * cam.aspect : 0f;
        float minX = mapLeft + worldHalfWidth;
        float maxX = mapRight - worldHalfWidth;
        if (minX > maxX)
        {
            float mid = (mapLeft + mapRight) * 0.5f;
            minX = maxX = mid;
        }
        Debug.Log(
            $"[CameraFollow] mapLeft={mapLeft:F3} mapRight={mapRight:F3} " +
            $"worldHalfWidth={worldHalfWidth:F3} deadZoneRatio={deadZoneRatio:F3} " +
            $"cameraMinX={minX:F3} cameraMaxX={maxX:F3} " +
            $"y={initialY:F3} z={initialZ:F3} target={(target != null ? target.name : "<none>")}");
    }
}
