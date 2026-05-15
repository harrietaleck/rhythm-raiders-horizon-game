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
    private const string BUTTON     = "Assets/Audios/button_sound.mp3";
    private const string COUNTDOWN  = "Assets/Audios/count_down_sound.mp3";
    private const string VICTORY    = "Assets/Audios/game_victory_sound.mp3";
    private const string GAMEOVER   = "Assets/Audios/piano_GameOver_Sound.mp3";
    private const string NARRATIVE  = "Assets/Audios/narrative_mp3.mp3";

    private static readonly string[] Scenes = {
        "Assets/Scenes/StartScene.unity",
        "Assets/Scenes/MainGameL1.unity",
        "Assets/Scenes/DeathScene.unity",
        "Assets/Scenes/VictoryScene.unity",
    };

    [MenuItem("Tools/Setup Audio")]
    public static void Setup()
    {
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
        existing.buttonSound     = btnClip;
        existing.countdownSound  = cdClip;
        existing.victorySound    = victoryClip;
        existing.gameOverSound   = gameOverClip;
        existing.narrativeSound  = narrativeClip;
        EditorUtility.SetDirty(existing.gameObject);

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

        // ── InGameNarrationController (MainGameL1 only) ───────────────────────
        if (sceneName == "MainGameL1")
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
}
