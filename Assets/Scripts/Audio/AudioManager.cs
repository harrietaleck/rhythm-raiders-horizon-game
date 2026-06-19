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
///   Countdown    — single countdown clip at level start
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
    public AudioClip backgroundMusic;   // L1 — background_sound.mp3
    public AudioClip level2Music;       // L2 — level2museumSound.mp3
    public AudioClip level3Music;       // L3 — level3GameSound.mp3
    public AudioClip buttonSound;       // button_sound.mp3
    public AudioClip countdownSound;    // count_down_sound.mp3
    public AudioClip victorySound;      // game_victory_sound.mp3
    public AudioClip gameOverSound;     // piano_GameOver_Sound.mp3
    public AudioClip narrativeSound;    // narrative_mp3.mp3
    [Tooltip("Optional second loop for richer in-game music (defaults to background clip).")]
    public AudioClip backgroundMusicLayer;

    [Header("Pickup SFX")]
    public AudioClip artifactPickupSound;
    public AudioClip healthPickupSound;
    public AudioClip sneakPickupSound;
    public AudioClip speedPickupSound;
    public AudioClip jumpPickupSound;

    [Header("Obstacle SFX")]
    public AudioClip obstacleHitSound;
    public AudioClip slowDownObstacleSound;

    [Header("Cutscene SFX (optional — procedural fallback if blank)")]
    public AudioClip curtainCutsceneSound;
    public AudioClip securityDoorSound;
    public AudioClip discoBallSound;

    // ── Audio sources ─────────────────────────────────────────────────────────
    [Header("Volume")]
    [Range(0f, 1f)] public float musicVolume    = 0.5f;
    [Range(0f, 1f)] public float sfxVolume      = 0.8f;
    [Range(0f, 1f)] public float narrativeVolume = 1.0f;

    private AudioSource _musicSource;
    private AudioSource _musicLayerSource;
    private AudioSource _sfxSource;
    private AudioSource _narrativeSource;
    private AudioSource _countdownSource;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Singleton — persist across scenes so music doesn't cut out
        if (Instance != null && Instance != this)
        {
            Instance.AbsorbClipReferencesFrom(this);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Build three AudioSource components
        _musicSource     = GetComponent<AudioSource>();
        _musicLayerSource = gameObject.AddComponent<AudioSource>();
        _sfxSource       = gameObject.AddComponent<AudioSource>();
        _narrativeSource = gameObject.AddComponent<AudioSource>();

        _musicSource.loop        = true;
        _musicSource.volume      = musicVolume;
        _musicSource.playOnAwake = false;
        _musicSource.Stop();

        _musicLayerSource.loop        = true;
        _musicLayerSource.volume      = musicVolume * 0.35f;
        _musicLayerSource.playOnAwake = false;
        _musicLayerSource.pitch       = 1.02f;

        _sfxSource.loop        = false;
        _sfxSource.volume      = sfxVolume;
        _sfxSource.playOnAwake = false;

        _narrativeSource.loop        = false;
        _narrativeSource.volume      = narrativeVolume;
        _narrativeSource.playOnAwake = false;

        _countdownSource = gameObject.AddComponent<AudioSource>();
        _countdownSource.loop         = false;
        _countdownSource.volume       = sfxVolume;
        _countdownSource.playOnAwake  = false;
        _countdownSource.spatialBlend = 0f;
        _countdownSource.priority     = 64;

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
        StopAllLoopingMusic();
        _narrativeSource.Stop();

        switch (sceneName)
        {
            case "StartScene":
                // Narrative audio plays when narration starts (StartSceneUI calls PlayNarrative)
                break;

            case "MainGameL1Opera":
                PlayBackground(GetClipForScene(sceneName), useMusicLayer: true);
                break;

            case "Level2Museum":
            case "Level3UndergroundDance":
                PlayBackground(GetClipForScene(sceneName), useMusicLayer: false);
                break;

            case "Level2IntroScene":
            case "Level3IntroScene":
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

    AudioClip GetClipForScene(string sceneName)
    {
        return sceneName switch
        {
            "Level2Museum" => level2Music != null ? level2Music : backgroundMusic,
            "Level3UndergroundDance" => level3Music != null ? level3Music : backgroundMusic,
            _ => backgroundMusic
        };
    }

    public void PlayBackground()
    {
        PlayBackground(backgroundMusic, useMusicLayer: true);
    }

    public void PlayBackground(AudioClip clip, bool useMusicLayer)
    {
        if (clip == null) return;

        StopAllLoopingMusic();

        _musicSource.clip   = clip;
        _musicSource.volume = musicVolume;
        _musicSource.loop   = true;
        _musicSource.Play();

        if (_musicLayerSource == null) return;

        if (useMusicLayer && clip == backgroundMusic)
        {
            var layerClip = backgroundMusicLayer != null ? backgroundMusicLayer : clip;
            _musicLayerSource.clip   = layerClip;
            _musicLayerSource.volume = musicVolume * 0.35f;
            _musicLayerSource.loop   = true;
            _musicLayerSource.Play();
        }
        else
        {
            StopMusicLayer();
        }
    }

    void AbsorbClipReferencesFrom(AudioManager other)
    {
        if (other == null) return;
        if (other.level2Music != null) level2Music = other.level2Music;
        if (other.level3Music != null) level3Music = other.level3Music;
        if (other.backgroundMusic != null) backgroundMusic = other.backgroundMusic;
        if (other.backgroundMusicLayer != null) backgroundMusicLayer = other.backgroundMusicLayer;
    }

    void StopMusicLayer()
    {
        if (_musicLayerSource == null) return;
        _musicLayerSource.Stop();
        _musicLayerSource.clip = null;
        _musicLayerSource.volume = 0f;
    }

    void StopAllLoopingMusic()
    {
        EnsureMusicSources();

        foreach (var src in GetComponents<AudioSource>())
        {
            if (src == _sfxSource || src == _narrativeSource || src == _countdownSource)
                continue;

            src.Stop();
            src.clip = null;
        }

        if (_musicSource != null)
            _musicSource.volume = musicVolume;
        StopMusicLayer();
    }

    void EnsureMusicSources()
    {
        if (_musicSource == null)
            _musicSource = GetComponent<AudioSource>();

        if (_musicLayerSource != null || _musicSource == null)
            return;

        foreach (var src in GetComponents<AudioSource>())
        {
            if (src == _musicSource || src == _sfxSource || src == _narrativeSource || src == _countdownSource)
                continue;

            _musicLayerSource = src;
            _musicLayerSource.loop = true;
            _musicLayerSource.playOnAwake = false;
            return;
        }
    }

    public void PlayVictory()
    {
        if (victorySound == null) return;
        _musicSource.clip   = victorySound;
        _musicSource.volume = musicVolume;
        _musicSource.loop   = false;
        _musicSource.Play();

        if (backgroundMusic != null)
            _sfxSource.PlayOneShot(backgroundMusic, musicVolume * 0.2f);
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

    /// <summary>Play the countdown clip once (visual numbers sync separately).</summary>
    public void PlayCountdown()
    {
        if (countdownSound == null) return;
        _countdownSource.Stop();
        _countdownSource.clip   = countdownSound;
        _countdownSource.volume = sfxVolume;
        _countdownSource.pitch  = 1f;
        _countdownSource.Play();
    }

    public void StopCountdown()
    {
        _countdownSource.Stop();
    }

    /// <summary>Deprecated — use PlayCountdown() once at countdown start.</summary>
    public void PlayCountdownTick()
    {
        PlayCountdown();
    }

    /// <summary>Play button click SFX. Wire to every Button via ButtonSoundPlayer.</summary>
    public void PlayButton()
    {
        if (buttonSound == null) return;
        _sfxSource.PlayOneShot(buttonSound, sfxVolume);
    }

    /// <summary>Shared pickup palette — pitch-shifted button SFX for cohesion with sneak.</summary>
    public void PlayPickupSfx(float pitch = 1f, float volumeScale = 0.75f)
    {
        if (buttonSound == null) return;
        _sfxSource.pitch = pitch;
        _sfxSource.PlayOneShot(buttonSound, sfxVolume * volumeScale);
        _sfxSource.pitch = 1f;
    }

    public void PlaySneakPickupSfx() => PlayClipOrFallback(sneakPickupSound, 0.82f, 0.65f);
    public void PlayArtifactPickupSfx() => PlayClipOrFallback(artifactPickupSound, 1.15f, 0.85f);
    public void PlayHealthPickupSfx() => PlayClipOrFallback(healthPickupSound, 1f, 0.7f);
    public void PlaySpeedPickupSfx() => PlayClipOrFallback(speedPickupSound, 1.35f, 0.8f);
    public void PlayJumpPickupSfx() => PlayClipOrFallback(jumpPickupSound, 1.22f, 0.75f);
    public void PlayObstacleHitSfx() => PlayClipOrFallback(obstacleHitSound, 0.55f, 0.85f);
    public void PlaySlowDownObstacleSfx() => PlayClipOrFallback(slowDownObstacleSound, 0.48f, 0.75f);

    public void PlayCutsceneStinger(LevelCutsceneType type, int phase = 0)
    {
        switch (type)
        {
            case LevelCutsceneType.OperaCurtain:
                if (curtainCutsceneSound != null)
                    _sfxSource.PlayOneShot(curtainCutsceneSound, sfxVolume);
                else if (phase == 0)
                    PlayPickupSfx(0.52f, 0.45f);
                else
                    PlayPickupSfx(0.68f, 0.55f);
                break;
            case LevelCutsceneType.MuseumSecurityDoor:
                if (securityDoorSound != null)
                    _sfxSource.PlayOneShot(securityDoorSound, sfxVolume);
                else if (phase == 0)
                    PlayPickupSfx(0.42f, 0.7f);
                else
                    PlayPickupSfx(0.58f, 0.65f);
                break;
            case LevelCutsceneType.ClubDiscoBall:
                if (discoBallSound != null)
                    _sfxSource.PlayOneShot(discoBallSound, sfxVolume);
                else if (phase == 0)
                    PlayPickupSfx(1.45f, 0.5f);
                else
                    PlayPickupSfx(1.75f, 0.6f);
                break;
        }
    }

    private void PlayClipOrFallback(AudioClip clip, float fallbackPitch, float fallbackVolumeScale)
    {
        if (clip != null)
        {
            _sfxSource.PlayOneShot(clip, sfxVolume);
            return;
        }

        PlayPickupSfx(fallbackPitch, fallbackVolumeScale);
    }

    public void StopMusic()
    {
        StopAllLoopingMusic();
    }

    public void SetMusicVolume(float v)
    {
        musicVolume = v;
        _musicSource.volume = v;
        if (_musicLayerSource != null && _musicLayerSource.isPlaying)
            _musicLayerSource.volume = v * 0.35f;
    }

    public void SetSfxVolume(float v)
    {
        sfxVolume = v;
        if (_countdownSource != null)
            _countdownSource.volume = v;
    }
}
