using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;

public class SettingsMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject settingsPanel;                 // SettingsPanel objesi

    [Header("Canvas Groups (Premium UI)")]
    public CanvasGroup settingsCanvasGroup;          // SettingsPanel üzerindeki CanvasGroup
    public CanvasGroup mainMenuCanvasGroup;          // MainMenuPanel üzerindeki CanvasGroup

    [Header("Window (Scale animasyonu için)")]
    public RectTransform settingsWindow;             // SettingsPanel içindeki pencere (Window)

    [Header("Audio Sliders (0-1)")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Quality")]
    public TMP_Dropdown qualityDropdown;

    [Header("Optional (PC)")]
    public Toggle fullscreenToggle;

    [Header("Audio Mixer")]
    public AudioMixer mixer;                         // MasterMixer asset
    public string masterParam = "MasterVol";
    public string musicParam  = "MusicVol";
    public string sfxParam    = "SFXVol";

    const string MASTER_KEY = "opt_master";
    const string MUSIC_KEY  = "opt_music";
    const string SFX_KEY    = "opt_sfx";
    const string QUAL_KEY   = "opt_quality";
    const string FULL_KEY   = "opt_fullscreen";

    Coroutine animCo;

    void Start()
    {
        LoadToUI();
        HookUI();
        ApplyAll();

        // Başlangıçta kapalı gibi dursun (overlay yöntemi)
        SetSettingsVisible(false, instant: true);
    }

    void HookUI()
    {
        if (masterSlider) masterSlider.onValueChanged.AddListener(v => { PlayerPrefs.SetFloat(MASTER_KEY, v); PlayerPrefs.Save(); ApplyAudio(); });
        if (musicSlider)  musicSlider.onValueChanged.AddListener(v => { PlayerPrefs.SetFloat(MUSIC_KEY,  v); PlayerPrefs.Save(); ApplyAudio(); });
        if (sfxSlider)    sfxSlider.onValueChanged.AddListener(v => { PlayerPrefs.SetFloat(SFX_KEY,    v); PlayerPrefs.Save(); ApplyAudio(); });

        if (qualityDropdown) qualityDropdown.onValueChanged.AddListener(_ => { SaveQuality(); ApplyQuality(); });

        if (fullscreenToggle) fullscreenToggle.onValueChanged.AddListener(_ => { SaveFullscreen(); ApplyFullscreen(); });
    }

    public void OpenSettings()
    {
        LoadToUI();
        SetSettingsVisible(true, instant: false);
    }

    public void CloseSettings()
    {
        SetSettingsVisible(false, instant: false);
    }

    void SetSettingsVisible(bool show, bool instant)
    {
        if (settingsPanel) settingsPanel.SetActive(true); // alpha ile yönetiyoruz

        // Arkayı kilitle
        if (mainMenuCanvasGroup)
        {
            mainMenuCanvasGroup.interactable = !show;
            mainMenuCanvasGroup.blocksRaycasts = !show;
        }

        if (instant)
        {
            if (settingsCanvasGroup)
            {
                settingsCanvasGroup.alpha = show ? 1f : 0f;
                settingsCanvasGroup.interactable = show;
                settingsCanvasGroup.blocksRaycasts = show;
            }
            if (settingsWindow)
                settingsWindow.localScale = show ? Vector3.one : new Vector3(0.95f, 0.95f, 1f);

            if (!show && settingsPanel) settingsPanel.SetActive(false);
            return;
        }

        if (animCo != null) StopCoroutine(animCo);
        animCo = StartCoroutine(AnimateSettings(show));
    }

    IEnumerator AnimateSettings(bool show)
    {
        float dur = 0.18f;
        float t = 0f;

        float a0 = settingsCanvasGroup ? settingsCanvasGroup.alpha : (show ? 0f : 1f);
        float a1 = show ? 1f : 0f;

        Vector3 s0 = settingsWindow ? settingsWindow.localScale : Vector3.one;
        Vector3 s1 = show ? Vector3.one : new Vector3(0.95f, 0.95f, 1f);

        // açılışta etkileşimi animasyon bitince aç
        if (settingsCanvasGroup)
        {
            settingsCanvasGroup.interactable = false;
            settingsCanvasGroup.blocksRaycasts = false;
        }

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            // küçük bir ease
            k = k * k * (3f - 2f * k);

            if (settingsCanvasGroup) settingsCanvasGroup.alpha = Mathf.Lerp(a0, a1, k);
            if (settingsWindow) settingsWindow.localScale = Vector3.Lerp(s0, s1, k);

            yield return null;
        }

        if (settingsCanvasGroup)
        {
            settingsCanvasGroup.alpha = a1;
            settingsCanvasGroup.interactable = show;
            settingsCanvasGroup.blocksRaycasts = show;
        }

        if (!show && settingsPanel) settingsPanel.SetActive(false);
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

    void SaveQuality()
    {
        if (qualityDropdown) PlayerPrefs.SetInt(QUAL_KEY, qualityDropdown.value);
        PlayerPrefs.Save();
    }

    void SaveFullscreen()
    {
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

    // 0..1 -> dB
    static float LinearToDb(float v)
    {
        v = Mathf.Clamp(v, 0.0001f, 1f);
        return Mathf.Log10(v) * 20f;
    }

    void ApplyAudio()
    {
        if (!mixer) return;

        float master = PlayerPrefs.GetFloat(MASTER_KEY, 1f);
        float music  = PlayerPrefs.GetFloat(MUSIC_KEY,  1f);
        float sfx    = PlayerPrefs.GetFloat(SFX_KEY,    1f);

        mixer.SetFloat(masterParam, LinearToDb(master));
        mixer.SetFloat(musicParam,  LinearToDb(music));
        mixer.SetFloat(sfxParam,    LinearToDb(sfx));
    }

    // İstersen UI'da Reset butonuna bağla
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
