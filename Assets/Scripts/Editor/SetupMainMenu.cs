using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Replaces StartScene background with the main menu art and wires Play / Pause / Settings buttons.
/// Run via Tools → Setup Main Menu.
/// </summary>
public class SetupMainMenu
{
    public const string MenuBgPath = "Assets/UI/Splash/title_horizon.jpg";

    // Stacked under the baked-in title of the sunset art.
    private static readonly Vector2 PlayMin      = new Vector2(0.35f, 0.462f);
    private static readonly Vector2 PlayMax      = new Vector2(0.65f, 0.530f);
    private static readonly Vector2 Level2Min    = new Vector2(0.35f, 0.387f);
    private static readonly Vector2 Level2Max    = new Vector2(0.65f, 0.455f);
    private static readonly Vector2 Level3Min    = new Vector2(0.35f, 0.312f);
    private static readonly Vector2 Level3Max    = new Vector2(0.65f, 0.380f);
    private static readonly Vector2 PauseMin     = new Vector2(0.35f, 0.237f);
    private static readonly Vector2 PauseMax     = new Vector2(0.65f, 0.305f);
    private static readonly Vector2 NarrativeMin = new Vector2(0.35f, 0.162f);
    private static readonly Vector2 NarrativeMax = new Vector2(0.65f, 0.230f);
    private static readonly Vector2 SettingsMin  = new Vector2(0.35f, 0.087f);
    private static readonly Vector2 SettingsMax  = new Vector2(0.65f, 0.155f);

    private static readonly Color ButtonNormal  = new Color(0.06f, 0.05f, 0.12f, 1f);
    private static readonly Color ButtonHover   = new Color(1f, 0.48f, 0.22f, 0.95f);
    private static readonly Color ButtonPressed = new Color(0.85f, 0.32f, 0.12f, 1f);

    [MenuItem("Tools/Setup Main Menu")]
    public static void Setup()
    {
        EnsureTextureImport();

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/StartScene.unity", OpenSceneMode.Single);
        var canvas = GameObject.Find("MainCanvas");
        if (canvas == null)
        {
            Debug.LogError("[SetupMainMenu] MainCanvas not found.");
            return;
        }

        ClearMenuUI(canvas);
        FixCanvasScale(canvas);
        ApplyBackground(canvas);
        RemoveLegacyTitle(canvas);

        var menuPanel = EnsurePanel(canvas, "MenuPanel", Vector2.zero, Vector2.one);
        menuPanel.transform.SetAsLastSibling();

        var playBtn      = EnsureMenuButton(menuPanel, "PlayButton",       "PLAY",      PlayMin, PlayMax);
        var level2Btn    = EnsureMenuButton(menuPanel, "PlayLevel2Button", "LEVEL 2",   Level2Min, Level2Max);
        var level3Btn    = EnsureMenuButton(menuPanel, "PlayLevel3Button", "LEVEL 3",   Level3Min, Level3Max);
        var pauseBtn     = EnsureMenuButton(menuPanel, "PauseButton",      "PAUSE",     PauseMin, PauseMax);
        var narrativeBtn = EnsureMenuButton(menuPanel, "NarrativeButton",  "NARRATIVE", NarrativeMin, NarrativeMax);
        var settingsBtn  = EnsureMenuButton(menuPanel, "SettingsButton",   "SETTINGS",  SettingsMin, SettingsMax);
        RemoveMenuSubtitle(menuPanel);

        var pausePanel = EnsureOverlay(canvas, "PausePanel", "PAUSED", "RESUME", out var pauseResumeBtn);
        var settingsPanel = EnsureSettingsPanel(canvas, out var settingsCloseBtn, out var musicSlider, out var sfxSlider);

        menuPanel.SetActive(false);
        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);

        var ui = canvas.GetComponent<StartSceneUI>() ?? canvas.AddComponent<StartSceneUI>();
        ui.menuPanel            = menuPanel;
        ui.playButton           = playBtn;
        ui.level2Button         = level2Btn;
        ui.level3Button         = level3Btn;
        ui.pauseButton          = pauseBtn;
        ui.narrativeButton      = narrativeBtn;
        ui.settingsButton       = settingsBtn;
        ui.pausePanel           = pausePanel;
        ui.settingsPanel        = settingsPanel;
        ui.pauseResumeButton    = pauseResumeBtn;
        ui.settingsCloseButton  = settingsCloseBtn;
        ui.musicVolumeSlider    = musicSlider;
        ui.sfxVolumeSlider      = sfxSlider;

        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SetupMainMenu] Main menu applied to StartScene.");
    }

    private static void ClearMenuUI(GameObject canvas)
    {
        string[] names = { "MenuPanel", "PausePanel", "SettingsPanel" };
        foreach (var n in names)
        {
            var t = canvas.transform.Find(n);
            if (t != null)
                Object.DestroyImmediate(t.gameObject);
        }

        var legacyPlay = canvas.transform.Find("PlayButton");
        if (legacyPlay != null)
            Object.DestroyImmediate(legacyPlay.gameObject);
    }

    private static void ApplyBackground(GameObject canvas)
    {
        var bg = canvas.transform.Find("Background");
        if (bg == null)
        {
            var go = new GameObject("Background");
            go.transform.SetParent(canvas.transform, false);
            go.AddComponent<Image>();
            bg = go.transform;
        }

        bg.SetAsFirstSibling();
        SetAnchors(bg.gameObject, Vector2.zero, Vector2.one);

        var img = bg.GetComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = false;
        img.preserveAspect = false;
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MenuBgPath);
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Simple;
        }
    }

    private static void RemoveLegacyTitle(GameObject canvas)
    {
        var title = canvas.transform.Find("TitleText");
        if (title != null)
            Object.DestroyImmediate(title.gameObject);
    }

    private static GameObject EnsurePanel(GameObject parent, string name, Vector2 min, Vector2 max)
    {
        var existing = parent.transform.Find(name);
        GameObject go;

        if (existing != null)
            go = existing.gameObject;
        else
        {
            go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
        }

        SetAnchors(go, min, max);
        return go;
    }

    private static Button EnsureMenuButton(GameObject parent, string name, string label,
                                           Vector2 min, Vector2 max)
    {
        var existing = parent.transform.Find(name);
        GameObject go;

        if (existing != null)
            go = existing.gameObject;
        else
            go = new GameObject(name);

        go.transform.SetParent(parent.transform, false);
        SetAnchors(go, min, max);

        var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = true;

        var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = ButtonNormal;
        colors.highlightedColor = ButtonHover;
        colors.selectedColor    = ButtonNormal;
        colors.pressedColor     = ButtonPressed;
        btn.colors = colors;

        var labelGo = go.transform.Find("Label")?.gameObject;
        if (labelGo == null)
        {
            labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
        }

        var lrt = labelGo.GetComponent<RectTransform>();
        if (lrt == null) lrt = labelGo.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        var tmp = labelGo.GetComponent<TextMeshProUGUI>() ?? labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 34;
        tmp.fontStyle = FontStyles.Normal;
        tmp.characterSpacing = 8f;
        tmp.color = Color.white;
        tmp.outlineWidth = 0f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        ApplyFont(tmp);

        return btn;
    }

    private static void RemoveMenuSubtitle(GameObject menuPanel)
    {
        var existing = menuPanel.transform.Find("MenuSubtitle");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);
    }

    private static void EnsureMenuSubtitle(GameObject menuPanel)
    {
        var existing = menuPanel.transform.Find("MenuSubtitle");
        GameObject go = existing != null ? existing.gameObject : new GameObject("MenuSubtitle");
        go.transform.SetParent(menuPanel.transform, false);

        if (go.GetComponent<RectTransform>() == null)
            go.AddComponent<RectTransform>();
        if (go.GetComponent<TextMeshProUGUI>() == null)
            go.AddComponent<TextMeshProUGUI>();

        SetAnchors(go, new Vector2(0.15f, 0.74f), new Vector2(0.85f, 0.82f));
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = "Collect 2 instruments. Outrun the guard.";
        tmp.fontSize = 28;
        tmp.fontStyle = FontStyles.Italic;
        tmp.color = new Color(1f, 0.95f, 0.8f, 0.95f);
        tmp.alignment = TextAlignmentOptions.Center;
        ApplyFont(tmp);
    }

    private static GameObject EnsureOverlay(GameObject canvas, string panelName, string title,
                                            string buttonLabel, out Button actionBtn)
    {
        var panel = EnsurePanel(canvas, panelName, Vector2.zero, Vector2.one);
        var dim = panel.GetComponent<Image>() ?? panel.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.65f);
        dim.raycastTarget = true;

        var titleTr = panel.transform.Find("Title");
        GameObject titleGo;
        if (titleTr == null)
        {
            titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panel.transform, false);
        }
        else
            titleGo = titleTr.gameObject;

        if (titleGo.GetComponent<RectTransform>() == null)
            titleGo.AddComponent<RectTransform>();
        if (titleGo.GetComponent<TextMeshProUGUI>() == null)
            titleGo.AddComponent<TextMeshProUGUI>();

        SetAnchors(titleGo, new Vector2(0.2f, 0.62f), new Vector2(0.8f, 0.78f));
        var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
        titleTmp.text = title;
        titleTmp.fontSize = 72;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = Color.white;
        titleTmp.alignment = TextAlignmentOptions.Center;
        ApplyFont(titleTmp);

        actionBtn = EnsureMenuButton(panel, "ActionButton", buttonLabel,
                                     new Vector2(0.35f, 0.38f), new Vector2(0.65f, 0.50f));
        return panel;
    }

    private static GameObject EnsureSettingsPanel(GameObject canvas, out Button closeBtn,
                                                  out Slider musicSlider, out Slider sfxSlider)
    {
        var panel = EnsurePanel(canvas, "SettingsPanel", Vector2.zero, Vector2.one);
        var dim = panel.GetComponent<Image>() ?? panel.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.75f);
        dim.raycastTarget = true;

        var box = EnsurePanel(panel, "SettingsBox", new Vector2(0.25f, 0.28f), new Vector2(0.75f, 0.72f));
        var boxImg = box.GetComponent<Image>() ?? box.AddComponent<Image>();
        boxImg.color = new Color(0.06f, 0.05f, 0.12f, 0.92f);

        var titleTmp = EnsureLabel(box, "Title", "SETTINGS", new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.96f), 48);

        EnsureLabel(box, "MusicLabel", "Music Volume", new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.74f), 32);
        musicSlider = EnsureSlider(box, "MusicSlider", new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.62f));

        EnsureLabel(box, "SfxLabel", "SFX Volume", new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.48f), 32);
        sfxSlider = EnsureSlider(box, "SfxSlider", new Vector2(0.08f, 0.26f), new Vector2(0.92f, 0.36f));

        closeBtn = EnsureMenuButton(box, "CloseButton", "CLOSE",
                                    new Vector2(0.25f, 0.06f), new Vector2(0.75f, 0.18f));
        return panel;
    }

    private static TextMeshProUGUI EnsureLabel(GameObject parent, string name, string text,
                                               Vector2 min, Vector2 max, int fontSize)
    {
        var t = parent.transform.Find(name);
        GameObject go;
        if (t != null) go = t.gameObject;
        else
        {
            go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
        }

        if (go.GetComponent<RectTransform>() == null)
            go.AddComponent<RectTransform>();
        if (go.GetComponent<TextMeshProUGUI>() == null)
            go.AddComponent<TextMeshProUGUI>();

        SetAnchors(go, min, max);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        ApplyFont(tmp);
        return tmp;
    }

    private static Slider EnsureSlider(GameObject parent, string name, Vector2 min, Vector2 max)
    {
        var existing = parent.transform.Find(name);
        if (existing != null)
            return existing.GetComponent<Slider>();

        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        SetAnchors(go, min, max);

        var bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        var bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        var faRt = fillArea.AddComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero;
        faRt.anchorMax = Vector2.one;
        faRt.offsetMin = new Vector2(8, 8);
        faRt.offsetMax = new Vector2(-8, -8);

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(1f, 0.55f, 0.15f, 1f);

        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(go.transform, false);
        var haRt = handleArea.AddComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero;
        haRt.anchorMax = Vector2.one;
        haRt.offsetMin = haRt.offsetMax = Vector2.zero;

        var handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        var hRt = handle.AddComponent<RectTransform>();
        hRt.sizeDelta = new Vector2(24, 24);
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;

        var slider = go.AddComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = hRt;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;
        return slider;
    }

    private static void EnsureTextureImport()
    {
        var importer = AssetImporter.GetAtPath(MenuBgPath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
    }

    private static void ApplyFont(TextMeshProUGUI tmp)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Fonts/Coiny SDF.asset");
        if (font == null)
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Unity UI Samples/Fonts/Jupiter/Jupiter SDF.asset");
        if (font != null) tmp.font = font;
    }

    private static void FixCanvasScale(GameObject canvas)
    {
        var rt = canvas.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.localScale = Vector3.one;
        EditorUtility.SetDirty(rt);
    }

    private static void SetAnchors(GameObject go, Vector2 min, Vector2 max)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }
}
