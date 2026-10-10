using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives the Death / Game Over scene.
/// Flow:
///   1. Scene fades in → baked background art (title is in the image)
///   2. Narration plays automatically (unless useBakedArt)
///   3. After narration → PLAY AGAIN and MAIN MENU buttons appear
/// </summary>
public class DeathHUD : MonoBehaviour
{
    [Header("Scene names")]
    public string gameSceneName  = "MainGameL1Opera";
    public string menuSceneName  = "StartScene";

    [Header("UI References")]
    public TextMeshProUGUI titleText;
    [Tooltip("Shows why the run ended (guard, obstacle, etc.). Auto-created if blank.")]
    public TextMeshProUGUI defeatReasonText;
    [Tooltip("Shows artifacts collected and value from the last run.")]
    public TextMeshProUGUI artifactStatsText;
    public Button          retryButton;      // becomes RESUME in-game
    public Button          mainMenuButton;   // becomes EXIT in-game
    public Button          nextButton;

    [Header("Narration lines")]
    [TextArea(2, 5)]
    public string[] narrationLines = new string[]
    {
        "You were caught by the security guard!",
        "Don't give up — the instruments are still out there.",
        "Try again and make it to the goal!"
    };

    public string speakerName = "";

    [Header("Scoreboard")]
    public ScoreboardHUD scoreboard;

    [Header("Layout")]
    [Tooltip("When true, art includes title and defeat text — skip typewriter narration.")]
    public bool useBakedArt = true;

    public float bakedArtButtonDelay = 2.5f;

    private void Awake()
    {
        GameplayCanvasGuard.EnsureCanvasScale(gameObject);
    }

    private void Start()
    {
        UICursor.UnlockForMenu();
        Time.timeScale = 1f;
        GameOverOverlay.Hide();
        GameplayCanvasGuard.FixAllCanvasesInScene();

        if (string.IsNullOrEmpty(gameSceneName) || GameplaySceneNames.IsGameplayScene(gameSceneName))
            gameSceneName = LevelProgress.GetGameplayScene();

        if (titleText != null)
            titleText.gameObject.SetActive(false);

        EnsureDefeatReasonText();
        if (defeatReasonText != null)
        {
            defeatReasonText.text = RunStats.GetCauseHeadline();
            ApplyDefeatReasonStyle(defeatReasonText);
            defeatReasonText.transform.SetAsLastSibling();
        }

        EnsureArtifactStatsText();
        if (artifactStatsText != null)
        {
            artifactStatsText.text = RunStats.FormatRunSummaryLine();
            ApplyArtifactStatsStyle(artifactStatsText);
            artifactStatsText.transform.SetAsLastSibling();
        }

        // Label the action buttons
        SetButtonLabel(retryButton,    "PLAY AGAIN");
        SetButtonLabel(mainMenuButton, "MAIN MENU");

        // Hide until narration finishes
        if (retryButton    != null) retryButton.gameObject.SetActive(false);
        if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(false);

        if (retryButton    != null) retryButton.onClick.AddListener(OnResume);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnExit);

        if (scoreboard == null)
            scoreboard = GetComponent<ScoreboardHUD>();

        if (retryButton    != null) UIButtonRaycastFix.Apply(retryButton);
        if (mainMenuButton != null) UIButtonRaycastFix.Apply(mainMenuButton);

        StartCoroutine(BeginNarration());
    }

    IEnumerator BeginNarration()
    {
        yield return NarrationManager.WaitForSceneFade();
        GameplayCanvasGuard.FixAllCanvasesInScene();

        if (useBakedArt)
        {
            NarrationManager.Instance?.Cancel();
            HideNarrationOverlay();
            OnNarrationComplete();
            yield break;
        }

        var lines = BuildLines();
        if (NarrationManager.Instance != null)
        {
            var nm = NarrationManager.Instance;
            nm.autoAdvance   = true;
            nm.autoDelay     = 2.5f;
            nm.advanceButton = nm.GetAdvanceButton(nextButton);
            nm.Play(lines, OnNarrationComplete);
        }
        else
        {
            OnNarrationComplete();
        }
    }

    // ── Callbacks ─────────────────────────────────────────────────────────────

    /// <summary>PLAY AGAIN — restart the current level.</summary>
    public void OnResume()
    {
        SceneFader.LoadScene(LevelProgress.GetGameplayScene());
    }

    /// <summary>MAIN MENU — return to the start scene.</summary>
    public void OnExit()
    {
        StartSceneUI.OpenDirectlyToMenu = true;
        SceneFader.LoadScene(menuSceneName);
    }

    public void OnScoreboardBack()
    {
        scoreboard?.Hide();
        OnNarrationComplete();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void OnNarrationComplete()
    {
        AudioManager.Instance?.StopNarrative();
        HideNarrationOverlay();
        // Apply raycast fix but do NOT BringToFront — reparenting breaks anchor layout
        if (retryButton    != null) { UIButtonRaycastFix.Apply(retryButton);    retryButton.gameObject.SetActive(true); }
        if (mainMenuButton != null) { UIButtonRaycastFix.Apply(mainMenuButton); mainMenuButton.gameObject.SetActive(true); }
    }

    private void HideNarrationOverlay()
    {
        if (NarrationManager.Instance != null && NarrationManager.Instance.narrationPanel != null)
            NarrationManager.Instance.narrationPanel.SetActive(false);

        var panel = GameObject.Find("NarrationPanel");
        if (panel != null)
            panel.SetActive(false);
    }

    private static void SetButtonLabel(Button btn, string label)
    {
        if (btn == null) return;
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = label;
    }

    private NarrationLine[] BuildLines()
    {
        var lines = RunStats.GetCauseNarration();
        var result = new NarrationLine[lines.Length];
        for (int i = 0; i < lines.Length; i++)
            result[i] = new NarrationLine(lines[i], speakerName);
        return result;
    }

    void EnsureDefeatReasonText()
    {
        if (defeatReasonText != null) return;

        defeatReasonText = transform.Find("DefeatReasonText")?.GetComponent<TextMeshProUGUI>();
        if (defeatReasonText != null) return;

        var canvas = GetComponent<RectTransform>();
        if (canvas == null) return;

        var go = new GameObject("DefeatReasonText");
        go.transform.SetParent(canvas, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.33f, 0.52f);
        rt.anchorMax = new Vector2(0.67f, 0.63f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        defeatReasonText = go.AddComponent<TextMeshProUGUI>();
        TmpUiUtility.EnsureFont(defeatReasonText);
        ApplyDefeatReasonStyle(defeatReasonText);
        defeatReasonText.raycastTarget = false;
    }

    static void ApplyDefeatReasonStyle(TextMeshProUGUI text)
    {
        if (text == null) return;
        text.fontSize = 36;
        text.enableAutoSizing = true;
        text.fontSizeMin = 20;
        text.fontSizeMax = 36;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.45f, 0.2f, 1f);
    }

    void EnsureArtifactStatsText()
    {
        if (artifactStatsText != null) return;

        artifactStatsText = transform.Find("ArtifactStatsText")?.GetComponent<TextMeshProUGUI>();
        if (artifactStatsText != null) return;

        var canvas = GetComponent<RectTransform>();
        if (canvas == null) return;

        var go = new GameObject("ArtifactStatsText");
        go.transform.SetParent(canvas, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.33f, 0.42f);
        rt.anchorMax = new Vector2(0.67f, 0.50f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        artifactStatsText = go.AddComponent<TextMeshProUGUI>();
        TmpUiUtility.EnsureFont(artifactStatsText);
        ApplyArtifactStatsStyle(artifactStatsText);
        artifactStatsText.raycastTarget = false;
    }

    static void ApplyArtifactStatsStyle(TextMeshProUGUI text)
    {
        if (text == null) return;
        text.fontSize = 30;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
    }
}
