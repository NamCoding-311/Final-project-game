using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Thanh Tiến Trình Chặng Phong Cách Plants vs. Zombies (PvZ Level Progress Bar):
// - Hiển thị khung gỗ bo góc và dải màu xanh lá cây tăng dần theo quãng đường xe đi.
// - Icon  trượt dọc theo mép thanh xanh kèm hiệu ứng nhấp nhô (bobbing) sống động.
// - Cắm các mốc cờ (Flag 1, Flag 2) ứng với Chặng 1 và Chặng 2.
// - Cắm icon Đầu Lâu (Skull) ở cuối thanh ứng với Đấu trường Boss cuối (Apex Boss).
// - Khi Đầu Zombie chạm tới lá cờ, lá cờ sẽ rung lên / phóng to cảnh báo đợt quái dồn tới!
public class ProgressBarUI : MonoBehaviour
{
    public static ProgressBarUI Instance { get; private set; }

    [Header("Target & Movement Tracking")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float totalLevelDistance = 500f; // Quãng đường tổng của Map (m)

    [Header("UI Bar Components")]
    [SerializeField] private Image fillImage;
    [SerializeField] private RectTransform fillContainerRect;

    [Header("Markers & Checkpoints")]
    [SerializeField] private RectTransform IconHeadMarker;
    [SerializeField] private RectTransform flag1Marker;
    [SerializeField] private RectTransform flag2Marker;
    [SerializeField] private RectTransform skullMarker;

    [Header("Checkpoint Distances (m)")]
    [SerializeField] private float stage1Distance = 150f;
    [SerializeField] private float stage2Distance = 320f;

    [Header("Optional Text")]
    [SerializeField] private TextMeshProUGUI progressPercentText;

    private float _startX;
    private bool _flag1Triggered = false;
    private bool _flag2Triggered = false;
    private bool _skullTriggered = false;
    private StageLevelManager _stageLevelManager;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ResolveUIReferences();
        FindPlayer();
        AutoDetectStageDistances();
        PositionCheckpointMarkers();

        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 0f;
        }

        UpdateMarkerPositions(0f);
    }

    // Tự động tìm kiếm các thành phần con nếu chưa kéo trong Inspector
    private void ResolveUIReferences()
    {
        if (fillContainerRect == null)
        {
            Transform found = transform.Find("FillContainer");
            if (found != null) fillContainerRect = found.GetComponent<RectTransform>();
        }

        if (fillContainerRect != null)
        {
            if (fillImage == null)
            {
                Transform foundFill = fillContainerRect.Find("Fill");
                if (foundFill != null) fillImage = foundFill.GetComponent<Image>();
            }

            if (IconHeadMarker == null)
            {
                Transform foundHead = fillContainerRect.Find("IconHeadMarker") 
                    ?? fillContainerRect.Find("IconHead") 
                    ?? fillContainerRect.Find("ZombieHead");
                if (foundHead != null) IconHeadMarker = foundHead.GetComponent<RectTransform>();
            }

            if (flag1Marker == null)
            {
                Transform foundF1 = fillContainerRect.Find("Flag1");
                if (foundF1 != null) flag1Marker = foundF1.GetComponent<RectTransform>();
            }

            if (flag2Marker == null)
            {
                Transform foundF2 = fillContainerRect.Find("Flag2");
                if (foundF2 != null) flag2Marker = foundF2.GetComponent<RectTransform>();
            }

            if (skullMarker == null)
            {
                Transform foundSkull = fillContainerRect.Find("Skull");
                if (foundSkull != null) skullMarker = foundSkull.GetComponent<RectTransform>();
            }
        }

        // Nếu Icon chưa gắn Sprite hình ảnh thì tạm ẩn component Image để không bị hiện ô vuông trắng
        if (IconHeadMarker != null)
        {
            Image headImg = IconHeadMarker.GetComponent<Image>();
            if (headImg != null)
            {
                headImg.enabled = (headImg.sprite != null);
            }
        }
    }

    private void FindPlayer()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        if (playerTransform != null)
        {
            _startX = playerTransform.position.x;
        }
    }

    // Tự động đọc mốc cự ly các chặng từ StageLevelManager nếu có trong Map
    private void AutoDetectStageDistances()
    {
        _stageLevelManager = FindAnyObjectByType<StageLevelManager>();
        if (_stageLevelManager != null)
        {
            var stages = _stageLevelManager.GetStages();
            if (stages != null && stages.Length >= 3)
            {
                stage1Distance = stages[0].stageDistance;
                stage2Distance = stages[1].stageDistance;
                totalLevelDistance = stages[2].stageDistance;
            }
        }
    }

    // Đặt vị trí các lá cờ và đầu lâu tương ứng trên thanh theo tỉ lệ % cự ly
    private void PositionCheckpointMarkers()
    {
        if (fillContainerRect == null) return;
        float barWidth = fillContainerRect.rect.width;

        if (flag1Marker != null && totalLevelDistance > 0f)
        {
            float ratio1 = Mathf.Clamp01(stage1Distance / totalLevelDistance);
            flag1Marker.anchoredPosition = new Vector2(ratio1 * barWidth, flag1Marker.anchoredPosition.y);
        }

        if (flag2Marker != null && totalLevelDistance > 0f)
        {
            float ratio2 = Mathf.Clamp01(stage2Distance / totalLevelDistance);
            flag2Marker.anchoredPosition = new Vector2(ratio2 * barWidth, flag2Marker.anchoredPosition.y);
        }

        if (skullMarker != null)
        {
            skullMarker.anchoredPosition = new Vector2(barWidth, skullMarker.anchoredPosition.y);
        }
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        float currentDist = Mathf.Max(0f, playerTransform.position.x - _startX);
        float progress = Mathf.Clamp01(currentDist / Mathf.Max(1f, totalLevelDistance));

        // Cập nhật dải màu xanh lá cây
        if (fillImage != null)
        {
            fillImage.fillAmount = progress;
        }

        // Cập nhật vị trí Đầu Zombie
        UpdateMarkerPositions(progress);

        // Kiểm tra chạm cờ Chặng 1
        if (!_flag1Triggered && currentDist >= stage1Distance)
        {
            _flag1Triggered = true;
            if (flag1Marker != null) StartCoroutine(AnimateFlagTrigger(flag1Marker));
        }

        // Kiểm tra chạm cờ Chặng 2
        if (!_flag2Triggered && currentDist >= stage2Distance)
        {
            _flag2Triggered = true;
            if (flag2Marker != null) StartCoroutine(AnimateFlagTrigger(flag2Marker));
        }

        // Kiểm tra chạm Đầu Lâu (Boss)
        if (!_skullTriggered && progress >= 0.98f)
        {
            _skullTriggered = true;
            if (skullMarker != null) StartCoroutine(AnimateSkullTrigger(skullMarker));
        }

        // Cập nhật số % hiển thị nếu có
        if (progressPercentText != null)
        {
            progressPercentText.text = $"{Mathf.FloorToInt(progress * 100f)}%";
        }
    }

    // Di chuyển Đầu Zombie trượt theo mép thanh xanh kèm nhịp nhấp nhô bobbing
    private void UpdateMarkerPositions(float progress)
    {
        if (IconHeadMarker == null || fillContainerRect == null) return;

        float barWidth = fillContainerRect.rect.width;
        float targetX = progress * barWidth;

        // Nhịp nhấp nhô đặc trưng PvZ khi zombie di chuyển
        float bobbingY = Mathf.Sin(Time.time * 7f) * 3f;

        IconHeadMarker.anchoredPosition = new Vector2(targetX, bobbingY);

        // Đảm bảo nếu chưa gán Sprite thì không bị hiện ô vuông màu trắng
        Image headImg = IconHeadMarker.GetComponent<Image>();
        if (headImg != null)
        {
            bool hasSprite = (headImg.sprite != null);
            if (headImg.enabled != hasSprite)
            {
                headImg.enabled = hasSprite;
            }
        }
    }

    // Hiệu ứng lá cờ nhấp nháy và giật nảy khi Đầu Zombie chạm mốc
    private IEnumerator AnimateFlagTrigger(RectTransform flag)
    {
        Vector3 origScale = flag.localScale;
        Image img = flag.GetComponent<Image>();
        Color origColor = img != null ? img.color : Color.white;

        for (int i = 0; i < 4; i++)
        {
            flag.localScale = origScale * 1.35f;
            if (img != null) img.color = Color.yellow;
            yield return new WaitForSeconds(0.12f);

            flag.localScale = origScale * 0.95f;
            if (img != null) img.color = Color.red;
            yield return new WaitForSeconds(0.12f);
        }

        flag.localScale = origScale;
        if (img != null) img.color = origColor;
    }

    // Hiệu ứng Đầu Lâu rung lắc dữ dội khi chạm vạch Boss
    private IEnumerator AnimateSkullTrigger(RectTransform skull)
    {
        Vector3 origScale = skull.localScale;
        Vector2 origPos = skull.anchoredPosition;

        for (int i = 0; i < 12; i++)
        {
            skull.localScale = origScale * Random.Range(1.2f, 1.45f);
            skull.anchoredPosition = origPos + new Vector2(Random.Range(-3f, 3f), Random.Range(-3f, 3f));
            yield return new WaitForSeconds(0.06f);
        }

        skull.localScale = origScale;
        skull.anchoredPosition = origPos;
    }
}
