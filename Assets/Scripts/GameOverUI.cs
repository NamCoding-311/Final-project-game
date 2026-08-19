using UnityEngine;
using UnityEngine.SceneManagement;

// Quản lý các nút bấm trên giao diện Game Over (Chơi lại màn, Thoát game)
public class GameOverUI : MonoBehaviour
{
    // Hàm được gắn trực tiếp vào sự kiện OnClick của Nút Restart
    public void RestartGame()
    {
        // 1. Đảm bảo khôi phục lại tốc độ thời gian trong game về 1 (tránh bị đứng hình khi load lại màn)
        Time.timeScale = 1f;

        // 2. Tải lại Scene hiện tại
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Tùy chọn: Hàm thoát game (nếu bạn có thêm nút Quit)
    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
        Debug.Log("Đã thoát game!");
    }
}