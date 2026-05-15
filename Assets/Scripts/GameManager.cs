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

    // ── Scene names ───────────────────────────────────────────────────────────
    [Header("Scene Names")]
    public string deathSceneName   = "DeathScene";
    public string victorySceneName = "VictoryScene";

    // ── Countdown ─────────────────────────────────────────────────────────────
    [Header("Countdown")]
    public TextMeshProUGUI countdownText;
    public int countdownSeconds = 5;

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
        new NarrationLine("Run! Collect the instruments before the guard catches you!", "Narrator"),
        new NarrationLine("Jump over obstacles and grab power-ups along the way.", "Narrator")
    };

    [Tooltip("Lines shown when the player grabs a power-up (optional mid-game hint).")]
    public NarrationLine[] midGameNarration = new NarrationLine[]
    {
        new NarrationLine("Great pick-up! Keep moving — the guard is right behind you!", "Narrator")
    };

    // ── Internal ──────────────────────────────────────────────────────────────
    private bool _gameOverTriggered = false;
    private bool _victoryTriggered  = false;
    private bool _midGameShown      = false;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        GameStarted = false;
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

        var enemyGO = enemyBase != null ? enemyBase.gameObject : null;
        if (enemyAnimator == null && enemyGO != null)
            enemyAnimator = enemyGO.GetComponentInChildren<Animator>();

        if (gameOverText != null) gameOverText.gameObject.SetActive(false);

        StartCoroutine(StartCountdown());
    }

    // ── Countdown ─────────────────────────────────────────────────────────────
    private IEnumerator StartCountdown()
    {
        if (playerMovement != null) playerMovement.enabled = false;
        if (enemyBase      != null) enemyBase.enabled      = false;
        if (playerAnimator != null) playerAnimator.SetFloat("Speed", 0f);
        if (enemyAnimator  != null) enemyAnimator.SetFloat("Speed", 0f);

        yield return NarrationManager.WaitForSceneFade();
        yield return PlayStartNarrationAndWait();

        for (int i = countdownSeconds; i > 0; i--)
        {
            if (countdownText != null)
            {
                countdownText.text = i.ToString();
                countdownText.gameObject.SetActive(true);
            }
            yield return new WaitForSeconds(1f);
        }

        if (countdownText != null)
        {
            countdownText.text = "GO!";
            yield return new WaitForSeconds(0.8f);
            countdownText.gameObject.SetActive(false);
        }

        // Start gameplay
        GameStarted = true;
        if (playerMovement != null) playerMovement.enabled = true;
        if (enemyBase      != null) enemyBase.enabled      = true;
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
        GameStarted = false;
        StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        if (playerMovement != null) playerMovement.enabled = false;

        if (playerAnimator != null)
        {
            playerAnimator.SetFloat("Speed", 0f);
            playerAnimator.SetBool("IsDead", true);
        }

        // Use unscaled time so these delays work even if Time.timeScale == 0
        // (e.g. narration panel was open when the player died)
        Time.timeScale = 1f;   // always restore time before the death sequence
        yield return new WaitForSecondsRealtime(0.3f);

        if (enemyBase     != null) enemyBase.enabled = false;
        if (enemyAnimator != null) enemyAnimator.SetFloat("Speed", 0f);

        yield return new WaitForSecondsRealtime(1.5f);

        // Show brief GAME OVER text
        if (gameOverText != null)
        {
            gameOverText.text = "GAME OVER";
            gameOverText.gameObject.SetActive(true);
        }

        yield return new WaitForSecondsRealtime(gameOverSceneDelay);

        // Load Death scene (narration plays there)
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
        StartCoroutine(VictorySequence());
    }

    private IEnumerator VictorySequence()
    {
        if (playerMovement != null) playerMovement.enabled = false;
        if (enemyBase      != null) enemyBase.enabled      = false;

        Time.timeScale = 1f;   // restore in case narration had it paused
        yield return new WaitForSecondsRealtime(1.5f);

        SceneFader.LoadScene(victorySceneName);
    }
}
