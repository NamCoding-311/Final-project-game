using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Định nghĩa một Chặng trong hành trình (Stage / Biome)
[Serializable]
public class BackgroundStage
{
    public string stageName = "Chặng 1";
    public float startDistance = 0f;
    public Sprite stageSprite;

    [Tooltip("Độ nâng/hạ trục Y riêng cho chặng này (Chặng 2 & 3 nên đặt khoảng +1.0 đến +1.5 để đẩy bờ sông & thành phố lên trên lan can)")]
    public float yOffset = 0f;

    [Tooltip("Tỷ lệ phóng to riêng cho chặng này (Chặng 2 & 3 nên đặt khoảng 1.45 để phủ kín nóc màn hình, không bị hở dải màu be)")]
    public float scaleFactor = 1f;
}

// Quản lý Background Parallax vô tận:
// Tự động căn đáy ảnh vào bờ kè (xóa sạch dải cam hở), tự đo chiều rộng từng ảnh (chống trùng lặp mặt trăng/nhà cửa)
public class InfiniteParallaxBackground : MonoBehaviour
{
    [Header("Camera & Player Reference")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform playerTransform;

    [Header("Parallax Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float parallaxEffect = 0.5f;

    [Header("Stages Configuration (Hệ thống Chặng)")]
    [SerializeField] private BackgroundStage[] stages;

    [Header("Sync with Stage Level Manager")]
    // Tự động đồng bộ chuyển cảnh với StageLevelManager (chuyển background ngay khi qua màn đánh Boss)
    [SerializeField] private bool syncWithStageLevelManager = true;

    [Header("Tinh Chinh Do Cao Background (Manual Height Fix)")]
    [Tooltip("Kéo tăng số này để nâng toàn bộ background lên khỏi mặt đường (khuyên dùng khoảng 2.0 đến 2.5 để thấy rõ sông và che kín dải màu be phía trên)")]
    [SerializeField] private float globalYOffset = 2.2f;

    [Header("Seamless Looping Elements")]
    [SerializeField] private Transform[] backgroundLayers;

    private float _lastCameraX;
    private float _startPosY;
    private float _startPlayerX;
    private int _currentStageIndex = 0;
    private float _initialBottomY;
    private StageLevelManager _stageLevelManager;

    public float GetStageYOffset(int index)
    {
        float stageOffset = 0f;
        if (stages != null && index >= 0 && index < stages.Length)
        {
            stageOffset = stages[index].yOffset;
        }
        return stageOffset + globalYOffset;
    }

    private void OnValidate()
    {
        if (Application.isPlaying && backgroundLayers != null && backgroundLayers.Length > 0)
        {
            ApplyStageSprites(_currentStageIndex);
        }
    }

    public float GetStageScale(int index)
    {
        if (stages == null || index < 0 || index >= stages.Length) return 1f;
        // Nếu người dùng chưa chỉnh hoặc scale <= 0.05 mà ảnh thấp (< 800px) thì tự động phóng 1.45f để phủ kín nóc
        if (stages[index].scaleFactor <= 0.05f || (stages[index].scaleFactor == 1f && stages[index].stageSprite != null && stages[index].stageSprite.rect.height < 800))
        {
            return 1.45f;
        }
        return stages[index].scaleFactor;
    }

    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;

        if (targetCamera != null)
        {
            _lastCameraX = targetCamera.transform.position.x;
        }

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        if (playerTransform != null)
        {
            _startPlayerX = playerTransform.position.x;
        }

        _startPosY = transform.position.y;

        // Tự động tìm tất cả các tấm ảnh con nếu chưa gán
        if (backgroundLayers == null || backgroundLayers.Length == 0)
        {
            int childCount = transform.childCount;
            if (childCount > 0)
            {
                backgroundLayers = new Transform[childCount];
                for (int i = 0; i < childCount; i++)
                {
                    backgroundLayers[i] = transform.GetChild(i);
                }
            }
        }

        // Tự động nhân bản thêm ảnh con nếu có ít hơn 5 tấm (đảm bảo luôn phủ kín cả vùng phía sau xe)
        if (backgroundLayers != null && backgroundLayers.Length > 0 && backgroundLayers.Length < 5)
        {
            List<Transform> layerList = new List<Transform>(backgroundLayers);
            while (layerList.Count < 5)
            {
                Transform clone = Instantiate(backgroundLayers[0], transform);
                clone.name = $"{backgroundLayers[0].name}_Clone_{layerList.Count}";
                layerList.Add(clone);
            }
            backgroundLayers = layerList.ToArray();
        }

        // Đo độ cao đáy chuẩn của ảnh đầu tiên để khóa đáy tất cả các ảnh vào đây (Chống hở dải màu cam)
        if (backgroundLayers != null && backgroundLayers.Length > 0)
        {
            SpriteRenderer sr0 = backgroundLayers[0].GetComponent<SpriteRenderer>();
            if (sr0 != null && sr0.sprite != null)
            {
                // Tọa độ Y mép dưới cùng của bức ảnh gốc
                _initialBottomY = backgroundLayers[0].position.y - sr0.bounds.extents.y;
            }
        }

        // Căn chỉnh vị trí ban đầu của các tấm ảnh nối tiếp nhau
        AlignAllSegments();

        // Đồng bộ chặng với StageLevelManager nếu được bật
        if (syncWithStageLevelManager)
        {
            _stageLevelManager = FindAnyObjectByType<StageLevelManager>();
            if (_stageLevelManager != null)
            {
                _currentStageIndex = _stageLevelManager.GetCurrentStageIndex();
            }
        }

        // Áp dụng ảnh cho tất cả các tấm ban đầu
        ApplyStageSprites(_currentStageIndex);
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        float cameraX = targetCamera.transform.position.x;
        float deltaCameraX = cameraX - _lastCameraX;
        _lastCameraX = cameraX;

        // 1. Di chuyển toàn bộ cụm Background theo hệ số Parallax
        float moveDistance = deltaCameraX * (1f - parallaxEffect);
        transform.position += Vector3.right * moveDistance;

        // 2. Tính quãng đường xe đã chạy được (mét) nếu không đồng bộ trực tiếp theo Event
        if (!syncWithStageLevelManager)
        {
            float currentDistance = playerTransform != null ? (playerTransform.position.x - _startPlayerX) : (cameraX - _lastCameraX);
            UpdateCurrentStage(currentDistance);
        }

        // 3. Tái chế và luân chuyển các tấm ảnh con khi bị tụt lại phía sau Camera
        if (backgroundLayers != null && backgroundLayers.Length > 1)
        {
            for (int i = 0; i < backgroundLayers.Length; i++)
            {
                Transform seg = backgroundLayers[i];
                SpriteRenderer sr = seg.GetComponent<SpriteRenderer>();
                float currentSegWidth = (sr != null && sr.sprite != null) ? sr.bounds.size.x : 17.74f;

                // Chỉ tái chế khi tấm ảnh đã nằm lại rất xa phía sau Camera (> 2.2 lần chiều rộng ~ 40m)
                // Giúp vùng phía sau xe luôn có ảnh nền hoàng hôn bao phủ, không bao giờ bị cắt cụt lộ khoảng trống xám
                if (cameraX - seg.position.x > currentSegWidth * 2.2f)
                {
                    // Tìm tấm ảnh nằm xa nhất phía trước
                    Transform furthestSeg = backgroundLayers[0];
                    float maxRightX = float.MinValue;

                    for (int j = 0; j < backgroundLayers.Length; j++)
                    {
                        if (backgroundLayers[j] != seg && backgroundLayers[j].position.x > maxRightX)
                        {
                            maxRightX = backgroundLayers[j].position.x;
                            furthestSeg = backgroundLayers[j];
                        }
                    }

                    // Đo chiều rộng của tấm nằm xa nhất để nối tiếp chính xác (không chồng chéo, không nhân đôi trăng)
                    SpriteRenderer furthestSr = furthestSeg.GetComponent<SpriteRenderer>();
                    float furthestWidth = (furthestSr != null && furthestSr.sprite != null) ? furthestSr.bounds.size.x : currentSegWidth;

                    // Đổi sang Sprite của chặng hiện tại
                    UpdateSegmentSprite(seg);

                    // Tính lại kích thước sau khi đổi Sprite
                    if (sr != null && sr.sprite != null)
                    {
                        currentSegWidth = sr.bounds.size.x;
                        float newHalfHeight = sr.bounds.extents.y;
                        float stageYOffset = GetStageYOffset(_currentStageIndex);

                        // KHÓA ĐÁY ẢNH VÀO BỜ KÈ VỚI Y OFFSET VÀ SCALE CHUẨN
                        float lockedPosY = _initialBottomY + newHalfHeight + stageYOffset;
                        seg.position = new Vector3(maxRightX + furthestWidth, lockedPosY, seg.position.z);
                    }
                    else
                    {
                        seg.position = new Vector3(maxRightX + furthestWidth, seg.position.y, seg.position.z);
                    }
                }
            }
        }
    }

    // Căn xếp các tấm ảnh nối tiếp nhau ngay từ đầu
    private void AlignAllSegments()
    {
        if (backgroundLayers == null || backgroundLayers.Length == 0) return;

        float stageYOffset = GetStageYOffset(_currentStageIndex);
        float targetScale = GetStageScale(_currentStageIndex);

        for (int i = 0; i < backgroundLayers.Length; i++)
        {
            Transform curr = backgroundLayers[i];
            if (curr == null) continue;
            SpriteRenderer currSr = curr.GetComponent<SpriteRenderer>();
            if (currSr != null && currSr.sprite != null)
            {
                curr.localScale = new Vector3(targetScale, targetScale, 1f);
                float currHalfHeight = currSr.bounds.extents.y;
                float lockedPosY = _initialBottomY + currHalfHeight + stageYOffset;

                if (i > 0)
                {
                    SpriteRenderer prevSr = backgroundLayers[i - 1].GetComponent<SpriteRenderer>();
                    float prevWidth = (prevSr != null && prevSr.sprite != null) ? prevSr.bounds.size.x : 17.74f;
                    curr.position = new Vector3(backgroundLayers[i - 1].position.x + prevWidth, lockedPosY, curr.position.z);
                }
                else
                {
                    curr.position = new Vector3(curr.position.x, lockedPosY, curr.position.z);
                }
            }
        }
    }

    // Xác định chặng hiện tại dựa trên quãng đường đã đi (chỉ dùng khi tắt syncWithStageLevelManager)
    private void UpdateCurrentStage(float currentDistance)
    {
        if (stages == null || stages.Length == 0) return;

        for (int i = stages.Length - 1; i >= 0; i--)
        {
            if (currentDistance >= stages[i].startDistance)
            {
                if (_currentStageIndex != i)
                {
                    _currentStageIndex = i;
                    ApplyStageSprites(_currentStageIndex);
                }
                break;
            }
        }
    }

    // Cập nhật Sprite và Scale cho tấm ảnh khi nhảy lên phía trước
    private void UpdateSegmentSprite(Transform seg)
    {
        if (stages == null || stages.Length == 0 || _currentStageIndex >= stages.Length) return;

        BackgroundStage stage = stages[_currentStageIndex];
        if (stage.stageSprite == null) return;

        SpriteRenderer sr = seg.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (sr.sprite != stage.stageSprite)
            {
                sr.sprite = stage.stageSprite;
            }
            float targetScale = GetStageScale(_currentStageIndex);
            seg.localScale = new Vector3(targetScale, targetScale, 1f);
        }
    }

    // Chuyển đổi sang Chặng mới (được gọi từ StageLevelManager khi bước lên xe chuyển chặng)
    public void SwitchToStage(int newStageIndex, float fadeDuration = 2.0f)
    {
        if (stages == null || stages.Length == 0) return;
        newStageIndex = Mathf.Clamp(newStageIndex, 0, stages.Length - 1);
        if (_currentStageIndex == newStageIndex) return;

        _currentStageIndex = newStageIndex;

        if (fadeDuration > 0.1f)
        {
            StopAllCoroutines();
            StartCoroutine(CrossfadeStageCoroutine(newStageIndex, fadeDuration));
        }
        else
        {
            ApplyStageSprites(newStageIndex);
        }
    }

    // Hiệu ứng hòa trộn mờ dần (Crossfade) giữa 2 background của 2 chặng
    private IEnumerator CrossfadeStageCoroutine(int newStageIndex, float duration)
    {
        BackgroundStage newStage = stages[newStageIndex];
        Sprite newSprite = newStage.stageSprite;
        if (newSprite == null) yield break;

        float newScale = GetStageScale(newStageIndex);
        float newYOffset = GetStageYOffset(newStageIndex);

        List<SpriteRenderer> overlays = new List<SpriteRenderer>();

        if (backgroundLayers != null)
        {
            foreach (Transform seg in backgroundLayers)
            {
                if (seg == null) continue;
                Transform overlayTr = seg.Find("StageFadeOverlay");
                GameObject overlayGo;
                if (overlayTr == null)
                {
                    overlayGo = new GameObject("StageFadeOverlay");
                    overlayGo.transform.SetParent(seg, false);
                }
                else
                {
                    overlayGo = overlayTr.gameObject;
                }

                SpriteRenderer mainSr = seg.GetComponent<SpriteRenderer>();
                SpriteRenderer overlaySr = overlayGo.GetComponent<SpriteRenderer>();
                if (overlaySr == null) overlaySr = overlayGo.AddComponent<SpriteRenderer>();

                overlaySr.sprite = newSprite;
                overlaySr.sortingLayerID = mainSr != null ? mainSr.sortingLayerID : 0;
                overlaySr.sortingOrder = mainSr != null ? mainSr.sortingOrder + 1 : 1;
                if (mainSr != null) overlaySr.material = mainSr.material;
                overlaySr.color = new Color(1f, 1f, 1f, 0f);

                float parentScale = seg.localScale.y > 0.01f ? seg.localScale.y : 1f;
                float relScale = newScale / parentScale;
                overlayGo.transform.localScale = new Vector3(relScale, relScale, 1f);

                float curYOffset = GetStageYOffset(_currentStageIndex);
                float curNewHalfHeight = (mainSr != null && mainSr.sprite != null) ? mainSr.bounds.extents.y : 4.435f;
                float targetNewHalfHeight = (newSprite.bounds.extents.y / newScale) * newScale;
                float diffY = (targetNewHalfHeight - curNewHalfHeight) + (newYOffset - curYOffset);
                overlayGo.transform.localPosition = new Vector3(0f, diffY / parentScale, -0.01f);

                overlayGo.SetActive(true);
                overlays.Add(overlaySr);
            }
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            foreach (var ov in overlays)
            {
                if (ov != null) ov.color = new Color(1f, 1f, 1f, t);
            }
            yield return null;
        }

        // Hoàn tất crossfade: Gán chính thức ảnh mới cho các tấm chính
        ApplyStageSprites(newStageIndex);

        // Ẩn overlay
        foreach (var ov in overlays)
        {
            if (ov != null) ov.gameObject.SetActive(false);
        }
    }

    // Áp dụng ảnh của chặng cho tất cả các tấm đang hiển thị và khóa chuẩn đáy ảnh vào bờ kè
    public void ApplyStageSprites(int stageIndex)
    {
        if (stages == null || stageIndex < 0 || stageIndex >= stages.Length) return;
        BackgroundStage stage = stages[stageIndex];
        if (stage.stageSprite == null) return;

        float targetScale = GetStageScale(stageIndex);
        float stageYOffset = GetStageYOffset(stageIndex);

        if (backgroundLayers != null)
        {
            foreach (Transform seg in backgroundLayers)
            {
                if (seg == null) continue;
                SpriteRenderer sr = seg.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = stage.stageSprite;
                    seg.localScale = new Vector3(targetScale, targetScale, 1f);

                    float newHalfHeight = sr.bounds.extents.y;
                    float lockedPosY = _initialBottomY + newHalfHeight + stageYOffset;
                    seg.position = new Vector3(seg.position.x, lockedPosY, seg.position.z);
                }
            }
        }
    }

    // Áp dụng ảnh của chặng đầu tiên khi bắt đầu game
    public void ApplyInitialStageSprites()
    {
        ApplyStageSprites(0);
    }

    [ContextMenu("Tự Động Cân Chỉnh Fit Tỷ Lệ & Tọa Độ Cho Tất Cả Chặng")]
    public void AutoFitAndAlignAllStages()
    {
        if (stages == null) return;
        for (int i = 0; i < stages.Length; i++)
        {
            if (stages[i].stageSprite != null && stages[i].stageSprite.rect.height < 800)
            {
                stages[i].scaleFactor = 1.45f;
                stages[i].yOffset = 1.1f;
            }
            else
            {
                stages[i].scaleFactor = 1.0f;
                stages[i].yOffset = 0f;
            }
        }
        ApplyStageSprites(_currentStageIndex);
        AlignAllSegments();
        Debug.Log("[InfiniteParallaxBackground] Đã tự động cân chỉnh Scale và Y Offset chuẩn đẹp cho tất cả các chặng!");
    }

    [ContextMenu("TEST: Đổi Sang Nền Chặng 1 (Sáng - frame)")]
    public void TestBgStage1() => SwitchToStage(0, 2.0f);

    [ContextMenu("TEST: Đổi Sang Nền Chặng 2 (Hoàng Hôn - 2ndpng)")]
    public void TestBgStage2() => SwitchToStage(1, 2.0f);

    [ContextMenu("TEST: Đổi Sang Nền Chặng 3 (Đêm - png3)")]
    public void TestBgStage3() => SwitchToStage(2, 2.0f);
}
