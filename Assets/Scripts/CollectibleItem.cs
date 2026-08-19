using UnityEngine;

// Loại vật phẩm có thể nhặt trên đường
public enum CollectibleType
{
    NitroFuel,    // Tăng tốc độ xe tạm thời
    HealthPack,   // Hồi máu cho xe/người chơi
    Coin          // Tiền vàng tích lũy
}

// Quản lý các vật phẩm nhặt được trên 3 làn đường (Bình xăng, Hộp cứu thương, Tiền)
public class CollectibleItem : MonoBehaviour
{
    [Header("Item Type")]
    // Chọn loại vật phẩm
    [SerializeField] private CollectibleType itemType = CollectibleType.NitroFuel;

    [Header("Values")]
    // Giá trị hồi phục hoặc tăng thêm (Lượng máu hồi / Số tiền nhận)
    [SerializeField] private int value = 20;

    // Tốc độ được tăng khi ăn Nitro
    [SerializeField] private float nitroBoostSpeed = 16f;

    [Header("Cleanup")]
    // Khoảng cách phía sau Player để tự hủy vật phẩm
    [SerializeField] private float despawnDistanceBehind = 30f;

    private Transform _playerTransform;

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
        // Tự hủy khi xe chạy qua bỏ lại phía sau
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
        if (other.CompareTag("Player"))
        {
            ApplyEffect(other.gameObject);
            Destroy(gameObject);
        }
    }

    // Kích hoạt tác dụng của vật phẩm lên xe
    private void ApplyEffect(GameObject player)
    {
        switch (itemType)
        {
            case CollectibleType.NitroFuel:
                Player3LaneMovement movement = player.GetComponent<Player3LaneMovement>();
                if (movement != null)
                {
                    movement.SetSpeed(nitroBoostSpeed);
                }
                break;

            case CollectibleType.HealthPack:
                PlayerHealth health = player.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    // Trừ số âm để hồi máu (hoặc bổ sung hàm Heal)
                    health.TakeDamage(-value);
                }
                break;

            case CollectibleType.Coin:
                // Tích lũy tiền vào PlayerPrefs hoặc GameManager
                int currentCoins = PlayerPrefs.GetInt("TotalCoins", 0);
                PlayerPrefs.SetInt("TotalCoins", currentCoins + value);
                PlayerPrefs.Save();
                break;
        }
    }
}
