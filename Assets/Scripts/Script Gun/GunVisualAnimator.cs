using System.Collections;
using UnityEngine;

// Điều khiển Hoạt Ảnh và Độ Giật (Recoil & Shoot Animation) cho từng khẩu súng:
// - Chạy chuỗi Sprite Sheet (Lóe lửa đầu nòng, giật khóa nòng, văng vỏ đạn) khi bắn.
// - Tự động đồng bộ tốc độ animation theo FireRate của súng (bắn càng nhanh, hoạt ảnh chạy càng nhanh).
// - Thêm hiệu ứng thụt lùi (Recoil Kickback) cơ học nảy về sau trên GameObject con (Visual) rồi hồi vị trí ban đầu (baseLocalPos),
//   hoàn toàn độc lập và không bao giờ xung đột với vị trí neo tay cầm (GunHolder).
public class GunVisualAnimator : MonoBehaviour
{
    private WeaponData _weaponData;
    private SpriteRenderer _sr;
    private Sprite _idleSprite;
    private Coroutine _currentAnimRoutine;
    private Vector3 _baseLocalPos = Vector3.zero;
    private bool _hasBaseLocalPos = false;

    // Tham chiếu Transform trực tiếp nhận hiệu ứng giật nòng (Visual child)
    private Transform _recoilTarget;

    private void Awake()
    {
        CacheRecoilTarget();
    }

    private void CacheRecoilTarget()
    {
        if (_recoilTarget == null)
        {
            _recoilTarget = transform;
            _baseLocalPos = transform.localPosition;
            _hasBaseLocalPos = true;
        }
    }

    public void Initialize(WeaponData data, SpriteRenderer sr)
    {
        _weaponData = data;
        _sr = sr;

        CacheRecoilTarget();

        // Nếu component này gắn trực tiếp trên GunHolder (chứa OnFootPlayerController ở cha),
        // không được reset vị trí local của GunHolder về (0,0,0) làm tuột khỏi tay nhân vật
        if (_hasBaseLocalPos)
        {
            transform.localPosition = _baseLocalPos;
        }

        if (_weaponData != null)
        {
            if (_weaponData.weaponSprite != null)
            {
                _idleSprite = _weaponData.weaponSprite;
            }
            else if (_weaponData.shootAnimationFrames != null && _weaponData.shootAnimationFrames.Length > 0)
            {
                _idleSprite = _weaponData.shootAnimationFrames[0];
            }

            if (_sr != null && _idleSprite != null)
            {
                _sr.sprite = _idleSprite;
            }
        }
    }

    public void SetFlipped(bool isFlipped)
    {
        if (_sr != null)
        {
            _sr.flipY = isFlipped;
        }
    }

    // Kích hoạt hoạt ảnh bắn khi súng xả đạn
    public void PlayShootAnimation()
    {
        if (_weaponData == null || _sr == null) return;
        if (!gameObject.activeInHierarchy) return;

        if (_currentAnimRoutine != null)
        {
            StopCoroutine(_currentAnimRoutine);
        }

        _currentAnimRoutine = StartCoroutine(ShootRoutine());
    }

    private IEnumerator ShootRoutine()
    {
        Sprite[] frames = _weaponData.shootAnimationFrames;
        float recoilDist = _weaponData.recoilDistance;

        // Nếu súng gắn trực tiếp trên GunHolder thay vì GunSprite con, giảm kickback tránh giật lệch tay
        bool isDirectGunHolder = (transform.parent != null && (name == "GunHolder" || name == "Gun"));
        if (isDirectGunHolder)
        {
            recoilDist = Mathf.Min(recoilDist * 0.2f, 0.02f);
        }

        // 1. Nếu có chuỗi frame animation (Sprite Sheet)
        if (frames != null && frames.Length > 0)
        {
            // Tự động căn chỉnh FPS: Đảm bảo animation bắn xong trước nhịp bắn tiếp theo
            float fps = Mathf.Max(20f, _weaponData.animationFps);
            float minDuration = _weaponData.fireRate * 0.85f;
            if (minDuration > 0.05f)
            {
                float calculatedFps = frames.Length / minDuration;
                fps = Mathf.Max(fps, calculatedFps);
            }

            float frameTime = 1f / fps;

            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null)
                {
                    _sr.sprite = frames[i];
                }

                // Hiệu ứng giật nòng: giật lùi ở 2-3 frame đầu rồi phục hồi dần dọc trục -X cục bộ
                if (recoilDist > 0.005f && _recoilTarget != null)
                {
                    float kickback = (i < 2) ? -recoilDist : Mathf.Lerp(-recoilDist, 0f, (float)i / frames.Length);
                    _recoilTarget.localPosition = _baseLocalPos + new Vector3(kickback, 0f, 0f);
                }

                yield return new WaitForSeconds(frameTime);
            }
        }
        else
        {
            // Fallback nếu súng chưa gán frame: Giật thụt lùi nhẹ bằng code
            if (_recoilTarget != null)
            {
                _recoilTarget.localPosition = _baseLocalPos + new Vector3(-0.03f, 0f, 0f);
            }
            yield return new WaitForSeconds(0.06f);
        }

        // Trả về vị trí cục bộ chuẩn và hình ảnh idle gốc
        if (_recoilTarget != null)
        {
            _recoilTarget.localPosition = _baseLocalPos;
        }

        if (_idleSprite != null && _sr != null)
        {
            _sr.sprite = _idleSprite;
        }

        _currentAnimRoutine = null;
    }

    private void OnDisable()
    {
        if (_currentAnimRoutine != null)
        {
            StopCoroutine(_currentAnimRoutine);
            _currentAnimRoutine = null;
        }

        if (_recoilTarget != null)
        {
            _recoilTarget.localPosition = _baseLocalPos;
        }

        if (_idleSprite != null && _sr != null)
        {
            _sr.sprite = _idleSprite;
        }
    }
}
