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
    private const string BG_PATH     = SetupNarrationBackground.NARRATION_BG_PATH;
    private const string FONT_PATH   = "Assets/Unity UI Samples/Fonts/Jupiter/Jupiter.ttf";
    private static readonly Vector2 NarrationTextMin = new Vector2(0.11f, 0.33f);
    private static readonly Vector2 NarrationTextMax = new Vector2(0.89f, 0.61f);

    // ── Menu entry ────────────────────────────────────────────────────────────
    [MenuItem("Tools/Setup Narration Scenes")]
    public static void SetupAll()
    {
        string currentScene = EditorSceneManager.GetActiveScene().path;

        SetupScene("Assets/Scenes/StartScene.unity",   BuildStartScene);
        SetupScene("Assets/Scenes/DeathScene.unity",   BuildDeathScene);
        SetupScene("Assets/Scenes/VictoryScene.unity", BuildVictoryScene);
        SetupScene(GameplaySceneNames.L1Path,   BuildMainGameNarration);

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

        // Full-screen narrative overlay (shown while lines play)
        var panel = EnsureNarrationOverlay(canvas);
        panel.SetActive(false);

        // Narration text on the white area of the art
        var narrationTmp = EnsureTMP(panel, "NarrationText", "",
                  30, FontStyle.Normal, new Color(0.12f, 0.12f, 0.12f),
                  NarrationTextMin, NarrationTextMax);
        narrationTmp.alignment = TextAlignmentOptions.Center;
        narrationTmp.verticalAlignment = VerticalAlignmentOptions.Middle;

        // PLAY button (hidden until narration done)
        var playBtn = EnsureButton(canvas, "PlayButton", "PLAY",
                                   new Vector2(0.35f, 0.08f), new Vector2(0.65f, 0.2f),
                                   new Color(0.1f, 0.8f, 0.3f));
        playBtn.SetActive(false);

        // Wire NarrationManager
        var nm = canvas.GetComponent<NarrationManager>() ?? canvas.AddComponent<NarrationManager>();
        nm.narrationPanel = panel;
        nm.narrationText  = panel.transform.Find("NarrationText")?.GetComponent<TextMeshProUGUI>();
        nm.speakerText    = null;
        nm.autoAdvance    = true;
        nm.autoDelay      = 2.5f;
        nm.charDelay      = 0.035f;
        nm.advanceButton  = panel.transform.Find("NextButton")?.GetComponent<Button>();

        // Wire StartSceneUI
        var ui = canvas.GetComponent<StartSceneUI>() ?? canvas.AddComponent<StartSceneUI>();
        ui.playButton = playBtn.GetComponent<Button>();
        ui.nextButton = nm.advanceButton;

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

        EnsureTMP(canvas, "TitleText", "GAME OVER",
                  80, FontStyle.Bold, new Color(1f, 0.2f, 0.2f),
                  new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.96f));

        var panel = EnsureNarrationOverlay(canvas);
        panel.SetActive(false);

        var narrationTmp = EnsureTMP(panel, "NarrationText", "",
                  30, FontStyle.Normal, new Color(0.12f, 0.12f, 0.12f),
                  NarrationTextMin, NarrationTextMax);
        narrationTmp.alignment = TextAlignmentOptions.Center;
        narrationTmp.verticalAlignment = VerticalAlignmentOptions.Middle;

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
        nm.speakerText    = null;
        nm.autoAdvance    = true;
        nm.autoDelay      = 2.5f;
        nm.charDelay      = 0.035f;
        nm.advanceButton  = panel.transform.Find("NextButton")?.GetComponent<Button>();

        // Wire DeathHUD
        var hud = canvas.GetComponent<DeathHUD>() ?? canvas.AddComponent<DeathHUD>();
        hud.titleText      = canvas.transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
        hud.retryButton    = retry.GetComponent<Button>();
        hud.mainMenuButton = menu.GetComponent<Button>();
        hud.nextButton     = nm.advanceButton;

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

        EnsureTMP(canvas, "TitleText", "YOU WIN!",
                  80, FontStyle.Bold, new Color(1f, 0.85f, 0.1f),
                  new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.96f));

        var panel = EnsureNarrationOverlay(canvas);
        panel.SetActive(false);

        var narrationTmp = EnsureTMP(panel, "NarrationText", "",
                  30, FontStyle.Normal, new Color(0.12f, 0.12f, 0.12f),
                  NarrationTextMin, NarrationTextMax);
        narrationTmp.alignment = TextAlignmentOptions.Center;
        narrationTmp.verticalAlignment = VerticalAlignmentOptions.Middle;

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
        nm.speakerText    = null;
        nm.autoAdvance    = true;
        nm.autoDelay      = 2.5f;
        nm.charDelay      = 0.035f;
        nm.advanceButton  = panel.transform.Find("NextButton")?.GetComponent<Button>();

        // Wire VictoryHUD
        var hud = canvas.GetComponent<VictoryHUD>() ?? canvas.AddComponent<VictoryHUD>();
        hud.titleText      = canvas.transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
        hud.playAgainButton = playAgain.GetComponent<Button>();
        hud.mainMenuButton  = menu.GetComponent<Button>();
        hud.nextButton      = nm.advanceButton;

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

        var panel = EnsureNarrationOverlay(canvasObj);
        panel.SetActive(false);

        var narrationTmp = EnsureTMP(panel, "NarrationText", "",
                  30, FontStyle.Normal, new Color(0.12f, 0.12f, 0.12f),
                  NarrationTextMin, NarrationTextMax);
        narrationTmp.alignment = TextAlignmentOptions.Center;
        narrationTmp.verticalAlignment = VerticalAlignmentOptions.Middle;

        // Wire NarrationManager on Canvas
        var nm = canvasObj.GetComponent<NarrationManager>() ?? canvasObj.AddComponent<NarrationManager>();
        nm.narrationPanel = panel;
        nm.narrationText  = panel.transform.Find("NarrationText")?.GetComponent<TextMeshProUGUI>();
        nm.speakerText    = null;
        nm.autoAdvance    = true;
        nm.autoDelay      = 2.5f;
        nm.charDelay      = 0.035f;
        nm.advanceButton  = panel.transform.Find("NextButton")?.GetComponent<Button>();

        EditorUtility.SetDirty(canvasObj);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Utility builders
    // ═════════════════════════════════════════════════════════════════════════

    private static GameObject EnsureNarrationOverlay(GameObject canvas)
    {
        var existing = canvas.transform.Find("NarrationPanel");
        GameObject panel;

        if (existing != null)
        {
            panel = existing.gameObject;
            var oldImg = panel.GetComponent<Image>();
            if (oldImg != null)
            {
                oldImg.color = Color.white;
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BG_PATH);
                if (sprite != null)
                {
                    oldImg.sprite = sprite;
                    oldImg.type = Image.Type.Simple;
                }
            }
        }
        else
        {
            panel = EnsureImage(canvas, "NarrationPanel", BG_PATH,
                                Color.white, Vector2.zero, Vector2.one);
        }

        SetAnchors(panel, Vector2.zero, Vector2.one);

        var speaker = panel.transform.Find("SpeakerText");
        if (speaker != null) speaker.gameObject.SetActive(false);

        SetupNarrationBackground.EnsureNarrationNextButton(panel.transform);

        return panel;
    }

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
        }
        UIInputFix.EnsureEventSystem();
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
            img.sprite = sprite;
            img.type   = Image.Type.Simple;
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
