using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives the Victory scene.
/// Flow:
///   1. Scene fades in → "YOU WIN!" title shown
///   2. Narration plays automatically
///   3. After narration → Play Again and Main Menu buttons appear
/// </summary>
public class VictoryHUD : MonoBehaviour
{
    [Header("Scene names")]
    public string gameSceneName     = "MainGameL1";
    public string mainMenuSceneName = "StartScene";

    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public Button          playAgainButton;
    public Button          mainMenuButton;
    public Button          nextButton;

    [Header("Narration lines")]
    [TextArea(2, 5)]
    public string[] narrationLines = new string[]
    {
        "Incredible! You collected all the instruments!",
        "The music of freedom rings through the halls.",
        "You outran the guard and saved the show. Congratulations!"
    };

    public string speakerName = "Narrator";

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Start()
    {
        UICursor.UnlockForMenu();
        Time.timeScale = 1f;

        if (titleText       != null) titleText.text = "YOU WIN!";
        if (playAgainButton != null) playAgainButton.gameObject.SetActive(false);
        if (mainMenuButton  != null) mainMenuButton.gameObject.SetActive(false);

        if (playAgainButton != null) playAgainButton.onClick.AddListener(OnPlayAgain);
        if (mainMenuButton  != null) mainMenuButton.onClick.AddListener(OnMainMenu);

        if (playAgainButton != null) UIButtonRaycastFix.Apply(playAgainButton);
        if (mainMenuButton  != null) UIButtonRaycastFix.Apply(mainMenuButton);

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

    public void OnPlayAgain() { SceneFader.LoadScene(gameSceneName); }
    public void OnMainMenu()  { SceneFader.LoadScene(mainMenuSceneName); }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void OnNarrationComplete()
    {
        AudioManager.Instance?.StopNarrative();
        if (playAgainButton != null) { UIButtonRaycastFix.Apply(playAgainButton); UIButtonRaycastFix.BringToFront(playAgainButton); playAgainButton.gameObject.SetActive(true); }
        if (mainMenuButton  != null) { UIButtonRaycastFix.Apply(mainMenuButton);  UIButtonRaycastFix.BringToFront(mainMenuButton);  mainMenuButton.gameObject.SetActive(true); }
    }

    private NarrationLine[] BuildLines()
    {
        var result = new NarrationLine[narrationLines.Length];
        for (int i = 0; i < narrationLines.Length; i++)
            result[i] = new NarrationLine(narrationLines[i], speakerName);
        return result;
    }
}
