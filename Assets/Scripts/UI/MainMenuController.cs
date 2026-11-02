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
        if (garagePanel != null)
            garagePanel.SetActive(true);
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
