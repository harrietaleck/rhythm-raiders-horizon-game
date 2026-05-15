using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// One-click setup for all narration/UI screens.
/// Run via  Tools → Setup Narration Scenes.
///
/// What it builds in each scene:
///   StartScene   → Background + NarrationPanel + Title + "NEXT" + "PLAY" buttons
///   DeathScene   → Background + NarrationPanel + "GAME OVER" title + "RETRY" + "MENU"
///   VictoryScene → Background + NarrationPanel + "YOU WIN!" title + "PLAY AGAIN" + "MENU"
///   MainGameL1   → Adds NarrationManager popup panel + SceneFader to existing Canvas
///
/// All scenes must be listed in Build Settings (File → Build Settings → Add Open Scenes).
/// </summary>
public class SetupNarrationScenes
{
    // ── Asset paths ───────────────────────────────────────────────────────────
    private const string BG_PATH     = "Assets/AIRIDev_Scifi_UI_Icons/Sprites/Background/Plain Background.png";
    private const string FONT_PATH   = "Assets/Unity UI Samples/Fonts/Jupiter/Jupiter.ttf";

    // ── Menu entry ────────────────────────────────────────────────────────────
    [MenuItem("Tools/Setup Narration Scenes")]
    public static void SetupAll()
    {
        string currentScene = EditorSceneManager.GetActiveScene().path;

        SetupScene("Assets/Scenes/StartScene.unity",   BuildStartScene);
        SetupScene("Assets/Scenes/DeathScene.unity",   BuildDeathScene);
        SetupScene("Assets/Scenes/VictoryScene.unity", BuildVictoryScene);
        SetupScene("Assets/Scenes/MainGameL1.unity",   BuildMainGameNarration);

        // Return to original scene
        EditorSceneManager.OpenScene(currentScene);

        Debug.Log("[SetupNarration] All 4 scenes configured. " +
                  "Remember to add them all to File → Build Settings.");
    }

    // ── Scene helpers ─────────────────────────────────────────────────────────
    private static void SetupScene(string path, System.Action builder)
    {
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        builder();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[SetupNarration] Saved {path}");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // START SCENE
    // ═════════════════════════════════════════════════════════════════════════
    private static void BuildStartScene()
    {
        var canvas = EnsureCanvas("MainCanvas");
        EnsureEventSystem();

        // SceneFader (must be on a DontDestroyOnLoad GO — create as a child for editor preview)
        EnsureFader(canvas);

        // Full-screen background
        var bg = EnsureImage(canvas, "Background", BG_PATH,
                             Color.white, new Vector2(0, 0), new Vector2(1, 1));

        // Title
        var title = EnsureTMP(canvas, "TitleText", "RHYTHM RAIDERS",
                              72, FontStyle.Bold, Color.white,
                              new Vector2(0.1f, 0.75f), new Vector2(0.9f, 0.95f));

        // Narration panel (dark semi-transparent box)
        var panel = EnsurePanel(canvas, "NarrationPanel",
                                new Color(0, 0, 0, 0.75f),
                                new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.72f));
        panel.SetActive(false);   // NarrationManager activates it

        // Speaker label
        EnsureTMP(panel, "SpeakerText", "Narrator",
                  26, FontStyle.Bold, new Color(1f, 0.85f, 0.2f),
                  new Vector2(0, 0.75f), new Vector2(1, 1f));

        // Narration text body
        EnsureTMP(panel, "NarrationText", "",
                  24, FontStyle.Normal, Color.white,
                  new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.75f));

        // NEXT button (inside panel)
        EnsureButton(panel, "NextButton", "NEXT ▶",
                     new Vector2(0.7f, 0f), new Vector2(1f, 0.22f),
                     new Color(0.2f, 0.6f, 1f));

        // PLAY button (outside panel, hidden until narration done)
        var playBtn = EnsureButton(canvas, "PlayButton", "PLAY",
                                   new Vector2(0.35f, 0.08f), new Vector2(0.65f, 0.2f),
                                   new Color(0.1f, 0.8f, 0.3f));
        playBtn.SetActive(false);

        // Wire NarrationManager
        var nm = canvas.GetComponent<NarrationManager>() ?? canvas.AddComponent<NarrationManager>();
        nm.narrationPanel = panel;
        nm.narrationText  = panel.transform.Find("NarrationText")?.GetComponent<TextMeshProUGUI>();
        nm.speakerText    = panel.transform.Find("SpeakerText")?.GetComponent<TextMeshProUGUI>();
        nm.autoAdvance    = false;
        nm.charDelay      = 0.03f;

        // Wire StartSceneUI
        var ui = canvas.GetComponent<StartSceneUI>() ?? canvas.AddComponent<StartSceneUI>();
        ui.playButton = playBtn.GetComponent<Button>();
        ui.nextButton = panel.transform.Find("NextButton")?.GetComponent<Button>();

        EditorUtility.SetDirty(canvas);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // DEATH SCENE
    // ═════════════════════════════════════════════════════════════════════════
    private static void BuildDeathScene()
    {
        var canvas = EnsureCanvas("MainCanvas");
        EnsureEventSystem();
        EnsureFader(canvas);

        EnsureImage(canvas, "Background", BG_PATH,
                    new Color(0.6f, 0.1f, 0.1f, 1f),   // red tint overlay
                    new Vector2(0, 0), new Vector2(1, 1));

        // Tinted overlay for mood
        var overlay = EnsurePanel(canvas, "Overlay",
                                  new Color(0.4f, 0f, 0f, 0.5f),
                                  new Vector2(0, 0), new Vector2(1, 1));

        EnsureTMP(canvas, "TitleText", "GAME OVER",
                  80, FontStyle.Bold, new Color(1f, 0.2f, 0.2f),
                  new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.96f));

        // Narration panel
        var panel = EnsurePanel(canvas, "NarrationPanel",
                                new Color(0, 0, 0, 0.8f),
                                new Vector2(0.05f, 0.38f), new Vector2(0.95f, 0.75f));
        panel.SetActive(false);

        EnsureTMP(panel, "SpeakerText", "Narrator",
                  26, FontStyle.Bold, new Color(1f, 0.4f, 0.4f),
                  new Vector2(0, 0.75f), new Vector2(1, 1f));

        EnsureTMP(panel, "NarrationText", "",
                  24, FontStyle.Normal, Color.white,
                  new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.75f));

        EnsureButton(panel, "NextButton", "NEXT ▶",
                     new Vector2(0.7f, 0f), new Vector2(1f, 0.22f),
                     new Color(0.8f, 0.2f, 0.2f));

        // Action buttons
        var retry = EnsureButton(canvas, "RetryButton", "RETRY",
                                 new Vector2(0.1f, 0.08f), new Vector2(0.45f, 0.22f),
                                 new Color(1f, 0.5f, 0f));
        retry.SetActive(false);

        var menu = EnsureButton(canvas, "MainMenuButton", "MAIN MENU",
                                new Vector2(0.55f, 0.08f), new Vector2(0.9f, 0.22f),
                                new Color(0.3f, 0.3f, 0.8f));
        menu.SetActive(false);

        // Wire NarrationManager
        var nm = canvas.GetComponent<NarrationManager>() ?? canvas.AddComponent<NarrationManager>();
        nm.narrationPanel = panel;
        nm.narrationText  = panel.transform.Find("NarrationText")?.GetComponent<TextMeshProUGUI>();
        nm.speakerText    = panel.transform.Find("SpeakerText")?.GetComponent<TextMeshProUGUI>();
        nm.autoAdvance    = false;

        // Wire DeathHUD
        var hud = canvas.GetComponent<DeathHUD>() ?? canvas.AddComponent<DeathHUD>();
        hud.titleText      = canvas.transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
        hud.retryButton    = retry.GetComponent<Button>();
        hud.mainMenuButton = menu.GetComponent<Button>();
        hud.nextButton     = panel.transform.Find("NextButton")?.GetComponent<Button>();

        EditorUtility.SetDirty(canvas);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // VICTORY SCENE
    // ═════════════════════════════════════════════════════════════════════════
    private static void BuildVictoryScene()
    {
        var canvas = EnsureCanvas("MainCanvas");
        EnsureEventSystem();
        EnsureFader(canvas);

        EnsureImage(canvas, "Background", BG_PATH,
                    Color.white,
                    new Vector2(0, 0), new Vector2(1, 1));

        // Gold overlay
        EnsurePanel(canvas, "Overlay",
                    new Color(1f, 0.8f, 0f, 0.15f),
                    new Vector2(0, 0), new Vector2(1, 1));

        EnsureTMP(canvas, "TitleText", "YOU WIN!",
                  80, FontStyle.Bold, new Color(1f, 0.85f, 0.1f),
                  new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.96f));

        // Narration panel
        var panel = EnsurePanel(canvas, "NarrationPanel",
                                new Color(0, 0, 0, 0.75f),
                                new Vector2(0.05f, 0.38f), new Vector2(0.95f, 0.75f));
        panel.SetActive(false);

        EnsureTMP(panel, "SpeakerText", "Narrator",
                  26, FontStyle.Bold, new Color(1f, 0.85f, 0.2f),
                  new Vector2(0, 0.75f), new Vector2(1, 1f));

        EnsureTMP(panel, "NarrationText", "",
                  24, FontStyle.Normal, Color.white,
                  new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.75f));

        EnsureButton(panel, "NextButton", "NEXT ▶",
                     new Vector2(0.7f, 0f), new Vector2(1f, 0.22f),
                     new Color(0.2f, 0.7f, 0.3f));

        var playAgain = EnsureButton(canvas, "PlayAgainButton", "PLAY AGAIN",
                                     new Vector2(0.1f, 0.08f), new Vector2(0.45f, 0.22f),
                                     new Color(0.1f, 0.75f, 0.3f));
        playAgain.SetActive(false);

        var menu = EnsureButton(canvas, "MainMenuButton", "MAIN MENU",
                                new Vector2(0.55f, 0.08f), new Vector2(0.9f, 0.22f),
                                new Color(0.3f, 0.3f, 0.8f));
        menu.SetActive(false);

        // Wire NarrationManager
        var nm = canvas.GetComponent<NarrationManager>() ?? canvas.AddComponent<NarrationManager>();
        nm.narrationPanel = panel;
        nm.narrationText  = panel.transform.Find("NarrationText")?.GetComponent<TextMeshProUGUI>();
        nm.speakerText    = panel.transform.Find("SpeakerText")?.GetComponent<TextMeshProUGUI>();
        nm.autoAdvance    = false;

        // Wire VictoryHUD
        var hud = canvas.GetComponent<VictoryHUD>() ?? canvas.AddComponent<VictoryHUD>();
        hud.titleText      = canvas.transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
        hud.playAgainButton = playAgain.GetComponent<Button>();
        hud.mainMenuButton  = menu.GetComponent<Button>();
        hud.nextButton      = panel.transform.Find("NextButton")?.GetComponent<Button>();

        EditorUtility.SetDirty(canvas);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // MAIN GAME — add narration popup to existing canvas
    // ═════════════════════════════════════════════════════════════════════════
    private static void BuildMainGameNarration()
    {
        // Find the existing Canvas
        var canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null)
        {
            Debug.LogWarning("[SetupNarration] No Canvas found in MainGameL1. Skipping narration panel.");
            return;
        }

        EnsureFader(canvasObj);

        // Small narration panel — bottom third of screen
        var panel = EnsurePanel(canvasObj, "NarrationPanel",
                                new Color(0, 0, 0, 0.8f),
                                new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.3f));
        panel.SetActive(false);

        EnsureTMP(panel, "SpeakerText", "Narrator",
                  22, FontStyle.Bold, new Color(1f, 0.85f, 0.2f),
                  new Vector2(0, 0.7f), new Vector2(1, 1f));

        EnsureTMP(panel, "NarrationText", "",
                  20, FontStyle.Normal, Color.white,
                  new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.7f));

        // Wire NarrationManager on Canvas
        var nm = canvasObj.GetComponent<NarrationManager>() ?? canvasObj.AddComponent<NarrationManager>();
        nm.narrationPanel = panel;
        nm.narrationText  = panel.transform.Find("NarrationText")?.GetComponent<TextMeshProUGUI>();
        nm.speakerText    = panel.transform.Find("SpeakerText")?.GetComponent<TextMeshProUGUI>();
        nm.autoAdvance    = true;
        nm.autoDelay      = 2.5f;
        nm.charDelay      = 0.025f;

        EditorUtility.SetDirty(canvasObj);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Utility builders
    // ═════════════════════════════════════════════════════════════════════════

    private static GameObject EnsureCanvas(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null) return existing;

        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }

    private static void EnsureFader(GameObject canvas)
    {
        // SceneFader now self-creates its own persistent Canvas + Image at runtime.
        // We just need a SceneFader GameObject in the scene — no fadeImage wiring needed.
        var legacyOverlay = canvas.transform.Find("FadeOverlay");
        if (legacyOverlay != null)
            legacyOverlay.gameObject.SetActive(false);

        if (canvas.transform.Find("SceneFader") != null) return;

        var faderGO = new GameObject("SceneFader");
        faderGO.transform.SetParent(canvas.transform, false);
        faderGO.AddComponent<SceneFader>();

        EditorUtility.SetDirty(canvas);
    }

    private static GameObject EnsureImage(GameObject parent, string name,
                                           string spritePath, Color tint,
                                           Vector2 anchorMin, Vector2 anchorMax)
    {
        var existing = parent.transform.Find(name);
        if (existing != null) return existing.gameObject;

        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.SetAsFirstSibling();   // behind everything

        var img = go.AddComponent<Image>();
        img.color = tint;
        img.raycastTarget = false;

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite != null)
        {
            img.sprite   = sprite;
            img.type     = Image.Type.Sliced;
        }

        SetAnchors(go, anchorMin, anchorMax);
        return go;
    }

    private static GameObject EnsurePanel(GameObject parent, string name,
                                           Color color,
                                           Vector2 anchorMin, Vector2 anchorMax)
    {
        var existing = parent.transform.Find(name);
        if (existing != null) return existing.gameObject;

        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = true;

        SetAnchors(go, anchorMin, anchorMax);
        return go;
    }

    private static TextMeshProUGUI EnsureTMP(GameObject parent, string name,
                                              string defaultText,
                                              int fontSize, FontStyle style,
                                              Color color,
                                              Vector2 anchorMin, Vector2 anchorMax)
    {
        var existing = parent.transform.Find(name);
        if (existing != null) return existing.GetComponent<TextMeshProUGUI>();

        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = defaultText;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;

        // Try to use Jupiter font
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Unity UI Samples/Fonts/Jupiter/Jupiter SDF.asset");
        if (font == null)
        {
            // fallback: find any TMP_FontAsset
            var guids = AssetDatabase.FindAssets("t:TMP_FontAsset Jupiter");
            if (guids.Length > 0)
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        if (font != null) tmp.font = font;

        tmp.raycastTarget = false;

        SetAnchors(go, anchorMin, anchorMax);
        return tmp;
    }

    private static GameObject EnsureButton(GameObject parent, string name,
                                            string label,
                                            Vector2 anchorMin, Vector2 anchorMax,
                                            Color bgColor)
    {
        var existing = parent.transform.Find(name);
        if (existing != null) return existing.gameObject;

        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var img = go.AddComponent<Image>();
        img.color = bgColor;

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = bgColor * 1.2f;
        colors.pressedColor     = bgColor * 0.8f;
        btn.colors = colors;

        // Label
        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 28;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;

        var lrt = labelGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;

        SetAnchors(go, anchorMin, anchorMax);
        return go;
    }

    private static void SetAnchors(GameObject go, Vector2 min, Vector2 max)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
