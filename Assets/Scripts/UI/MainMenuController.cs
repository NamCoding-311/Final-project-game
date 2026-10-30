
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject garagePanel;
    public GameObject settingsPanel;

    // CONTINUE
    public void ContinueGame()
    {
        Time.timeScale = 1f;

        string savedScene = PlayerPrefs.GetString("LastLevel", "map1");

        if (!Application.CanStreamedLevelBeLoaded(savedScene))
            savedScene = "map1";

        SceneManager.LoadScene(savedScene);
    }

    // NEW GAME
    public void NewGame()
    {
        Time.timeScale = 1f;
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

    // MISSION
    public void OpenMission()
    {
        Time.timeScale = 1f;

        if (Application.CanStreamedLevelBeLoaded("Arena"))
            SceneManager.LoadScene("Arena");
        else
            Debug.LogError("Arena scene not found in Build Profiles!");
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
