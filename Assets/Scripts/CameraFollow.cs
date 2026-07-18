using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.2f;
    [SerializeField] private GameObject mapObject; // đổi từ BoxCollider2D sang GameObject
    [SerializeField] private Camera cam;

    private BoxCollider2D _mapBoundsCollider;
    private Vector3 _velocity = Vector3.zero;
    private Vector2 _minBounds;
    private Vector2 _maxBounds;

    private void Start()
    {
        if (cam == null) cam = Camera.main;

        if (mapObject == null)
        {
            Debug.LogError("CameraFollow: chưa gán Map Object trong Inspector!", this);
            enabled = false;
            return;
        }

        // Lấy BoxCollider2D lúc runtime, sau khi PlaceholderMapGenerator đã tạo xong
        _mapBoundsCollider = mapObject.GetComponent<BoxCollider2D>();
        if (_mapBoundsCollider == null)
        {
            Debug.LogError("CameraFollow: GameObject được gán chưa có BoxCollider2D nào cả!", this);
            enabled = false;
            return;
        }

        if (target == null)
        {
            Debug.LogError("CameraFollow: chưa gán Target (Player) trong Inspector!", this);
            enabled = false;
            return;
        }

        CalculateBounds();
    }

    private void CalculateBounds()
    {
        Bounds b = _mapBoundsCollider.bounds;

        float camHalfHeight = cam.orthographicSize;
        float camHalfWidth = camHalfHeight * cam.aspect;

        _minBounds = new Vector2(b.min.x + camHalfWidth, b.min.y + camHalfHeight);
        _maxBounds = new Vector2(b.max.x - camHalfWidth, b.max.y - camHalfHeight);
    }

    private void LateUpdate()
    {
        if (target == null || _mapBoundsCollider == null) return;

        Vector3 desired = target.position;
        desired.z = transform.position.z;

        Vector3 smoothed = Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);

        smoothed.x = Mathf.Clamp(smoothed.x, _minBounds.x, _maxBounds.x);
        smoothed.y = Mathf.Clamp(smoothed.y, _minBounds.y, _maxBounds.y);

        transform.position = smoothed;
    }
}