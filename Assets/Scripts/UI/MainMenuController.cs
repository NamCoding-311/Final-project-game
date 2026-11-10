using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject garagePanel;
    public GameObject settingsPanel;

    // CONTINUE: Tiếp tục màn chơi đã lưu gần nhất
    public void ContinueGame()
    {
        Time.timeScale = 1f;

        string savedScene = PlayerPrefs.GetString("LastLevel", "map1");

        if (!Application.CanStreamedLevelBeLoaded(savedScene))
            savedScene = "map1";

        SceneManager.LoadScene(savedScene);
    }

    // NEW GAME: Chơi mới từ Map 1 và reset tiến trình
    public void NewGame()
    {
        Time.timeScale = 1f;
        PlayerPrefs.SetString("LastLevel", "map1");
        PlayerPrefs.Save();
        SceneManager.LoadScene("map1");
    }

    // GARAGE
    public void OpenGarage()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded("garage"))
        {
            SceneManager.LoadScene("garage");
            return;
        }

        Debug.LogError("Garage scene is not included in the active Build Settings.", this);
    }

    public void CloseGarage()
    {
        if (garagePanel != null)
            garagePanel.SetActive(false);
    }

    // MISSION: Mở sảnh nhiệm vụ (Misson lobby) hoặc Đấu trường sinh tồn (ArenaSurvival)
    public void OpenMission()
    {
        Time.timeScale = 1f;

        if (Application.CanStreamedLevelBeLoaded("Misson lobby"))
            SceneManager.LoadScene("Misson lobby");
        else if (Application.CanStreamedLevelBeLoaded("ArenaSurvival"))
            SceneManager.LoadScene("ArenaSurvival");
        else
            Debug.LogError("Mission scene not found in Build Profiles!");
    }

    // ARENA SURVIVAL: Mở chế độ Đấu trường sinh tồn
    public void OpenArenaSurvival()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded("ArenaSurvival"))
            SceneManager.LoadScene("ArenaSurvival");
        else
            Debug.LogError("ArenaSurvival scene not found in active Build Settings!");
    }

    // SETTINGS
    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    // QUIT
    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}
