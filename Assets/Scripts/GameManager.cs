using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Manages the pre-game countdown (5-4-3-2-1-GO!) then enables gameplay.
/// Also handles the Game Over and Victory sequences, scene transitions,
/// and in-game narration hints.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool GameStarted { get; private set; } = false;
    public static bool EndlessRun;
    public static bool IsGameOver { get; private set; }

    // ── Scene names ───────────────────────────────────────────────────────────
    [Header("Scene Names")]
    public string deathSceneName   = "DeathScene";
    public string victorySceneName = "VictoryScene";
    public string levelIntroSceneName = "Level2IntroScene";

    [Header("Level")]
    [Tooltip("If 0, uses LevelProgress.CurrentLevel.")]
    public int levelNumber;

    // ── Countdown ─────────────────────────────────────────────────────────────
    [Header("Countdown")]
    public GameObject countdownOverlay;
    public TextMeshProUGUI countdownText;
    public int countdownSeconds = 3;

    // ── Game Over ─────────────────────────────────────────────────────────────
    [Header("Game Over")]
    public TextMeshProUGUI gameOverText;
    [Tooltip("Seconds after GAME OVER text appears before loading DeathScene")]
    public float gameOverSceneDelay = 2.5f;

    // ── Scene References ──────────────────────────────────────────────────────
    [Header("Scene References")]
    public PlayerMovement playerMovement;
    public EnemyBase      enemyBase;
    public Animator       playerAnimator;
    public Animator       enemyAnimator;

    // ── In-game narration ─────────────────────────────────────────────────────
    [Header("In-Game Narration")]
    [Tooltip("Controller for the in-game narration popup (has the RESUME button).")]
    public InGameNarrationController inGameNarration;

    [Tooltip("Lines shown as a popup right after GO! at the start of the run.")]
    public NarrationLine[] startNarration = new NarrationLine[]
    {
        new NarrationLine("Run! Collect the instruments before the guard catches you!"),
        new NarrationLine("Jump over obstacles and grab power-ups along the way.")
    };

    [Tooltip("Lines shown when the player grabs a power-up (optional mid-game hint).")]
    public NarrationLine[] midGameNarration = new NarrationLine[]
    {
        new NarrationLine("Great pick-up! Keep moving — the guard is right behind you!")
    };

    // ── Internal ──────────────────────────────────────────────────────────────
    private bool _gameOverTriggered = false;
    private bool _victoryTriggered  = false;
    private bool _midGameShown      = false;
    private LevelDefinition _levelDef;

    public int ActiveLevel => levelNumber > 0 ? levelNumber : LevelProgress.CurrentLevel;

    public void ApplyLevelDefinition(LevelDefinition def)
    {
        _levelDef = def;
        if (def == null) return;
        levelNumber = def.levelNumber;
        levelIntroSceneName = def.nextIntroScene;
        startNarration = new NarrationLine[]
        {
            new NarrationLine(def.startHint1),
            new NarrationLine(def.startHint2)
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        GameStarted = false;
        EndlessRun = false;
        IsGameOver = false;
        _gameOverTriggered = false;
        _victoryTriggered = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (GetComponent<LevelBootstrap>() == null)
            gameObject.AddComponent<LevelBootstrap>();

        EnsureGameOverCanvas();
        GameplayCanvasGuard.FixAllCanvasesInScene();
    }

    void EnsureGameOverCanvas()
    {
        if (gameOverText == null) return;
        var canvas = gameOverText.GetComponentInParent<Canvas>();
        if (canvas != null)
            GameplayCanvasGuard.EnsureCanvasScale(canvas.gameObject);
    }

    private void LateUpdate()
    {
        GameplayCanvasGuard.FixAllCanvasesInScene();
        EnsureGameplayRunning();
    }

    void EnsureGameplayRunning()
    {
        if (!GameStarted || IsGameOver || _gameOverTriggered || _victoryTriggered)
            return;

        var pause = FindAnyObjectByType<PauseMenuHUD>();
        if (pause != null && pause.IsOpen)
            return;

        if (Time.timeScale != 1f)
            Time.timeScale = 1f;

        if (AudioListener.pause)
            AudioListener.pause = false;

        if (playerMovement != null && !playerMovement.enabled)
            playerMovement.enabled = true;

        if (enemyBase != null && !enemyBase.enabled)
            enemyBase.enabled = true;
    }

    private void Start()
    {
        // Auto-find references if not assigned in Inspector
        if (playerMovement == null) playerMovement = FindAnyObjectByType<PlayerMovement>();
        if (enemyBase      == null) enemyBase      = FindAnyObjectByType<EnemyBase>();

        var playerGO = playerMovement != null ? playerMovement.gameObject : null;
        if (playerAnimator == null && playerGO != null)
        {
            var animCtrl = playerGO.GetComponent<PlayerAnimationController>();
            playerAnimator = animCtrl != null && animCtrl.characterAnimator != null
                ? animCtrl.characterAnimator
                : playerGO.GetComponentInChildren<Animator>();
        }

        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsDead", false);
            playerAnimator.SetFloat("Speed", 0f);
        }

        var enemyGO = enemyBase != null ? enemyBase.gameObject : null;
        if (enemyAnimator == null && enemyGO != null)
            enemyAnimator = enemyGO.GetComponentInChildren<Animator>();

        if (gameOverText != null) gameOverText.gameObject.SetActive(false);

        EnsureGameOverCanvas();
        GameplayCanvasGuard.FixAllCanvasesInScene();

        var pickup = playerGO != null ? playerGO.GetComponent<PickupBase>() : null;
        if (pickup != null)
        {
            pickup.ResetForNewRun();
        }

        if (levelNumber <= 0)
            levelNumber = LevelProgress.CurrentLevel;
        if (_levelDef == null)
            ApplyLevelDefinition(LevelCatalog.Get(ActiveLevel));

        StartCoroutine(StartCountdown());
    }

    // ── Countdown ─────────────────────────────────────────────────────────────
    private IEnumerator StartCountdown()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (playerMovement != null) playerMovement.enabled = false;
        if (enemyBase      != null) enemyBase.enabled      = false;
        if (playerAnimator != null) playerAnimator.SetFloat("Speed", 0f);
        if (enemyAnimator  != null) enemyAnimator.SetFloat("Speed", 0f);

        yield return NarrationManager.WaitForSceneFade();
        NarrationManager.Instance?.Cancel();

        // Play the level's opening cutscene (Opera curtains → L1, Museum doors → L2, Disco ball → L3)
        var def = _levelDef ?? LevelCatalog.Get(ActiveLevel);
        if (def.gameplayCutscene != LevelCutsceneType.None)
            yield return LevelCutscenePlayer.PlayAndWait(def.gameplayCutscene);

        // No countdown — gameplay begins immediately after cutscene
        GameStarted = true;
        RunStats.MarkRunStart();   // start the run timer
        if (playerMovement != null) playerMovement.enabled = true;
        if (enemyBase      != null) enemyBase.enabled      = true;

        FindAnyObjectByType<RunLengthController>()?.PopulatePickupsForRun();

        TriggerLevelStartDialogue();
    }

    void TriggerLevelStartDialogue()
    {
        if (AIDialogueService.Instance == null) return;
        var def = _levelDef ?? LevelCatalog.Get(ActiveLevel);
        // Pass startHint1 as detail so the LLM prompt gets scene-specific context
        // (drum traps / lasers / curtains) rather than the generic codename.
        AIDialogueService.Instance.Speak(new DialogueContext
        {
            speakerName      = "Narrator",
            levelName        = def.displayName,
            eventType        = DialogueEvent.LevelStart,
            detail           = def.startHint1,
            artifactsRequired = def.artifactsToWin
        });
    }

    // ── In-game narration ─────────────────────────────────────────────────────
    private IEnumerator PlayStartNarrationAndWait()
    {
        if (startNarration == null || startNarration.Length == 0) yield break;
        if (NarrationManager.Instance == null) yield break;

        NarrationManager.Instance.autoAdvance = true;
        NarrationManager.Instance.autoDelay   = 3f;
        yield return NarrationManager.Instance.PlayAndWait(startNarration);
    }

    /// <summary>
    /// Call this from PickupBase or power-up scripts to trigger a mid-game hint
    /// (fires only once per run).
    /// </summary>
    public void TriggerMidGameNarration()
    {
        if (_midGameShown || NarrationManager.Instance == null) return;
        if (midGameNarration == null || midGameNarration.Length == 0) return;

        _midGameShown = true;
        NarrationManager.Instance.autoAdvance = true;
        NarrationManager.Instance.autoDelay   = 2f;
        NarrationManager.Instance.Play(midGameNarration);
    }

    // ── Game Over ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Called from PlayerAnimationController or EnemyBase the first time the player dies.
    /// </summary>
    public void TriggerGameOver()
    {
        if (_gameOverTriggered) return;
        _gameOverTriggered = true;
        IsGameOver = true;
        GameStarted = false;

        Time.timeScale = 1f;
        AudioListener.pause = false;
        UICursor.UnlockForMenu();
        NarrationManager.Instance?.Cancel();
        GameplayDialogueOverlay.Hide();

        ShowGameOverOverlay();

        StartCoroutine(GameOverSequence());
    }

    void ShowGameOverOverlay()
    {
        var pickup = FindAnyObjectByType<PickupBase>();
        if (pickup != null)
            RunStats.SaveFrom(pickup);

        string body = "GAME OVER\n\n" + RunStats.GetCauseHeadline() +
                      "\n\n" + RunStats.FormatRunSummaryLine();
        GameOverOverlay.Show(body);
        // PresentGameOverText() removed — GameOverOverlay is the sole display.
    }

    private IEnumerator GameOverSequence()
    {
        var pickup = FindAnyObjectByType<PickupBase>();
        RunStats.SaveFrom(pickup);

        if (playerMovement != null) playerMovement.enabled = false;

        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsDead", true);
            playerAnimator.SetFloat("Speed", 0f);
        }

        EnsureGameOverCanvas();

        Time.timeScale = 1f;
        yield return new WaitForSecondsRealtime(0.3f);

        if (enemyBase     != null) enemyBase.enabled = false;
        if (enemyAnimator != null) enemyAnimator.SetFloat("Speed", 0f);

        yield return new WaitForSecondsRealtime(1.2f);

        // GameOverOverlay already displayed the text in ShowGameOverOverlay().
        // PresentGameOverText() call removed to prevent the double-text bug.

        yield return new WaitForSecondsRealtime(gameOverSceneDelay);

        GameOverOverlay.Hide();
        if (string.IsNullOrEmpty(deathSceneName))
            deathSceneName = "DeathScene";

        SceneFader.LoadScene(deathSceneName);
    }

    // ── Victory ───────────────────────────────────────────────────────────────
    /// <summary>
    /// Loads the victory scene after the win condition is met (ArtifactsToWin artifacts collected).
    /// </summary>
    public void TriggerVictory()
    {
        if (_victoryTriggered) return;

        PickupBase pickup = playerMovement != null
            ? playerMovement.GetComponent<PickupBase>()
            : null;
        if (pickup != null && pickup.artifactAmount < PickupBase.ArtifactsToWin)
            return;

        _victoryTriggered = true;
        GameStarted = false;
        GameplayDialogueOverlay.Hide();
        StartCoroutine(VictorySequence());
    }

    private IEnumerator VictorySequence()
    {
        if (playerMovement != null) playerMovement.enabled = false;
        if (enemyBase      != null) enemyBase.enabled      = false;

        Time.timeScale = 1f;
        yield return new WaitForSecondsRealtime(1.5f);

        // Save run stats so the VictoryScene scoreboard can display them.
        var pickup = playerMovement != null
            ? playerMovement.GetComponent<PickupBase>()
            : FindAnyObjectByType<PickupBase>();
        RunStats.SaveFrom(pickup);

        // Record the completed level but do NOT advance yet.
        // The "Next Level" button on the VictoryScene scoreboard does the advance.
        LevelProgress.SetLevel(ActiveLevel);

        // Always go straight to the Game Center / scoreboard screen.
        // The Level2IntroScene / Level3IntroScene transitions are removed —
        // level selection happens from the scoreboard via the Next Level button.
        SceneFader.LoadScene(victorySceneName);
    }

    void PresentGameOverText()
    {
        GameplayCanvasGuard.FixAllCanvasesInScene();

        if (gameOverText == null)
        {
            var go = GameObject.Find("GameOverText");
            if (go != null)
                gameOverText = go.GetComponent<TextMeshProUGUI>();
        }

        if (gameOverText == null) return;

        var canvas = gameOverText.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            GameplayCanvasGuard.EnsureCanvasScale(canvas.gameObject);
            canvas.overrideSorting = true;
            canvas.sortingOrder = 200;
        }

        var rt = gameOverText.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(900f, 420f);
        rt.localScale = Vector3.one;

        TmpUiUtility.EnsureFont(gameOverText);
        gameOverText.text = "GAME OVER\n\n" + RunStats.GetCauseHeadline() +
                            "\n\n" + RunStats.FormatRunSummaryLine();
        gameOverText.alignment = TextAlignmentOptions.Center;
        gameOverText.enableAutoSizing = false;
        gameOverText.fontSize = 48f;
        gameOverText.color = Color.white;
        gameOverText.gameObject.SetActive(true);
        gameOverText.transform.SetAsLastSibling();
    }
}
