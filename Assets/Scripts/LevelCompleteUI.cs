using UnityEngine;
using UnityEngine.SceneManagement;

// Quản lý hiển thị bảng Hoàn Thành Màn Chơi (Victory) và chuyển màn
public class LevelCompleteUI : MonoBehaviour
{
    [SerializeField] private GameObject levelCompletePanel;

    private bool _hasCompleted;

    private void Start()
    {
        Time.timeScale = 1f;

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }
    }

    // Hiển thị bảng hoàn thành màn chơi
    public void ShowCompletePanel()
    {
        if (_hasCompleted) return;

        _hasCompleted = true;
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }
        Time.timeScale = 0f;
    }

    // Chuyen sang Map 2 (Ben Tre)
    public void ContinueToMap2()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("map2");
    }

    // Chuyen sang Map 3
    public void ContinueToMap3()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("map3");
    }

    // Choi lai Map 1
    public void ReplayMap1()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("map1");
    }

    // Choi lai Map 2
    public void ReplayMap2()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("map2");
    }

    // Choi lai Map 3
    public void ReplayMap3()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("map3");
    }

    // Về sảnh chính (Lobby)
    public void ReturnToLobby()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main lobby");
    }
}
