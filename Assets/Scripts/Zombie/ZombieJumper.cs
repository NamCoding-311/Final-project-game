using System.Collections;
using UnityEngine;
// Zombie Jumper: Biết nhún người và phóng vồ bay qua không trung đâm vào xe
public class ZombieJumper : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHP = 40;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private int jumpDamage = 20;
    [Header("Jump Attack")]
    [SerializeField] private float jumpTriggerDistance = 8f; // Khoảng cách kích hoạt cú nhảy
    [SerializeField] private float jumpWindupTime = 0.35f;    // Thời gian khựng lại nhún đà
    [SerializeField] private float jumpFlightDuration = 0.6f; // Thời gian bay trên không
    [SerializeField] private float jumpArcHeight = 2.0f;     // Độ cao nhảy vọt lên không
    private Transform _playerTransform;
    private PlayerHealth _playerHealth;
    private SpriteRenderer _spriteRenderer;
    private bool _hasJumped = false;
    private bool _isJumping = false;
    [Header("Road Boundaries (Khóa trên mặt đường)")]
    [SerializeField] private bool clampToRoad = true;
    [SerializeField] private float minRoadY = -3.45f;
    [SerializeField] private float maxRoadY = 3.0f;

    private int _currentHP;
    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }
    private void Start()
    {
        _currentHP = maxHP;

        // Tự động đồng bộ ranh giới đường từ ZombieSpawner
        ZombieSpawner spawner = FindAnyObjectByType<ZombieSpawner>();
        if (spawner != null)
        {
            minRoadY = spawner.GetRoadMinY();
            maxRoadY = spawner.GetRoadMaxY();
        }

        // Ưu tiên tìm người đi bộ OnFootPlayer trước
        OnFootPlayerController onFoot = FindAnyObjectByType<OnFootPlayerController>();
        if (onFoot != null && onFoot.gameObject.activeInHierarchy)
        {
            _playerTransform = onFoot.transform;
            _playerHealth = onFoot.GetComponent<PlayerHealth>();
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _playerTransform = player.transform;
            _playerHealth = player.GetComponent<PlayerHealth>();
        }
    }

    public void SetTarget(Transform target)
    {
        _playerTransform = target;
        if (target != null)
        {
            _playerHealth = target.GetComponent<PlayerHealth>()
                ?? target.GetComponentInParent<PlayerHealth>()
                ?? target.GetComponentInChildren<PlayerHealth>();
        }
    }
    private void Update()
    {
        if (_playerTransform == null || _isJumping) return;

        // Tự hủy nếu xe đã vượt qua nó quá xa (25m) để tránh tốn bộ nhớ
        if (_playerTransform.position.x - transform.position.x > 25f)
        {
            Destroy(gameObject);
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, _playerTransform.position);

        // Tự động lật mặt theo hướng người chơi
        if (_spriteRenderer != null)
        {
            _spriteRenderer.flipX = _playerTransform.position.x < transform.position.x;
        }

        // Nếu chưa nhảy và xe vào tầm nhảy (<= jumpTriggerDistance) -> Kích hoạt nhảy vồ!
        if (!_hasJumped && distanceToPlayer <= jumpTriggerDistance)
        {
            StartCoroutine(PerformLeapAttackRoutine());
            return;
        }

        // Di chuyển tiếp cận người chơi (cả trước và sau khi nhảy)
        Vector3 dir = (_playerTransform.position - transform.position).normalized;
        Vector3 nextPos = transform.position + dir * moveSpeed * Time.deltaTime;
        if (clampToRoad)
        {
            nextPos.y = Mathf.Clamp(nextPos.y, minRoadY, maxRoadY);
        }
        transform.position = nextPos;
    }
    // Chuỗi nhảy vồ hình Parabol
    private IEnumerator PerformLeapAttackRoutine()
    {
        _hasJumped = true;
        _isJumping = true;
        // 1. Nhún người lấy đà (nhấp nháy đổi màu cảnh báo)
        if (_spriteRenderer != null) _spriteRenderer.color = Color.yellow;
        yield return new WaitForSeconds(jumpWindupTime);
        if (_spriteRenderer != null) _spriteRenderer.color = Color.white;
        // 2. Điểm xuất phát và điểm đích (vị trí xe tại thời điểm nhảy)
        Vector2 startPos = transform.position;
        Vector2 targetPos = _playerTransform.position;
        if (clampToRoad)
        {
            targetPos.y = Mathf.Clamp(targetPos.y, minRoadY, maxRoadY);
        }
        // 3. Bay trên không trung theo đường cong Parabol
        float elapsed = 0f;
        while (elapsed < jumpFlightDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / jumpFlightDuration);
            // Tọa độ Lerp + vòng cung Parabol
            Vector2 currentPos = Vector2.Lerp(startPos, targetPos, t);
            currentPos.y += 4f * jumpArcHeight * t * (1f - t);
            transform.position = currentPos;
            yield return null;
        }
        transform.position = targetPos;
        _isJumping = false;
    }
    // Tăng chỉ số theo Wave
    public void ScaleStats(float hpMultiplier, float damageMultiplier, float speedMultiplier)
    {
        maxHP = Mathf.RoundToInt(maxHP * hpMultiplier);
        _currentHP = maxHP;
        jumpDamage = Mathf.RoundToInt(jumpDamage * damageMultiplier);
        moveSpeed *= speedMultiplier;
    }

    // Nhận sát thương khi bị bắn
    public void TakeDamage(int damage)
    {
        _currentHP -= damage;
        if (_currentHP <= 0)
        {
            Zombie.NotifyZombieDeath();

            DistanceTrackerUI tracker = FindAnyObjectByType<DistanceTrackerUI>();
            if (tracker != null)
            {
                tracker.AddZombieKill();
            }

            if (ArenaManager.Instance != null)
            {
                ArenaManager.Instance.AddKill();
            }

            if (ArenaManager.Instance != null || BrotatoLevelSystem.Instance != null)
            {
                GameObject gem = new GameObject("MaterialGem");
                gem.transform.position = transform.position;
                MaterialGem mg = gem.AddComponent<MaterialGem>();
                mg.SetGemData(2, new Color(0.2f, 0.8f, 1f), 0.9f);
                ArenaPowerUp.TryDropRandom(transform.position, 8f);
            }
            Destroy(gameObject);
        }
    }
    // Va chạm với xe
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (_playerHealth != null)
            {
                _playerHealth.TakeDamage(jumpDamage);
            }
            Destroy(gameObject); // Đâm vào xe tự hủy hoặc nổ
        }
    }
}