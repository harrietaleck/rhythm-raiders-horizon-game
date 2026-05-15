using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Central audio manager — handles all game sounds.
/// Add to every scene via Tools → Setup Audio.
///
/// Sounds:
///   Background   — looping music during MainGameL1
///   Button       — click SFX on any button
///   Countdown    — tick sound per countdown number
///   Victory      — plays on VictoryScene load
///   GameOver     — piano plays on DeathScene load
///   Narrative    — plays during intro narration in StartScene
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ── Clips (assign in Inspector or via Setup Audio editor tool) ────────────
    [Header("Audio Clips")]
    public AudioClip backgroundMusic;   // background_sound.mp3
    public AudioClip buttonSound;       // button_sound.mp3
    public AudioClip countdownSound;    // count_down_sound.mp3
    public AudioClip victorySound;      // game_victory_sound.mp3
    public AudioClip gameOverSound;     // piano_GameOver_Sound.mp3
    public AudioClip narrativeSound;    // narrative_mp3.mp3

    // ── Audio sources ─────────────────────────────────────────────────────────
    [Header("Volume")]
    [Range(0f, 1f)] public float musicVolume    = 0.5f;
    [Range(0f, 1f)] public float sfxVolume      = 0.8f;
    [Range(0f, 1f)] public float narrativeVolume = 1.0f;

    private AudioSource _musicSource;
    private AudioSource _sfxSource;
    private AudioSource _narrativeSource;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Singleton — persist across scenes so music doesn't cut out
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Build three AudioSource components
        _musicSource     = GetComponent<AudioSource>();
        _sfxSource       = gameObject.AddComponent<AudioSource>();
        _narrativeSource = gameObject.AddComponent<AudioSource>();

        _musicSource.loop        = true;
        _musicSource.volume      = musicVolume;
        _musicSource.playOnAwake = false;

        _sfxSource.loop        = false;
        _sfxSource.volume      = sfxVolume;
        _sfxSource.playOnAwake = false;

        _narrativeSource.loop        = false;
        _narrativeSource.volume      = narrativeVolume;
        _narrativeSource.playOnAwake = false;

        // Auto-play music based on starting scene
        AutoPlayForScene(SceneManager.GetActiveScene().name);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
    {
        AutoPlayForScene(scene.name);
    }

    // ── Auto-play logic per scene ─────────────────────────────────────────────
    private void AutoPlayForScene(string sceneName)
    {
        _musicSource.Stop();
        _narrativeSource.Stop();

        switch (sceneName)
        {
            case "StartScene":
                // Narrative audio plays when narration starts (StartSceneUI calls PlayNarrative)
                break;

            case "MainGameL1":
                PlayBackground();
                break;

            case "VictoryScene":
                PlayVictory();
                break;

            case "DeathScene":
                PlayGameOver();
                break;
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void PlayBackground()
    {
        if (backgroundMusic == null) return;
        _musicSource.clip   = backgroundMusic;
        _musicSource.volume = musicVolume;
        _musicSource.loop   = true;
        _musicSource.Play();
    }

    public void PlayVictory()
    {
        if (victorySound == null) return;
        _musicSource.clip   = victorySound;
        _musicSource.volume = musicVolume;
        _musicSource.loop   = false;
        _musicSource.Play();
    }

    public void PlayGameOver()
    {
        if (gameOverSound == null) return;
        _musicSource.clip   = gameOverSound;
        _musicSource.volume = musicVolume;
        _musicSource.loop   = false;
        _musicSource.Play();
    }

    public void PlayNarrative()
    {
        if (narrativeSound == null) return;
        _narrativeSource.clip   = narrativeSound;
        _narrativeSource.volume = narrativeVolume;
        _narrativeSource.loop   = false;
        _narrativeSource.Play();
    }

    public void StopNarrative()
    {
        _narrativeSource.Stop();
    }

    /// <summary>Play countdown tick. Call once per countdown number.</summary>
    public void PlayCountdownTick()
    {
        if (countdownSound == null) return;
        _sfxSource.PlayOneShot(countdownSound, sfxVolume);
    }

    /// <summary>Play button click SFX. Wire to every Button via ButtonSoundPlayer.</summary>
    public void PlayButton()
    {
        if (buttonSound == null) return;
        _sfxSource.PlayOneShot(buttonSound, sfxVolume);
    }

    public void StopMusic()
    {
        _musicSource.Stop();
    }

    public void SetMusicVolume(float v)
    {
        musicVolume = v;
        _musicSource.volume = v;
    }
}
