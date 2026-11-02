using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Hệ thống Cửa Hàng Giữa Các Hiệp (Inter-Wave Shop) chuẩn phong cách Brotato:
// 1. Sau khi hoàn thành một Wave (hết giờ, quái sạch), tự động tạm dừng game và mở Shop.
// 2. Dùng ngọc/vật liệu (Materials) nhặt được để mua thêm súng mới hoặc nâng cấp chỉ số.
// 3. Hỗ trợ 4 thẻ hàng ngẫu nhiên: Súng mới (Shotgun, AR, Sniper), Giáp, Tốc độ, Hồi máu, Nam châm.
// 4. Có nút Đổi Vật Phẩm (Reroll) và nút Vào Wave Tiếp Theo (Next Wave).
public class ArenaShopSystem : MonoBehaviour
{
    public static ArenaShopSystem Instance { get; private set; }

    [Header("UI Canvas References (Kéo thả UI của bạn vào đây)")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TextMeshProUGUI waveTitleText;
    [SerializeField] private TextMeshProUGUI materialsText;
    [SerializeField] private Button rerollButton;
    [SerializeField] private TextMeshProUGUI rerollCostText;
    [SerializeField] private Button nextWaveButton;
    [SerializeField] private TextMeshProUGUI nextWaveButtonText;

    [System.Serializable]
    public class ShopCardUI
    {
        public GameObject cardRoot;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descText;
        public TextMeshProUGUI priceText;
        public Image borderOrIcon;
        public Image itemIcon;
        public Button buyButton;
        public TextMeshProUGUI buyButtonText;
        public Button lockButton;
        public TextMeshProUGUI lockButtonText;
    }

    [Tooltip("4 Thẻ bài vật phẩm trong Shop")]
    [SerializeField] private ShopCardUI[] shopCardsUI = new ShopCardUI[4];

    [Header("Shop Settings")]
    [SerializeField] private int baseRerollCost = 15;
    [SerializeField] private List<WeaponData> availableWeapons = new List<WeaponData>();

    public class ShopItemOffer
    {
        public string Title;
        public string Description;
        public int Price;
        public Color ThemeColor;
        public Sprite Icon;
        public bool IsSold;
        public bool IsLocked;
        public Action OnBuy;
    }

    private bool _isShopOpen = false;
    private int _currentWaveNumber = 1;
    private int _currentRerollCost;
    private List<ShopItemOffer> _currentOffers = new List<ShopItemOffer>();

    private Sprite _cachedMedkitSprite;

    private void Awake()
    {
        Instance = this;
        _currentRerollCost = baseRerollCost;
        LoadMedkitAsset();
    }

    private void Start()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        // Tự động tải các file WeaponData trong Assets/Weapon nếu chưa gán
        if (availableWeapons.Count == 0)
        {
            WeaponData[] loaded = Resources.FindObjectsOfTypeAll<WeaponData>();
            foreach (var w in loaded)
            {
                if (w != null && !availableWeapons.Contains(w))
                {
                    availableWeapons.Add(w);
                }
            }
        }

        if (rerollButton != null)
        {
            rerollButton.onClick.AddListener(OnRerollClicked);
        }

        if (nextWaveButton != null)
        {
            nextWaveButton.onClick.AddListener(OnNextWaveClicked);
        }
    }



    // =========================================================================
    // 1. MỞ VÀ ĐÓNG SHOP


    // =========================================================================
    public void OpenShop(int finishedWave)
    {
        _isShopOpen = true;
        _currentWaveNumber = finishedWave;
        _currentRerollCost = baseRerollCost;
        LoadMedkitAsset();
        Time.timeScale = 0f; // Tạm dừng game để người chơi bình tĩnh shopping

        GenerateShopOffers();
        UpdateShopUI();

        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
        }
    }

    public void CloseShop()
    {
        _isShopOpen = false;
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }

        Time.timeScale = 1f; // Tiếp tục game
    }



    // =========================================================================
    // 2. SINH 4 VẬT PHẨM NGẪU NHIÊN TRONG CỬA HÀNG


    // =========================================================================
    private void GenerateShopOffers()
    {
        _currentOffers.Clear();
        List<ShopItemOffer> pool = new List<ShopItemOffer>();

        // A. Thêm các loại súng nếu chưa đầy số lượng tối đa (6 khẩu)
        ArenaAutoShooting shooting = ArenaAutoShooting.Instance ?? FindAnyObjectByType<ArenaAutoShooting>();
        bool canEquipMoreGuns = shooting == null || shooting.GetCurrentWeaponCount() < shooting.GetMaxWeaponCount();

        if (canEquipMoreGuns && availableWeapons.Count > 0)
        {
            foreach (var weapon in availableWeapons)
            {
                if (weapon == null) continue;
                WeaponData wData = weapon;

                int weaponPrice = 55 + (_currentWaveNumber * 5);
                if (wData.weaponName.ToLower().Contains("shotgun") || wData.name.ToLower().Contains("shotgun")) weaponPrice = 65 + (_currentWaveNumber * 5);
                if (wData.weaponName.ToLower().Contains("sniper") || wData.name.ToLower().Contains("sniper")) weaponPrice = 75 + (_currentWaveNumber * 5);

                string wName = (wData.weaponName ?? wData.name).ToLower();
                Sprite gunIcon = GetIcon("Icon_Rifle");
                if (wName.Contains("shotgun")) gunIcon = GetIcon("Icon_Shotgun");
                else if (wName.Contains("revolver")) gunIcon = GetIcon("Icon_Revolver");
                else if (wName.Contains("piston") || wName.Contains("pistol")) gunIcon = GetIcon("Icon_Pistol");
                else if (wName.Contains("mp5") || wName.Contains("machine")) gunIcon = GetIcon("Icon_MachineGun");
                else if (wName.Contains("bazooka") || wName.Contains("rocket")) gunIcon = GetIcon("Icon_Bazooka");
                else if (wName.Contains("molotov")) gunIcon = GetIcon("Icon_Molotov");

                pool.Add(new ShopItemOffer
                {
                    Title = $"[GUN] {wData.weaponName.ToUpper()}",
                    Description = $"Adds 1 orbiting gun that auto-aims and fires!",
                    Price = weaponPrice,
                    ThemeColor = new Color(1f, 0.5f, 0.1f), // Màu cam vũ khí
                    Icon = gunIcon,
                    IsSold = false,
                    OnBuy = () => {
                        if (shooting != null) shooting.EquipNewWeapon(wData);
                    }
                });
            }
        }

        // B. Các vật phẩm nâng cấp chỉ số bị động (Passives)
        // 1. Tăng sát thương
        pool.Add(new ShopItemOffer
        {
            Title = "[AMMO] BONUS DAMAGE",
            Description = "+4 Damage for ALL equipped guns!",
            Price = 40 + (_currentWaveNumber * 4),
            ThemeColor = new Color(1f, 0.3f, 0.3f), // Đỏ
            Icon = GetIcon("Icon_Damage"),
            IsSold = false,
            OnBuy = () => {
                if (shooting != null) shooting.AddBonusDamage(4);
            }
        });

        // 2. Tăng tốc độ bắn
        pool.Add(new ShopItemOffer
        {
            Title = "[BARREL] ATTACK SPEED",
            Description = "+25% Fire Rate for ALL equipped guns!",
            Price = 40 + (_currentWaveNumber * 4),
            ThemeColor = new Color(1f, 0.85f, 0.2f), // Vàng
            Icon = GetIcon("Icon_AttackSpeed"),
            IsSold = false,
            OnBuy = () => {
                if (shooting != null) shooting.AddAttackSpeed(0.25f);
            }
        });

        // 3. Giáp chống chịu
        pool.Add(new ShopItemOffer
        {
            Title = "[ARMOR] KEVLAR DEFENSE",
            Description = "+15% Damage Reduction from all attacks!",
            Price = 45 + (_currentWaveNumber * 3),
            ThemeColor = new Color(0.4f, 0.7f, 1f), // Xanh dương
            Icon = GetIcon("Icon_Armor"),
            IsSold = false,
            OnBuy = () => {
                PlayerHealth hp = FindAnyObjectByType<PlayerHealth>();
                if (hp != null) hp.AddDamageReduction(0.15f);
            }
        });

        // 4. Giày chạy nhanh
        pool.Add(new ShopItemOffer
        {
            Title = "[BOOTS] MOVE SPEED",
            Description = "+15% Move Speed to dodge enemies easily!",
            Price = 35 + (_currentWaveNumber * 3),
            ThemeColor = new Color(0.3f, 1f, 0.7f), // Xanh ngọc
            Icon = GetIcon("Icon_MoveSpeed"),
            IsSold = false,
            OnBuy = () => {
                OnFootPlayerController player = FindAnyObjectByType<OnFootPlayerController>();
                if (player != null) player.AddBonusSpeed(1.2f);
            }
        });

        // 5. Túi cứu thương
        pool.Add(new ShopItemOffer
        {
            Title = "[MEDIC] MEDKIT",
            Description = "+30 Max HP and instantly restore 50 HP!",
            Price = 35 + (_currentWaveNumber * 2),
            ThemeColor = new Color(0.2f, 1f, 0.4f), // Xanh lá
            Icon = _cachedMedkitSprite,
            IsSold = false,
            OnBuy = () => {
                PlayerHealth hp = FindAnyObjectByType<PlayerHealth>();
                if (hp != null)
                {
                    hp.IncreaseMaxHP(30);
                    hp.Heal(50);
                }
            }
        });

        // 6. Nhẫn nam châm
        pool.Add(new ShopItemOffer
        {
            Title = "[MAGNET] MAGNET RING",
            Description = "+50% Material Gem pickup radius!",
            Price = 30 + (_currentWaveNumber * 2),
            ThemeColor = new Color(0.85f, 0.4f, 1f), // Tím
            Icon = GetIcon("Icon_Magnet"),
            IsSold = false,
            OnBuy = () => {
                BrotatoLevelSystem levelSys = BrotatoLevelSystem.Instance ?? FindAnyObjectByType<BrotatoLevelSystem>();
                if (levelSys != null) levelSys.AddMagnetRadius(2.0f);
            }
        });

        // 7. Kính ngắm chí mạng (Crit Chance)
        pool.Add(new ShopItemOffer
        {
            Title = "[SCOPE] CRIT CHANCE",
            Description = "+6% Chance to deal 200% CRITICAL Damage!",
            Price = 40 + (_currentWaveNumber * 3),
            ThemeColor = new Color(1f, 0.65f, 0.1f), // Cam sáng
            Icon = GetIcon("Icon_Crit"),
            IsSold = false,
            OnBuy = () => {
                if (shooting != null) shooting.AddCritChance(0.06f);
            }
        });

        // 8. Răng nanh ma cà rồng (Life Steal)
        pool.Add(new ShopItemOffer
        {
            Title = "[FANGS] LIFE STEAL",
            Description = "+3% Chance to heal 1 HP per hit on enemies!",
            Price = 45 + (_currentWaveNumber * 4),
            ThemeColor = new Color(0.85f, 0.15f, 0.35f), // Đỏ rượu
            Icon = GetIcon("Icon_LifeSteal"),
            IsSold = false,
            OnBuy = () => {
                if (shooting != null) shooting.AddLifeSteal(0.03f);
            }
        });

        // 9. Máy thu hoạch (Harvesting)
        pool.Add(new ShopItemOffer
        {
            Title = "[HARVEST] HARVESTING",
            Description = "+10 Bonus Materials awarded after each Wave!",
            Price = 30 + (_currentWaveNumber * 2),
            ThemeColor = new Color(0.2f, 0.85f, 0.5f), // Xanh ngọc lục
            Icon = GetIcon("Icon_Harvest"),
            IsSold = false,
            OnBuy = () => {
                BrotatoLevelSystem levelSys = BrotatoLevelSystem.Instance ?? FindAnyObjectByType<BrotatoLevelSystem>();
                if (levelSys != null) levelSys.SpendMaterials(-10);
            }
        });

        // Giữ lại các thẻ đang được KHÓA (Locked) từ lượt trước
        List<ShopItemOffer> newOffers = new List<ShopItemOffer>();
        for (int i = 0; i < 4; i++)
        {
            if (i < _currentOffers.Count && _currentOffers[i] != null && _currentOffers[i].IsLocked && !_currentOffers[i].IsSold)
            {
                newOffers.Add(_currentOffers[i]);
            }
            else
            {
                newOffers.Add(null);
            }
        }

        // Điền các ô còn trống bằng vật phẩm ngẫu nhiên từ pool
        for (int i = 0; i < 4; i++)
        {
            if (newOffers[i] == null && pool.Count > 0)
            {
                int rnd = UnityEngine.Random.Range(0, pool.Count);
                newOffers[i] = pool[rnd];
                pool.RemoveAt(rnd);
            }
        }

        _currentOffers = newOffers;
    }



    // =========================================================================
    // 3. CẬP NHẬT GIAO DIỆN SHOP (CANVAS TMP)


    // =========================================================================
    private void UpdateShopUI()
    {
        BrotatoLevelSystem levelSys = BrotatoLevelSystem.Instance ?? FindAnyObjectByType<BrotatoLevelSystem>();
        int playerMaterials = levelSys != null ? levelSys.GetMaterials() : 0;

        if (waveTitleText != null) waveTitleText.text = $"WAVE {_currentWaveNumber} COMPLETED!";
        if (materialsText != null) materialsText.text = $"COINS: 🪙 {playerMaterials}";
        if (rerollCostText != null) rerollCostText.text = $"Reroll ({_currentRerollCost})";
        if (nextWaveButtonText != null) nextWaveButtonText.text = $"NEXT WAVE {_currentWaveNumber + 1} >";

        if (rerollButton != null)
        {
            rerollButton.interactable = playerMaterials >= _currentRerollCost;
        }

        // Cập nhật 4 Card UI
        for (int i = 0; i < shopCardsUI.Length && i < _currentOffers.Count; i++)
        {
            ShopCardUI card = shopCardsUI[i];
            ShopItemOffer offer = _currentOffers[i];
            if (card == null || card.cardRoot == null) continue;

            card.cardRoot.SetActive(true);
            if (card.nameText != null) card.nameText.text = offer.IsLocked ? $"🔒 {offer.Title}" : offer.Title;
            if (card.descText != null) card.descText.text = offer.Description;
            if (card.priceText != null) card.priceText.text = offer.IsSold ? "SOLD OUT" : (offer.IsLocked ? $"{offer.Price} COINS [LOCKED]" : $"{offer.Price} COINS");
            if (card.borderOrIcon != null)
            {
                card.borderOrIcon.color = offer.ThemeColor;
            }

            if (card.itemIcon != null)
            {
                if (offer.Icon != null)
                {
                    card.itemIcon.gameObject.SetActive(true);
                    card.itemIcon.sprite = offer.Icon;
                    if (offer.Title.Contains("MEDIC"))
                    {
                        card.itemIcon.color = Color.white;
                    }
                    else
                    {
                        card.itemIcon.color = offer.ThemeColor;
                    }
                }
                else
                {
                    card.itemIcon.gameObject.SetActive(false);
                }
            }

            if (card.lockButton != null)
            {
                card.lockButton.onClick.RemoveAllListeners();
                int itemIndex = i;
                card.lockButton.onClick.AddListener(() => ToggleLock(itemIndex));
            }

            if (card.lockButtonText != null)
            {
                card.lockButtonText.text = offer.IsLocked ? "LOCKED" : "LOCK";
            }

            if (card.buyButton != null)
            {
                card.buyButton.interactable = !offer.IsSold && playerMaterials >= offer.Price;
                card.buyButton.onClick.RemoveAllListeners();

                int itemIndex = i;
                card.buyButton.onClick.AddListener(() => BuyItem(itemIndex));
            }

            if (card.buyButtonText != null)
            {
                card.buyButtonText.text = offer.IsSold ? "SOLD" : "BUY";
            }
        }
    }

    public void ToggleLock(int index)
    {
        if (index < 0 || index >= _currentOffers.Count || _currentOffers[index] == null) return;
        _currentOffers[index].IsLocked = !_currentOffers[index].IsLocked;
        UpdateShopUI();
    }

    private void BuyItem(int index)
    {
        if (index < 0 || index >= _currentOffers.Count) return;
        ShopItemOffer offer = _currentOffers[index];
        if (offer.IsSold) return;

        BrotatoLevelSystem levelSys = BrotatoLevelSystem.Instance ?? FindAnyObjectByType<BrotatoLevelSystem>();
        if (levelSys != null && levelSys.SpendMaterials(offer.Price))
        {
            offer.IsSold = true;
            offer.OnBuy?.Invoke();
            UpdateShopUI();
        }
    }

    private void OnRerollClicked()
    {
        BrotatoLevelSystem levelSys = BrotatoLevelSystem.Instance ?? FindAnyObjectByType<BrotatoLevelSystem>();
        if (levelSys != null && levelSys.SpendMaterials(_currentRerollCost))
        {
            _currentRerollCost += 5; // Tăng dần giá đổi hàng
            GenerateShopOffers();
            UpdateShopUI();
        }
    }

    private void OnNextWaveClicked()
    {
        CloseShop();

        if (ArenaManager.Instance != null)
        {
            ArenaManager.Instance.StartWave(_currentWaveNumber + 1);
        }
    }



    // =========================================================================
    // 4. GIAO DIỆN ONGUI DỰ PHÒNG (NẾU CHƯA GÁN CANVAS UI THÌ TỰ ĐỘNG HIỆN ĐẸP MẮT)


    // =========================================================================
    private void OnGUI()
    {
        if (!_isShopOpen) return;
        if (shopPanel != null && shopPanel.activeInHierarchy) return;

        BrotatoLevelSystem levelSys = BrotatoLevelSystem.Instance ?? FindAnyObjectByType<BrotatoLevelSystem>();
        int playerMaterials = levelSys != null ? levelSys.GetMaterials() : 0;

        float screenW = Screen.width;
        float screenH = Screen.height;

        // Nền tối mờ toàn màn hình
        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.DrawTexture(new Rect(0, 0, screenW, screenH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Khung Cửa Hàng ở giữa màn hình
        float panelW = Mathf.Min(960, screenW - 40);
        float panelH = Mathf.Min(620, screenH - 40);
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
        titleStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
        GUI.Label(new Rect(panelX, panelY + 20, panelW, 45), $"WAVE {_currentWaveNumber} COMPLETED - SURVIVAL SHOP", titleStyle);

        // Số tiền
        GUIStyle moneyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        moneyStyle.normal.textColor = new Color(0.2f, 1f, 0.55f);
        GUI.Label(new Rect(panelX, panelY + 65, panelW, 35), $"Available Coins: 🪙 {playerMaterials} Coins", moneyStyle);

        // 4 Thẻ hàng
        float cardGap = 15f;
        float cardW = (panelW - 60f - (cardGap * 3f)) / 4f;
        float cardH = 340f;
        float cardY = panelY + 115f;

        for (int i = 0; i < _currentOffers.Count; i++)
        {
            ShopItemOffer item = _currentOffers[i];
            float cardX = panelX + 30f + (i * (cardW + cardGap));
            Rect cardRect = new Rect(cardX, cardY, cardW, cardH);

            // Khung thẻ
            GUI.color = item.IsSold ? new Color(0.3f, 0.3f, 0.3f, 0.5f) : new Color(0.15f, 0.18f, 0.24f, 0.95f);
            GUI.Box(cardRect, "");
            GUI.color = Color.white;

            // Tên vật phẩm
            GUIStyle itemNameStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                wordWrap = true
            };
            itemNameStyle.normal.textColor = item.IsSold ? Color.gray : item.ThemeColor;
            GUI.Label(new Rect(cardX + 8, cardY + 15, cardW - 16, 50), item.Title, itemNameStyle);

            // Mô tả
            GUIStyle descStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            descStyle.normal.textColor = item.IsSold ? Color.gray : Color.white;
            GUI.Label(new Rect(cardX + 10, cardY + 70, cardW - 20, 150), item.Description, descStyle);

            // Giá tiền & Nút Mua & Nút Khóa (Lock Item chuẩn Brotato)
            if (!item.IsSold)
            {
                bool canAfford = playerMaterials >= item.Price;
                GUI.color = canAfford ? new Color(0.3f, 0.85f, 0.4f) : new Color(0.5f, 0.5f, 0.5f);
                if (GUI.Button(new Rect(cardX + 15, cardY + cardH - 85, cardW - 30, 42), $"BUY (🪙 {item.Price})"))
                {
                    if (canAfford)
                    {
                        BuyItem(i);
                    }
                }

                GUI.color = item.IsLocked ? new Color(1f, 0.85f, 0.2f) : new Color(0.65f, 0.65f, 0.65f);
                if (GUI.Button(new Rect(cardX + 15, cardY + cardH - 38, cardW - 30, 30), item.IsLocked ? "🔒 LOCKED" : "🔓 LOCK"))
                {
                    ToggleLock(i);
                }
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = Color.gray;
                GUI.Box(new Rect(cardX + 15, cardY + cardH - 85, cardW - 30, 75), "SOLD OUT");
                GUI.color = Color.white;
            }
        }

        // 2 Nút hành động phía dưới
        float btnY = panelY + panelH - 80f;

        // Nút Đổi Hàng
        bool canReroll = playerMaterials >= _currentRerollCost;
        GUI.color = canReroll ? new Color(0.9f, 0.6f, 0.2f) : Color.gray;
        if (GUI.Button(new Rect(panelX + 40, btnY, 260, 52), $"🎲 Reroll ({_currentRerollCost} 🪙)"))
        {
            if (canReroll)
            {
                OnRerollClicked();
            }
        }

        // Nút Vào Wave Tiếp Theo
        GUI.color = new Color(0.2f, 0.8f, 1f);
        if (GUI.Button(new Rect(panelX + panelW - 300, btnY, 260, 52), $"NEXT WAVE {_currentWaveNumber + 1} ▶"))
        {
            OnNextWaveClicked();
        }

        GUI.color = Color.white;
    }

    private readonly System.Collections.Generic.Dictionary<string, Sprite> _iconCache = new System.Collections.Generic.Dictionary<string, Sprite>();

    public Sprite GetIcon(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (_iconCache.TryGetValue(key, out var s) && s != null) return s;

#if UNITY_EDITOR
        string path = "Assets/UI/ShopIcons/" + key + ".png";
        s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s != null)
        {
            _iconCache[key] = s;
            return s;
        }
#endif
        return null;
    }

    private void LoadMedkitAsset()
    {
#if UNITY_EDITOR
        var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/UI/Medkid UI/vecteezy_first-aid-kit-pixel-art_72639614.png");
        foreach (var a in assets)
        {
            if (a is Sprite s) { _cachedMedkitSprite = s; break; }
        }
#endif
    }
}
