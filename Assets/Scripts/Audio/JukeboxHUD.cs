using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Sound points and the jukebox. During gameplay a small chip shows your sound points (banked
/// notes, plus the notes this run will add when you finish). Outside gameplay, the Shop's
/// JUKEBOX button opens the jukebox: spend points to unlock tracks and pick the music you run to.
/// The pick is kept for every level until you change it.
/// </summary>
public class JukeboxHUD : MonoBehaviour
{
    static readonly Color Cyan = new Color(0.35f, 0.85f, 1f, 1f);
    static readonly Color Gold = new Color(1f, 0.84f, 0.35f, 1f);
    static readonly Color Dim = new Color(0.72f, 0.8f, 0.9f, 1f);

    public static JukeboxHUD Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance._panel != null;

    bool _shopMode;
    GameObject _chip;
    TextMeshProUGUI _chipText;
    GameObject _panel;
    TextMeshProUGUI _pointsText, _nowText, _statusText;
    TextMeshProUGUI[] _rowLabels, _rowButtonLabels;
    Button[] _rowButtons;
    float _prevTimeScale = 1f;
    float _statusUntil;
    int _lastPoints = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool gameplay = AudioManager.IsGameplayScene(scene.name);
        bool shop = scene.name == ShopSceneUI.SceneName;
        if (!gameplay && !shop) return;
        if (Instance != null) return;
        var go = new GameObject("JukeboxHUD");
        var hud = go.AddComponent<JukeboxHUD>();
        hud._shopMode = shop;
    }

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_panel != null) RestoreTime();
    }

    void Start() => BuildChip();

    void Update()
    {
        bool running = _shopMode || (GameManager.GameStarted && !GameManager.IsGameOver);
        if (_chip != null) _chip.SetActive(running && _panel == null);

        if (_panel == null)
        {
            if (_chip != null && _chip.activeSelf) RefreshChip();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || !running)
        {
            Close();
            return;
        }
        if (_statusText != null && _statusUntil > 0f && Time.unscaledTime > _statusUntil)
        {
            _statusText.text = "";
            _statusUntil = 0f;
        }
    }

    static bool PauseOpen()
    {
        foreach (var p in FindObjectsByType<PauseMenuHUD>(FindObjectsSortMode.None))
            if (p.IsOpen) return true;
        return false;
    }

    // ── Chip ─────────────────────────────────────────────────────────────────

    void BuildChip()
    {
        var canvas = MakeCanvas("JukeboxChip", 120);
        canvas.transform.SetParent(transform, false);
        _chip = canvas;
        var img = new GameObject("Chip", typeof(RectTransform)).AddComponent<Image>();
        img.transform.SetParent(canvas.transform, false);
        Stretch(img.rectTransform, _shopMode ? new Vector2(0.78f, 0.03f) : new Vector2(0.8f, 0.025f),
                                   _shopMode ? new Vector2(0.975f, 0.11f) : new Vector2(0.985f, 0.085f));
        Skin(img, banner: true);
        _chipText = Label(img.transform, "Text", "", new Vector2(0.06f, 0f), new Vector2(0.94f, 1f), 24, Color.white);
        if (_shopMode)
        {
            var btn = img.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(Open);
            img.gameObject.AddComponent<ButtonSoundPlayer>();
        }
        else img.raycastTarget = false;
        RefreshChip();
    }

    void RefreshChip()
    {
        int run = _shopMode ? 0 : ShopData.RunNotesAvailable;
        int key = ShopData.Notes * 100000 + run;
        if (key == _lastPoints) return;
        _lastPoints = key;
        _chipText.text = _shopMode
            ? $"<color=#5AD9FF>JUKEBOX</color>  SOUND POINTS <color=#FFD65A>{ShopData.Notes}</color>"
            : $"SOUND POINTS <color=#FFD65A>{ShopData.Notes}</color>" + (run > 0 ? $"  <color=#7CFF9A>+{run}</color>" : "");
    }

    // ── Panel ────────────────────────────────────────────────────────────────

    public void Open()
    {
        if (_panel != null || !_shopMode) return;   // buying happens outside gameplay
        _prevTimeScale = Time.timeScale;
        if (!_shopMode) Time.timeScale = 0f;
        UICursor.UnlockForMenu();
        UIInputFix.EnsureEventSystem();

        _panel = MakeCanvas("Jukebox", 360);
        _panel.transform.SetParent(transform, false);
        var dim = new GameObject("Dim", typeof(RectTransform)).AddComponent<Image>();
        dim.transform.SetParent(_panel.transform, false);
        Stretch(dim.rectTransform, Vector2.zero, Vector2.one);
        dim.color = new Color(0f, 0f, 0.02f, 0.65f);

        var win = new GameObject("Window", typeof(RectTransform)).AddComponent<Image>();
        win.transform.SetParent(_panel.transform, false);
        Stretch(win.rectTransform, new Vector2(0.24f, 0.1f), new Vector2(0.76f, 0.9f));
        Skin(win, banner: false);
        var w = win.transform;

        Label(w, "Title", "JUKEBOX", new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.97f), 46, Gold);
        _pointsText = Label(w, "Points", "", new Vector2(0.05f, 0.81f), new Vector2(0.95f, 0.88f), 24, Color.white);
        _nowText = Label(w, "Now", "", new Vector2(0.05f, 0.75f), new Vector2(0.95f, 0.81f), 22, Cyan);

        int n = ShopData.Music.Length;
        _rowLabels = new TextMeshProUGUI[n];
        _rowButtons = new Button[n];
        _rowButtonLabels = new TextMeshProUGUI[n];
        float top = 0.73f, rowH = 0.105f;
        for (int i = 0; i < n; i++)
        {
            float y1 = top - i * rowH, y0 = y1 - rowH + 0.012f;
            var row = new GameObject("Row" + i, typeof(RectTransform)).AddComponent<Image>();
            row.transform.SetParent(w, false);
            Stretch(row.rectTransform, new Vector2(0.06f, y0), new Vector2(0.94f, y1));
            row.color = new Color(0.05f, 0.1f, 0.18f, 0.55f);
            row.raycastTarget = false;
            _rowLabels[i] = Label(row.transform, "Name", "", new Vector2(0.04f, 0.05f), new Vector2(0.66f, 0.95f), 24, Color.white);
            _rowLabels[i].alignment = TextAlignmentOptions.Left;
            int index = i;
            _rowButtons[i] = MakeButton(row.transform, "", new Vector2(0.68f, 0.12f), new Vector2(0.98f, 0.88f), () => Pick(index));
            _rowButtonLabels[i] = _rowButtons[i].GetComponentInChildren<TextMeshProUGUI>();
        }

        _statusText = Label(w, "Status", "", new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.2f), 22, Gold);
        Label(w, "Hint", "Collect notes on your runs to earn sound points. Your pick plays in every level.",
              new Vector2(0.05f, 0.095f), new Vector2(0.95f, 0.14f), 17, Dim);
        MakeButton(w, "CLOSE", new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.09f), Close);
        RefreshPanel();
    }

    void Pick(int i)
    {
        var track = ShopData.Music[i];
        bool owned = ShopData.OwnsMusic(i);
        if (ShopData.SelectedMusic == i) { Status(track.name + " is already playing"); return; }
        if (ShopData.SelectMusic(i))
        {
            Status(owned ? "Now playing " + track.name : $"Unlocked {track.name}!  -{track.cost} points");
            AudioManager.Instance?.ApplySelectedMusic();
        }
        else Status($"Not enough sound points - {track.name} costs {track.cost}");
        _lastPoints = -1;
        RefreshPanel();
    }

    void RefreshPanel()
    {
        _pointsText.text = $"SOUND POINTS  <color=#FFD65A>{ShopData.Notes}</color>";
        var sel = ShopData.Music[ShopData.SelectedMusic];
        _nowText.text = "NOW PLAYING:  " + (ShopData.SelectedMusic == 0 ? "LEVEL THEME" : sel.name);

        for (int i = 0; i < ShopData.Music.Length; i++)
        {
            var t = ShopData.Music[i];
            bool owned = ShopData.OwnsMusic(i);
            bool playing = ShopData.SelectedMusic == i;
            _rowLabels[i].text = $"<color={(playing ? "#FFD65A" : "#FFFFFF")}>{t.name}</color>\n<size=70%><color=#9FB8D1>{t.artist}</color></size>";
            _rowButtonLabels[i].text = playing ? "PLAYING" : owned ? "PLAY" : $"BUY  {t.cost}";
            _rowButtons[i].interactable = !playing;
        }
    }

    void Status(string message)
    {
        _statusText.text = message;
        _statusUntil = Time.unscaledTime + 2.5f;
    }

    public void Close()
    {
        if (_panel == null) return;
        Destroy(_panel);
        _panel = null;
        RestoreTime();
        _lastPoints = -1;
        if (_shopMode)
            FindAnyObjectByType<ShopSceneUI>()?.SendMessage("Refresh", SendMessageOptions.DontRequireReceiver);
    }

    void RestoreTime()
    {
        if (_shopMode || PauseOpen()) return;   // the pause menu owns the clock now
        Time.timeScale = _prevTimeScale > 0f ? _prevTimeScale : 1f;
        if (GameManager.GameStarted && !GameManager.IsGameOver) UICursor.LockForGameplay();
    }

    // ── Building blocks (Sci-Fi HUD skin) ────────────────────────────────────

    static GameObject MakeCanvas(string name, int order)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    static void Skin(Image img, bool banner)
    {
        var skin = HudSkin.Get();
        var sprite = skin != null ? (banner ? skin.banner : skin.window) : null;
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = banner ? 4f : 2f;
            img.color = new Color(1f, 1f, 1f, 0.97f);
        }
        else img.color = banner ? new Color(0.06f, 0.2f, 0.32f, 0.92f) : new Color(0.02f, 0.04f, 0.09f, 0.95f);
    }

    static TextMeshProUGUI Label(Transform parent, string name, string text, Vector2 min, Vector2 max, float size, Color color)
    {
        var tmp = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        tmp.transform.SetParent(parent, false);
        Stretch(tmp.rectTransform, min, max);
        var skin = HudSkin.Get();
        if (skin != null && skin.font != null) tmp.font = skin.font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 12f;
        tmp.fontSizeMax = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.richText = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    static Button MakeButton(Transform parent, string text, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction onClick)
    {
        var img = new GameObject("Button", typeof(RectTransform)).AddComponent<Image>();
        img.transform.SetParent(parent, false);
        Stretch(img.rectTransform, min, max);
        Skin(img, banner: true);
        var btn = img.gameObject.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.8f, 0.95f, 1f);
        colors.disabledColor = new Color(0.55f, 0.65f, 0.7f, 0.8f);
        btn.colors = colors;
        btn.onClick.AddListener(onClick);
        img.gameObject.AddComponent<ButtonSoundPlayer>();
        Label(img.transform, "Label", text, new Vector2(0.04f, 0f), new Vector2(0.96f, 1f), 24, Color.white);
        return btn;
    }

    static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
