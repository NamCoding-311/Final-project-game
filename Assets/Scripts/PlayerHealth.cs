using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Quản lý lượng máu, nhận sát thương, hồi máu và xử lý khi người chơi chết
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    // Lượng máu tối đa của xe/người chơi
    [SerializeField] private int maxHP = 100;

    [Header("UI References")]
    // Thanh máu Slider (Tùy chọn)
    [SerializeField] private Slider hpBar;

    // Tham chiếu đến bộ theo dõi quãng đường để hiện kết quả khi Game Over
    [SerializeField] private DistanceTrackerUI distanceTracker;

    // Panel Game Over hiển thị khi chết
    [SerializeField] private GameObject gameOverPanel;

    private int _currentHP;
    private bool _isDead;

    private void Start()
    {
        _currentHP = maxHP;
        UpdateHPBar();

        if (distanceTracker == null)
        {
            distanceTracker = FindAnyObjectByType<DistanceTrackerUI>();
        }
    }

    // Nhận sát thương khi bị Zombie cắn hoặc đâm phải chướng ngại vật
    public void TakeDamage(int amount)
    {
        if (_isDead) return;

        _currentHP -= amount;
        _currentHP = Mathf.Clamp(_currentHP, 0, maxHP);
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

    // Cập nhật giá trị hiển thị trên thanh máu UI
    private void UpdateHPBar()
    {
        if (hpBar != null)
        {
            hpBar.value = (float)_currentHP / maxHP;
        }
    }

    // Xử lý khi xe phát nổ hoặc người chơi hết máu
    private void Die()
    {
        _isDead = true;

        // Hiển thị kết quả điểm số và Kỷ lục quãng đường
        if (distanceTracker != null)
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