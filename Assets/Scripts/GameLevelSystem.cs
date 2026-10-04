using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Hệ thống Cấp Độ & Nâng Cấp Kỹ Năng chuẩn phong cách Brotato / Roguelite:
// 1. Quản lý Điểm kinh nghiệm (XP), Cấp độ (Level), và Tiền nhặt được (Materials).
// 2. Tự động tạm dừng game khi Lên Cấp và hiển thị 3 Thẻ bài Nâng Cấp ngẫu nhiên.
// 3. Hỗ trợ hiển thị bằng Canvas TextMeshPro tùy chỉnh hoặc fallback OnGUI.
public class BrotatoLevelSystem : MonoBehaviour
{
    public static BrotatoLevelSystem Instance { get; private set; }

    [Header("Custom HUD Canvas (Tùy chọn - Kéo thả UI của bạn vào đây)")]
    [Tooltip("Thanh trượt kinh nghiệm Slider")]
    [SerializeField] private Slider customXpSlider;

    [Tooltip("Text hiển thị Cấp độ (ví dụ: TextMeshPro 'LV. 1')")]
    [SerializeField] private TextMeshProUGUI customLevelText;

    [Tooltip("Text hiển thị Số tiền/Vật liệu nhặt được")]
    [SerializeField] private TextMeshProUGUI customMaterialsText;

    [Tooltip("Text hiển thị tiến trình XP (ví dụ: 'XP: 3 / 5')")]
    [SerializeField] private TextMeshProUGUI customXpText;

    [Header("Custom Level Up Panel (Bảng Chọn 3 Thẻ Bài Tùy Chỉnh)")]
    [Tooltip("Panel cha chứa bảng Level Up (sẽ tự động bật/tắt SetActive)")]
    [SerializeField] private GameObject customLevelUpPanel;

    [System.Serializable]
    public class CustomCardUI
    {
        public GameObject cardRoot;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI descriptionText;
        public Image borderOrBackground;
        public Button selectButton;
    }

    [Tooltip("Danh sách 3 thẻ bài UI trên Canvas")]
    [SerializeField] private CustomCardUI[] customCardsUI = new CustomCardUI[3];

    [Header("Progression Stats")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentXP = 0;
    [SerializeField] private int xpRequired = 5;
    [SerializeField] private int totalMaterials = 0;

    [Header("Magnet Radius")]
    [SerializeField] private float magnetRadius = 3.5f;

    // Trạng thái mở bảng chọn thẻ nâng cấp
    private bool _isLevelUpWindowOpen = false;
    private List<UpgradeCard> _currentOfferedUpgrades = new List<UpgradeCard>();

    public class UpgradeCard
    {
        public string Title;
        public string Description;
        public Color ThemeColor;
        public Action OnSelect;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateCustomHUD();
        if (customLevelUpPanel != null) customLevelUpPanel.SetActive(false);
    }

    public float GetMagnetRadius() => magnetRadius;
    public int GetMaterials() => totalMaterials;

    // Cập nhật giá trị hiển thị trên Canvas UI tùy chỉnh
    public void UpdateCustomHUD()
    {
        if (customXpSlider != null)
        {
            customXpSlider.minValue = 0;
            customXpSlider.maxValue = xpRequired;
            customXpSlider.value = currentXP;
        }

        if (customLevelText != null) customLevelText.text = $"LV. {currentLevel}";
        if (customMaterialsText != null) customMaterialsText.text = $"{totalMaterials}";
        if (customXpText != null) customXpText.text = $"{currentXP} / {xpRequired}";
    }

    // Nhặt ngọc tăng XP & Tiền
    public void AddXP(int amount)
    {
        totalMaterials += amount;
        currentXP += amount;
        UpdateCustomHUD();

        if (currentXP >= xpRequired)
        {
            currentXP -= xpRequired;
            currentLevel++;
            xpRequired = Mathf.RoundToInt(xpRequired * 1.35f) + 3;
            UpdateCustomHUD();

            TriggerLevelUp();
        }
    }

    // Kích hoạt bảng chọn 3 thẻ nâng cấp
    private void TriggerLevelUp()
    {
        _isLevelUpWindowOpen = true;
        Time.timeScale = 0f; // Dừng game lại để người chơi bình tĩnh chọn thẻ

        GenerateThreeRandomUpgrades();

        // Kích hoạt giao diện Canvas Level Up nếu người dùng đã gán vào
        if (customLevelUpPanel != null)
        {
            customLevelUpPanel.SetActive(true);

            for (int i = 0; i < customCardsUI.Length && i < _currentOfferedUpgrades.Count; i++)
            {
                CustomCardUI cardUI = customCardsUI[i];
                UpgradeCard data = _currentOfferedUpgrades[i];
                if (cardUI == null) continue;

                if (cardUI.cardRoot != null) cardUI.cardRoot.SetActive(true);
                if (cardUI.titleText != null) cardUI.titleText.text = data.Title;
                if (cardUI.descriptionText != null) cardUI.descriptionText.text = data.Description;
                if (cardUI.borderOrBackground != null) cardUI.borderOrBackground.color = data.ThemeColor;

                if (cardUI.selectButton != null)
                {
                    cardUI.selectButton.onClick.RemoveAllListeners();
                    cardUI.selectButton.onClick.AddListener(() => {
                        SelectUpgrade(data);
                        if (customLevelUpPanel != null) customLevelUpPanel.SetActive(false);
                    });
                }
            }
        }
    }

    // Sinh 3 thẻ ngẫu nhiên không trùng lặp
    private void GenerateThreeRandomUpgrades()
    {
        List<UpgradeCard> allPool = new List<UpgradeCard>
        {
            new UpgradeCard
            {
                Title = "💥 DAMAGE",
                Description = "+3 Damage per projectile",
                ThemeColor = new Color(1f, 0.3f, 0.3f),
                OnSelect = () => {
                    if (ArenaAutoShooting.Instance != null) ArenaAutoShooting.Instance.AddBonusDamage(3);
                }
            },
            new UpgradeCard
            {
                Title = "⚡ ATTACK SPEED",
                Description = "+20% Weapon Fire Rate",
                ThemeColor = new Color(1f, 0.85f, 0.2f),
                OnSelect = () => {
                    if (ArenaAutoShooting.Instance != null) ArenaAutoShooting.Instance.AddAttackSpeed(0.2f);
                }
            },
            new UpgradeCard
            {
                Title = "👟 MOVE SPEED",
                Description = "+15% Movement Speed",
                ThemeColor = new Color(0.3f, 0.85f, 1f),
                OnSelect = () => {
                    OnFootPlayerController player = FindAnyObjectByType<OnFootPlayerController>();
                    if (player != null) player.AddBonusSpeed(1.0f);
                }
            },
            new UpgradeCard
            {
                Title = "🩸 MAX HP",
                Description = "+25 Max HP & Instant Heal",
                ThemeColor = new Color(0.2f, 1f, 0.4f),
                OnSelect = () => {
                    PlayerHealth hp = FindAnyObjectByType<PlayerHealth>();
                    if (hp != null) hp.IncreaseMaxHP(25);
                }
            },
            new UpgradeCard
            {
                Title = "🧲 MAGNET RANGE",
                Description = "+35% Material Pickup Radius",
                ThemeColor = new Color(0.9f, 0.4f, 1f),
                OnSelect = () => {
                    magnetRadius += 1.5f;
                }
            }
        };

        // Trộn ngẫu nhiên và lấy 3 thẻ
        _currentOfferedUpgrades.Clear();
        for (int i = 0; i < 3 && allPool.Count > 0; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, allPool.Count);
            _currentOfferedUpgrades.Add(allPool[randomIndex]);
            allPool.RemoveAt(randomIndex);
        }
    }

    private void SelectUpgrade(UpgradeCard card)
    {
        card.OnSelect?.Invoke();
        _isLevelUpWindowOpen = false;
        Time.timeScale = 1f; // Tiếp tục game
    }

    // Giao diện HUD tích hợp sẵn
    private void OnGUI()
    {
        // Tự động ẩn HUD OnGUI nếu người dùng đã tự thiết kế Canvas HUD
        bool hasCustomHUD = customXpSlider != null || customLevelText != null || customMaterialsText != null;
        if (hasCustomHUD && (!_isLevelUpWindowOpen || customLevelUpPanel != null))
        {
            return;
        }

        // 1. THANH TIẾN TRÌNH XP VÀ CẤP ĐỘ Ở ĐÁY MÀN HÌNH (chỉ vẽ nếu chưa có Canvas HUD)
        if (!hasCustomHUD)
        {
            float barWidth = Screen.width * 0.7f;
            float barHeight = 18f;
            float barX = (Screen.width - barWidth) * 0.5f;
            float barY = Screen.height - 35f;

            // Vẽ nền thanh XP (xám đen)
            GUI.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);
            GUI.DrawTexture(new Rect(barX, barY, barWidth, barHeight), Texture2D.whiteTexture);

            // Vẽ phần % XP đã đạt (xanh ngọc)
            float progress = Mathf.Clamp01((float)currentXP / Mathf.Max(1, xpRequired));
            GUI.color = new Color(0.1f, 0.95f, 0.35f, 1f);
            GUI.DrawTexture(new Rect(barX, barY, barWidth * progress, barHeight), Texture2D.whiteTexture);

            // Text Cấp độ và Tiền
            GUI.color = Color.white;
            GUIStyle hudStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Label(new Rect(barX, barY - 24f, barWidth, 24f), $"LV. {currentLevel}   |   💎 Materials: {totalMaterials}   |   XP: {currentXP} / {xpRequired}", hudStyle);
        }

        // 2. BẢNG CHỌN 3 THẺ NÂNG CẤP KHI LEVEL UP (chỉ vẽ nếu chưa có customLevelUpPanel)
        if (_isLevelUpWindowOpen && customLevelUpPanel == null)
        {
            // Làm mờ nền tối toàn màn hình
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            // Tiêu đề LEVEL UP
            GUI.color = Color.yellow;
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 38,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Label(new Rect(0, Screen.height * 0.16f, Screen.width, 50f), "⭐ LEVEL UP! CHOOSE AN UPGRADE ⭐", headerStyle);

            // Vẽ 3 Thẻ bài nằm ngang
            float cardWidth = 230f;
            float cardHeight = 280f;
            float spacing = 35f;
            float totalWidth = (cardWidth * 3) + (spacing * 2);
            float startX = (Screen.width - totalWidth) * 0.5f;
            float cardY = Screen.height * 0.32f;

            for (int i = 0; i < _currentOfferedUpgrades.Count; i++)
            {
                UpgradeCard card = _currentOfferedUpgrades[i];
                float x = startX + i * (cardWidth + spacing);
                Rect cardRect = new Rect(x, cardY, cardWidth, cardHeight);

                // Khung viền thẻ bài
                GUI.color = card.ThemeColor;
                GUI.DrawTexture(cardRect, Texture2D.whiteTexture);

                // Ruột nền thẻ bài tối
                GUI.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);
                GUI.DrawTexture(new Rect(cardRect.x + 4, cardRect.y + 4, cardRect.width - 8, cardRect.height - 8), Texture2D.whiteTexture);

                // Tiêu đề thẻ
                GUI.color = card.ThemeColor;
                GUIStyle cardTitleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                GUI.Label(new Rect(cardRect.x + 10, cardRect.y + 25, cardRect.width - 20, 35), card.Title, cardTitleStyle);

                // Mô tả thẻ
                GUI.color = Color.white;
                GUIStyle descStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
                GUI.Label(new Rect(cardRect.x + 15, cardRect.y + 80, cardRect.width - 30, 90), card.Description, descStyle);

                // Nút CHỌN
                GUI.color = card.ThemeColor;
                GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 17,
                    fontStyle = FontStyle.Bold
                };

                if (GUI.Button(new Rect(cardRect.x + 30, cardRect.y + cardHeight - 65, cardRect.width - 60, 45), "SELECT", btnStyle))
                {
                    SelectUpgrade(card);
                }
            }
        }
    }
}

// Alias để tương thích tên file GameLevelSystem.cs
public class GameLevelSystem : BrotatoLevelSystem { }

