using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;

    [Header("Audio Sliders (0-1)")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Quality")]
    public TMP_Dropdown qualityDropdown;

    [Header("Optional (PC)")]
    public Toggle fullscreenToggle;

    const string MASTER_KEY = "opt_master";
    const string MUSIC_KEY  = "opt_music";
    const string SFX_KEY    = "opt_sfx";
    const string QUAL_KEY   = "opt_quality";
    const string FULL_KEY   = "opt_fullscreen";

    void Start()
    {
        LoadToUI();
        HookUI();
        ApplyAll();
    }

    void HookUI()
    {
        if (masterSlider) masterSlider.onValueChanged.AddListener(_ => { SaveFromUI(); ApplyAudio(); });
        if (musicSlider)  musicSlider.onValueChanged.AddListener(_ => { SaveFromUI(); ApplyAudio(); });
        if (sfxSlider)    sfxSlider.onValueChanged.AddListener(_ => { SaveFromUI(); ApplyAudio(); });

        if (qualityDropdown) qualityDropdown.onValueChanged.AddListener(_ => { SaveFromUI(); ApplyQuality(); });

        if (fullscreenToggle) fullscreenToggle.onValueChanged.AddListener(_ => { SaveFromUI(); ApplyFullscreen(); });
    }

    public void OpenSettings()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(true);
        LoadToUI();
    }

    public void CloseSettings()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
    }

    void LoadToUI()
    {
        float master = PlayerPrefs.GetFloat(MASTER_KEY, 1f);
        float music  = PlayerPrefs.GetFloat(MUSIC_KEY,  1f);
        float sfx    = PlayerPrefs.GetFloat(SFX_KEY,    1f);
        int quality  = PlayerPrefs.GetInt(QUAL_KEY, QualitySettings.GetQualityLevel());
        int full     = PlayerPrefs.GetInt(FULL_KEY, Screen.fullScreen ? 1 : 0);

        if (masterSlider) masterSlider.SetValueWithoutNotify(master);
        if (musicSlider)  musicSlider.SetValueWithoutNotify(music);
        if (sfxSlider)    sfxSlider.SetValueWithoutNotify(sfx);

        if (qualityDropdown)
            qualityDropdown.SetValueWithoutNotify(Mathf.Clamp(quality, 0, QualitySettings.names.Length - 1));

        if (fullscreenToggle)
            fullscreenToggle.SetIsOnWithoutNotify(full == 1);
    }

    void SaveFromUI()
    {
        if (masterSlider) PlayerPrefs.SetFloat(MASTER_KEY, masterSlider.value);
        if (musicSlider)  PlayerPrefs.SetFloat(MUSIC_KEY,  musicSlider.value);
        if (sfxSlider)    PlayerPrefs.SetFloat(SFX_KEY,    sfxSlider.value);

        if (qualityDropdown) PlayerPrefs.SetInt(QUAL_KEY, qualityDropdown.value);

        if (fullscreenToggle) PlayerPrefs.SetInt(FULL_KEY, fullscreenToggle.isOn ? 1 : 0);

        PlayerPrefs.Save();
    }

    public void ApplyAll()
    {
        ApplyAudio();
        ApplyQuality();
        ApplyFullscreen();
    }

    void ApplyQuality()
    {
        int q = PlayerPrefs.GetInt(QUAL_KEY, QualitySettings.GetQualityLevel());
        q = Mathf.Clamp(q, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(q, true);
    }

    void ApplyFullscreen()
    {
        if (!fullscreenToggle) return;
        Screen.fullScreen = PlayerPrefs.GetInt(FULL_KEY, 1) == 1;
    }

    void ApplyAudio()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(MASTER_KEY, 1f);
        // Music/SFX için AudioMixer ekleyince burayı güçlendireceğiz.
    }

    public void ResetDefaults()
    {
        PlayerPrefs.SetFloat(MASTER_KEY, 1f);
        PlayerPrefs.SetFloat(MUSIC_KEY,  1f);
        PlayerPrefs.SetFloat(SFX_KEY,    1f);
        PlayerPrefs.SetInt(QUAL_KEY, QualitySettings.GetQualityLevel());
        PlayerPrefs.SetInt(FULL_KEY, 1);
        PlayerPrefs.Save();

        LoadToUI();
        ApplyAll();
    }
}
