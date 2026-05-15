using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Core narration/dialogue engine.
/// Drop onto any Canvas. Assign the panel, text label, and optional audio source.
/// Call  NarrationManager.Instance.Play(lines, onComplete)  from any scene script.
///
/// Features:
///   • Typewriter character-by-character reveal
///   • Optional voice/narration AudioClip per line
///   • Manual advance (click/tap) OR auto-advance after a set delay
///   • UnityEvent callback when all lines are done
/// </summary>
[DefaultExecutionOrder(-50)]
public class NarrationManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static NarrationManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Panel")]
    [Tooltip("The root panel GameObject that contains the narration UI.")]
    public GameObject narrationPanel;

    [Header("Text")]
    [Tooltip("TextMeshProUGUI that shows the narration text.")]
    public TextMeshProUGUI narrationText;

    [Tooltip("Optional speaker/title label (e.g. 'Narrator').")]
    public TextMeshProUGUI speakerText;

    [Header("Typewriter")]
    [Tooltip("Seconds between each character reveal.")]
    public float charDelay = 0.03f;

    [Header("Auto Advance")]
    [Tooltip("If true, lines advance automatically after autoDelay seconds.")]
    public bool autoAdvance = false;
    [Tooltip("Seconds to wait after a line finishes before advancing (autoAdvance only).")]
    public float autoDelay  = 2.5f;

    [Header("Input")]
    [Tooltip("Optional NEXT button — wired automatically when Play() starts.")]
    public Button advanceButton;

    [Header("Audio")]
    [Tooltip("AudioSource used for narration voice clips.")]
    public AudioSource voiceSource;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private NarrationLine[] _lines;
    private int              _index;
    private Coroutine        _typeRoutine;
    private bool             _lineComplete;
    private UnityAction      _onAllDone;

    public bool IsPlaying => _lines != null;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // One per scene is fine; don't make it cross-scene
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (narrationPanel != null) narrationPanel.SetActive(false);
    }

    private void Update()
    {
        if (_lines == null) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
            Advance();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Start a narration sequence.</summary>
    /// <param name="lines">Array of NarrationLine to display in order.</param>
    /// <param name="onComplete">Optional callback fired when all lines finish.</param>
    public void Play(NarrationLine[] lines, UnityAction onComplete = null)
    {
        if (lines == null || lines.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }
        _lines     = lines;
        _index     = 0;
        _onAllDone = onComplete;

        BindAdvanceButton();

        if (narrationPanel != null)
        {
            narrationPanel.SetActive(true);
            narrationPanel.transform.SetAsLastSibling();

            var panelImage = narrationPanel.GetComponent<Image>();
            if (panelImage != null)
                panelImage.raycastTarget = false;

            UIButtonRaycastFix.DisableTextRaycasts(narrationPanel);
        }

        if (advanceButton != null)
            advanceButton.transform.SetAsLastSibling();

        ShowLine(_index);
    }

    /// <summary>Play lines and wait until the sequence finishes (uses realtime waits).</summary>
    public IEnumerator PlayAndWait(NarrationLine[] lines, UnityAction onComplete = null)
    {
        if (lines == null || lines.Length == 0)
        {
            onComplete?.Invoke();
            yield break;
        }

        bool done = false;
        Play(lines, () =>
        {
            onComplete?.Invoke();
            done = true;
        });

        while (!done)
            yield return null;
    }

    /// <summary>Wait until SceneFader finishes fading in (safe to call when no fader exists).</summary>
    public static System.Collections.IEnumerator WaitForSceneFade()
    {
        while (SceneFader.Instance != null && SceneFader.Instance.IsBlocking)
            yield return null;
        yield return null;
    }

    private void BindAdvanceButton()
    {
        if (advanceButton == null) return;

        UIButtonRaycastFix.Apply(advanceButton);
        UIButtonRaycastFix.BringToFront(advanceButton);
        advanceButton.interactable = true;
        advanceButton.onClick.RemoveListener(Advance);
        advanceButton.onClick.AddListener(Advance);
        advanceButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// Call this from a UI Button's OnClick, or from input polling.
    /// If a line is still typing, it snaps to the full text.
    /// If the line is done, it advances to the next line.
    /// </summary>
    public void Advance()
    {
        if (_lines == null) return;

        if (!_lineComplete)
        {
            // Snap — stop typewriter and show full text immediately
            if (_typeRoutine != null) StopCoroutine(_typeRoutine);
            if (narrationText != null) narrationText.text = _lines[_index].text;
            _lineComplete = true;

            if (!autoAdvance) return;   // wait for next Advance() call
        }

        // Move to next line
        _index++;
        if (_index < _lines.Length)
        {
            ShowLine(_index);
        }
        else
        {
            Finish();
        }
    }

    /// <summary>Immediately hide narration without completing it.</summary>
    public void Cancel()
    {
        if (_typeRoutine != null) StopCoroutine(_typeRoutine);
        _lines = null;
        if (narrationPanel != null) narrationPanel.SetActive(false);
    }

    // ── Internal ──────────────────────────────────────────────────────────────
    private void ShowLine(int idx)
    {
        _lineComplete = false;
        var line = _lines[idx];

        if (speakerText != null)
        {
            speakerText.text = line.speaker;
            speakerText.gameObject.SetActive(!string.IsNullOrEmpty(line.speaker));
        }

        if (narrationText != null) narrationText.text = "";

        // Voice clip
        if (voiceSource != null && line.voiceClip != null)
        {
            voiceSource.Stop();
            voiceSource.clip = line.voiceClip;
            voiceSource.Play();
        }

        if (_typeRoutine != null) StopCoroutine(_typeRoutine);
        _typeRoutine = StartCoroutine(TypeLine(line.text));
    }

    private IEnumerator TypeLine(string fullText)
    {
        for (int i = 0; i <= fullText.Length; i++)
        {
            if (narrationText != null)
                narrationText.text = fullText.Substring(0, i);
            yield return new WaitForSecondsRealtime(charDelay);
        }

        _lineComplete = true;

        if (autoAdvance)
            yield return new WaitForSecondsRealtime(autoDelay);
        else
            yield break;   // manual advance — wait for Advance() call

        // Auto advance
        _index++;
        if (_index < _lines.Length)
            ShowLine(_index);
        else
            Finish();
    }

    private void Finish()
    {
        _lines = null;
        if (narrationPanel != null) narrationPanel.SetActive(false);
        if (advanceButton != null) advanceButton.gameObject.SetActive(false);
        _onAllDone?.Invoke();
    }
}

// ── Data ──────────────────────────────────────────────────────────────────────

[System.Serializable]
public class NarrationLine
{
    [Tooltip("Optional speaker name shown above the text box.")]
    public string    speaker;

    [TextArea(2, 6)]
    [Tooltip("The narration text for this line.")]
    public string    text;

    [Tooltip("Optional voice audio clip to play with this line.")]
    public AudioClip voiceClip;

    // Convenience constructor
    public NarrationLine(string text, string speaker = "", AudioClip clip = null)
    {
        this.text      = text;
        this.speaker   = speaker;
        this.voiceClip = clip;
    }
}
