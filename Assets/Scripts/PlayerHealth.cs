using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Quản lý lượng máu, nhận sát thương, hồi máu và xử lý khi người chơi chết
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    // Lượng máu tối đa của xe/người chơi
    [SerializeField] private int maxHP = 100;

    [Header("UI References")]
    // Thanh máu Slider (Tùy chọn)
    [SerializeField] private Slider hpBar;

    // Chữ hiển thị lượng máu (Ví dụ: "100 / 100")
    [SerializeField] private TextMeshProUGUI hpText;

    // Tham chiếu đến bộ theo dõi quãng đường để hiện kết quả khi Game Over
    [SerializeField] private DistanceTrackerUI distanceTracker;

    // Panel Game Over hiển thị khi chết
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private float damageReduction = 0f;

    private int _currentHP;
    private bool _isDead;

    private void Awake()
    {
        // Luôn đảm bảo thời gian game chạy bình thường khi bắt đầu
        Time.timeScale = 1f;
    }

    private void Start()
    {
        _currentHP = maxHP;
        UpdateHPBar();

        if (distanceTracker == null)
        {
            distanceTracker = FindAnyObjectByType<DistanceTrackerUI>();
        }
    }

    public void AddDamageReduction(float percentage)
    {
        damageReduction = Mathf.Clamp(damageReduction + percentage, 0f, 0.75f);
    }

    // Nhan sat thuong khi bi Zombie can hoac dam phai chuong ngai vat
    public void TakeDamage(int amount)
    {
        if (_isDead) return;

        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(amount * (1f - damageReduction)));
        _currentHP -= finalDamage;
        _currentHP = Mathf.Clamp(_currentHP, 0, maxHP);

        Debug.Log($"[PlayerHealth] ({gameObject.name}) Took {finalDamage} damage! HP: {_currentHP}/{maxHP}");

        UpdateHPBar();

        if (_currentHP <= 0)
        {
            Die();
        }
    }

    // Hồi máu khi nhặt hộp cứu thương (Health Pack)
    public void Heal(int amount)
    {
        if (_isDead) return;

        _currentHP = Mathf.Clamp(_currentHP + amount, 0, maxHP);
        UpdateHPBar();
    }

    // Tăng lượng máu tối đa và hồi phục (Dùng cho nâng cấp Brotato)
    public void IncreaseMaxHP(int amount)
    {
        if (_isDead) return;
        maxHP += amount;
        _currentHP = Mathf.Clamp(_currentHP + amount, 0, maxHP);
        UpdateHPBar();
    }

    // Cập nhật giá trị hiển thị trên thanh máu UI
    private void UpdateHPBar()
    {
        if (hpBar != null)
        {
            hpBar.value = (float)_currentHP / maxHP;
        }

        if (hpText != null)
        {
            hpText.text = $"{_currentHP} / {maxHP}";
        }
    }

    // Xử lý khi xe phát nổ hoặc người chơi hết máu
    private void Die()
    {
        _isDead = true;

        // Hiển thị kết quả điểm số và màn hình Game Over
        if (ArenaGameOverSystem.Instance != null)
        {
            ArenaGameOverSystem.Instance.TriggerGameOver();
        }
        else if (distanceTracker != null)
        {
            distanceTracker.ShowGameOverResults();
        }
        else if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // Tạm dừng thời gian trong game
        Time.timeScale = 0f;
    }

    // Chơi lại màn hiện tại
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}