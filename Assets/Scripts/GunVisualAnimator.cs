using System.Collections;
using UnityEngine;

// Điều khiển Hoạt Ảnh và Độ Giật (Recoil & Shoot Animation) cho từng khẩu súng:
// - Chạy chuỗi Sprite Sheet (Lóe lửa đầu nòng, giật khóa nòng, văng vỏ đạn) khi bắn.
// - Tự động đồng bộ tốc độ animation theo FireRate của súng (bắn càng nhanh, hoạt ảnh chạy càng nhanh).
// - Thêm hiệu ứng thụt lùi (Recoil Kickback) cơ học nảy về sau trên GameObject con (Visual) rồi hồi vị trí ban đầu (0, 0, 0),
//   hoàn toàn độc lập và không bao giờ xung đột với vị trí quỹ đạo bay (Orbit) quanh nhân vật.
public class GunVisualAnimator : MonoBehaviour
{
    private WeaponData _weaponData;
    private SpriteRenderer _sr;
    private Sprite _idleSprite;
    private Coroutine _currentAnimRoutine;

    public void Initialize(WeaponData data, SpriteRenderer sr)
    {
        _weaponData = data;
        _sr = sr;
        transform.localPosition = Vector3.zero;

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
                if (recoilDist > 0.01f)
                {
                    float kickback = (i < 2) ? -recoilDist : Mathf.Lerp(-recoilDist, 0f, (float)i / frames.Length);
                    transform.localPosition = new Vector3(kickback, 0f, 0f);
                }

                yield return new WaitForSeconds(frameTime);
            }
        }
        else
        {
            // Fallback nếu súng chưa gán frame: Giật thụt lùi nhẹ bằng code
            transform.localPosition = new Vector3(-0.08f, 0f, 0f);
            yield return new WaitForSeconds(0.06f);
        }

        // Trả về vị trí cục bộ chuẩn (0, 0, 0) và hình ảnh idle gốc
        transform.localPosition = Vector3.zero;
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
        transform.localPosition = Vector3.zero;
        if (_idleSprite != null && _sr != null)
        {
            _sr.sprite = _idleSprite;
        }
    }
}
