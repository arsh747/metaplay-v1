using UnityEngine;

namespace TriviaGame
{

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    public AudioClip correctClip;
    public AudioClip wrongClip;
    public AudioClip clickClip;

    const string MusicPrefKey = "trivia_music_on";
    const string SfxPrefKey = "trivia_sfx_on";

    private bool musicOn = true;
    private bool sfxOn = true;

    public bool MusicOn => musicOn;
    public bool SfxOn => sfxOn;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Load saved preferences (defaults to "on" the first time the game runs).
        musicOn = PlayerPrefs.GetInt(MusicPrefKey, 1) == 1;
        sfxOn = PlayerPrefs.GetInt(SfxPrefKey, 1) == 1;
    }

    private void Start()
    {
        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.volume = musicOn ? 1f : 0f;
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }

        if (sfxSource != null)
        {
            sfxSource.volume = 1f; // sfxOn is checked per-clip in PlaySFX instead.
        }
    }

    // ---------------- EXPLICIT SETTERS ----------------
    // UIManager passes the Toggle's actual value here (evt.newValue), so the
    // checkbox and the real audio state can never drift apart, unlike a
    // blind "flip whatever it currently is" toggle would.
    public void SetMusic(bool on)
    {
        musicOn = on;
        PlayerPrefs.SetInt(MusicPrefKey, on ? 1 : 0);
        PlayerPrefs.Save();

        if (musicSource != null)
        {
            musicSource.volume = musicOn ? 1f : 0f;
            if (musicOn && !musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }
    }

    public void SetSFX(bool on)
    {
        sfxOn = on;
        PlayerPrefs.SetInt(SfxPrefKey, on ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void PlayCorrect() => PlaySFX(correctClip);
    public void PlayWrong() => PlaySFX(wrongClip);
    public void PlayClick() => PlaySFX(clickClip);

    void PlaySFX(AudioClip clip)
    {
        if (!sfxOn || clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }
}

}
