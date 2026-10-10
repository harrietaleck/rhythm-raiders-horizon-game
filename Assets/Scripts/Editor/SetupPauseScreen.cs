using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Creates the pause menu UI with background art, buttons, and settings sliders.
/// Run via Tools → Setup Pause Screen.
/// </summary>
public static class SetupPauseScreen
{
    private static readonly Vector2 WindowMin = new Vector2(0.31f, 0.14f);
    private static readonly Vector2 WindowMax = new Vector2(0.69f, 0.87f);
    private static readonly Vector2 InfoMin   = new Vector2(0.745f, 0.20f);
    private static readonly Vector2 InfoMax   = new Vector2(0.965f, 0.66f);

    [MenuItem("Tools/Setup Pause Screen")]
    public static void Setup()
    {
        SetupScene(GameplaySceneNames.L1Path,  true,  true);
        SetupScene(GameplaySceneNames.L2Path,  true,  true);
        SetupScene(GameplaySceneNames.L3Path,  true,  true);
        SetupScene("Assets/Scenes/StartScene.unity", false, false);
        Debug.Log("[SetupPauseScreen] Pause UI applied to all gameplay scenes and StartScene.");
    }

    private static void SetupScene(string scenePath, bool requireGameStarted, bool showMainMenuButton)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var canvas = FindCanvas();
        if (canvas == null)
        {
            Debug.LogError($"[SetupPauseScreen] Canvas not found in {scenePath}");
            return;
        }

        if (scenePath.Contains("StartScene"))
            RemoveLegacyPauseUI(canvas);

        FixCanvasScale(canvas);
        FixCanvasScaler(canvas);
        EnsureGraphicRaycaster(canvas);
        EnsureEventSystem(scene);

        var pausePanel = EnsurePausePanel(canvas);
        EnsureBackground(pausePanel);

        var window = CreatePanel(pausePanel, "Window", WindowMin, WindowMax);
        window.layer = 5;
        SciFiUiKit.StyleWindow(window.AddComponent<Image>());

        var title = EnsureLabel(window, "Title", "PAUSED", new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.95f), 64);
        SciFiUiKit.StyleText(title, 64, SciFiUiKit.TextMain);
        title.characterSpacing = 18;
        title.alignment = TextAlignmentOptions.Center;
        CreateDivider(window, new Vector2(0.14f, 0.785f), new Vector2(0.86f, 0.79f));

        var buttonList = showMainMenuButton
            ? new[] { "RESUME", "RESTART LEVEL", "SETTINGS", "CONTROLS", "MAIN MENU" }
            : new[] { "RESUME", "SETTINGS", "CONTROLS" };
        var names = showMainMenuButton
            ? new[] { "ResumeButton", "RestartButton", "SettingsButton", "ControlsButton", "MainMenuButton" }
            : new[] { "ResumeButton", "SettingsButton", "ControlsButton" };

        var buttons = new System.Collections.Generic.Dictionary<string, Button>();
        const float rowTop = 0.72f, rowHeight = 0.115f, rowGap = 0.022f;
        for (int i = 0; i < names.Length; i++)
        {
            float top = rowTop - i * (rowHeight + rowGap);
            buttons[names[i]] = EnsureSciFiButton(window, names[i], buttonList[i],
                new Vector2(0.10f, top - rowHeight), new Vector2(0.90f, top), 30);
        }

        var info = BuildInfoPanel(pausePanel, out var levelTitle, out var levelName, out var objective, out var progress);
        info.SetActive(showMainMenuButton);

        var settingsPanel = EnsureSettingsPanel(canvas, out var closeBtn, out var musicSlider, out var sfxSlider);
        var controlsPanel = EnsureControlsPanel(canvas, out var controlsCloseBtn);
        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(false);

        var hud = canvas.GetComponent<PauseMenuHUD>() ?? canvas.AddComponent<PauseMenuHUD>();
        hud.pausePanel = pausePanel;
        hud.resumeButton = buttons["ResumeButton"];
        hud.settingsButton = buttons["SettingsButton"];
        hud.controlsButton = buttons["ControlsButton"];
        hud.restartButton = buttons.TryGetValue("RestartButton", out var restart) ? restart : null;
        hud.mainMenuButton = buttons.TryGetValue("MainMenuButton", out var menu) ? menu : null;
        hud.levelTitleText = levelTitle;
        hud.levelNameText = levelName;
        hud.objectiveText = objective;
        hud.progressText = progress;
        hud.controlsPanel = controlsPanel;
        hud.controlsCloseButton = controlsCloseBtn;
        hud.settingsPanel = settingsPanel;
        hud.settingsCloseButton = closeBtn;
        var resumeBtn = hud.resumeButton;
        hud.musicVolumeSlider = musicSlider;
        hud.sfxVolumeSlider = sfxSlider;
        hud.pauseTimeOnOpen = true;
        hud.listenForEscape = true;
        hud.requireGameStarted = requireGameStarted;
        hud.showMainMenuButton = showMainMenuButton;

        var startUI = canvas.GetComponent<StartSceneUI>();
        if (startUI != null)
        {
            startUI.pauseMenuHud = hud;
            startUI.pausePanel = pausePanel;
            startUI.settingsPanel = settingsPanel;
            startUI.pauseResumeButton = resumeBtn;
            startUI.settingsCloseButton = closeBtn;
            startUI.musicVolumeSlider = musicSlider;
            startUI.sfxVolumeSlider = sfxSlider;
            EditorUtility.SetDirty(startUI);
        }

        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static GameObject FindCanvas()
    {
        var canvas = GameObject.Find("MainCanvas") ?? GameObject.Find("Canvas");
        if (canvas != null) return canvas;

        var canvasComp = Object.FindAnyObjectByType<Canvas>();
        return canvasComp != null ? canvasComp.gameObject : null;
    }

    private static void RemoveLegacyPauseUI(GameObject canvas)
    {
        string[] legacy = { "PausePanel", "PauseMenuPanel", "SettingsPanel" };
        foreach (var name in legacy)
        {
            var t = canvas.transform.Find(name);
            if (t != null)
                Object.DestroyImmediate(t.gameObject);
        }

        var content = canvas.transform.Find("PauseMenuPanel/Content");
        if (content != null)
            Object.DestroyImmediate(content.parent.gameObject);
    }

    private static GameObject EnsurePausePanel(GameObject canvas)
    {
        var existing = canvas.transform.Find("PauseMenuPanel");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        var panel = CreatePanel(canvas, "PauseMenuPanel", Vector2.zero, Vector2.one);
        panel.layer = 5;
        panel.transform.SetAsLastSibling();
        return panel;
    }

    private static void EnsureBackground(GameObject pausePanel)
    {
        var bg = pausePanel.transform.Find("Background");
        GameObject go;
        if (bg == null)
        {
            go = new GameObject("Background");
            go.transform.SetParent(pausePanel.transform, false);
        }
        else go = bg.gameObject;

        go.layer = 5;
        go.transform.SetAsFirstSibling();
        SetAnchors(go, Vector2.zero, Vector2.one);

        SciFiUiKit.StyleBackground(go.GetComponent<Image>() ?? go.AddComponent<Image>());
    }

    private static void CreateDivider(GameObject parent, Vector2 min, Vector2 max)
    {
        var go = CreatePanel(parent, "Divider", min, max);
        go.layer = 5;
        var img = go.AddComponent<Image>();
        img.color = SciFiUiKit.Cyan;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.offsetMin = new Vector2(0f, -1f);
        rt.offsetMax = new Vector2(0f, 1f);
    }

    private static GameObject BuildInfoPanel(GameObject pausePanel, out TextMeshProUGUI levelTitle,
                                             out TextMeshProUGUI levelName, out TextMeshProUGUI objective,
                                             out TextMeshProUGUI progress)
    {
        var info = CreatePanel(pausePanel, "InfoPanel", InfoMin, InfoMax);
        info.layer = 5;
        var img = info.AddComponent<Image>();
        SciFiUiKit.StyleWindow(img);
        img.raycastTarget = false;

        var def = LevelCatalog.Get(1);
        levelTitle = InfoText(info, "LevelTitle", "LEVEL 1", 0.82f, 0.93f, 34, SciFiUiKit.TextMain);
        levelName  = InfoText(info, "LevelName",  def.displayName.ToUpperInvariant(), 0.74f, 0.82f, 24, SciFiUiKit.TextDim);
        CreateDivider(info, new Vector2(0.12f, 0.71f), new Vector2(0.88f, 0.715f));

        InfoText(info, "ObjectiveHeader", "OBJECTIVE", 0.60f, 0.68f, 24, SciFiUiKit.Cyan);
        objective  = InfoText(info, "Objective", def.startHint1, 0.38f, 0.60f, 20, SciFiUiKit.TextMain);
        objective.textWrappingMode = TextWrappingModes.Normal;
        objective.alignment = TextAlignmentOptions.TopLeft;

        InfoText(info, "ProgressHeader", "PROGRESS", 0.25f, 0.34f, 24, SciFiUiKit.Cyan);
        progress   = InfoText(info, "Progress", "0 / 2", 0.12f, 0.25f, 34, SciFiUiKit.TextMain);
        InfoText(info, "ProgressCaption", "Fragments Collected", 0.06f, 0.13f, 18, SciFiUiKit.TextDim);
        return info;
    }

    private static TextMeshProUGUI InfoText(GameObject parent, string name, string text,
                                            float yMin, float yMax, float size, Color color)
    {
        var tmp = EnsureLabel(parent, name, text, new Vector2(0.12f, yMin), new Vector2(0.88f, yMax), (int)size);
        SciFiUiKit.StyleText(tmp, size, color);
        tmp.characterSpacing = 3;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        return tmp;
    }

    private static GameObject EnsureControlsPanel(GameObject canvas, out Button closeBtn)
    {
        var existing = canvas.transform.Find("PauseControlsPanel");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        var panel = CreatePanel(canvas, "PauseControlsPanel", Vector2.zero, Vector2.one);
        panel.layer = 5;
        var dim = panel.AddComponent<Image>();
        dim.color = SciFiUiKit.Dim;
        dim.raycastTarget = true;

        var box = CreatePanel(panel, "ControlsBox", new Vector2(0.30f, 0.20f), new Vector2(0.70f, 0.80f));
        box.layer = 5;
        SciFiUiKit.StyleWindow(box.AddComponent<Image>());

        var title = EnsureLabel(box, "Title", "CONTROLS", new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.95f), 50);
        SciFiUiKit.StyleText(title, 50, SciFiUiKit.TextMain);
        title.characterSpacing = 14;
        CreateDivider(box, new Vector2(0.14f, 0.79f), new Vector2(0.86f, 0.795f));

        var body = EnsureLabel(box, "Body",
            "<color=#5AD9FF>A / D</color>  or  <color=#5AD9FF>ARROWS</color>      Move left / right\n\n" +
            "<color=#5AD9FF>SPACE</color>      Jump\n\n" +
            "<color=#5AD9FF>ESC</color>      Pause",
            new Vector2(0.10f, 0.28f), new Vector2(0.90f, 0.75f), 28);
        SciFiUiKit.StyleText(body, 28, SciFiUiKit.TextMain);
        body.alignment = TextAlignmentOptions.Center;

        closeBtn = EnsureSciFiButton(box, "CloseButton", "BACK", new Vector2(0.25f, 0.07f), new Vector2(0.75f, 0.20f), 30);
        return panel;
    }

    private static GameObject EnsureSettingsPanel(GameObject canvas, out Button closeBtn,
                                                  out Slider musicSlider, out Slider sfxSlider)
    {
        var existing = canvas.transform.Find("PauseSettingsPanel");
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        var panel = CreatePanel(canvas, "PauseSettingsPanel", Vector2.zero, Vector2.one);
        panel.layer = 5;
        var dim = panel.GetComponent<Image>() ?? panel.AddComponent<Image>();
        dim.color = SciFiUiKit.Dim;
        dim.raycastTarget = true;

        var box = CreatePanel(panel, "SettingsBox", new Vector2(0.30f, 0.20f), new Vector2(0.70f, 0.80f));
        box.layer = 5;
        SciFiUiKit.StyleWindow(box.GetComponent<Image>() ?? box.AddComponent<Image>());

        var title = EnsureLabel(box, "Title", "SETTINGS", new Vector2(0.08f, 0.82f), new Vector2(0.92f, 0.95f), 50);
        SciFiUiKit.StyleText(title, 50, SciFiUiKit.TextMain);
        title.characterSpacing = 14;
        CreateDivider(box, new Vector2(0.14f, 0.79f), new Vector2(0.86f, 0.795f));

        var musicLabel = EnsureLabel(box, "MusicLabel", "MUSIC VOLUME", new Vector2(0.10f, 0.64f), new Vector2(0.90f, 0.72f), 26);
        SciFiUiKit.StyleText(musicLabel, 26, SciFiUiKit.Cyan);
        musicSlider = EnsureSlider(box, "MusicSlider", new Vector2(0.10f, 0.54f), new Vector2(0.90f, 0.62f));
        SciFiUiKit.StyleSlider(musicSlider);

        var sfxLabel = EnsureLabel(box, "SfxLabel", "SFX VOLUME", new Vector2(0.10f, 0.40f), new Vector2(0.90f, 0.48f), 26);
        SciFiUiKit.StyleText(sfxLabel, 26, SciFiUiKit.Cyan);
        sfxSlider = EnsureSlider(box, "SfxSlider", new Vector2(0.10f, 0.30f), new Vector2(0.90f, 0.38f));
        SciFiUiKit.StyleSlider(sfxSlider);

        closeBtn = EnsureSciFiButton(box, "CloseButton", "BACK", new Vector2(0.25f, 0.07f), new Vector2(0.75f, 0.20f), 30);
        return panel;
    }

    private static Button EnsureSciFiButton(GameObject parent, string name, string label,
                                            Vector2 min, Vector2 max, float fontSize)
    {
        var tr = parent.transform.Find(name);
        if (tr != null)
            Object.DestroyImmediate(tr.gameObject);

        var go = CreatePanel(parent, name, min, max);
        go.layer = 5;
        go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();

        var labelGo = new GameObject("Label");
        labelGo.layer = 5;
        labelGo.transform.SetParent(go.transform, false);
        SetAnchors(labelGo, Vector2.zero, Vector2.one);

        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        SciFiUiKit.StyleButton(btn, tmp, fontSize);

        UIButtonRaycastFix.Apply(btn);
        return btn;
    }

    private static TextMeshProUGUI EnsureLabel(GameObject parent, string name, string text, Vector2 min, Vector2 max, int size)
    {
        var tr = parent.transform.Find(name);
        GameObject go;
        if (tr == null)
        {
            go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
        }
        else go = tr.gameObject;

        go.layer = 5;
        SetAnchors(go, min, max);
        var tmp = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        ApplyFont(tmp);
        return tmp;
    }

    private static Slider EnsureSlider(GameObject parent, string name, Vector2 min, Vector2 max)
    {
        var existing = parent.transform.Find(name);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        var go = CreatePanel(parent, name, min, max);
        go.layer = 5;

        var bg = new GameObject("Background");
        bg.layer = 5;
        bg.transform.SetParent(go.transform, false);
        var bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        var fillArea = new GameObject("Fill Area");
        fillArea.layer = 5;
        fillArea.transform.SetParent(go.transform, false);
        var faRt = fillArea.AddComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero;
        faRt.anchorMax = Vector2.one;
        faRt.offsetMin = new Vector2(8, 8);
        faRt.offsetMax = new Vector2(-8, -8);

        var fill = new GameObject("Fill");
        fill.layer = 5;
        fill.transform.SetParent(fillArea.transform, false);
        var fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = SciFiUiKit.Cyan;

        var handleArea = new GameObject("Handle Slide Area");
        handleArea.layer = 5;
        handleArea.transform.SetParent(go.transform, false);
        var haRt = handleArea.AddComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero;
        haRt.anchorMax = Vector2.one;
        haRt.offsetMin = haRt.offsetMax = Vector2.zero;

        var handle = new GameObject("Handle");
        handle.layer = 5;
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

    private static GameObject CreatePanel(GameObject parent, string name, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        SetAnchors(go, min, max);
        return go;
    }

    private static void ApplyFont(TextMeshProUGUI tmp)
    {
        var font = SciFiUiKit.TmpFont;
        if (font == null) return;
        tmp.font = font;
        if (font.material != null) tmp.fontSharedMaterial = font.material;
    }

    /// <summary>GraphicRaycaster is required for button clicks to register.</summary>
    private static void EnsureGraphicRaycaster(GameObject canvas)
    {
        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.AddComponent<GraphicRaycaster>();
            EditorUtility.SetDirty(canvas);
        }
    }

    /// <summary>EventSystem is required for any UI interaction.</summary>
    private static void EnsureEventSystem(UnityEngine.SceneManagement.Scene scene)
    {
        var es = Object.FindAnyObjectByType<EventSystem>();
        if (es != null) return;

        var go = new GameObject("EventSystem");
        UnityEditor.SceneManagement.EditorSceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
        EditorUtility.SetDirty(go);
    }

    private static void FixCanvasScale(GameObject canvas)
    {
        var rt = canvas.GetComponent<RectTransform>();
        if (rt != null && rt.localScale == Vector3.zero)
            rt.localScale = Vector3.one;
    }

    private static void FixCanvasScaler(GameObject canvas)
    {
        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) return;
        if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }
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
