using UnityEngine;

// Thành phần tự động phát nhạc nền (BGM) cho từng Scene
// Hỗ trợ vòng lặp, đồng bộ âm lượng MusicVolume từ Cài đặt thời gian thực
[RequireComponent(typeof(AudioSource))]
public class SceneBGMPlayer : MonoBehaviour
{
    [Header("BGM Configuration")]
    [Tooltip("Tệp nhạc nền phát cho Scene này")]
    [SerializeField] private AudioClip bgmClip;

    [Tooltip("Mức âm lượng gốc chuẩn (0.0 đến 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float baseVolume = 0.65f;

    [Tooltip("Tự động lặp lại khi hết bài")]
    [SerializeField] private bool loop = true;

    [Tooltip("Tự động phát khi Scene bắt đầu")]
    [SerializeField] private bool playOnStart = true;

    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        _audioSource.clip = bgmClip;
        _audioSource.loop = loop;
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f; // 2D Stereo phẳng, âm thanh đều 2 tai
        _audioSource.ignoreListenerPause = true; // Tiếp tục phát khi menu Pause bật (tạo không khí)

        ApplyVolume();
    }

    private void OnEnable()
    {
        SettingsController.OnMusicVolumeChanged += HandleMusicVolumeChanged;
        ApplyVolume();
    }

    private void OnDisable()
    {
        SettingsController.OnMusicVolumeChanged -= HandleMusicVolumeChanged;
    }

    private void Start()
    {
        if (playOnStart && _audioSource != null && bgmClip != null && !_audioSource.isPlaying)
        {
            ApplyVolume();
            _audioSource.Play();
        }
    }

    private void HandleMusicVolumeChanged(float newVolume)
    {
        if (_audioSource != null)
        {
            _audioSource.volume = baseVolume * Mathf.Clamp01(newVolume);
        }
    }

    public void ApplyVolume()
    {
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 1f);
        if (_audioSource != null)
        {
            _audioSource.volume = baseVolume * Mathf.Clamp01(musicVol);
        }
    }

    public void SetTrack(AudioClip clip, float volume = 0.65f)
    {
        bgmClip = clip;
        baseVolume = volume;
        if (_audioSource != null)
        {
            _audioSource.clip = clip;
            ApplyVolume();
            if (!_audioSource.isPlaying)
            {
                _audioSource.Play();
            }
        }
    }
}
