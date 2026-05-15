using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives the Death / Game Over scene.
/// Flow:
///   1. Scene fades in → "GAME OVER" title shown
///   2. Narration plays automatically
///   3. After narration → RESUME and EXIT buttons appear
///      • RESUME → reloads MainGameL1
///      • EXIT   → quits the application
/// </summary>
public class DeathHUD : MonoBehaviour
{
    [Header("Scene names")]
    public string gameSceneName = "MainGameL1";

    [Header("UI References")]
    public TextMeshProUGUI titleText;
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

    public string speakerName = "Narrator";

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Start()
    {
        UICursor.UnlockForMenu();
        Time.timeScale = 1f;

        if (titleText != null) titleText.text = "GAME OVER";

        // Label the action buttons
        SetButtonLabel(retryButton,    "RESUME");
        SetButtonLabel(mainMenuButton, "EXIT");

        // Hide until narration finishes
        if (retryButton    != null) retryButton.gameObject.SetActive(false);
        if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(false);

        if (retryButton    != null) retryButton.onClick.AddListener(OnResume);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnExit);

        if (retryButton    != null) UIButtonRaycastFix.Apply(retryButton);
        if (mainMenuButton != null) UIButtonRaycastFix.Apply(mainMenuButton);

        StartCoroutine(BeginNarration());
    }

    IEnumerator BeginNarration()
    {
        yield return NarrationManager.WaitForSceneFade();

        var lines = BuildLines();
        if (NarrationManager.Instance != null)
        {
            NarrationManager.Instance.autoAdvance   = false;
            NarrationManager.Instance.advanceButton = nextButton;
            NarrationManager.Instance.Play(lines, OnNarrationComplete);
        }
        else
        {
            OnNarrationComplete();
        }
    }

    // ── Callbacks ─────────────────────────────────────────────────────────────

    /// <summary>RESUME — reload the game scene.</summary>
    public void OnResume()
    {
        SceneFader.LoadScene(gameSceneName);
    }

    /// <summary>EXIT — quit the application (stops Play mode in Editor).</summary>
    public void OnExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void OnNarrationComplete()
    {
        AudioManager.Instance?.StopNarrative();
        if (retryButton    != null) { UIButtonRaycastFix.Apply(retryButton);    UIButtonRaycastFix.BringToFront(retryButton);    retryButton.gameObject.SetActive(true); }
        if (mainMenuButton != null) { UIButtonRaycastFix.Apply(mainMenuButton); UIButtonRaycastFix.BringToFront(mainMenuButton); mainMenuButton.gameObject.SetActive(true); }
    }

    private static void SetButtonLabel(Button btn, string label)
    {
        if (btn == null) return;
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = label;
    }

    private NarrationLine[] BuildLines()
    {
        var result = new NarrationLine[narrationLines.Length];
        for (int i = 0; i < narrationLines.Length; i++)
            result[i] = new NarrationLine(narrationLines[i], speakerName);
        return result;
    }
}
