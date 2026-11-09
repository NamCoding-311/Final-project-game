using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Quáº£n lÃ½ hiá»ƒn thá»‹ báº£ng HoÃ n ThÃ nh MÃ n ChÆ¡i (Victory), tÃ­nh Ä‘iá»ƒm thÆ°á»Ÿng vÃ  chuyá»ƒn mÃ n
public class LevelCompleteUI : MonoBehaviour
{
    [Header("UI Panels & Text")]
    [SerializeField] private GameObject levelCompletePanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private TextMeshProUGUI totalFundsText;

    private bool _hasCompleted;
    private GameRewardManager.CampaignRewardResult _lastReward;

    private void Start()
    {
        Time.timeScale = 1f;

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }
    }

    // Hiá»ƒn thá»‹ báº£ng hoÃ n thÃ nh mÃ n chÆ¡i vÃ  phÃ¡t thÆ°á»Ÿng
    public void ShowCompletePanel()
    {
        if (_hasCompleted) return;
        _hasCompleted = true;

        if (GameRewardManager.Instance == null)
        {
            var mgrGo = new GameObject("GameRewardManager");
            mgrGo.AddComponent<GameRewardManager>();
        }

        string sceneName = SceneManager.GetActiveScene().name;
        int sectorIndex = 1;
        if (sceneName.Contains("2")) sectorIndex = 2;
        else if (sceneName.Contains("3")) sectorIndex = 3;

        float distance = sectorIndex * 160f;
        int kills = sectorIndex * 12;
        float hpPercent = 1.0f;

        PlayerHealth ph = FindAnyObjectByType<PlayerHealth>();
        if (ph != null && ph.MaxHealth > 0)
        {
            hpPercent = Mathf.Clamp01((float)ph.CurrentHealth / ph.MaxHealth);
        }

        _lastReward = GameRewardManager.Instance.CalculateAndAwardCampaignReward(sectorIndex, distance, kills, hpPercent, true);

        if (titleText != null)
        {
            titleText.text = $"SECTOR 0{sectorIndex} SECURED!";
        }

        if (rewardText != null)
        {
            StartCoroutine(GameRewardManager.AnimateCashCounter(rewardText, 0, _lastReward.totalCash, "BOUNTY REWARD: +$", $" [RANK {_lastReward.rank}]"));
        }

        if (totalFundsText != null)
        {
            totalFundsText.text = $"TOTAL FUNDS: ${GameRewardManager.Instance.GetCurrentCash():N0}";
        }

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    // Chuyá»ƒn sang Map 2
    public void ContinueToMap2()
    {
        Time.timeScale = 1f;
        PlayerPrefs.SetString("LastLevel", "map2");
        PlayerPrefs.Save();
        SceneManager.LoadScene("map2");
    }

    // Chuyá»ƒn sang Map 3
    public void ContinueToMap3()
    {
        Time.timeScale = 1f;
        PlayerPrefs.SetString("LastLevel", "map3");
        PlayerPrefs.Save();
        SceneManager.LoadScene("map3");
    }

    // ChÆ¡i láº¡i Map 1
    public void ReplayMap1()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("map1");
    }

    // ChÆ¡i láº¡i Map 2
    public void ReplayMap2()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("map2");
    }

    // ChÆ¡i láº¡i Map 3
    public void ReplayMap3()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("map3");
    }

    // Vá» Mission Lobby / BÃ n tÃ¡c chiáº¿n
    public void ReturnToMissionLobby()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded("Misson lobby"))
            SceneManager.LoadScene("Misson lobby");
        else if (Application.CanStreamedLevelBeLoaded("Main menu"))
            SceneManager.LoadScene("Main menu");
        else
            SceneManager.LoadScene(0);
    }

    // Vá» Menu chÃ­nh
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

    // OnGUI Fallback hiá»ƒn thá»‹ thÆ°á»Ÿng náº¿u chÆ°a gáº¯n Canvas UI
    private void OnGUI()
    {
        if (!_hasCompleted || (levelCompletePanel != null && levelCompletePanel.activeSelf && rewardText != null)) return;

        float screenW = Screen.width;
        float screenH = Screen.height;

        GUI.color = new Color(0f, 0f, 0f, 0.88f);
        GUI.DrawTexture(new Rect(0, 0, screenW, screenH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float w = Mathf.Min(560, screenW - 40);
        float h = 420;
        float x = (screenW - w) * 0.5f;
        float y = (screenH - h) * 0.5f;

        GUI.Box(new Rect(x, y, w, h), "");

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = new Color(1f, 0.85f, 0.25f);
        GUI.Label(new Rect(x, y + 25, w, 40), $"SECTOR 0{_lastReward.sectorIndex} SECURED!", titleStyle);

        GUIStyle rankStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        rankStyle.normal.textColor = new Color(0.4f, 0.9f, 0.5f);
        GUI.Label(new Rect(x, y + 75, w, 35), $"TACTICAL RATING: RANK {_lastReward.rank}", rankStyle);

        GUIStyle rewardStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        rewardStyle.normal.textColor = new Color(1f, 0.95f, 0.85f);
        GUI.Label(new Rect(x, y + 130, w, 40), $"BOUNTY REWARD: +${_lastReward.totalCash:N0} CASH", rewardStyle);

        GUIStyle fundsStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            alignment = TextAnchor.MiddleCenter
        };
        fundsStyle.normal.textColor = new Color(0.5f, 0.8f, 1f);
        GUI.Label(new Rect(x, y + 180, w, 35), $"TOTAL FUNDS: ${GameRewardManager.Instance?.GetCurrentCash() ?? 0:N0}", fundsStyle);

        float btnW = (w - 60) / 2f;
        float btnY = y + h - 85;

        GUI.color = new Color(0.85f, 0.45f, 0.15f);
        string nextText = _lastReward.sectorIndex == 1 ? "SECTOR 02 >" : (_lastReward.sectorIndex == 2 ? "SECTOR 03 >" : "CAMPAIGN COMPLETE!");
        if (GUI.Button(new Rect(x + 20, btnY, btnW, 55), nextText))
        {
            if (_lastReward.sectorIndex == 1) ContinueToMap2();
            else if (_lastReward.sectorIndex == 2) ContinueToMap3();
            else ReturnToMissionLobby();
        }

        GUI.color = new Color(0.3f, 0.35f, 0.45f);
        if (GUI.Button(new Rect(x + 35 + btnW, btnY, btnW, 55), "MISSION LOBBY"))
        {
            ReturnToMissionLobby();
        }
    }
}