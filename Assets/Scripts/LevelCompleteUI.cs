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

    // Chuyển sang Map 2 (Bến Tre)
    public void ContinueToMap2()
    {
        Time.timeScale = 1f;
        PlayerPrefs.SetString("LastLevel", "map2");
        PlayerPrefs.Save();
        SceneManager.LoadScene("map2");
    }

    // Chuyển sang Map 3
    public void ContinueToMap3()
    {
        Time.timeScale = 1f;
        PlayerPrefs.SetString("LastLevel", "map3");
        PlayerPrefs.Save();
        SceneManager.LoadScene("map3");
    }

    // Chơi lại Map 1
    public void ReplayMap1()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("map1");
    }

    // Chơi lại Map 2
    public void ReplayMap2()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("map2");
    }

    // Chơi lại Map 3
    public void ReplayMap3()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("map3");
    }

    // Về Menu chính
    public void ReturnToLobby()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded("Main menu"))
            SceneManager.LoadScene("Main menu");
        else if (Application.CanStreamedLevelBeLoaded("Misson lobby"))
            SceneManager.LoadScene("Misson lobby");
        else
            SceneManager.LoadScene(0);
    }
}
