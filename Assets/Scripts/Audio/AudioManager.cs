using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I;

    [Header("Mixer")]
    public AudioMixer mixer;

    [Header("Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Clips")]
    public AudioClip menuMusic;
    public AudioClip uiClick;

    const string MASTER_KEY = "opt_master";
    const string MUSIC_KEY  = "opt_music";
    const string SFX_KEY    = "opt_sfx";

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        ApplySavedVolumes();  // oyun açılınca kayıtlı sesleri uygula
        PlayMenuMusic();      // istersen menüde otomatik çalsın
    }

    public void PlayMenuMusic()
    {
        PlayMusic(menuMusic);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (!musicSource || clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlayUIClick()
    {
        if (!sfxSource || uiClick == null) return;
        sfxSource.PlayOneShot(uiClick, 1f);
    }

    // 0..1 slider -> dB
    static float LinearToDb(float v)
    {
        v = Mathf.Clamp(v, 0.0001f, 1f);
        return Mathf.Log10(v) * 20f;
    }

    public void SetMaster(float linear01)
    {
        PlayerPrefs.SetFloat(MASTER_KEY, linear01);
        PlayerPrefs.Save();
        mixer.SetFloat("MasterVol", LinearToDb(linear01));
    }

    public void SetMusic(float linear01)
    {
        PlayerPrefs.SetFloat(MUSIC_KEY, linear01);
        PlayerPrefs.Save();
        mixer.SetFloat("MusicVol", LinearToDb(linear01));
    }

    public void SetSFX(float linear01)
    {
        PlayerPrefs.SetFloat(SFX_KEY, linear01);
        PlayerPrefs.Save();
        mixer.SetFloat("SFXVol", LinearToDb(linear01));
    }

    public void ApplySavedVolumes()
    {
        float master = PlayerPrefs.GetFloat(MASTER_KEY, 1f);
        float music  = PlayerPrefs.GetFloat(MUSIC_KEY,  1f);
        float sfx    = PlayerPrefs.GetFloat(SFX_KEY,    1f);

        mixer.SetFloat("MasterVol", LinearToDb(master));
        mixer.SetFloat("MusicVol",  LinearToDb(music));
        mixer.SetFloat("SFXVol",    LinearToDb(sfx));
    }
}
