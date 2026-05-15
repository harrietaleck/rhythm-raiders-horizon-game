using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attached to the Canvas in MainGameL1.
/// For start-of-game hints  → shown by NarrationManager directly, auto-advances,
///                             gameplay continues (no pause).
/// For deliberate mid-game pauses (power-ups etc.) → call ShowPanel() which
///                             pauses time until the player presses RESUME.
/// </summary>
public class InGameNarrationController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject        narrationPanel;
    public Button            resumeButton;
    public TextMeshProUGUI   narrationText;
    public TextMeshProUGUI   speakerText;

    private void Start()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(OnResume);

            // Ensure label says "RESUME"
            var label = resumeButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = "RESUME";
        }

        // Hide the RESUME button at start; it only shows when ShowPanel() is called
        if (resumeButton != null) resumeButton.gameObject.SetActive(false);

        if (narrationPanel != null) narrationPanel.SetActive(false);
    }

    /// <summary>
    /// Show the narration panel and PAUSE the game.
    /// Use this only for deliberate interruptions (power-up hints, cutscenes).
    /// Start-of-game hints use NarrationManager.Play() directly without pausing.
    /// </summary>
    public void ShowPanel()
    {
        if (narrationPanel != null) narrationPanel.SetActive(true);
        if (resumeButton   != null) resumeButton.gameObject.SetActive(true);
        Time.timeScale = 0f;
    }

    /// <summary>Hide panel and resume the game.</summary>
    public void OnResume()
    {
        NarrationManager.Instance?.Cancel();
        if (narrationPanel != null) narrationPanel.SetActive(false);
        if (resumeButton   != null) resumeButton.gameObject.SetActive(false);
        Time.timeScale = 1f;
    }
}
