using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives the Victory / Game Center screen.
///
/// Flow:
///   1. Scene fades in → "YOU WIN!" title + scoreboard shown immediately
///   2. Narration plays automatically (auto-advances)
///   3. After narration → action buttons appear:
///        • NEXT LEVEL  — advances to the next gameplay scene  (hidden on final level)
///        • PLAY AGAIN  — replays the just-completed level
///        • MAIN MENU   — returns to StartScene
/// </summary>
public class VictoryHUD : MonoBehaviour
{
    [Header("Scene names")]
    public string mainMenuSceneName = "StartScene";

    [Header("UI References")]
    public TextMeshProUGUI titleText;
    [Tooltip("Optional dedicated text field for the scoreboard. " +
             "If null the score is appended to titleText.")]
    public TextMeshProUGUI scoreText;
    public Button          playAgainButton;
    public Button          mainMenuButton;
    [Tooltip("'Next Level' button — automatically hidden when the player " +
             "has completed the final level.")]
    public Button          nextButton;

    [Header("Narration lines")]
    [TextArea(2, 5)]
    public string[] narrationLines = new string[]
    {
        "Level complete! The artifact is secured.",
        "Every step forward brings you closer to the final score.",
        "Ready for the next challenge?"
    };

    public string speakerName = "";

    // ── Internal ──────────────────────────────────────────────────────────────
    private int  _completedLevel;
    private bool _hasNextLevel;
    private bool _namePromptOpen;
    private bool _hallPending;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        GameplayCanvasGuard.EnsureCanvasScale(gameObject);
    }

    private void Start()
    {
        UICursor.UnlockForMenu();
        Time.timeScale = 1f;

        _completedLevel = LevelProgress.CurrentLevel;
        _hasNextLevel   = _completedLevel < LevelProgress.MaxLevel;

        // ── Title ─────────────────────────────────────────────────────────────
        if (titleText != null) titleText.text = "YOU WIN!";

        // ── Scoreboard ────────────────────────────────────────────────────────
        ShowScoreboard();

        // First win: ask for the player's name so records are saved under it.
        if (!PlayerRecords.HasPlayerName)
        {
            _namePromptOpen = true;
            RecordsUI.ShowNamePrompt(() =>
            {
                _namePromptOpen = false;
                ShowScoreboard();
                if (_hallPending) { _hallPending = false; RecordsUI.ShowHallOfFame(null); }
            });
        }

        // ── Buttons — hidden until narration finishes ──────────────────────
        SetButtonVisible(playAgainButton, false);
        SetButtonVisible(mainMenuButton,  false);
        SetButtonVisible(nextButton,      false);

        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(OnPlayAgain);
            UIButtonRaycastFix.Apply(playAgainButton);
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenu);
            UIButtonRaycastFix.Apply(mainMenuButton);
        }
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextLevel);
            UIButtonRaycastFix.Apply(nextButton);

            // Update the label so it's clear what the button does
            var label = nextButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = _hasNextLevel ? "NEXT LEVEL" : "FINISH";
        }

        StartCoroutine(BeginNarration());
    }

    // ── Scoreboard ────────────────────────────────────────────────────────────

    private void ShowScoreboard()
    {
        // Try to find a dedicated ScoreText object in the scene if not assigned
        if (scoreText == null)
        {
            var go = GameObject.Find("ScoreText");
            if (go != null) scoreText = go.GetComponent<TextMeshProUGUI>();
        }
        if (scoreText == null)
            scoreText = RecordsUI.ShowResultsPanel("LEVEL RESULTS", OnChangeName);

        var def       = LevelCatalog.Get(_completedLevel);
        string runner = PlayerRecords.HasPlayerName ? $"<color=#5AD9FF>RUNNER  {PlayerRecords.PlayerName}</color>\n" : "";
        string header = $"{runner}<b>Level {_completedLevel}  —  {def.displayName}</b>";
        string body   = RunStats.FormatScoreSummary() + "\n" + RecordLine();

        if (scoreText != null)
        {
            scoreText.text = $"{header}\n\n{body}";
        }
        else if (titleText != null)
        {
            // Fallback: append score under the YOU WIN title using rich-text size tag
            titleText.text = $"YOU WIN!\n\n<size=55%>{header}\n{body}</size>";
        }
    }

    private void OnChangeName()
    {
        if (_namePromptOpen) return;
        _namePromptOpen = true;
        RecordsUI.ShowNamePrompt(() =>
        {
            _namePromptOpen = false;
            ShowScoreboard();
            if (_hallPending) { _hallPending = false; RecordsUI.ShowHallOfFame(null); }
        });
    }

    /// <summary>Saved-record summary for this level: new best or the best to beat.</summary>
    private string RecordLine()
    {
        var rec = PlayerRecords.LastRecorded;
        var best = PlayerRecords.Best(_completedLevel);
        string who = PlayerRecords.PlayerName;
        if (rec != null && rec.level == _completedLevel && PlayerRecords.LastWasNewBest)
            return $"<color=#FFD65A><b>NEW BEST!</b>  {rec.score:N0} pts  -  {rec.stars}/3 stars  -  saved for {who}</color>";
        if (best != null)
            return $"<color=#5AD9FF>Best: {best.score:N0} pts  -  {best.stars}/3 stars  ({best.playerName})</color>";
        return "";
    }

    // ── Narration ─────────────────────────────────────────────────────────────

    IEnumerator BeginNarration()
    {
        yield return NarrationManager.WaitForSceneFade();

        var lines = BuildLines();
        if (NarrationManager.Instance != null)
        {
            var nm = NarrationManager.Instance;
            nm.autoAdvance   = true;
            nm.autoDelay     = 2.5f;
            nm.advanceButton = null;   // auto-advances; no manual button needed
            nm.Play(lines, OnNarrationComplete);
        }
        else
        {
            OnNarrationComplete();
        }
    }

    // ── Callbacks ─────────────────────────────────────────────────────────────

    private void OnNarrationComplete()
    {
        AudioManager.Instance?.StopNarrative();
        HideNarrationOverlay();

        // Reveal action buttons
        SetButtonVisible(playAgainButton, true);
        SetButtonVisible(mainMenuButton,  true);

        // Next Level only appears if there's somewhere to go
        if (nextButton != null)
            SetButtonVisible(nextButton, true);   // label already says FINISH if final level

        // Final level won: show the full campaign scores and the Hall of Fame.
        if (!_hasNextLevel)
        {
            if (_namePromptOpen) _hallPending = true;
            else RecordsUI.ShowHallOfFame(null);
        }
    }

    public void OnPlayAgain()
    {
        // Replay the level the player just completed
        SceneFader.LoadScene(LevelCatalog.GetGameplayScene(_completedLevel));
    }

    public void OnNextLevel()
    {
        if (_hasNextLevel)
        {
            LevelProgress.AdvanceLevel();
            int next = LevelProgress.CurrentLevel;
            SceneFader.LoadScene(LevelCatalog.GetGameplayScene(next));
        }
        else
        {
            LevelProgress.ResetToFirstLevel();
            SceneFader.LoadScene(mainMenuSceneName);
        }
    }

    public void OnMainMenu()
    {
        SceneFader.LoadScene(mainMenuSceneName);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void SetButtonVisible(Button btn, bool visible)
    {
        if (btn != null) btn.gameObject.SetActive(visible);
    }

    private void HideNarrationOverlay()
    {
        if (NarrationManager.Instance?.narrationPanel != null)
            NarrationManager.Instance.narrationPanel.SetActive(false);

        var panel = GameObject.Find("NarrationPanel");
        if (panel != null) panel.SetActive(false);
    }

    private NarrationLine[] BuildLines()
    {
        if (narrationLines == null || narrationLines.Length == 0)
            return System.Array.Empty<NarrationLine>();

        var result = new NarrationLine[narrationLines.Length];
        for (int i = 0; i < narrationLines.Length; i++)
            result[i] = new NarrationLine(narrationLines[i], speakerName);
        return result;
    }
}
