using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Giao diện hiển thị hồi chiêu Nitro / Dash chuyên nghiệp (Nhóm 1):
// - Thanh đo năng lượng Nitro (Nitro Cooldown Gauge) kiểu cơ khí quân sự hậu tận thế.
// - Tự động liên kết với Player3LaneMovement và gắn vào HUD Canvas dưới thanh HPBar.
// - Khi đang hồi chiêu (1.5s): Thanh dải màu Cyan nạp dần từ 0 -> 100% kèm số giây đếm ngược.
// - Khi sẵn sàng (READY): Bùng sáng màu Vàng Cam Nitro kèm hiệu ứng nhịp thở / chớp giật báo hiệu người chơi nhấn SHIFT.
// - Khi đang lướt (DASHING): Đổi sang màu Trắng/Điện tử chớp nhoáng với chữ "BOOSTING!".
public class DashCooldownHUD : MonoBehaviour
{
    public static DashCooldownHUD Instance { get; private set; }

    [Header("Target Movement")]
    [SerializeField] private Player3LaneMovement playerMovement;

    [Header("UI Customization")]
    [SerializeField] private bool autoCreateHUD = true;
    [SerializeField] private Vector2 hudOffsetFromHPBar = new Vector2(0f, -54f);
    [SerializeField] private Vector2 hudSize = new Vector2(280f, 22f);

    [Header("Colors")]
    [SerializeField] private Color chargingColor = new Color(0f, 0.85f, 1f, 0.95f);   // Electric Cyan
    [SerializeField] private Color readyColor = new Color(1f, 0.65f, 0.1f, 1f);        // Flame Gold
    [SerializeField] private Color boostingColor = new Color(1f, 1f, 0.3f, 1f);       // Bright Flash Yellow
    [SerializeField] private Color frameBgColor = new Color(0.12f, 0.13f, 0.16f, 0.95f);

    [Header("Sprite Icon")]
    [SerializeField] private Sprite nitroIconSprite;

    // Runtime UI Components
    private GameObject _hudRoot;
    private Image _fillImage;
    private Image _iconImage;
    private TextMeshProUGUI _statusText;
    private RectTransform _hudRect;

    private bool _wasReady = false;
    private float _pulseScale = 1f;

    // Cache textures cho OnGUI Fallback
    private Texture2D _guiBgTex;
    private Texture2D _guiFillTex;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        FindPlayerMovement();
        LoadDefaultIcon();

        if (autoCreateHUD)
        {
            SetupCanvasHUD();
        }
    }

    private void FindPlayerMovement()
    {
        if (playerMovement == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerMovement = player.GetComponent<Player3LaneMovement>();
            }
        }
    }

    private void LoadDefaultIcon()
    {
#if UNITY_EDITOR
        if (nitroIconSprite == null)
        {
            nitroIconSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/ffffff/transparent/1x1/delapouite/lightning-flame.png");
        }
#endif
    }

    // Tự động tìm Canvas và thanh HPBar để dựng thanh Nitro liền kề
    public void SetupCanvasHUD()
    {
        if (_hudRoot != null) return;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // Tìm HPBar làm mốc tọa độ
        GameObject hpBarObj = GameObject.Find("HPBar");
        RectTransform hpBarRect = hpBarObj != null ? hpBarObj.GetComponent<RectTransform>() : null;

        // 1. Tạo Root Panel
        _hudRoot = new GameObject("DashCooldownHUD");
        _hudRoot.transform.SetParent(canvas.transform, false);
        _hudRect = _hudRoot.AddComponent<RectTransform>();

        if (hpBarRect != null)
        {
            _hudRect.anchorMin = hpBarRect.anchorMin;
            _hudRect.anchorMax = hpBarRect.anchorMax;
            _hudRect.pivot = hpBarRect.pivot;
            _hudRect.anchoredPosition = hpBarRect.anchoredPosition + hudOffsetFromHPBar;
        }
        else
        {
            // Mặc định góc trên bên phải
            _hudRect.anchorMin = new Vector2(1f, 1f);
            _hudRect.anchorMax = new Vector2(1f, 1f);
            _hudRect.pivot = new Vector2(1f, 1f);
            _hudRect.anchoredPosition = new Vector2(-30f, -85f);
        }
        _hudRect.sizeDelta = hudSize;

        // 2. Nền khung (Background Frame)
        Image bgImg = _hudRoot.AddComponent<Image>();
        bgImg.color = frameBgColor;

        // Viền ngoài nhẹ
        Outline outline = _hudRoot.AddComponent<Outline>();
        outline.effectColor = new Color(0.25f, 0.28f, 0.35f, 0.9f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // 3. Icon Tia Sét / Nitro Lửa ở góc trái
        GameObject iconObj = new GameObject("NitroIcon");
        iconObj.transform.SetParent(_hudRoot.transform, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(-16f, 0f);
        iconRect.sizeDelta = new Vector2(28f, 28f);

        _iconImage = iconObj.AddComponent<Image>();
        if (nitroIconSprite != null)
        {
            _iconImage.sprite = nitroIconSprite;
            _iconImage.preserveAspect = true;
        }
        _iconImage.color = readyColor;

        // 4. Thanh Fill Bar
        GameObject fillObj = new GameObject("FillArea");
        fillObj.transform.SetParent(_hudRoot.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        _fillImage = fillObj.AddComponent<Image>();
        _fillImage.type = Image.Type.Filled;
        _fillImage.fillMethod = Image.FillMethod.Horizontal;
        _fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        _fillImage.color = readyColor;

        // 5. Text Trạng Thái
        GameObject textObj = new GameObject("StatusText");
        textObj.transform.SetParent(_hudRoot.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        _statusText = textObj.AddComponent<TextMeshProUGUI>();
        _statusText.alignment = TextAlignmentOptions.Center;
        _statusText.fontSize = 12f;
        _statusText.fontStyle = FontStyles.Bold;
        _statusText.color = Color.white;
        _statusText.enableWordWrapping = false;

        // Đổ bóng chữ sắc nét
        _statusText.outlineWidth = 0.25f;
        _statusText.outlineColor = Color.black;
    }

    private void Update()
    {
        if (playerMovement == null)
        {
            FindPlayerMovement();
            if (playerMovement == null) return;
        }

        // Cập nhật giá trị đo
        float progress = playerMovement.GetDashCooldownNormalized();
        float remaining = playerMovement.GetDashCooldownRemaining();
        bool isDashing = playerMovement.IsDashing();
        bool isReady = playerMovement.IsDashReady();

        // Kích hoạt nhịp giật phóng to khi vừa hồi chiêu xong (Ready Pulse)
        if (isReady && !_wasReady)
        {
            _pulseScale = 1.18f;
        }
        _wasReady = isReady;

        // Thu nhỏ mượt dần về kích thước chuẩn
        _pulseScale = Mathf.MoveTowards(_pulseScale, 1f, Time.deltaTime * 3f);

        // Cập nhật UI Canvas nếu đang tồn tại
        if (_hudRoot != null)
        {
            if (_hudRect != null)
            {
                _hudRect.localScale = Vector3.one * _pulseScale;
            }

            if (_fillImage != null)
            {
                _fillImage.fillAmount = progress;

                if (isDashing)
                {
                    _fillImage.color = boostingColor;
                }
                else if (isReady)
                {
                    // Hiệu ứng nhịp thở rực sáng khi sẵn sàng
                    float breath = 0.9f + (0.1f * Mathf.Sin(Time.time * 7f));
                    _fillImage.color = readyColor * breath;
                }
                else
                {
                    _fillImage.color = chargingColor;
                }
            }

            if (_iconImage != null)
            {
                _iconImage.color = isReady ? readyColor : (isDashing ? boostingColor : chargingColor);
                _iconImage.transform.localScale = Vector3.one * (isReady ? (1f + 0.1f * Mathf.Sin(Time.time * 6f)) : 1f);
            }

            if (_statusText != null)
            {
                if (isDashing)
                {
                    _statusText.text = "<color=#FFFF00>⚡ BOOSTING! ⚡</color>";
                }
                else if (isReady)
                {
                    _statusText.text = "<color=#FFAA00>★ NITRO READY [SHIFT] ★</color>";
                }
                else
                {
                    _statusText.text = $"NITRO: {remaining:F1}s [SHIFT]";
                }
            }
        }
    }

    // OnGUI Fallback: Luôn đảm bảo người chơi nhìn thấy thanh Nitro kể cả khi Canvas chưa nạp
    private void OnGUI()
    {
        if (_hudRoot != null && _hudRoot.activeInHierarchy) return;
        if (playerMovement == null) return;

        float progress = playerMovement.GetDashCooldownNormalized();
        float remaining = playerMovement.GetDashCooldownRemaining();
        bool isDashing = playerMovement.IsDashing();
        bool isReady = playerMovement.IsDashReady();

        if (_guiBgTex == null)
        {
            _guiBgTex = new Texture2D(1, 1);
            _guiBgTex.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.12f, 0.95f));
            _guiBgTex.Apply();
        }

        if (_guiFillTex == null)
        {
            _guiFillTex = new Texture2D(1, 1);
            _guiFillTex.SetPixel(0, 0, Color.white);
            _guiFillTex.Apply();
        }

        float barWidth = 260f;
        float barHeight = 22f;
        float x = Screen.width - barWidth - 30f;
        float y = 88f;

        // Vẽ viền nền
        GUI.DrawTexture(new Rect(x - 2, y - 2, barWidth + 4, barHeight + 4), _guiBgTex);

        // Vẽ Fill
        Color fillColor = isDashing ? boostingColor : (isReady ? readyColor : chargingColor);
        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(x, y, barWidth * progress, barHeight), _guiFillTex);
        GUI.color = Color.white;

        // Vẽ Text
        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.alignment = TextAnchor.MiddleCenter;
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.fontSize = 12;
        textStyle.normal.textColor = Color.white;

        string label = isDashing ? "⚡ BOOSTING! ⚡" : (isReady ? "★ NITRO READY [SHIFT] ★" : $"NITRO: {remaining:F1}s [SHIFT]");
        GUI.Label(new Rect(x, y, barWidth, barHeight), label, textStyle);
    }
}
