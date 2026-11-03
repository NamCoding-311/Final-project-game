using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    public Slider masterVolumeSlider;
    public Slider soundVolumeSlider;
    public Slider musicVolumeSlider;

    public void SetMasterVolume(float value)
    {
        PlayerPrefs.SetFloat("MasterVolume", value);
    }

    public void SetSoundVolume(float value)
    {
        PlayerPrefs.SetFloat("SoundVolume", value);
    }

    public void SetMusicVolume(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
    }
}