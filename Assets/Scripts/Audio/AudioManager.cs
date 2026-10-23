using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Central audio manager — handles all game sounds.
/// Add to every scene via Tools → Setup Audio.
///
/// Sounds:
///   Background   — looping level music (or the player's jukebox pick) in gameplay
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
    public AudioClip rooftopMusic;      // L1 — Endless Sunrise (Infraction)
    public AudioClip backgroundMusic;   // background_sound.mp3 (jukebox: Opera Groove)
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
        // One-shots (stingers, cutscene hits) must not ring on into the next level.
        if (_sfxSource != null) _sfxSource.Stop();

        switch (sceneName)
        {
            case "StartScene":
                // Narrative audio plays when narration starts (StartSceneUI calls PlayNarrative)
                break;

            case "MainGameL1Opera":
            case "Level2Museum":
            case "Level3UndergroundDance":
                PlayBackground(GameplayClipFor(sceneName), useMusicLayer: false);
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
            "MainGameL1Opera" => rooftopMusic != null ? rooftopMusic : backgroundMusic,
            "Level2Museum" => level2Music != null ? level2Music : backgroundMusic,
            "Level3UndergroundDance" => level3Music != null ? level3Music : backgroundMusic,
            _ => backgroundMusic
        };
    }

    public static bool IsGameplayScene(string sceneName) =>
        sceneName == "MainGameL1Opera" || sceneName == "Level2Museum" || sceneName == "Level3UndergroundDance";

    /// <summary>Clip for a jukebox track id; null for "theme" or a missing clip.</summary>
    public AudioClip ClipForTrack(string id)
    {
        return id switch
        {
            "sunrise" => rooftopMusic,
            "opera" => backgroundMusic,
            "museum" => level2Music,
            "vault" => level3Music,
            _ => null
        };
    }

    /// <summary>The player's jukebox pick if they chose one, otherwise the level's own theme.</summary>
    public AudioClip GameplayClipFor(string sceneName)
    {
        var picked = ClipForTrack(ShopData.SelectedMusicId);
        return picked != null ? picked : GetClipForScene(sceneName);
    }

    /// <summary>Switches the music right away after the player picks a track in the jukebox.</summary>
    public void ApplySelectedMusic()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (!IsGameplayScene(scene)) return;
        var clip = GameplayClipFor(scene);
        if (clip != null && _musicSource != null && _musicSource.clip == clip && _musicSource.isPlaying) return;
        PlayBackground(clip, useMusicLayer: false);
    }

    /// <summary>Plays the current gameplay track (the jukebox pick or the level theme).</summary>
    public void PlayBackground()
    {
        PlayBackground(GameplayClipFor(SceneManager.GetActiveScene().name), useMusicLayer: false);
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
        if (other.rooftopMusic != null) rooftopMusic = other.rooftopMusic;
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

    /// <summary>
    /// Obstacle bump with its own sound: every laser (low laser, grid, fence, sweep) zaps with the
    /// laser shot, drums boom with the bass drum; anything else uses the default hit sound.
    /// </summary>
    public void PlayObstacleHitSfx(GameObject obstacle)
    {
        var clip = ObstacleClip(obstacle);
        if (clip != null && _sfxSource != null) { _sfxSource.PlayOneShot(clip, sfxVolume); return; }
        PlayObstacleHitSfx();
    }

    /// <summary>The laser / drum obstacle clip for this object, or null.</summary>
    public static AudioClip ObstacleClip(GameObject obstacle)
    {
        if (obstacle == null) return null;
        var lib = FxLibrary.Get();
        if (lib == null) return null;
        var h = obstacle.GetComponent<HealthDecreaseObstacle>();
        string n = ((h != null ? h.DisplayName : "") + " " + obstacle.name).ToLowerInvariant();
        if (n.Contains("laser")) return lib.laserHitSound;
        if (n.Contains("drum")) return lib.drumHitSound;
        return null;
    }

    /// <summary>A laser / drum sound played at a point in the world (e.g. a drum starting to roll).</summary>
    public void PlayObstacleSfxAt(AudioClip clip, Vector3 pos, float volume = 1f)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, pos, sfxVolume * volume);
    }
    static AudioClip _boom;

    /// <summary>Procedural grenade blast: noise crack plus a low rumble, decaying over ~1 s.</summary>
    public void PlayExplosionSfx()
    {
        if (_sfxSource == null) return;
        if (_boom == null)
        {
            const int rate = 44100;
            int n = (int)(rate * 1.1f);
            var data = new float[n];
            float lp = 0f;
            var rng = new System.Random(7);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += (noise - lp) * Mathf.Lerp(0.6f, 0.04f, Mathf.Clamp01(t / 0.25f));   // bright crack, then muffled
                float rumble = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(70f, 38f, t) * t);
                float env = Mathf.Exp(-t * 4.2f) * Mathf.Clamp01(t * 400f);
                data[i] = Mathf.Clamp((lp * 1.6f + rumble * 0.55f) * env, -1f, 1f);
            }
            _boom = AudioClip.Create("GrenadeBoom", n, 1, rate, false);
            _boom.SetData(data, 0);
        }
        _sfxSource.PlayOneShot(_boom, sfxVolume * 1.1f);
    }

    public void PlayGuardShotSfx() => PlayClipOrFallback(obstacleHitSound, 1.9f, 0.45f);
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
