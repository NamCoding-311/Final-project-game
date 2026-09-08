using System;
using UnityEngine;

// Định nghĩa một Chặng trong hành trình (Stage / Biome)
[Serializable]
public class BackgroundStage
{
    public string stageName = "Chặng 1";
    public float startDistance = 0f;
    public Sprite stageSprite;
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

    [Header("Seamless Looping Elements")]
    [SerializeField] private Transform[] backgroundLayers;

    private float _lastCameraX;
    private float _startPosY;
    private float _startPlayerX;
    private int _currentStageIndex = 0;
    private float _initialBottomY;

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
            System.Collections.Generic.List<Transform> layerList = new System.Collections.Generic.List<Transform>(backgroundLayers);
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

        // Áp dụng ảnh của chặng 1 cho tất cả các tấm ban đầu
        ApplyInitialStageSprites();
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

        // 2. Tính quãng đường xe đã chạy được (mét)
        float currentDistance = playerTransform != null ? (playerTransform.position.x - _startPlayerX) : (cameraX - _lastCameraX);
        UpdateCurrentStage(currentDistance);

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

                        // KHÓA ĐÁY ẢNH VÀO BỜ KÈ (Triệt tiêu 100% khe hở màu cam)
                        float lockedPosY = _initialBottomY + newHalfHeight;
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
        if (backgroundLayers == null || backgroundLayers.Length <= 1) return;

        for (int i = 1; i < backgroundLayers.Length; i++)
        {
            SpriteRenderer prevSr = backgroundLayers[i - 1].GetComponent<SpriteRenderer>();
            float prevWidth = (prevSr != null && prevSr.sprite != null) ? prevSr.bounds.size.x : 17.74f;

            Transform curr = backgroundLayers[i];
            SpriteRenderer currSr = curr.GetComponent<SpriteRenderer>();
            float currHalfHeight = (currSr != null && currSr.sprite != null) ? currSr.bounds.extents.y : 4.435f;

            float lockedPosY = _initialBottomY + currHalfHeight;
            curr.position = new Vector3(backgroundLayers[i - 1].position.x + prevWidth, lockedPosY, curr.position.z);
        }
    }

    // Xác định chặng hiện tại dựa trên quãng đường đã đi
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
                }
                break;
            }
        }
    }

    // Cập nhật Sprite cho tấm ảnh khi nhảy lên phía trước
    private void UpdateSegmentSprite(Transform seg)
    {
        if (stages == null || stages.Length == 0 || _currentStageIndex >= stages.Length) return;

        Sprite targetSprite = stages[_currentStageIndex].stageSprite;
        if (targetSprite == null) return;

        SpriteRenderer sr = seg.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != targetSprite)
        {
            sr.sprite = targetSprite;
        }
    }

    // Áp dụng ảnh của chặng đầu tiên cho tất cả các tấm khi bắt đầu game
    private void ApplyInitialStageSprites()
    {
        if (stages != null && stages.Length > 0 && stages[0].stageSprite != null)
        {
            foreach (Transform seg in backgroundLayers)
            {
                if (seg != null)
                {
                    SpriteRenderer sr = seg.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sprite = stages[0].stageSprite;
                    }
                }
            }
        }
    }
}
