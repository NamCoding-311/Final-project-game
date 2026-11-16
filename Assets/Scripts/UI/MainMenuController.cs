using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject garagePanel;
    public GameObject settingsPanel;

    [Header("Save Status Indicator")]
    [SerializeField] private TMPro.TextMeshProUGUI saveStatusText;

    private void Start()
    {
        UpdateSaveStatusUI();
    }

    private void UpdateSaveStatusUI()
    {
        if (saveStatusText != null)
        {
            saveStatusText.text = GameSaveManager.GetSaveSummaryText();
        }
    }

    // CONTINUE: Tiếp tục màn chơi và chặng đã lưu gần nhất
    public void ContinueGame()
    {
        Time.timeScale = 1f;

        GameSaveManager.SetResumeFlag(true);
        string savedScene = GameSaveManager.GetSavedScene();

        Debug.Log($"[MainMenu] Continuing game from: {savedScene} (Stage: {GameSaveManager.GetSavedStage(savedScene)})");
        SceneManager.LoadScene(savedScene);
    }

    // NEW GAME: Chơi mới từ Map 1 và xóa sạch tiến trình cũ
    public void NewGame()
    {
        Time.timeScale = 1f;
        GameSaveManager.ClearSaveData();
        GameSaveManager.SetResumeFlag(false);
        Debug.Log("[MainMenu] Starting New Game from Map 1.");
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
