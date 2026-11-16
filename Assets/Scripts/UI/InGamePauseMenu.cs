using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class InGamePauseMenu : MonoBehaviour
{
    public static InGamePauseMenu Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private SettingsController settingsController;

    [Header("HUD Quick Access")]
    [SerializeField] private Button hudPauseButton;

    [Header("Action Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Audio Preview")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip previewClickClip;

    private bool _isPaused = false;
    public bool IsPaused => _isPaused;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        // Tự động nạp và áp dụng Master Volume ngay khi vào màn chơi
        float master = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AudioListener.volume = master;

        SetupButtons();
    }

    private void OnEnable()
    {
        SetupButtons();
    }

    private void Start()
    {
        _isPaused = false;
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        if (hudPauseButton != null)
        {
            hudPauseButton.gameObject.SetActive(true);
        }
        Time.timeScale = 1f;
    }

    private void Update()
    {
        // Nhận diện cả phím ESC và phím P để tạm dừng / mở cài đặt
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            TogglePause();
        }
        else if (_isPaused)
        {
            // Phím tắt nhanh khi đang ở menu Pause
            if (Input.GetKeyDown(KeyCode.R))
            {
                RetryLevel();
            }
        }
    }

    public void TogglePause()
    {
        if (_isPaused)
        {
            ResumeGame();
        }
        else
        {
            if (CanOpenPauseMenu())
            {
                PauseGame();
            }
        }
    }

    private bool CanOpenPauseMenu()
    {
        // Không mở menu Pause nếu game đã kết thúc (Game Over hoặc Hoàn thành)
        var dt = FindObjectOfType<DistanceTrackerUI>();
        if (dt != null && dt.IsGameOver) return false;

        var lc = FindObjectOfType<LevelCompleteUI>();
        if (lc != null && lc.HasCompleted) return false;

        var ago = ArenaGameOverSystem.Instance;
        if (ago != null && ago.IsWindowOpen) return false;

        return true;
    }

    private void SetupButtons()
    {
        if (hudPauseButton != null)
        {
            hudPauseButton.onClick.RemoveAllListeners();
            hudPauseButton.onClick.AddListener(TogglePause);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(RetryLevel);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
    }

    public void PauseGame()
    {
        _isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        if (hudPauseButton != null)
        {
            hudPauseButton.gameObject.SetActive(false);
        }

        if (settingsController != null)
        {
            settingsController.LoadAndApplySettings();
        }
    }

    public void ResumeGame()
    {
        _isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (hudPauseButton != null)
        {
            hudPauseButton.gameObject.SetActive(true);
        }

        PlayerPrefs.Save();
    }

    public void RetryLevel()
    {
        Time.timeScale = 1f;
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        // Tự động lưu lại Map và Chặng đang chơi dở
        var slm = FindAnyObjectByType<StageLevelManager>();
        int currentStage = slm != null ? slm.GetCurrentStageIndex() : 0;
        GameSaveManager.SaveCheckpoint(SceneManager.GetActiveScene().name, currentStage);

        Time.timeScale = 1f;
        PlayerPrefs.Save();
        if (Application.CanStreamedLevelBeLoaded("Main menu"))
        {
            SceneManager.LoadScene("Main menu");
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    private void OnApplicationQuit()
    {
        var slm = FindAnyObjectByType<StageLevelManager>();
        int currentStage = slm != null ? slm.GetCurrentStageIndex() : 0;
        GameSaveManager.SaveCheckpoint(SceneManager.GetActiveScene().name, currentStage);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        Time.timeScale = 1f;
    }
}
