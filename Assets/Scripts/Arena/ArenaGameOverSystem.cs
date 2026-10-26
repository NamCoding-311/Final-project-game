using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Hệ thống Màn Hình Thắng / Thua & Tổng Kết Trận Đấu (Game Over & Victory)
// Chuẩn phong cách Brotato / Vampire Survivors:
// 1. Thất Bại (Game Over): Khi người chơi hết máu.
// 2. Chiến Thắng (Victory): Khi người chơi sống sót qua toàn bộ 10 Waves (hoặc maxWaves).
// 3. Hiển thị chi tiết bảng thống kê: Số Wave, Thời gian sinh tồn, Kills, Gems, Súng sở hữu.
// 4. Các nút bấm: Chơi lại (Restart), Về Menu chính (Main Menu), Chế độ Vô tận (Endless Mode).
// 5. Hỗ trợ 100% Canvas UI TMP và tự động có Fallback OnGUI nếu chưa gắn UI.
public class ArenaGameOverSystem : MonoBehaviour
{
    public static ArenaGameOverSystem Instance { get; private set; }

    [Header("Canvas UI References")]
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private Image accentStrip;

    [Header("Stats UI Elements")]
    [SerializeField] private TextMeshProUGUI waveStatText;
    [SerializeField] private TextMeshProUGUI timeStatText;
    [SerializeField] private TextMeshProUGUI killsStatText;
    [SerializeField] private TextMeshProUGUI gemsStatText;
    [SerializeField] private TextMeshProUGUI weaponsStatText;

    [Header("Action Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button endlessButton;

    private bool _isWindowOpen = false;
    private bool _isVictory = false;

    private int _finalWave = 1;
    private float _finalTime = 0f;
    private int _finalKills = 0;
    private int _finalGems = 0;
    private int _finalWeapons = 1;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (endGamePanel != null)
        {
            endGamePanel.SetActive(false);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (menuButton != null)
        {
            menuButton.onClick.AddListener(OnMenuClicked);
        }

        if (endlessButton != null)
        {
            endlessButton.onClick.AddListener(OnEndlessClicked);
        }
    }

    // =========================================================================
    // 1. KÍCH HOẠT THẮNG / THUA
    // =========================================================================

    public void TriggerGameOver()
    {
        if (_isWindowOpen) return;

        _isVictory = false;
        OpenEndGameWindow();
    }

    public void TriggerVictory()
    {
        if (_isWindowOpen) return;

        _isVictory = true;
        OpenEndGameWindow();
    }

    private void OpenEndGameWindow()
    {
        _isWindowOpen = true;
        Time.timeScale = 0f; // Tạm dừng game để người chơi xem kết quả

        // Thu thập thống kê trận đấu
        _finalWave = ArenaManager.Instance != null ? ArenaManager.Instance.GetCurrentWave() : 1;
        _finalTime = Time.timeSinceLevelLoad;
        _finalKills = ArenaManager.Instance != null ? ArenaManager.Instance.GetTotalKills() : 0;
        
        BrotatoLevelSystem levelSys = BrotatoLevelSystem.Instance ?? FindAnyObjectByType<BrotatoLevelSystem>();
        _finalGems = levelSys != null ? levelSys.GetLifetimeMaterials() : 0;

        ArenaAutoShooting shooting = ArenaAutoShooting.Instance ?? FindAnyObjectByType<ArenaAutoShooting>();
        _finalWeapons = shooting != null ? shooting.GetCurrentWeaponCount() : 1;

        UpdateCanvasUI();

        if (endGamePanel != null)
        {
            endGamePanel.SetActive(true);
        }
    }

    // =========================================================================
    // 2. CẬP NHẬT GIAO DIỆN CANVAS TMP
    // =========================================================================

    private void UpdateCanvasUI()
    {
        string formattedTime = FormatTime(_finalTime);

        if (_isVictory)
        {
            if (titleText != null)
            {
                titleText.text = "VICTORY!";
                titleText.color = new Color(1f, 0.85f, 0.2f); // Màu vàng hoàng kim
            }

            if (subtitleText != null)
            {
                subtitleText.text = $"YOU SURVIVED ALL {_finalWave} WAVES AND CLEARED THE ZOMBIE APOCALYPSE!";
                subtitleText.color = new Color(0.85f, 0.95f, 1f);
            }

            if (accentStrip != null)
            {
                accentStrip.color = new Color(1f, 0.85f, 0.2f);
            }

            if (endlessButton != null)
            {
                endlessButton.gameObject.SetActive(true);
            }
        }
        else
        {
            if (titleText != null)
            {
                titleText.text = "GAME OVER";
                titleText.color = new Color(0.95f, 0.25f, 0.25f); // Màu đỏ máu
            }

            if (subtitleText != null)
            {
                subtitleText.text = $"YOU FOUGHT BRAVELY BUT WERE OVERWHELMED AT WAVE {_finalWave}!";
                subtitleText.color = new Color(0.95f, 0.8f, 0.8f);
            }

            if (accentStrip != null)
            {
                accentStrip.color = new Color(0.95f, 0.25f, 0.25f);
            }

            if (endlessButton != null)
            {
                endlessButton.gameObject.SetActive(false); // Thua thì không có nút Vô Tận
            }
        }

        if (waveStatText != null) waveStatText.text = _isVictory ? $"{_finalWave} (COMPLETED)" : $"WAVE {_finalWave}";
        if (timeStatText != null) timeStatText.text = formattedTime;
        if (killsStatText != null) killsStatText.text = $"{_finalKills} KILLS";
        if (gemsStatText != null) gemsStatText.text = $"{_finalGems} GEMS";
        if (weaponsStatText != null) weaponsStatText.text = $"{_finalWeapons}/6 WEAPONS";
    }

    private string FormatTime(float seconds)
    {
        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{mins:00}:{secs:00}";
    }

    // =========================================================================
    // 3. SỰ KIỆN NÚT BẤM (BUTTON HANDLERS)
    // =========================================================================

    public void OnRestartClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnMenuClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main lobby");
    }

    public void OnEndlessClicked()
    {
        _isWindowOpen = false;
        if (endGamePanel != null)
        {
            endGamePanel.SetActive(false);
        }

        if (ArenaManager.Instance != null)
        {
            ArenaManager.Instance.SetEndlessMode(true);
        }

        // Mở Cửa Hàng để người chơi chuẩn bị trang bị bước vào Wave 11
        ArenaShopSystem shop = ArenaShopSystem.Instance ?? FindAnyObjectByType<ArenaShopSystem>();
        if (shop != null)
        {
            shop.OpenShop(_finalWave);
        }
        else
        {
            Time.timeScale = 1f;
            if (ArenaManager.Instance != null)
            {
                ArenaManager.Instance.StartWave(_finalWave + 1);
            }
        }
    }

    // =========================================================================
    // 4. GIAO DIỆN ONGUI DỰ PHÒNG (NẾU CHƯA CÓ CANVAS UI)
    // =========================================================================

    private void OnGUI()
    {
        if (!_isWindowOpen) return;
        if (endGamePanel != null && endGamePanel.activeInHierarchy) return;

        float screenW = Screen.width;
        float screenH = Screen.height;

        // Nền đen mờ 90%
        GUI.color = new Color(0f, 0f, 0f, 0.90f);
        GUI.DrawTexture(new Rect(0, 0, screenW, screenH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Khung tổng kết ở chính giữa
        float panelW = Mathf.Min(680, screenW - 40);
        float panelH = Mathf.Min(560, screenH - 40);
        float panelX = (screenW - panelW) * 0.5f;
        float panelY = (screenH - panelH) * 0.5f;

        GUI.Box(new Rect(panelX, panelY, panelW, panelH), "");

        // Tiêu đề
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = _isVictory ? new Color(1f, 0.85f, 0.2f) : new Color(0.95f, 0.25f, 0.25f);
        GUI.Label(new Rect(panelX, panelY + 25, panelW, 45), _isVictory ? "VICTORY!" : "GAME OVER", titleStyle);

        // Phụ đề
        GUIStyle subStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        subStyle.normal.textColor = new Color(0.85f, 0.9f, 0.95f);
        string subText = _isVictory
            ? $"You survived all {_finalWave} waves and defeated the Zombie Boss!"
            : $"You fought bravely but were overwhelmed at Wave {_finalWave}!";
        GUI.Label(new Rect(panelX + 30, panelY + 75, panelW - 60, 40), subText, subStyle);

        // Bảng thống kê chỉ số
        float statStartY = panelY + 135f;
        float rowHeight = 42f;

        DrawStatRow(panelX + 40, statStartY, panelW - 80, "WAVES SURVIVED:", _isVictory ? $"{_finalWave} (COMPLETED)" : $"Wave {_finalWave}", Color.cyan);
        DrawStatRow(panelX + 40, statStartY + rowHeight, panelW - 80, "BATTLE TIME:", FormatTime(_finalTime), Color.white);
        DrawStatRow(panelX + 40, statStartY + rowHeight * 2, panelW - 80, "ZOMBIES KILLED:", $"{_finalKills} Kills", new Color(1f, 0.45f, 0.45f));
        DrawStatRow(panelX + 40, statStartY + rowHeight * 3, panelW - 80, "GEMS COLLECTED:", $"{_finalGems} Gems", new Color(0.2f, 1f, 0.55f));
        DrawStatRow(panelX + 40, statStartY + rowHeight * 4, panelW - 80, "WEAPONS OWNED:", $"{_finalWeapons}/6 Weapons", new Color(1f, 0.75f, 0.2f));

        // Nút hành động phía dưới
        float btnY = panelY + panelH - 85f;

        if (_isVictory)
        {
            float btnW = (panelW - 80f) / 3f;

            GUI.color = new Color(0.2f, 0.75f, 0.35f);
            if (GUI.Button(new Rect(panelX + 30, btnY, btnW - 10, 52), "RESTART"))
            {
                OnRestartClicked();
            }

            GUI.color = new Color(0.95f, 0.75f, 0.2f);
            if (GUI.Button(new Rect(panelX + 30 + btnW, btnY, btnW - 10, 52), "ENDLESS >"))
            {
                OnEndlessClicked();
            }

            GUI.color = new Color(0.4f, 0.5f, 0.65f);
            if (GUI.Button(new Rect(panelX + 30 + btnW * 2, btnY, btnW - 10, 52), "MAIN MENU"))
            {
                OnMenuClicked();
            }
        }
        else
        {
            float btnW = (panelW - 70f) / 2f;

            GUI.color = new Color(0.2f, 0.75f, 0.35f);
            if (GUI.Button(new Rect(panelX + 30, btnY, btnW, 52), "RESTART"))
            {
                OnRestartClicked();
            }

            GUI.color = new Color(0.4f, 0.5f, 0.65f);
            if (GUI.Button(new Rect(panelX + 40 + btnW, btnY, btnW, 52), "MAIN MENU"))
            {
                OnMenuClicked();
            }
        }

        GUI.color = Color.white;
    }

    private void DrawStatRow(float x, float y, float w, string label, string value, Color valueColor)
    {
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        labelStyle.normal.textColor = new Color(0.8f, 0.85f, 0.9f);
        GUI.Label(new Rect(x, y, w * 0.55f, 35), label, labelStyle);

        GUIStyle valueStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 19,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight
        };
        valueStyle.normal.textColor = valueColor;
        GUI.Label(new Rect(x + w * 0.55f, y, w * 0.45f, 35), value, valueStyle);
    }
}
