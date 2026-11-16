using System.Collections;
using UnityEngine;

public enum ObstacleWeight
{
    Light,  // Rào nhựa, biển báo, rác, pallet gỗ (-2 km/h, không rung lắc, dễ húc văng)
    Medium, // Xe máy cháy, thùng phuy, bao cát, hòm hàng (-3.5 km/h, xe chao đảo nhẹ)
    Heavy   // Dải bê tông, đống đổ nát, xác xe hơi, xe tải, cột điện (-5 km/h, rung camera mạnh)
}

/// <summary>
/// Quản lý chướng ngại vật trên đường.
/// - Phân loại Light / Medium / Heavy với độ giảm tốc và hiệu ứng khác nhau.
/// - Tông phải vật cản Nặng (Heavy) gây rung lắc màn hình (Camera Shake).
/// - Xe đang Dash (Shift) sẽ húc nát bay vật cản mà không bị giảm tốc.
/// - Có hiệu ứng văng lộn và mờ dần (Fling & Fade) khi bị tông vỡ thay vì biến mất đột ngột.
/// </summary>
public class RoadObstacle : MonoBehaviour
{
    [Header("Obstacle Weight & Penalty")]
    [SerializeField] private ObstacleWeight obstacleWeight = ObstacleWeight.Medium;
    [SerializeField] private float customSpeedPenalty = 0f; // Nếu > 0 sẽ ghi đè theo giá trị này
    [SerializeField] private float minSpeedAfterHit = 2.5f;

    [Header("Heavy Obstacle Camera Shake")]
    [SerializeField] private bool enableCameraShake = true;
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeMagnitude = 0.28f;

    [Header("Hit Visual Effect")]
    [SerializeField] private bool useFlingAnimation = true;

    [Header("Despawn")]
    [SerializeField] private float despawnDistanceBehind = 35f;

    private Transform _playerTransform;
    private bool _hasTriggered = false;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
        }
    }

    private void Update()
    {
        if (_hasTriggered) return;

        // Tự hủy khi xe đã đi qua và bỏ xa phía sau
        if (_playerTransform != null)
        {
            if (_playerTransform.position.x - transform.position.x > despawnDistanceBehind)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasTriggered) return;

        Player3LaneMovement movement = other.GetComponent<Player3LaneMovement>();
        if (movement == null) movement = other.GetComponentInParent<Player3LaneMovement>();

        if (movement != null || other.CompareTag("Player"))
        {
            _hasTriggered = true;

            // Tắt collider ngay lập tức để tránh va chạm kép
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            // 1. Nếu Player đang bấm SHIFT (Dash / Ram): Húc bay vật cản, KHÔNG giảm tốc độ
            if (movement != null && movement.IsDashing())
            {
                // Rung nhẹ cực nhanh báo hiệu cú húc uy lực
                CameraFollow cf = Object.FindAnyObjectByType<CameraFollow>();
                if (cf != null) cf.TriggerShake(0.12f, 0.15f);

                HandleHitEffect(new Vector2(10f, Random.Range(3f, 7f)));
                return;
            }

            // 2. Nếu va chạm bình thường: Giảm tốc theo cấp độ nặng/nhẹ
            float penalty = GetSpeedPenalty();
            if (movement != null)
            {
                float currentSpeed = movement.GetCurrentSpeed();
                movement.SetSpeed(Mathf.Max(minSpeedAfterHit, currentSpeed - penalty));
            }

            // Rung lắc camera cho vật cản Nặng (Heavy)
            if (enableCameraShake && obstacleWeight == ObstacleWeight.Heavy)
            {
                CameraFollow cf = Object.FindAnyObjectByType<CameraFollow>();
                if (cf != null) cf.TriggerShake(shakeDuration, shakeMagnitude);
            }

            // Hiệu ứng vật thể bị đâm văng
            Vector2 launchDir = new Vector2(Random.Range(2f, 6f), Random.Range(2f, 6f));
            HandleHitEffect(launchDir);
        }
    }

    public float GetSpeedPenalty()
    {
        if (customSpeedPenalty > 0.05f) return customSpeedPenalty;
        switch (obstacleWeight)
        {
            case ObstacleWeight.Light: return 2.0f;
            case ObstacleWeight.Heavy: return 5.0f;
            case ObstacleWeight.Medium:
            default: return 3.5f;
        }
    }

    public void SetWeight(ObstacleWeight weight)
    {
        obstacleWeight = weight;
    }

    private void HandleHitEffect(Vector2 launchDir)
    {
        if (useFlingAnimation && gameObject.activeInHierarchy)
        {
            StartCoroutine(FlingAndFadeRoutine(launchDir));
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator FlingAndFadeRoutine(Vector2 launchDir)
    {
        float duration = 0.35f;
        float elapsed = 0f;
        Vector3 vel = new Vector3(launchDir.x, launchDir.y, 0f);
        float rotSpeed = Random.Range(-450f, 450f);
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position += vel * Time.deltaTime;
            transform.Rotate(0f, 0f, rotSpeed * Time.deltaTime);
            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(1f, 0f, elapsed / duration);
                sr.color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}

