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
    private Coroutine        _subtitleHideRoutine;
    private bool             _lineComplete;
    private UnityAction      _onAllDone;

    public bool IsPlaying => _lines != null;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // One per scene is fine; don't make it cross-scene
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        EnsureParentCanvasScale();
        if (narrationPanel != null) narrationPanel.SetActive(false);
        EnsureAllFonts();
    }

    void EnsureAllFonts()
    {
        TmpUiUtility.EnsureFont(narrationText);
        TmpUiUtility.EnsureFont(speakerText);
    }

    /// <summary>Non-blocking subtitle during gameplay — top bar overlay, auto-hides.</summary>
    public void ShowGameplaySubtitle(string text, string speaker, float duration = 2.8f)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        if (_typeRoutine != null)
        {
            StopCoroutine(_typeRoutine);
            _typeRoutine = null;
        }

        if (_subtitleHideRoutine != null)
        {
            StopCoroutine(_subtitleHideRoutine);
            _subtitleHideRoutine = null;
        }

        _lines = null;
        _onAllDone = null;

        if (narrationPanel != null && narrationPanel.activeSelf)
            narrationPanel.SetActive(false);

        GameplayDialogueOverlay.Show(text, speaker, duration);
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

        UIInputFix.EnsureEventSystem();
        UICursor.UnlockForMenu();
        EnsureParentCanvasScale();
        EnsureAllFonts();

        advanceButton = GetAdvanceButton(advanceButton);

        if (narrationPanel != null)
        {
            narrationPanel.SetActive(true);
            narrationPanel.transform.SetAsLastSibling();

            var panelImage = narrationPanel.GetComponent<Image>();
            if (panelImage != null)
                panelImage.raycastTarget = false;

            UIButtonRaycastFix.DisableTextRaycasts(narrationPanel);
        }

        BindAdvanceButton();

        PrepareNarrationText();

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
        float waited = 0f;
        const float maxWait = 2.5f;

        while (SceneFader.Instance != null && SceneFader.Instance.IsBlocking && waited < maxWait)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (SceneFader.Instance != null && SceneFader.Instance.IsBlocking)
            SceneFader.ForceClearFade();

        yield return null;
    }

    private void BindAdvanceButton()
    {
        if (advanceButton == null) return;

        EnsureParentCanvasScale();
        StyleAdvanceButton(advanceButton);

        if (narrationPanel != null)
        {
            advanceButton.transform.SetParent(narrationPanel.transform, false);
            var rt = advanceButton.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.40f, 0.11f);
                rt.anchorMax = new Vector2(0.60f, 0.21f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
            }
        }

        advanceButton.transform.SetAsLastSibling();

        advanceButton.interactable = true;
        advanceButton.onClick.RemoveListener(Advance);
        advanceButton.onClick.AddListener(Advance);
        advanceButton.gameObject.SetActive(true);

        if (advanceButton.GetComponent<NarrationAdvanceButton>() == null)
            advanceButton.gameObject.AddComponent<NarrationAdvanceButton>();
    }

    private static void StyleAdvanceButton(Button btn)
    {
        UIButtonRaycastFix.Apply(btn);

        var img = btn.GetComponent<Image>();
        if (img == null) return;

        // Skinned in the editor (Sci-Fi GUI); keep its sprites and only fix the label.
        if (img.sprite != null && btn.transition == Selectable.Transition.SpriteSwap)
        {
            img.raycastTarget = true;
            btn.targetGraphic = img;
            var skinnedLabel = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (skinnedLabel != null)
            {
                TmpUiUtility.EnsureFont(skinnedLabel);
                skinnedLabel.text = "NEXT";
                skinnedLabel.raycastTarget = false;
            }
            return;
        }

        img.sprite = null;
        img.type = Image.Type.Simple;
        img.color = Color.white;
        img.raycastTarget = true;
        btn.targetGraphic = img;

        var colors = btn.colors;
        colors.normalColor      = new Color(0.06f, 0.05f, 0.12f, 1f);
        colors.highlightedColor = new Color(1f, 0.48f, 0.22f, 0.95f);
        colors.pressedColor     = new Color(0.85f, 0.32f, 0.12f, 1f);
        colors.selectedColor    = colors.normalColor;
        btn.colors = colors;

        var label = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            TmpUiUtility.EnsureFont(label);
            label.text = "NEXT";
            label.fontSize = 28;
            label.fontStyle = FontStyles.Normal;
            label.characterSpacing = 8;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }
    }

    private void EnsureParentCanvasScale()
    {
        var canvasRt = narrationPanel != null
            ? narrationPanel.GetComponentInParent<Canvas>()?.GetComponent<RectTransform>()
            : GetComponent<RectTransform>();
        if (canvasRt != null)
            canvasRt.localScale = Vector3.one;
    }

    private void PrepareNarrationText()
    {
        if (narrationText == null) return;

        TmpUiUtility.EnsureFont(narrationText);
        narrationText.gameObject.SetActive(true);
        narrationText.fontSize = 30;
        narrationText.color = Color.white;
        narrationText.alignment = TextAlignmentOptions.Center;
        narrationText.verticalAlignment = VerticalAlignmentOptions.Middle;
        narrationText.textWrappingMode = TextWrappingModes.Normal;
        narrationText.raycastTarget = false;
        if (TmpUiUtility.IsReady(narrationText))
            narrationText.ForceMeshUpdate(true);
    }

    private void RefreshNarrationText(string value)
    {
        TmpUiUtility.SetSafeText(narrationText, value);
    }

    /// <summary>
    /// Call this from a UI Button's OnClick, or from input polling.
    /// If a line is still typing, it snaps to the full text.
    /// If the line is done, it advances to the next line.
    /// </summary>
    public void Advance()
    {
        if (_lines == null) return;
        if (_index >= _lines.Length)
        {
            Finish();
            return;
        }

        if (!_lineComplete)
        {
            if (_typeRoutine != null) StopCoroutine(_typeRoutine);
            RefreshNarrationText(_lines[_index].text);
            _lineComplete = true;
            if (!autoAdvance) return;
        }
        else if (_typeRoutine != null)
        {
            StopCoroutine(_typeRoutine);
        }

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
        if (_subtitleHideRoutine != null) StopCoroutine(_subtitleHideRoutine);
        _typeRoutine = null;
        _subtitleHideRoutine = null;
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
            TmpUiUtility.SetSafeText(speakerText, line.speaker);
            speakerText.gameObject.SetActive(!string.IsNullOrEmpty(line.speaker));
        }

        RefreshNarrationText("");

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
        int lineIdx = _index;

        for (int i = 0; i <= fullText.Length; i++)
        {
            RefreshNarrationText(fullText.Substring(0, i));
            yield return new WaitForSecondsRealtime(charDelay);
        }

        _lineComplete = true;

        if (!autoAdvance)
            yield break;

        yield return new WaitForSecondsRealtime(autoDelay);

        if (_lines == null || _index != lineIdx)
            yield break;

        _index++;
        if (_index < _lines.Length)
            ShowLine(_index);
        else
            Finish();
    }

    public Button GetAdvanceButton(Button overrideBtn = null)
    {
        if (overrideBtn != null)
        {
            advanceButton = overrideBtn;
            return overrideBtn;
        }

        if (advanceButton != null) return advanceButton;
        if (narrationPanel == null) return null;

        var found = narrationPanel.transform.Find("NextButton")?.GetComponent<Button>();
        if (found != null)
        {
            advanceButton = found;
            return found;
        }

        advanceButton = CreateRuntimeNextButton();
        return advanceButton;
    }

    private Button CreateRuntimeNextButton()
    {
        if (narrationPanel == null) return null;

        var go = new GameObject("NextButton");
        go.transform.SetParent(narrationPanel.transform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.70f, 0.04f);
        rt.anchorMax = new Vector2(0.94f, 0.18f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var lrt = labelGo.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        labelGo.AddComponent<TextMeshProUGUI>();
        TmpUiUtility.EnsureFont(labelGo.GetComponent<TextMeshProUGUI>());

        StyleAdvanceButton(btn);
        go.SetActive(false);
        return btn;
    }

    private void Finish()
    {
        if (_subtitleHideRoutine != null)
        {
            StopCoroutine(_subtitleHideRoutine);
            _subtitleHideRoutine = null;
        }

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
