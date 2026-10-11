using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight gameplay subtitles in a comms panel on the left edge. Does not open the narration panel.
/// </summary>
public class GameplayDialogueOverlay : MonoBehaviour
{
    public static GameplayDialogueOverlay Instance { get; private set; }

    const int SortOrder = 420;
    const float DefaultDuration = 2.8f;

    GameObject _root;
    TextMeshProUGUI _speaker;
    TextMeshProUGUI _line;
    RectTransform _panel;
    CanvasGroup _group;
    GameObject _speakerTab;
    Coroutine _hideRoutine;

    const float SlideDistance = 640f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap() => EnsureInstance();

    public static void EnsureInstance()
    {
        if (Instance != null) return;

        var go = new GameObject("_GameplayDialogueOverlay");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<GameplayDialogueOverlay>();
        Instance.Build();
    }

    void Build()
    {
        _root = new GameObject("Canvas");
        _root.transform.SetParent(transform, false);

        var canvas = _root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortOrder;

        var scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        _root.AddComponent<GraphicRaycaster>();

        var skin = HudSkin.Get();

        // Comms panel on the left edge, below the stats panel, clear of the running lane.
        var panel = new GameObject("CommsPanel");
        panel.transform.SetParent(_root.transform, false);
        _panel = panel.AddComponent<RectTransform>();
        _panel.anchorMin = new Vector2(0.012f, 0.40f);
        _panel.anchorMax = new Vector2(0.30f, 0.60f);
        _panel.offsetMin = _panel.offsetMax = Vector2.zero;
        _group = panel.AddComponent<CanvasGroup>();
        _group.blocksRaycasts = false;
        _group.interactable = false;

        var bg = panel.AddComponent<Image>();
        bg.raycastTarget = false;
        if (skin != null && skin.window != null)
        {
            bg.sprite = skin.window;
            bg.type = Image.Type.Sliced;
            bg.pixelsPerUnitMultiplier = 3f;
            bg.color = new Color(1f, 1f, 1f, 0.95f);
        }
        else
        {
            bg.color = new Color(0.02f, 0.04f, 0.09f, 0.82f);
        }

        // Speaker tab sitting on the panel's top edge.
        var tab = new GameObject("SpeakerTab");
        tab.transform.SetParent(panel.transform, false);
        var tabRt = tab.AddComponent<RectTransform>();
        tabRt.anchorMin = new Vector2(0.05f, 0.80f);
        tabRt.anchorMax = new Vector2(0.62f, 1.06f);
        tabRt.offsetMin = tabRt.offsetMax = Vector2.zero;
        var tabImg = tab.AddComponent<Image>();
        tabImg.raycastTarget = false;
        if (skin != null && skin.banner != null)
        {
            tabImg.sprite = skin.banner;
            tabImg.type = Image.Type.Sliced;
            tabImg.pixelsPerUnitMultiplier = 5f;
        }
        else
        {
            tabImg.color = new Color(0.05f, 0.1f, 0.2f, 0.95f);
        }
        _speakerTab = tab;

        _speaker = MakeText(tab.transform, "Speaker", new Vector2(0.06f, 0f), new Vector2(0.94f, 1f), 20f, HudSkin.Cyan, skin);
        _speaker.alignment = TextAlignmentOptions.Center;
        _speaker.characterSpacing = 5f;
        _speaker.fontStyle = FontStyles.UpperCase;

        _line = MakeText(panel.transform, "Line", new Vector2(0.07f, 0.1f), new Vector2(0.94f, 0.78f), 24f, Color.white, skin);
        _line.alignment = TextAlignmentOptions.TopLeft;
        _line.textWrappingMode = TextWrappingModes.Normal;
        _line.enableAutoSizing = true;
        _line.fontSizeMin = 16f;
        _line.fontSizeMax = 24f;
        _line.lineSpacing = 4f;

        _root.SetActive(false);
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, Vector2 min, Vector2 max, float size, Color color, HudSkin skin)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (skin != null && skin.font != null) tmp.font = skin.font;
        else TmpUiUtility.EnsureFont(tmp);
        tmp.fontSize = size;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    public static void Show(string text, string speaker, float duration = DefaultDuration)
    {
        EnsureInstance();
        Instance.ShowInternal(text, speaker, duration);
    }

    public static void Hide()
    {
        if (Instance == null) return;
        Instance.HideInternal();
    }

    void ShowInternal(string text, string speaker, float duration)
    {
        if (_line == null) return;

        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }

        bool hasSpeaker = !string.IsNullOrWhiteSpace(speaker);
        if (_speakerTab != null) _speakerTab.SetActive(hasSpeaker);
        if (hasSpeaker && _speaker != null)
            TmpUiUtility.SetSafeText(_speaker, speaker);

        TmpUiUtility.SetSafeText(_line, text ?? string.Empty);
        _root.SetActive(true);

        _hideRoutine = StartCoroutine(ShowThenHide(Mathf.Max(1.2f, duration)));
    }

    void HideInternal()
    {
        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }

        if (_root != null)
            _root.SetActive(false);
    }

    IEnumerator ShowThenHide(float seconds)
    {
        yield return Slide(0f, 1f, 0.22f);
        yield return new WaitForSecondsRealtime(seconds);
        yield return Slide(1f, 0f, 0.2f);
        HideInternal();
    }

    IEnumerator Slide(float from, float to, float time)
    {
        for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
        {
            SetVisible(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / time)));
            yield return null;
        }
        SetVisible(to);
    }

    void SetVisible(float k)
    {
        if (_group != null) _group.alpha = k;
        if (_panel != null) _panel.anchoredPosition = new Vector2(-SlideDistance * (1f - k), 0f);
    }
}
