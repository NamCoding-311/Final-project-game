using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Quáº£n lÃ½ hiá»ƒn thá»‹ quÃ£ng Ä‘Æ°á»ng cháº¡y Ä‘Æ°á»£c (Distance), Ä‘iá»ƒm cao nháº¥t (Highscore), tÃ­nh tiá»n thÆ°á»Ÿng khi cháº¿t vÃ  giao diá»‡n Game Over
public class DistanceTrackerUI : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform playerTransform;

    [Header("In-Game HUD Elements")]
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI killCountText;

    [Header("Game Over Screen Elements")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalDistanceText;
    [SerializeField] private TextMeshProUGUI bestDistanceText;
    [SerializeField] private TextMeshProUGUI sectorSummaryText;
    [SerializeField] private TextMeshProUGUI killSummaryText;
    [SerializeField] private TextMeshProUGUI speedSummaryText;

    [Header("Reward / Cash Elements (Tiá»n ThÆ°á»Ÿng Khi Cháº¿t)")]
    [SerializeField] private TextMeshProUGUI distanceScrapText;
    [SerializeField] private TextMeshProUGUI mutantScrapText;
    [SerializeField] private TextMeshProUGUI earnedCashText;
    [SerializeField] private TextMeshProUGUI totalFundsText;

    [Header("Action Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button lobbyButton;

    private float _startX;
    private float _currentDistance;
    private int _zombieKillCount;
    private Player3LaneMovement _playerMovement;
    private float _lastSpeed;

    private bool _isGameOver = false;
    private GameRewardManager.CampaignRewardResult _deathReward;

    // Cache textures cho OnGUI Fallback
    private Texture2D _metalFrameTex;
    private Texture2D _btnTex;

    private void Start()
    {
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

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(RetryLevel);
        }

        if (lobbyButton != null)
        {
            lobbyButton.onClick.RemoveAllListeners();
            lobbyButton.onClick.AddListener(ReturnToLobby);
        }
    }

    private void Update()
    {
        if (_isGameOver)
        {
            // PhÃ­m táº¯t bÃ n phÃ­m khi Game Over
            if (Input.GetKeyDown(KeyCode.R)) RetryLevel();
            if (Input.GetKeyDown(KeyCode.Escape)) ReturnToLobby();
            return;
        }

        if (playerTransform == null) return;

        _currentDistance = Mathf.Max(0f, playerTransform.position.x - _startX);

        if (_playerMovement != null)
        {
            _lastSpeed = _playerMovement.GetCurrentSpeed();
        }

        if (distanceText != null)
        {
            distanceText.text = $"{Mathf.FloorToInt(_currentDistance)} m";
        }

        if (speedText != null)
        {
            speedText.text = $"{Mathf.FloorToInt(_lastSpeed)} km/h";
        }
    }

    public void AddZombieKill()
    {
        _zombieKillCount++;
        if (killCountText != null)
        {
            killCountText.text = $"Kills: {_zombieKillCount}";
        }
    }

    // Gá»i hÃ m nÃ y khi Player cháº¿t Ä‘á»ƒ hiá»‡n káº¿t quáº£, tÃ­nh tiá»n thu lÆ°á»£m Ä‘Æ°á»£c vÃ  lÆ°u Ká»· lá»¥c
    public void ShowGameOverResults()
    {
        if (_isGameOver) return;
        _isGameOver = true;

        int finalScore = Mathf.FloorToInt(_currentDistance);
        int bestScore = PlayerPrefs.GetInt("BestDistance", 0);

        if (finalScore > bestScore)
        {
            bestScore = finalScore;
            PlayerPrefs.SetInt("BestDistance", bestScore);
            PlayerPrefs.Save();
        }

        // Tá»± Ä‘á»™ng nháº­n diá»‡n Sector hiá»‡n táº¡i
        string sceneName = SceneManager.GetActiveScene().name;
        int sectorIndex = 1;
        if (sceneName.Contains("2")) sectorIndex = 2;
        else if (sceneName.Contains("3")) sectorIndex = 3;

        // Äáº£m báº£o GameRewardManager tá»“n táº¡i
        if (GameRewardManager.Instance == null)
        {
            var mgrGo = new GameObject("GameRewardManager");
            mgrGo.AddComponent<GameRewardManager>();
        }

        // Khi ngÆ°á»i chÆ¡i ngÃ£ xuá»‘ng giá»¯a Ä‘Æ°á»ng: trao tiá»n an á»§i theo cá»± ly vÃ  sá»‘ quÃ¡i Ä‘Ã£ hÃºc Ä‘Æ°á»£c (Rank C)
        _deathReward = GameRewardManager.Instance.CalculateAndAwardCampaignReward(sectorIndex, _currentDistance, _zombieKillCount, 0f, false);

        if (sectorSummaryText != null)
        {
            sectorSummaryText.text = $"Sector 0{sectorIndex}";
        }

        if (finalDistanceText != null)
        {
            finalDistanceText.text = $"{finalScore:N0} m";
        }

        if (killSummaryText != null)
        {
            killSummaryText.text = $"{_zombieKillCount:N0} Mutants";
        }

        if (speedSummaryText != null)
        {
            speedSummaryText.text = $"{Mathf.FloorToInt(_lastSpeed)} Km/h";
        }

        if (bestDistanceText != null)
        {
            bestDistanceText.text = $"Best Record: {bestScore} m";
        }

        if (distanceScrapText != null)
        {
            distanceScrapText.text = $"+${_deathReward.distanceCash:N0} Cash";
        }

        if (mutantScrapText != null)
        {
            mutantScrapText.text = $"+${_deathReward.killCash:N0} Cash";
        }

        if (earnedCashText != null)
        {
            StartCoroutine(GameRewardManager.AnimateCashCounter(earnedCashText, 0, _deathReward.totalCash, "+$", " CASH"));
        }

        if (totalFundsText != null)
        {
            totalFundsText.text = $"${GameRewardManager.Instance.GetCurrentCash():N0} CASH";
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    public void RetryLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToLobby()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded("Misson lobby"))
            SceneManager.LoadScene("Misson lobby");
        else if (Application.CanStreamedLevelBeLoaded("Main menu"))
            SceneManager.LoadScene("Main menu");
        else
            SceneManager.LoadScene(0);
    }

    // Giao diá»‡n OnGUI tá»± Ä‘á»™ng hiá»ƒn thá»‹ theo PhÆ°Æ¡ng Ã¡n 1 (Khung kim loáº¡i rá»‰)
    private void OnGUI()
    {
        // Chi ve OnGUI neu khong co Canvas GameOverPanel
        if (!_isGameOver || gameOverPanel != null) return;

        float screenW = Screen.width;
        float screenH = Screen.height;

        // Phá»§ ná»n má» tá»‘i
        GUI.color = new Color(0.02f, 0.02f, 0.03f, 0.70f);
        GUI.DrawTexture(new Rect(0, 0, screenW, screenH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Táº£i frame náº¿u chÆ°a cÃ³
        if (_metalFrameTex == null)
        {
            _metalFrameTex = Resources.Load<Texture2D>("Rusty Pixel Metal Game UI Frame");
            if (_metalFrameTex == null)
            {
#if UNITY_EDITOR
                _metalFrameTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/UI/mainpage/Rusty Pixel Metal Game UI Frame.png");
#endif
            }
        }

        float fw = Mathf.Min(1100, screenW - 40);
        float fh = Mathf.Min(660, screenH - 40);
        float fx = (screenW - fw) * 0.5f;
        float fy = (screenH - fh) * 0.5f;

        if (_metalFrameTex != null)
        {
            GUI.DrawTexture(new Rect(fx, fy, fw, fh), _metalFrameTex);
        }
        else
        {
            GUI.Box(new Rect(fx, fy, fw, fh), "");
        }

        // Header TiÃªu Ä‘á» Game Over
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(Mathf.Min(26, screenW * 0.02f)),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        titleStyle.normal.textColor = new Color(0.98f, 0.25f, 0.25f);
        GUI.Label(new Rect(fx + 65, fy + 55, fw - 130, 35), "SIGNAL LOST // VEHICLE DESTROYED", titleStyle);

        GUIStyle subStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleLeft
        };
        subStyle.normal.textColor = new Color(0.80f, 0.78f, 0.75f);
        GUI.Label(new Rect(fx + 67, fy + 88, fw - 130, 25), "CASUALTY ASSESSMENT & SALVAGE RECOVERY REPORT", subStyle);

        // 2 Khá»‘i Cá»™t
        float colY = fy + 120;
        float btnH = 46;
        float btnY = fy + fh - 68;
        float colH = btnY - colY - 16;
        float colW = (fw - 160) * 0.5f;
        float c1x = fx + 65;
        float c2x = c1x + colW + 30;

        // Cá»™t 1: Telemetry
        GUI.color = new Color(0.08f, 0.08f, 0.10f, 0.88f);
        GUI.DrawTexture(new Rect(c1x, colY, colW, colH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle secH = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
        secH.normal.textColor = new Color(1f, 0.75f, 0.25f);
        GUI.Label(new Rect(c1x + 20, colY + 12, colW - 40, 22), "[ COMBAT TELEMETRY ]", secH);

        GUIStyle statLbl = new GUIStyle(GUI.skin.label) { fontSize = 11 };
        statLbl.normal.textColor = new Color(0.70f, 0.68f, 0.65f);
        GUIStyle statVal = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };

        float sy = colY + 38;
        GUI.Label(new Rect(c1x + 20, sy, colW - 40, 16), "DISTANCE REACHED:", statLbl);
        statVal.normal.textColor = Color.white;
        GUI.Label(new Rect(c1x + 20, sy + 15, colW - 40, 18), $"{Mathf.FloorToInt(_currentDistance)} Meters", statVal);

        sy += 38;
        GUI.Label(new Rect(c1x + 20, sy, colW - 40, 16), "MUTANTS ELIMINATED:", statLbl);
        statVal.normal.textColor = new Color(1f, 0.45f, 0.45f);
        GUI.Label(new Rect(c1x + 20, sy + 15, colW - 40, 18), $"{_zombieKillCount} Mutants Rammed", statVal);

        sy += 38;
        GUI.Label(new Rect(c1x + 20, sy, colW - 40, 16), "IMPACT VELOCITY:", statLbl);
        statVal.normal.textColor = Color.white;
        GUI.Label(new Rect(c1x + 20, sy + 15, colW - 40, 18), $"{Mathf.FloorToInt(_lastSpeed)} Km/h", statVal);

        sy += 38;
        GUI.Label(new Rect(c1x + 20, sy, colW - 40, 16), "CAUSE OF LOSS:", statLbl);
        statVal.normal.textColor = new Color(1f, 0.75f, 0.25f);
        GUI.Label(new Rect(c1x + 20, sy + 15, colW - 40, 18), "Armor Breached / Horde Swarm", statVal);

        // Cá»™t 2: Salvage Bounty
        GUI.color = new Color(0.08f, 0.08f, 0.10f, 0.88f);
        GUI.DrawTexture(new Rect(c2x, colY, colW, colH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(c2x + 20, colY + 12, colW - 40, 22), "[ SALVAGE BOUNTY ]", secH);

        float sy2 = colY + 38;
        GUI.Label(new Rect(c2x + 20, sy2, colW - 40, 16), "DISTANCE SCRAP (+$1/m):", statLbl);
        statVal.normal.textColor = Color.white;
        GUI.Label(new Rect(c2x + 20, sy2 + 15, colW - 40, 18), $"+${_deathReward.distanceCash:N0} Cash ({Mathf.FloorToInt(_currentDistance)}m)", statVal);

        sy2 += 38;
        GUI.Label(new Rect(c2x + 20, sy2, colW - 40, 16), "MUTANTS SCRAP (+$10/kill):", statLbl);
        statVal.normal.textColor = new Color(1f, 0.45f, 0.45f);
        GUI.Label(new Rect(c2x + 20, sy2 + 15, colW - 40, 18), $"+${_deathReward.killCash:N0} Cash ({_zombieKillCount} Rammed)", statVal);

        // Há»™p vÃ ng Tiá»n ThÆ°á»Ÿng Tá»•ng
        float boxY = sy2 + 38;
        float boxH = Mathf.Max(85, colH - (boxY - colY) - 10);
        GUI.color = new Color(0.16f, 0.12f, 0.08f, 0.95f);
        GUI.DrawTexture(new Rect(c2x + 15, boxY, colW - 30, boxH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle subGold = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold };
        subGold.normal.textColor = new Color(1f, 0.80f, 0.30f);
        GUI.Label(new Rect(c2x + 25, boxY + 8, colW - 50, 16), "TOTAL SALVAGE CASH EARNED:", subGold);

        GUIStyle cashLg = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        cashLg.normal.textColor = new Color(0.35f, 0.95f, 0.50f);
        GUI.Label(new Rect(c2x + 25, boxY + 26, colW - 50, 30), $"+${_deathReward.totalCash:N0} CASH", cashLg);

        GUIStyle fundLbl = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
        fundLbl.normal.textColor = new Color(0.4f, 0.85f, 1f);
        GUI.Label(new Rect(c2x + 25, boxY + 58, colW - 50, 18), $"TOTAL FUNDS: ${GameRewardManager.Instance?.GetCurrentCash() ?? 0:N0}", fundLbl);

        // NÃºt báº¥m phÃ­a dÆ°á»›i (TÃ¡ch biá»‡t hoÃ n toÃ n, khÃ´ng Ä‘Ã¨ chá»¯)
        float btnW = (fw - 200) * 0.5f;

        GUI.color = new Color(0.85f, 0.25f, 0.20f);
        if (GUI.Button(new Rect(c1x, btnY, btnW, btnH), "[R]  RETRY SECTOR"))
        {
            RetryLevel();
        }

        GUI.color = new Color(0.30f, 0.35f, 0.45f);
        if (GUI.Button(new Rect(c2x, btnY, btnW, btnH), "[ESC]  MISSION LOBBY"))
        {
            ReturnToLobby();
        }
    }
}