using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Quản lý hiển thị quãng đường chạy được (Distance), điểm cao nhất (Highscore), và giao diện khi kết thúc game
public class DistanceTrackerUI : MonoBehaviour
{
    [Header("Target")]
    // Transform của Player/Xe
    [SerializeField] private Transform playerTransform;

    [Header("In-Game HUD Elements")]
    // Text hiển thị quãng đường hiện tại (dùng TextMeshPro hoặc Text UI)
    [SerializeField] private TextMeshProUGUI distanceText;

    // Text hiển thị tốc độ hiện tại (Tùy chọn)
    [SerializeField] private TextMeshProUGUI speedText;

    // Text hiển thị số Zombie đã tiêu diệt (Tùy chọn)
    [SerializeField] private TextMeshProUGUI killCountText;

    [Header("Game Over Screen Elements")]
    // Panel Game Over
    [SerializeField] private GameObject gameOverPanel;

    // Text hiển thị quãng đường cuối cùng trong bảng Game Over
    [SerializeField] private TextMeshProUGUI finalDistanceText;

    // Text hiển thị kỷ lục chạy xa nhất
    [SerializeField] private TextMeshProUGUI bestDistanceText;

    // Vị trí X lúc bắt đầu xuất phát
    private float _startX;

    // Quãng đường hiện tại tính theo mét
    private float _currentDistance;

    // Số zombie đã hạ gục
    private int _zombieKillCount;

    // Tham chiếu đến bộ điều khiển di chuyển để đọc tốc độ
    private Player3LaneMovement _playerMovement;

    private void Start()
    {
        // Tự động tìm Player nếu chưa được gán
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        if (playerTransform != null)
        {
            _startX = playerTransform.position.x;
            _playerMovement = playerTransform.GetComponent<Player3LaneMovement>();
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // Tính quãng đường chạy được dựa trên độ dịch chuyển trục X
        _currentDistance = Mathf.Max(0f, playerTransform.position.x - _startX);

        // Cập nhật text quãng đường (làm tròn số nguyên)
        if (distanceText != null)
        {
            distanceText.text = $"{Mathf.FloorToInt(_currentDistance)} m";
        }

        // Cập nhật text tốc độ (km/h ảo cho sinh động)
        if (speedText != null && _playerMovement != null)
        {
            float speedKmh = _playerMovement.GetCurrentSpeed() * 3.6f;
            speedText.text = $"{Mathf.FloorToInt(speedKmh)} km/h";
        }
    }

    // Tăng số đếm Zombie bị tiêu diệt
    public void AddZombieKill()
    {
        _zombieKillCount++;
        if (killCountText != null)
        {
            killCountText.text = $"Kills: {_zombieKillCount}";
        }
    }

    // Gọi hàm này khi Player chết để hiện kết quả và lưu Kỷ lục (Highscore)
    public void ShowGameOverResults()
    {
        int finalScore = Mathf.FloorToInt(_currentDistance);
        int bestScore = PlayerPrefs.GetInt("BestDistance", 0);

        // Kiểm tra và lưu kỷ lục mới
        if (finalScore > bestScore)
        {
            bestScore = finalScore;
            PlayerPrefs.SetInt("BestDistance", bestScore);
            PlayerPrefs.Save();
        }

        if (finalDistanceText != null)
        {
            finalDistanceText.text = $"Distance: {finalScore} m";
        }

        if (bestDistanceText != null)
        {
            bestDistanceText.text = $"Best: {bestScore} m";
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }
}
