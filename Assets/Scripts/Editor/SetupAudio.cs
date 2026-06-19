using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Wires all audio clips and adds AudioManager + ButtonSoundPlayer to every scene.
/// Run via  Tools → Setup Audio.
/// </summary>
public class SetupAudio
{
    // Audio clip paths
    private const string BACKGROUND = "Assets/Audios/background_sound.mp3";
    private const string LEVEL2_MUSIC = "Assets/Audios/level2museumSound.mp3";
    private const string LEVEL3_MUSIC = "Assets/Audios/level3GameSound.mp3";
    private const string BUTTON     = "Assets/Audios/button_sound.mp3";
    private const string COUNTDOWN  = "Assets/Audios/count_down_sound.mp3";
    private const string VICTORY    = "Assets/Audios/game_victory_sound.mp3";
    private const string GAMEOVER   = "Assets/Audios/piano_GameOver_Sound.mp3";
    private const string NARRATIVE  = "Assets/Audios/narrative_mp3.mp3";
    private const string PICKUP_ARTIFACT = "Assets/Audios/Pickups/pickup_artifact.wav";
    private const string PICKUP_HEALTH   = "Assets/Audios/Pickups/pickup_health.wav";
    private const string PICKUP_SNEAK    = "Assets/Audios/Pickups/pickup_sneak.wav";
    private const string PICKUP_SPEED    = "Assets/Audios/Pickups/pickup_speed.wav";
    private const string PICKUP_JUMP     = "Assets/Audios/Pickups/pickup_jump.wav";
    private const string OBSTACLE_HIT    = "Assets/Audios/Pickups/obstacle_hit.wav";
    private const string OBSTACLE_SLOW   = "Assets/Audios/Pickups/obstacle_slowdown.wav";

    private static readonly string[] Scenes = {
        "Assets/Scenes/StartScene.unity",
        "Assets/Scenes/MainGameL1Opera.unity",
        "Assets/Scenes/Level2Museum.unity",
        "Assets/Scenes/Level3UndergroundDance.unity",
        "Assets/Scenes/DeathScene.unity",
        "Assets/Scenes/VictoryScene.unity",
    };

    [MenuItem("Tools/Setup Audio")]
    public static void Setup()
    {
        if (!System.IO.File.Exists(PICKUP_ARTIFACT))
            PickupSfxGenerator.GenerateAll();

        FixCountdownImport();

        string currentScene = EditorSceneManager.GetActiveScene().path;

        foreach (string scenePath in Scenes)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            SetupSceneAudio(scene.name);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SetupAudio] Wired audio in {scene.name}");
        }

        EditorSceneManager.OpenScene(currentScene);
        Debug.Log("[SetupAudio] Done — all scenes have AudioManager and button sounds.");
    }

    private static void SetupSceneAudio(string sceneName)
    {
        // Load clips
        var bgClip        = AssetDatabase.LoadAssetAtPath<AudioClip>(BACKGROUND);
        var level2Clip    = AssetDatabase.LoadAssetAtPath<AudioClip>(LEVEL2_MUSIC);
        var level3Clip    = AssetDatabase.LoadAssetAtPath<AudioClip>(LEVEL3_MUSIC);
        var btnClip       = AssetDatabase.LoadAssetAtPath<AudioClip>(BUTTON);
        var cdClip        = AssetDatabase.LoadAssetAtPath<AudioClip>(COUNTDOWN);
        var victoryClip   = AssetDatabase.LoadAssetAtPath<AudioClip>(VICTORY);
        var gameOverClip  = AssetDatabase.LoadAssetAtPath<AudioClip>(GAMEOVER);
        var narrativeClip = AssetDatabase.LoadAssetAtPath<AudioClip>(NARRATIVE);

        // ── AudioManager ──────────────────────────────────────────────────────
        var existing = Object.FindAnyObjectByType<AudioManager>();
        if (existing == null)
        {
            var go = new GameObject("AudioManager");
            existing = go.AddComponent<AudioManager>();
        }

        existing.backgroundMusic = bgClip;
        existing.backgroundMusicLayer = bgClip;
        existing.level2Music = level2Clip;
        existing.level3Music = level3Clip;
        existing.buttonSound     = btnClip;
        existing.countdownSound  = cdClip;
        existing.victorySound    = victoryClip;
        existing.gameOverSound   = gameOverClip;
        existing.narrativeSound  = narrativeClip;
        existing.artifactPickupSound = AssetDatabase.LoadAssetAtPath<AudioClip>(PICKUP_ARTIFACT);
        existing.healthPickupSound   = AssetDatabase.LoadAssetAtPath<AudioClip>(PICKUP_HEALTH);
        existing.sneakPickupSound    = AssetDatabase.LoadAssetAtPath<AudioClip>(PICKUP_SNEAK);
        existing.speedPickupSound    = AssetDatabase.LoadAssetAtPath<AudioClip>(PICKUP_SPEED);
        existing.jumpPickupSound     = AssetDatabase.LoadAssetAtPath<AudioClip>(PICKUP_JUMP);
        existing.obstacleHitSound    = AssetDatabase.LoadAssetAtPath<AudioClip>(OBSTACLE_HIT);
        existing.slowDownObstacleSound = AssetDatabase.LoadAssetAtPath<AudioClip>(OBSTACLE_SLOW);
        EditorUtility.SetDirty(existing.gameObject);

        var musicSource = existing.GetComponent<AudioSource>();
        if (musicSource != null)
        {
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            EditorUtility.SetDirty(musicSource);
        }

        // ── ButtonSoundPlayer on every Button ─────────────────────────────────
        var allButtons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (var btn in allButtons)
        {
            if (btn.GetComponent<ButtonSoundPlayer>() == null)
            {
                btn.gameObject.AddComponent<ButtonSoundPlayer>();
                EditorUtility.SetDirty(btn.gameObject);
            }
        }

        // ── InGameNarrationController (MainGameL1Opera only) ─────────────────
        if (sceneName == "MainGameL1Opera")
        {
            var narPanel = GameObject.Find("NarrationPanel");
            if (narPanel != null)
            {
                var ctrl = narPanel.GetComponentInParent<InGameNarrationController>();
                if (ctrl == null)
                {
                    // Add to Canvas
                    var canvas = GameObject.Find("Canvas");
                    if (canvas != null)
                    {
                        ctrl = canvas.GetComponent<InGameNarrationController>()
                               ?? canvas.AddComponent<InGameNarrationController>();

                        ctrl.narrationPanel = narPanel;

                        var resumeBtn = narPanel.transform.Find("NextButton");
                        if (resumeBtn != null)
                        {
                            ctrl.resumeButton = resumeBtn.GetComponent<Button>();
                            // Rename label to RESUME
                            var lbl = resumeBtn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                            if (lbl != null) lbl.text = "RESUME";
                        }

                        var nm = canvas.GetComponent<NarrationManager>();
                        if (nm != null)
                        {
                            ctrl.narrationText = nm.narrationText;
                            ctrl.speakerText   = nm.speakerText;
                        }

                        EditorUtility.SetDirty(canvas);
                    }
                }
            }
        }
    }

    private static void FixCountdownImport()
    {
        var importer = AssetImporter.GetAtPath(COUNTDOWN) as AudioImporter;
        if (importer == null) return;

        importer.forceToMono = true;
        var settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = 1f;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
    }
}
