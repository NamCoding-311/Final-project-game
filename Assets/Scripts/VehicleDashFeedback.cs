using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Hiệu ứng Hình ảnh & Âm thanh chuyên biệt cho cú Dash của xe (Nhóm 1):
// 1. Ghost Trail / Tàn ảnh bóng ma mờ dần xé gió phía sau xe.
// 2. Nitro Flame & Smoke: Tia lửa khói phụt ra từ ống xả khi kích hoạt Dash.
// 3. Audio Feedback: Tiếng gầm rú nitro (charge.mp3) và tiếng va đập nghiền nát (ram impact).
// 4. Kinetic Shockwave: Vòng sóng xung kích va chạm khi đâm chết Zombie lúc Dash.
public class VehicleDashFeedback : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player3LaneMovement movement;

    [Header("Ghost Trail Settings (Tàn ảnh vệt bóng xe)")]
    [SerializeField] private bool enableGhostTrail = true;
    [SerializeField] private float ghostSpawnInterval = 0.038f;
    [SerializeField] private float ghostLifetime = 0.24f;
    [SerializeField] private Color ghostColorStart = new Color(0.15f, 0.85f, 1f, 0.75f); // Neon Cyan
    [SerializeField] private Color ghostColorEnd = new Color(1f, 0.45f, 0.1f, 0.6f);     // Nitro Flame Orange

    [Header("Exhaust Nitro Spark/Smoke Settings")]
    [SerializeField] private bool enableExhaustFlames = true;
    [SerializeField] private Vector3 exhaustLocalOffset = new Vector3(-1.35f, -0.2f, 0f);
    [SerializeField] private float exhaustSpawnInterval = 0.03f;

    [Header("Audio Feedback")]
    [SerializeField] private AudioClip dashSound;
    [SerializeField] private AudioClip ramImpactSound;
    [SerializeField] [Range(0f, 1f)] private float dashVolume = 0.95f;
    [SerializeField] [Range(0f, 1f)] private float impactVolume = 0.9f;

    private AudioSource _audioSource;
    private float _nextGhostTime;
    private float _nextExhaustTime;
    private List<SpriteRenderer> _cachedRenderers = new List<SpriteRenderer>();
    private bool _wasDashing = false;

    // Sprite trắng 1x1 dùng để vẽ tia lửa / khói / vòng xung kích procedural
    private static Sprite _proceduralPixelSprite;
    private static Sprite GetProceduralSprite()
    {
        if (_proceduralPixelSprite == null)
        {
            Texture2D tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            Color[] colors = new Color[16 * 16];
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f));
                    float alpha = Mathf.Clamp01(1f - (dist / 7.5f));
                    colors[y * 16 + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            _proceduralPixelSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        }
        return _proceduralPixelSprite;
    }

    private void Awake()
    {
        if (movement == null) movement = GetComponent<Player3LaneMovement>();

        // Thiết lập AudioSource chuyên dụng không làm gián đoạn tiếng súng
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f; // 2D âm thanh trực diện sống động

        // Load âm thanh mặc định nếu chưa gán
        LoadDefaultAudio();

        CacheRenderers();
    }

    private void LoadDefaultAudio()
    {
#if UNITY_EDITOR
        if (dashSound == null)
        {
            dashSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/sound eff/charge.mp3");
        }
        if (ramImpactSound == null)
        {
            ramImpactSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/sound eff/shotgun.mp3");
        }
#endif
    }

    public void CacheRenderers()
    {
        _cachedRenderers.Clear();
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in renderers)
        {
            if (r.gameObject != this.gameObject && r.name.Contains("Shadow")) continue;
            _cachedRenderers.Add(r);
        }
    }

    private void Update()
    {
        if (movement == null) return;

        bool isDashing = movement.IsDashing();

        // Khoảnh khắc bắt đầu Dash
        if (isDashing && !_wasDashing)
        {
            OnDashTriggered();
        }

        // Trong quá trình đang Dash
        if (isDashing)
        {
            if (enableGhostTrail && Time.time >= _nextGhostTime)
            {
                _nextGhostTime = Time.time + ghostSpawnInterval;
                SpawnGhostAfterimage();
            }

            if (enableExhaustFlames && Time.time >= _nextExhaustTime)
            {
                _nextExhaustTime = Time.time + exhaustSpawnInterval;
                SpawnExhaustParticles();
            }
        }

        _wasDashing = isDashing;
    }

    // Kích hoạt phản hồi khi bắt đầu Dash
    private void OnDashTriggered()
    {
        // 1. Âm thanh Nitro Boost
        PlayDashAudio();

        // 2. Camera Punch & Shake
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.TriggerDashPunch(0.35f, 0.25f);
        }

        // 3. Phụt ngay đợt tia lửa lớn ban đầu
        SpawnExhaustBurst();
    }

    private void PlayDashAudio()
    {
        if (dashSound == null) LoadDefaultAudio();
        if (dashSound != null && _audioSource != null)
        {
            float sfxVol = PlayerPrefs.GetFloat("SoundVolume", 1f);
            _audioSource.pitch = Random.Range(0.97f, 1.06f);
            _audioSource.PlayOneShot(dashSound, dashVolume * sfxVol);
        }
    }

    // Phát âm thanh và hiệu ứng nghiền nát khi tông trúng Zombie
    public void PlayRamImpactFeedback(Vector3 hitPoint)
    {
        // Âm thanh đâm va
        if (ramImpactSound == null) LoadDefaultAudio();
        if (ramImpactSound != null && _audioSource != null)
        {
            float sfxVol = PlayerPrefs.GetFloat("SoundVolume", 1f);
            _audioSource.pitch = Random.Range(1.1f, 1.25f);
            _audioSource.PlayOneShot(ramImpactSound, impactVolume * sfxVol);
        }

        // Camera giật nhẹ thêm khi nghiền nát Zombie
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.TriggerShake(0.12f, 0.24f);
        }

        // Vòng xung kích kinetic shockwave tại điểm va chạm
        SpawnKineticShockwave(hitPoint);
    }

    // Tạo tàn ảnh sau xe (Ghost Afterimage)
    private void SpawnGhostAfterimage()
    {
        if (_cachedRenderers.Count == 0) CacheRenderers();

        float t = Random.value;
        Color tintColor = Color.Lerp(ghostColorStart, ghostColorEnd, t);

        for (int i = 0; i < _cachedRenderers.Count; i++)
        {
            SpriteRenderer sr = _cachedRenderers[i];
            if (sr == null || !sr.gameObject.activeInHierarchy || sr.sprite == null) continue;

            GameObject ghostObj = new GameObject("DashGhostSprite");
            ghostObj.transform.position = sr.transform.position;
            ghostObj.transform.rotation = sr.transform.rotation;
            ghostObj.transform.localScale = sr.transform.lossyScale;

            SpriteRenderer ghostSr = ghostObj.AddComponent<SpriteRenderer>();
            ghostSr.sprite = sr.sprite;
            ghostSr.flipX = sr.flipX;
            ghostSr.flipY = sr.flipY;
            ghostSr.sortingLayerID = sr.sortingLayerID;
            ghostSr.sortingOrder = Mathf.Max(0, sr.sortingOrder - 1);
            ghostSr.material = sr.material;
            ghostSr.color = tintColor;

            GhostAfterimageFader fader = ghostObj.AddComponent<GhostAfterimageFader>();
            fader.Initialize(tintColor, ghostLifetime);
        }
    }

    // Tạo các hạt lửa và khói phụt ra từ pô xe
    private void SpawnExhaustParticles()
    {
        Vector3 spawnPos = transform.TransformPoint(exhaustLocalOffset);
        int count = Random.Range(2, 4);

        for (int i = 0; i < count; i++)
        {
            GameObject spark = new GameObject("NitroSpark");
            spark.transform.position = spawnPos + (Vector3)(Random.insideUnitCircle * 0.15f);

            SpriteRenderer sr = spark.AddComponent<SpriteRenderer>();
            sr.sprite = GetProceduralSprite();
            sr.sortingOrder = 15;

            // Màu lửa: Cam vàng hoặc Xanh Nitro
            bool isFlame = Random.value > 0.4f;
            Color sparkColor = isFlame
                ? new Color(1f, Random.Range(0.4f, 0.8f), 0.1f, 0.9f)
                : new Color(0.1f, 0.9f, 1f, 0.9f);

            sr.color = sparkColor;
            float scale = Random.Range(0.2f, 0.45f);
            spark.transform.localScale = Vector3.one * scale;

            Vector2 velocity = new Vector2(-Random.Range(4f, 9f), Random.Range(-1.5f, 1.5f));
            SparkFader fader = spark.AddComponent<SparkFader>();
            fader.Initialize(sparkColor, velocity, Random.Range(0.12f, 0.22f));
        }
    }

    private void SpawnExhaustBurst()
    {
        Vector3 spawnPos = transform.TransformPoint(exhaustLocalOffset);
        for (int i = 0; i < 8; i++)
        {
            GameObject spark = new GameObject("NitroBurstSpark");
            spark.transform.position = spawnPos + (Vector3)(Random.insideUnitCircle * 0.25f);

            SpriteRenderer sr = spark.AddComponent<SpriteRenderer>();
            sr.sprite = GetProceduralSprite();
            sr.sortingOrder = 16;
            Color burstColor = new Color(1f, Random.Range(0.5f, 0.9f), 0.15f, 1f);
            sr.color = burstColor;
            spark.transform.localScale = Vector3.one * Random.Range(0.35f, 0.65f);

            Vector2 velocity = new Vector2(-Random.Range(6f, 14f), Random.Range(-2.5f, 2.5f));
            SparkFader fader = spark.AddComponent<SparkFader>();
            fader.Initialize(burstColor, velocity, Random.Range(0.15f, 0.25f));
        }
    }

    // Hiệu ứng vòng sóng xung kích khi đâm va
    private void SpawnKineticShockwave(Vector3 position)
    {
        GameObject waveObj = new GameObject("RamShockwave");
        waveObj.transform.position = position;

        SpriteRenderer sr = waveObj.AddComponent<SpriteRenderer>();
        sr.sprite = GetProceduralSprite();
        sr.sortingOrder = 20;
        sr.color = new Color(1f, 0.9f, 0.5f, 0.85f);
        waveObj.transform.localScale = Vector3.one * 0.2f;

        ShockwaveExpand expander = waveObj.AddComponent<ShockwaveExpand>();
        expander.Initialize(0.18f, 2.2f);
    }
}

// Component phụ tự làm mờ và giải phóng tàn ảnh bóng ma
public class GhostAfterimageFader : MonoBehaviour
{
    private SpriteRenderer _sr;
    private Color _startColor;
    private float _duration;
    private float _elapsed;
    private Vector3 _startScale;

    public void Initialize(Color startColor, float duration)
    {
        _sr = GetComponent<SpriteRenderer>();
        _startColor = startColor;
        _duration = Mathf.Max(0.05f, duration);
        _startScale = transform.localScale;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float progress = _elapsed / _duration;

        if (progress >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        if (_sr != null)
        {
            // Mờ dần theo thời gian
            Color c = _startColor;
            c.a = Mathf.Lerp(_startColor.a, 0f, progress * progress);
            _sr.color = c;
        }

        // Thu nhỏ nhẹ nhàng tạo độ sâu
        transform.localScale = Vector3.Lerp(_startScale, _startScale * 0.92f, progress);
    }
}

// Component phụ điều khiển tia lửa pô xe bay ngược về sau rồi tắt
public class SparkFader : MonoBehaviour
{
    private SpriteRenderer _sr;
    private Color _startColor;
    private Vector2 _velocity;
    private float _duration;
    private float _elapsed;

    public void Initialize(Color startColor, Vector2 velocity, float duration)
    {
        _sr = GetComponent<SpriteRenderer>();
        _startColor = startColor;
        _velocity = velocity;
        _duration = Mathf.Max(0.05f, duration);
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float t = _elapsed / _duration;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += (Vector3)(_velocity * Time.deltaTime);

        if (_sr != null)
        {
            Color c = _startColor;
            c.a = Mathf.Lerp(_startColor.a, 0f, t);
            _sr.color = c;
        }

        transform.localScale *= (1f - (Time.deltaTime * 3f));
    }
}

// Component mở rộng vòng sóng xung kích va chạm
public class ShockwaveExpand : MonoBehaviour
{
    private SpriteRenderer _sr;
    private float _duration;
    private float _maxScale;
    private float _elapsed;
    private Color _startColor;

    public void Initialize(float duration, float maxScale)
    {
        _sr = GetComponent<SpriteRenderer>();
        _duration = Mathf.Max(0.05f, duration);
        _maxScale = maxScale;
        if (_sr != null) _startColor = _sr.color;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float t = _elapsed / _duration;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        float currentScale = Mathf.Lerp(0.2f, _maxScale, Mathf.Sin(t * Mathf.PI * 0.5f));
        transform.localScale = new Vector3(currentScale * 1.5f, currentScale, 1f);

        if (_sr != null)
        {
            Color c = _startColor;
            c.a = Mathf.Lerp(_startColor.a, 0f, t * t);
            _sr.color = c;
        }
    }
}
