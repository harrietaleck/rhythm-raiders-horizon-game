using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Persistent full-screen result card built from the Sci-Fi GUI pack (the same window frame,
/// angled button plates and Zekton font as the gameplay HUD). Red for GAME OVER, green with
/// orange plates for a win. Pops in with a short animation. Survives scene Canvas scale bugs.
/// </summary>
public class GameOverOverlay : MonoBehaviour
{
    public static GameOverOverlay Instance { get; private set; }

    const int SortOrder = 850;

    struct Theme
    {
        public Color frame, plate, tile, fill, glow, title, value, label, kicker;
    }

    static readonly Theme Lose = new Theme
    {
        frame = new Color(0.62f, 0.12f, 0.11f, 1f),
        plate = new Color(0.95f, 0.16f, 0.12f, 1f),
        tile = new Color(0.45f, 0.06f, 0.06f, 1f),
        fill = new Color(0.16f, 0.02f, 0.03f, 0.88f),
        glow = new Color(0.9f, 0.08f, 0.06f, 0.5f),
        title = Color.white,
        value = Color.white,
        label = new Color(1f, 0.7f, 0.66f, 1f),
        kicker = new Color(1f, 0.55f, 0.5f, 1f),
    };

    static readonly Theme Win = new Theme
    {
        frame = new Color(0.12f, 0.5f, 0.22f, 1f),
        plate = new Color(1f, 0.6f, 0.1f, 1f),
        tile = new Color(0.06f, 0.36f, 0.14f, 1f),
        fill = new Color(0.02f, 0.13f, 0.05f, 0.88f),
        glow = new Color(0.1f, 0.85f, 0.3f, 0.45f),
        title = Color.white,
        value = new Color(1f, 0.7f, 0.25f, 1f),
        label = new Color(0.65f, 1f, 0.72f, 1f),
        kicker = new Color(0.6f, 1f, 0.65f, 1f),
    };

    Sprite _windowSrc, _plateSrc;
    GameObject _root;
    CanvasGroup _group;
    RectTransform _card, _titlePlateRt;
    Image _vignette, _glow, _fill, _frame, _titlePlate;
    TextMeshProUGUI _kicker, _title, _body;
    readonly Image[] _tile = new Image[3];
    readonly TextMeshProUGUI[] _tileLabel = new TextMeshProUGUI[3];
    readonly TextMeshProUGUI[] _tileValue = new TextMeshProUGUI[3];
    Material _titleMat;
    Theme _theme;
    float _shownAt = -10f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap() => EnsureInstance();

    public static void EnsureInstance()
    {
        if (Instance != null) return;
        var go = new GameObject("_GameOverOverlay");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<GameOverOverlay>();
        Instance.Build();
    }

    // ── Build ────────────────────────────────────────────────────────────────
    void Build()
    {
        var skin = HudSkin.Get();
        Sprite window = skin != null ? skin.window : null;
        Sprite plate = skin != null ? skin.banner : null;
        _windowSrc = window;
        _plateSrc = plate;

        _root = new GameObject("Canvas", typeof(RectTransform));
        _root.transform.SetParent(transform, false);
        var canvas = _root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortOrder;
        var scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        _group = _root.AddComponent<CanvasGroup>();
        _group.blocksRaycasts = false;
        _group.interactable = false;

        var dim = Img(_root.transform, "Dim", Vector2.zero, Vector2.one, null, 0f);
        dim.color = new Color(0f, 0f, 0f, 0.72f);
        _vignette = Img(_root.transform, "Vignette", Vector2.zero, Vector2.one, Radial(true), 0f);

        var cardGo = new GameObject("Card", typeof(RectTransform));
        cardGo.transform.SetParent(_root.transform, false);
        _card = cardGo.GetComponent<RectTransform>();
        _card.anchorMin = _card.anchorMax = new Vector2(0.5f, 0.47f);
        _card.sizeDelta = new Vector2(1000f, 520f);

        // Soft glow, solid dark glass, then the pack's window frame over it.
        _glow = Img(_card, "Glow", Vector2.zero, Vector2.one, Radial(false), 0f);
        _glow.rectTransform.offsetMin = new Vector2(-220f, -200f);
        _glow.rectTransform.offsetMax = new Vector2(220f, 200f);
        _fill = Img(_card, "Fill", Vector2.zero, Vector2.one, null, 0f);
        _fill.rectTransform.offsetMin = new Vector2(26f, 22f);
        _fill.rectTransform.offsetMax = new Vector2(-26f, -22f);
        _frame = Img(_card, "Frame", Vector2.zero, Vector2.one, window, 2.2f);

        // Title on the pack's angled button plate, riding over the top edge of the window.
        _titlePlate = Img(_card, "TitlePlate", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), plate, 3.6f);
        _titlePlateRt = _titlePlate.rectTransform;
        _titlePlateRt.sizeDelta = new Vector2(700f, 170f);
        _titlePlateRt.anchoredPosition = new Vector2(0f, -10f);
        _title = Label(_titlePlateRt, "Title", new Vector2(0.12f, 0.16f), new Vector2(0.88f, 0.86f), 86f, skin, 6f);
        _titleMat = new Material(_title.fontSharedMaterial);
        _title.fontMaterial = _titleMat;

        _kicker = Label(_card, "Kicker", new Vector2(0.1f, 0.66f), new Vector2(0.9f, 0.74f), 26f, skin, 16f);
        _body = Label(_card, "Reason", new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.65f), 40f, skin, 3f);

        // Three stat tiles, each on a small button plate from the pack.
        for (int i = 0; i < 3; i++)
        {
            float x0 = 0.075f + i * 0.292f, x1 = x0 + 0.27f;
            _tile[i] = Img(_card, "Tile" + i, new Vector2(x0, 0.12f), new Vector2(x1, 0.44f), plate, 5.5f);
            _tileLabel[i] = Label(_tile[i].rectTransform, "Label", new Vector2(0.14f, 0.58f), new Vector2(0.86f, 0.84f), 22f, skin, 8f);
            _tileValue[i] = Label(_tile[i].rectTransform, "Value", new Vector2(0.12f, 0.14f), new Vector2(0.88f, 0.6f), 54f, skin, 2f);
        }

        _root.SetActive(false);
    }

    static Image Img(Transform parent, string name, Vector2 min, Vector2 max, Sprite sprite, float ppu)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        if (sprite != null)
        {
            img.sprite = sprite;
            if (ppu > 0f && sprite.border.sqrMagnitude > 0f)
            {
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = ppu;
            }
        }
        return img;
    }

    static TextMeshProUGUI Label(Transform parent, string name, Vector2 min, Vector2 max, float size, HudSkin skin, float spacing)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        if (skin != null && skin.font != null) t.font = skin.font; else TmpUiUtility.EnsureFont(t);
        t.alignment = TextAlignmentOptions.Center;
        t.fontSize = size;
        t.enableAutoSizing = true;
        t.fontSizeMin = 12f;
        t.fontSizeMax = size;
        t.characterSpacing = spacing;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    static readonly System.Collections.Generic.Dictionary<string, Sprite> _recolored = new System.Collections.Generic.Dictionary<string, Sprite>();

    /// <summary>
    /// A copy of a GUI-pack sprite in another colour: its brightness (frame lines, glow, glass
    /// sheen) is kept and mapped onto the target colour; alpha is unchanged. Works on textures
    /// that aren't readable by copying through a RenderTexture. Cached per sprite and colour.
    /// </summary>
    static Sprite Recolor(Sprite src, Color c)
    {
        if (src == null) return null;
        string key = src.GetInstanceID() + "_" + ColorUtility.ToHtmlStringRGBA(c);
        if (_recolored.TryGetValue(key, out var cached) && cached != null) return cached;

        var tex = src.texture;
        var rect = src.textureRect;
        int w = (int)rect.width, h = (int)rect.height;
        var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var prev = RenderTexture.active;
        Graphics.Blit(tex, rt);
        RenderTexture.active = rt;
        var copy = new Texture2D(w, h, TextureFormat.RGBA32, false);
        copy.ReadPixels(new Rect(rect.x, rect.y, w, h), 0, 0);
        copy.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        var px = copy.GetPixels32();
        for (int i = 0; i < px.Length; i++)
        {
            var p = px[i];
            float l = Mathf.Max(p.r, Mathf.Max(p.g, p.b)) / 255f;
            // Darker glass becomes a deep shade, bright edges/glow go toward a light tint.
            float k = Mathf.Lerp(0.45f, 1.25f, l);
            float hi = Mathf.Clamp01((l - 0.75f) * 2.5f) * 0.45f;
            px[i] = new Color32(
                (byte)(Mathf.Clamp01(c.r * k + hi) * 255f),
                (byte)(Mathf.Clamp01(c.g * k + hi) * 255f),
                (byte)(Mathf.Clamp01(c.b * k + hi) * 255f),
                p.a);
        }
        copy.SetPixels32(px);
        copy.Apply();
        copy.wrapMode = TextureWrapMode.Clamp;
        copy.filterMode = FilterMode.Bilinear;
        var s = Sprite.Create(copy, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), src.pixelsPerUnit, 0, SpriteMeshType.FullRect, src.border);
        _recolored[key] = s;
        return s;
    }

    static Sprite _glowSprite, _vigSprite;

    /// <summary>Soft round gradient for the glow (bright centre) or the vignette (dark edges).</summary>
    static Sprite Radial(bool vignette)
    {
        if (vignette && _vigSprite != null) return _vigSprite;
        if (!vignette && _glowSprite != null) return _glowSprite;
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                float a = vignette ? Edge(0.45f, 1.25f, d) : Mathf.Pow(1f - Edge(0f, 1f, d), 3f);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        var s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        if (vignette) _vigSprite = s; else _glowSprite = s;
        return s;
    }

    static float Edge(float a, float b, float x)
    {
        float t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    // ── Show ─────────────────────────────────────────────────────────────────
    /// <summary>Legacy entry: first line is the title, the last line the stats, the rest the reason.</summary>
    public static void Show(string body)
    {
        var text = body ?? "";
        int cut = text.IndexOf('\n');
        string title = cut >= 0 ? text.Substring(0, cut) : text;
        string rest = cut >= 0 ? text.Substring(cut).Trim() : "";
        string summary = "";
        int last = rest.LastIndexOf('\n');
        if (last >= 0) { summary = rest.Substring(last).Trim(); rest = rest.Substring(0, last).Trim(); }
        ShowResult(title, rest, summary, false);
    }

    /// <summary>Result card: red when the run is lost, green with orange plates when it is won.</summary>
    public static void ShowResult(string title, string body, string summary, bool victory)
    {
        EnsureInstance();
        var o = Instance;
        if (o._title == null) return;
        var th = victory ? Win : Lose;
        o._theme = th;

        // HUD fonts carry capitals only.
        TmpUiUtility.SetSafeText(o._kicker, victory ? "MISSION SUCCESS" : "RUN ENDED");
        TmpUiUtility.SetSafeText(o._title, (title ?? "").ToUpperInvariant());
        TmpUiUtility.SetSafeText(o._body, (body ?? "").ToUpperInvariant());

        o._vignette.color = new Color(th.glow.r * 0.5f, th.glow.g * 0.5f, th.glow.b * 0.5f, 0.6f);
        o._glow.color = th.glow;
        o._fill.color = th.fill;
        // The pack's sprites are pale steel-blue glass; recolour them (keeping their shading and
        // glow) so the frame and plates read clearly red / green / orange.
        o._frame.sprite = Recolor(o._windowSrc, th.frame);
        o._titlePlate.sprite = Recolor(o._plateSrc, th.plate);
        o._frame.color = Color.white;
        o._titlePlate.color = Color.white;
        o._title.color = th.title;
        o._kicker.color = th.kicker;
        o._body.color = new Color(0.94f, 0.95f, 0.97f, 1f);
        if (o._titleMat != null && o._titleMat.HasProperty("_UnderlayColor"))
        {
            o._titleMat.EnableKeyword("UNDERLAY_ON");
            o._titleMat.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.6f));
            o._titleMat.SetFloat("_UnderlaySoftness", 0.35f);
            o._titleMat.SetFloat("_UnderlayDilate", 0.15f);
            o._titleMat.SetFloat("_UnderlayOffsetX", 0.6f);
            o._titleMat.SetFloat("_UnderlayOffsetY", -0.6f);
        }

        // "Distance: 367m | Fragments: 1 | $500" → three tiles.
        var parts = (summary ?? "").Split('|');
        for (int i = 0; i < 3; i++)
        {
            string label = "", val = "";
            if (i < parts.Length)
            {
                var p = parts[i].Trim();
                int c = p.IndexOf(':');
                if (c >= 0) { label = p.Substring(0, c).Trim(); val = p.Substring(c + 1).Trim(); }
                else if (p.StartsWith("$")) { label = "CASH"; val = p; }
                else val = p;
            }
            o._tile[i].gameObject.SetActive(!string.IsNullOrEmpty(val));
            o._tile[i].sprite = Recolor(o._plateSrc, th.tile);
            o._tile[i].color = Color.white;
            TmpUiUtility.SetSafeText(o._tileLabel[i], label.ToUpperInvariant());
            TmpUiUtility.SetSafeText(o._tileValue[i], val.ToUpperInvariant());
            o._tileLabel[i].color = th.label;
            o._tileValue[i].color = th.value;
        }

        o._root.SetActive(true);
        o._shownAt = Time.unscaledTime;
        o._group.alpha = 0f;
        o._card.localScale = Vector3.one * 0.86f;
    }

    public static void Hide()
    {
        if (Instance == null || Instance._root == null) return;
        Instance._root.SetActive(false);
    }

    // ── Animate ──────────────────────────────────────────────────────────────
    void Update()
    {
        if (_root == null || !_root.activeSelf) return;
        float t = Time.unscaledTime - _shownAt;

        // Pop in: fade up and settle from a slight zoom with a small overshoot; the title plate
        // drops in a moment later.
        float k = Mathf.Clamp01(t / 0.35f);
        float ease = 1f + 2.4f * Mathf.Pow(k - 1f, 3f) + 1.4f * Mathf.Pow(k - 1f, 2f);
        _group.alpha = Mathf.Clamp01(t / 0.2f);
        _card.localScale = Vector3.one * Mathf.Lerp(0.86f, 1f, ease);
        float kp = Mathf.Clamp01((t - 0.12f) / 0.3f);
        float ep = 1f + 2.4f * Mathf.Pow(kp - 1f, 3f) + 1.4f * Mathf.Pow(kp - 1f, 2f);
        _titlePlateRt.anchoredPosition = new Vector2(0f, Mathf.Lerp(50f, -10f, ep));

        // Breathing glow.
        float pulse = 0.5f + 0.5f * Mathf.Sin(t * 3f);
        var g = _theme.glow;
        _glow.color = new Color(g.r, g.g, g.b, g.a * (0.65f + 0.35f * pulse));
        _titlePlate.color = Color.Lerp(Color.white, new Color(1.3f, 1.3f, 1.3f, 1f), 0.25f * pulse);
    }
}
