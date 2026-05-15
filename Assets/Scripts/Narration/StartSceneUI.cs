using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives the Start / Intro scene.
/// Flow:
///   1. Scene fades in → narrative audio starts + typewriter narration plays
///   2. Player clicks NEXT to advance each line (audio keeps playing)
///   3. After all lines → audio fades out, PLAY button appears
///   4. PLAY → fades to MainGameL1
/// </summary>
public class StartSceneUI : MonoBehaviour
{
    [Header("Scene to load")]
    public string gameSceneName = "MainGameL1";

    [Header("Buttons")]
    public Button playButton;
    public Button nextButton;

    [Header("Narration lines (edit text in Inspector)")]
    [TextArea(2, 5)]
    public string[] narrationLines = new string[]
    {
        "Welcome to Rhythm Raiders!",
        "You are a musician on the run. Collect all the instruments before the security guard catches you!",
        "Use the arrow keys or WASD to move, and Space to jump.",
        "Avoid obstacles, collect power-ups, and reach the goal. Good luck!"
    };

    public string speakerName = "Narrator";

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Start()
    {
        UICursor.UnlockForMenu();
        Time.timeScale = 1f;

        if (playButton != null) playButton.gameObject.SetActive(false);

        if (playButton != null)
            UIButtonRaycastFix.Apply(playButton);

        StartCoroutine(BeginIntro());
    }

    IEnumerator BeginIntro()
    {
        yield return NarrationManager.WaitForSceneFade();

        AudioManager.Instance?.PlayNarrative();

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

    // ── Button callbacks ──────────────────────────────────────────────────────
    public void OnNext()
    {
        NarrationManager.Instance?.Advance();
    }

    public void OnPlay()
    {
        AudioManager.Instance?.StopNarrative();
        SceneFader.LoadScene(gameSceneName);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private void OnNarrationComplete()
    {
        // Stop narrative audio when all lines are done
        AudioManager.Instance?.StopNarrative();

        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (playButton != null)
        {
            playButton.gameObject.SetActive(true);
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(OnPlay);
        }
    }

    private NarrationLine[] BuildLines()
    {
        var result = new NarrationLine[narrationLines.Length];
        for (int i = 0; i < narrationLines.Length; i++)
            result[i] = new NarrationLine(narrationLines[i], speakerName);
        return result;
    }
}
