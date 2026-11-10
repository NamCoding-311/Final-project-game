using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsController : MonoBehaviour
{
    [Header("Sliders")]
    public Slider masterVolumeSlider;
    public Slider soundVolumeSlider;
    public Slider musicVolumeSlider;

    [Header("Value Labels (0% - 100%)")]
    public TextMeshProUGUI masterValueText;
    public TextMeshProUGUI soundValueText;
    public TextMeshProUGUI musicValueText;

    private void Awake()
    {
        AutoBindReferences();
    }

    private void OnEnable()
    {
        AutoBindReferences();
        LoadAndApplySettings();
        RegisterSliderEvents();
    }

    private void OnDisable()
    {
        PlayerPrefs.Save();
    }

    // Tự động tìm kiếm các component nếu chưa kéo thả vào Inspector
    public void AutoBindReferences()
    {
        if (masterVolumeSlider == null) masterVolumeSlider = transform.Find("MasterVolumeSlider")?.GetComponent<Slider>();
        if (soundVolumeSlider == null) soundVolumeSlider = transform.Find("SoundVolumeSlider")?.GetComponent<Slider>();
        if (musicVolumeSlider == null) musicVolumeSlider = transform.Find("MusicVolumeSlider")?.GetComponent<Slider>();

        if (masterValueText == null) masterValueText = transform.Find("MasterLabel_Val")?.GetComponent<TextMeshProUGUI>();
        if (soundValueText == null) soundValueText = transform.Find("SoundLabel_Val")?.GetComponent<TextMeshProUGUI>();
        if (musicValueText == null) musicValueText = transform.Find("MusicLabel_Val")?.GetComponent<TextMeshProUGUI>();
    }

    // Nạp âm lượng đã lưu từ PlayerPrefs và hiển thị lên UI
    public void LoadAndApplySettings()
    {
        float master = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float sound = PlayerPrefs.GetFloat("SoundVolume", 1f);
        float music = PlayerPrefs.GetFloat("MusicVolume", 1f);

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(master);
        }
        UpdateLabel(masterValueText, master);
        AudioListener.volume = master;

        if (soundVolumeSlider != null)
        {
            soundVolumeSlider.SetValueWithoutNotify(sound);
        }
        UpdateLabel(soundValueText, sound);

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.SetValueWithoutNotify(music);
        }
        UpdateLabel(musicValueText, music);
    }

    private void RegisterSliderEvents()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        if (soundVolumeSlider != null)
        {
            soundVolumeSlider.onValueChanged.RemoveListener(SetSoundVolume);
            soundVolumeSlider.onValueChanged.AddListener(SetSoundVolume);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveListener(SetMusicVolume);
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }
    }

    // Xử lý khi người chơi kéo thanh Master Volume
    public void SetMasterVolume(float value)
    {
        PlayerPrefs.SetFloat("MasterVolume", value);
        AudioListener.volume = value;
        UpdateLabel(masterValueText, value);
    }

    // Xử lý khi người chơi kéo thanh Sound Volume
    public void SetSoundVolume(float value)
    {
        PlayerPrefs.SetFloat("SoundVolume", value);
        UpdateLabel(soundValueText, value);
    }

    // Xử lý khi người chơi kéo thanh Music Volume
    public void SetMusicVolume(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        UpdateLabel(musicValueText, value);
    }

    // Cập nhật nhãn hiển thị phần trăm (ví dụ: 100%, 75%, 0%)
    private void UpdateLabel(TextMeshProUGUI label, float value)
    {
        if (label != null)
        {
            int percent = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f);
            label.text = $"{percent}%";
        }
    }
}
