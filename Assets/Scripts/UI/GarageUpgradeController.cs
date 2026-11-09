using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Builds the garage's HP and weapon upgrade controls and saves purchases.</summary>
public class GarageUpgradeController : MonoBehaviour
{
    public const string HpLevelKey = "GarageUpgrade_HP";
    public const string GunLevelKey = "GarageUpgrade_Gun";
    public const string DashLevelKey = "GarageUpgrade_Dash";

    [SerializeField] private RectTransform upgradePanel;
    [SerializeField] private TMP_Text cashText;
    [SerializeField] private int maxUpgradeLevel = 5;
    [SerializeField] private int hpCost = 500;
    [SerializeField] private int gunCost = 750;
    [SerializeField] private int dashCost = 400;
    [SerializeField] private bool seedDemoCashOnce = true;
    [SerializeField] private int demoCashAmount = 10000;

    private const string DemoCashSeededKey = "GarageDemoCashSeeded";

    private const int HpPerLevel = 25;
    private const int DamagePerLevel = 2;

    private void OnEnable()
    {
        GameRewardManager.OnCashChanged += HandleCashChanged;
    }

    private void OnDisable()
    {
        GameRewardManager.OnCashChanged -= HandleCashChanged;
    }

    private void Start()
    {
        if (upgradePanel == null)
        {
            Transform panel = transform.Find("UpgradePanel");
            if (panel != null) upgradePanel = panel as RectTransform;
        }

        if (upgradePanel == null)
        {
            Debug.LogError("GarageUpgradeController: UpgradePanel was not found.", this);
            return;
        }

        SeedDemoCashIfNeeded();
        BuildCashLabel();
        BuildDashButton();
        BuildButton("HPUpgradeButton", "MAX HP", HpLevelKey, hpCost, new Vector2(-45f, -290f));
        BuildButton("GunUpgradeButton", "WEAPON DAMAGE", GunLevelKey, gunCost, new Vector2(345f, -290f));
        Refresh();
    }

    public void GoToCurrentMap()
    {
        Time.timeScale = 1f;
        string currentMap = PlayerPrefs.GetString("LastLevel", "map1");
        if (!Application.CanStreamedLevelBeLoaded(currentMap))
        {
            Debug.LogWarning($"Garage: map '{currentMap}' is unavailable; loading map1.", this);
            currentMap = "map1";
        }

        SceneManager.LoadScene(currentMap);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        // Resolve the configured scene by its asset path first. This avoids relying
        // on display-name matching (the scene file contains a space in its name).
        int menuBuildIndex = SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/Main menu.unity");
        if (menuBuildIndex >= 0)
        {
            SceneManager.LoadScene(menuBuildIndex);
            return;
        }

        // Main menu is the first scene in this project's Build Settings. Use it as
        // a fallback if the active build profile does not expose the path lookup.
        if (SceneManager.sceneCountInBuildSettings > 0)
        {
            SceneManager.LoadScene(0);
            return;
        }

        Debug.LogError("Garage: Main menu scene is not included in the active build profile.", this);
    }

    private void SeedDemoCashIfNeeded()
    {
        if (!seedDemoCashOnce || PlayerPrefs.GetInt(DemoCashSeededKey, 0) != 0) return;

        PlayerPrefs.SetInt(GameRewardManager.PREF_PLAYER_CASH, Mathf.Max(0, demoCashAmount));
        PlayerPrefs.SetInt(GameRewardManager.PREF_TOTAL_COINS, Mathf.Max(0, demoCashAmount));
        PlayerPrefs.SetInt(DemoCashSeededKey, 1);
        PlayerPrefs.Save();
    }

    private void BuildCashLabel()
    {
        if (cashText != null) return;
        Transform money = transform.Find("money");
        if (money == null) return;

        Transform existing = money.Find("CashBalance");
        GameObject labelObject;
        if (existing != null)
        {
            labelObject = existing.gameObject;
        }
        else
        {
            labelObject = new GameObject("CashBalance", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(money, false);
        }

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(75f, 0f);
        rect.sizeDelta = new Vector2(260f, 70f);
        cashText = labelObject.GetComponent<TextMeshProUGUI>();
        cashText.alignment = TextAlignmentOptions.Center;
        cashText.fontSize = 30f;
        cashText.color = Color.white;
        cashText.raycastTarget = false;
    }

    private void HandleCashChanged(int _) => Refresh();

    private void BuildDashButton()
    {
        Transform dashCard = upgradePanel.Find("DashCard");
        if (dashCard == null) return;

        Button button = dashCard.GetComponent<Button>();
        if (button == null) button = dashCard.gameObject.AddComponent<Button>();

        TMP_Text label = dashCard.GetComponentInChildren<TMP_Text>();
        if (label == null)
        {
            GameObject labelObject = new GameObject("DashUpgradeLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(dashCard, false);
            label = labelObject.GetComponent<TextMeshProUGUI>();
            TMP_Text fontTemplate = upgradePanel.Find("HPUpgradeButton")?.GetComponentInChildren<TMP_Text>();
            if (fontTemplate != null) label.font = fontTemplate.font;
        }

        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 12f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 8f;
        label.fontSizeMax = 12f;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Truncate;
        label.color = Color.white;
        label.raycastTarget = false;
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0.05f, 0.24f);
        labelRect.anchorMax = new Vector2(0.96f, 0.44f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        button.targetGraphic = dashCard.GetComponent<Image>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => Purchase(DashLevelKey, "ENGINE DASH", dashCost));
        RefreshButton(button, label, "ENGINE DASH", DashLevelKey, dashCost);
    }

    private void BuildButton(string objectName, string title, string key, int baseCost, Vector2 position)
    {
        Transform existing = upgradePanel.Find(objectName);
        GameObject buttonObject;
        if (existing != null)
        {
            buttonObject = existing.gameObject;
        }
        else
        {
            buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(upgradePanel, false);
        }

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(340f, 113f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = true;

        TMP_Text label = buttonObject.GetComponentInChildren<TMP_Text>();
        if (label == null)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 5f);
            labelRect.offsetMax = new Vector2(-8f, -5f);
            label = labelObject.GetComponent<TextMeshProUGUI>();
        }

        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 12f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 8f;
        label.fontSizeMax = 12f;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Truncate;
        label.color = Color.white;
        label.raycastTarget = false;
        RectTransform labelRectTransform = label.rectTransform;
        labelRectTransform.anchorMin = new Vector2(0.05f, 0.24f);
        labelRectTransform.anchorMax = new Vector2(0.96f, 0.44f);
        labelRectTransform.offsetMin = Vector2.zero;
        labelRectTransform.offsetMax = Vector2.zero;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => Purchase(key, title, baseCost));
        RefreshButton(button, label, title, key, baseCost);
    }

    private void Purchase(string key, string title, int baseCost)
    {
        int level = PlayerPrefs.GetInt(key, 0);
        if (level >= maxUpgradeLevel) return;
        int cost = baseCost * (level + 1);
        if (!TrySpendCash(cost, title))
        {
            Refresh();
            return;
        }

        PlayerPrefs.SetInt(key, level + 1);
        PlayerPrefs.Save();
        Refresh();
    }

    private bool TrySpendCash(int amount, string reason)
    {
        if (GameRewardManager.Instance != null)
            return GameRewardManager.Instance.SpendCash(amount, $"Garage {reason}");

        int cash = PlayerPrefs.GetInt(GameRewardManager.PREF_PLAYER_CASH, 2500);
        if (cash < amount) return false;
        cash -= amount;
        PlayerPrefs.SetInt(GameRewardManager.PREF_PLAYER_CASH, cash);
        PlayerPrefs.SetInt(GameRewardManager.PREF_TOTAL_COINS, cash);
        PlayerPrefs.Save();
        return true;
    }

    private void Refresh()
    {
        RefreshDashButton();
        RefreshButton("HPUpgradeButton", "MAX HP", HpLevelKey, hpCost);
        RefreshButton("GunUpgradeButton", "WEAPON DAMAGE", GunLevelKey, gunCost);
        if (cashText != null) cashText.text = $"${GetCash():N0}";
    }

    private void RefreshDashButton()
    {
        if (upgradePanel == null) return;
        Transform dashCard = upgradePanel.Find("DashCard");
        if (dashCard == null) return;
        RefreshButton(dashCard.GetComponent<Button>(), dashCard.GetComponentInChildren<TMP_Text>(), "ENGINE DASH", DashLevelKey, dashCost);
    }

    private void RefreshButton(string objectName, string title, string key, int baseCost)
    {
        if (upgradePanel == null) return;
        Transform found = upgradePanel.Find(objectName);
        if (found == null) return;
        RefreshButton(found.GetComponent<Button>(), found.GetComponentInChildren<TMP_Text>(), title, key, baseCost);
    }

    private void RefreshButton(Button button, TMP_Text label, string title, string key, int baseCost)
    {
        if (button == null || label == null) return;
        int level = PlayerPrefs.GetInt(key, 0);
        bool maxed = level >= maxUpgradeLevel;
        int cost = baseCost * (level + 1);
        string effect = key == HpLevelKey
            ? $"+{HpPerLevel} HP"
            : key == GunLevelKey
                ? $"+{DamagePerLevel} DAMAGE"
                : "+1 DASH SPEED / +0.05s";
        label.text = maxed
            ? $"LV {level}/{maxUpgradeLevel}  |  MAX"
            : $"{effect} | LV {level}/{maxUpgradeLevel} | UPGRADE ${cost:N0}";
        button.interactable = !maxed && GetCash() >= cost;
    }

    private static int GetCash()
    {
        return GameRewardManager.Instance != null
            ? GameRewardManager.Instance.GetCurrentCash()
            : PlayerPrefs.GetInt(GameRewardManager.PREF_PLAYER_CASH, 2500);
    }
}
