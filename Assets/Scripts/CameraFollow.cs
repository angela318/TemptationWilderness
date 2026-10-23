using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // drag Player here in Inspector

    [Header("Follow Settings")]
    public float smoothSpeed = 5f;

    [Header("Map Bounds (match WildernessGenerator map size)")]
    public float mapWidth  = 40f;
    public float mapHeight = 40f;

    Camera cam;
    bool   snapped = false;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);

        if (!snapped)
        {
            transform.position = desired;
            snapped = true;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
        }

        // Clamp so camera never shows outside the map
        float halfW = mapWidth  / 2f;
        float halfH = mapHeight / 2f;

        float camHalfH = cam.orthographicSize;
        float camHalfW = cam.orthographicSize * cam.aspect;

        float clampedX = Mathf.Clamp(transform.position.x, -halfW + camHalfW, halfW - camHalfW);
        float clampedY = Mathf.Clamp(transform.position.y, -halfH + camHalfH, halfH - camHalfH);

        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }
}
