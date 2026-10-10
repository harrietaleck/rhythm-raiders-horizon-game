using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Builds GuideScene (How To Play + Credits) with the Sci-Fi GUI skin.
/// Run via Tools → Setup Guide Scene.
/// </summary>
public static class SetupGuideScene
{
    public const string ScenePath = "Assets/Scenes/GuideScene.unity";

    const string Hex = "#5AD9FF";

    [MenuItem("Tools/Setup Guide Scene")]
    public static void Run()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGo.AddComponent<AudioListener>();

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();

        var canvasGo = new GameObject("MainCanvas", typeof(RectTransform));
        canvasGo.layer = 5;
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var bg = Panel(canvasGo, "Background", Vector2.zero, Vector2.one);
        var bgImg = bg.AddComponent<Image>();
        SciFiUiKit.StyleBackground(bgImg);
        bgImg.raycastTarget = false;

        var guide = BuildGuide(canvasGo, out var creditsBtn, out var guideMenuBtn);
        var credits = BuildCredits(canvasGo, out var guideBtn, out var creditsMenuBtn);

        var faderGo = new GameObject("SceneFader");
        faderGo.AddComponent<SceneFader>();

        var ui = canvasGo.AddComponent<GuideSceneUI>();
        ui.guidePanel = guide;
        ui.creditsPanel = credits;
        ui.creditsButton = creditsBtn;
        ui.guideButton = guideBtn;
        ui.guideMenuButton = guideMenuBtn;
        ui.creditsMenuButton = creditsMenuBtn;
        credits.SetActive(false);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();
        Debug.Log("[SetupGuideScene] GuideScene created.");
    }

    static GameObject BuildGuide(GameObject canvas, out Button creditsBtn, out Button menuBtn)
    {
        var window = Window(canvas, "GuidePanel", new Vector2(0.17f, 0.07f), new Vector2(0.83f, 0.93f), "HOW TO PLAY");

        Section(window, "Goal", "GOAL",
            "Collect <color=" + Hex + ">2 Beat Fragments</color> in each level while the security guard chases you.\n" +
            "Each obstacle you hit costs shield. Get caught or run out and it's game over.", 0.66f, 0.81f);

        Section(window, "Controls", "CONTROLS",
            "<color=" + Hex + ">A / D</color> or <color=" + Hex + ">ARROWS</color>   Move left / right      " +
            "<color=" + Hex + ">SPACE</color>   Jump      <color=" + Hex + ">ESC</color>   Pause", 0.53f, 0.65f);

        var levels = Label(window, "LevelsHeader", "LEVELS", new Vector2(0.10f, 0.45f), new Vector2(0.90f, 0.51f), 28, SciFiUiKit.Cyan);
        levels.characterSpacing = 8;
        levels.alignment = TextAlignmentOptions.MidlineLeft;

        LevelColumn(window, 1, "Sprint across the sunset rooftops. Jump the gaps between buildings and don't fall off the edge.", 0.10f, 0.36f);
        LevelColumn(window, 2, "A faster run through the museum halls. Jump the rolling drums and dodge the alarm traps.", 0.37f, 0.63f);
        LevelColumn(window, 3, "The fastest run. Lasers and sound-wave traps everywhere. Stay on the beat.", 0.64f, 0.90f);

        creditsBtn = MakeButton(window, "CreditsButton", "CREDITS", new Vector2(0.12f, 0.05f), new Vector2(0.46f, 0.16f));
        menuBtn    = MakeButton(window, "MainMenuButton", "MAIN MENU", new Vector2(0.54f, 0.05f), new Vector2(0.88f, 0.16f));
        return window;
    }

    static GameObject BuildCredits(GameObject canvas, out Button guideBtn, out Button menuBtn)
    {
        var window = Window(canvas, "CreditsPanel", new Vector2(0.25f, 0.10f), new Vector2(0.75f, 0.90f), "CREDITS");

        var game = Label(window, "GameTitle", "RHYTHM RAIDERS: HORIZON",
            new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.76f), 34, SciFiUiKit.Cyan);
        game.characterSpacing = 6;

        Label(window, "RoleCaption", "CREATED BY",
            new Vector2(0.08f, 0.54f), new Vector2(0.92f, 0.62f), 24, SciFiUiKit.TextDim).characterSpacing = 8;

        var name = Label(window, "CreatorName", "Harriet Manda",
            new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.54f), 56, SciFiUiKit.TextMain);
        name.characterSpacing = 4;

        Label(window, "Roles", "Game Design  |  Development  |  Art Direction",
            new Vector2(0.08f, 0.33f), new Vector2(0.92f, 0.41f), 22, SciFiUiKit.TextDim);

        Label(window, "Thanks", "Thanks for playing!",
            new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.30f), 26, SciFiUiKit.TextMain);

        guideBtn = MakeButton(window, "GuideButton", "HOW TO PLAY", new Vector2(0.10f, 0.05f), new Vector2(0.48f, 0.16f));
        menuBtn  = MakeButton(window, "MainMenuButton", "MAIN MENU", new Vector2(0.52f, 0.05f), new Vector2(0.90f, 0.16f));
        return window;
    }

    static GameObject Window(GameObject parent, string name, Vector2 min, Vector2 max, string title)
    {
        var window = Panel(parent, name, min, max);
        SciFiUiKit.StyleWindow(window.AddComponent<Image>());

        var t = Label(window, "Title", title, new Vector2(0.06f, 0.86f), new Vector2(0.94f, 0.97f), 60, SciFiUiKit.TextMain);
        t.characterSpacing = 16;

        var div = Panel(window, "Divider", new Vector2(0.14f, 0.835f), new Vector2(0.86f, 0.84f));
        var rt = div.GetComponent<RectTransform>();
        rt.offsetMin = new Vector2(0f, -1f);
        rt.offsetMax = new Vector2(0f, 1f);
        var img = div.AddComponent<Image>();
        img.color = SciFiUiKit.Cyan;
        img.raycastTarget = false;
        return window;
    }

    static void Section(GameObject window, string name, string header, string body, float yMin, float yMax)
    {
        float split = yMax - (yMax - yMin) * 0.3f;
        var h = Label(window, name + "Header", header, new Vector2(0.10f, split), new Vector2(0.90f, yMax), 28, SciFiUiKit.Cyan);
        h.characterSpacing = 8;
        h.alignment = TextAlignmentOptions.MidlineLeft;

        var b = Label(window, name + "Body", body, new Vector2(0.10f, yMin), new Vector2(0.90f, split), 24, SciFiUiKit.TextMain);
        b.alignment = TextAlignmentOptions.TopLeft;
        b.textWrappingMode = TextWrappingModes.Normal;
    }

    static void LevelColumn(GameObject window, int level, string howTo, float xMin, float xMax)
    {
        var def = LevelCatalog.Get(level);
        string difficulty = level == 1 ? "EASY" : level == 2 ? "MEDIUM" : "HARD";

        var h = Label(window, "Level" + level + "Header", "LEVEL " + level + "  <size=70%><color=#9FB8D1>" + difficulty + "</color></size>",
            new Vector2(xMin, 0.39f), new Vector2(xMax, 0.45f), 24, SciFiUiKit.TextMain);
        h.characterSpacing = 4;
        h.alignment = TextAlignmentOptions.TopLeft;

        var n = Label(window, "Level" + level + "Name", def.displayName.ToUpper(),
            new Vector2(xMin, 0.34f), new Vector2(xMax, 0.39f), 22, SciFiUiKit.Cyan);
        n.alignment = TextAlignmentOptions.TopLeft;

        var b = Label(window, "Level" + level + "Body",
            howTo + "\n<color=" + Hex + ">" + def.hitCount + " hits</color> allowed.",
            new Vector2(xMin, 0.18f), new Vector2(xMax - 0.02f, 0.34f), 19, SciFiUiKit.TextDim);
        b.alignment = TextAlignmentOptions.TopLeft;
        b.textWrappingMode = TextWrappingModes.Normal;
    }

    static TextMeshProUGUI Label(GameObject parent, string name, string text, Vector2 min, Vector2 max,
                                 float size, Color color)
    {
        var go = Panel(parent, name, min, max);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        SciFiUiKit.StyleText(tmp, size, color);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    static Button MakeButton(GameObject parent, string name, string label, Vector2 min, Vector2 max)
    {
        var go = Panel(parent, name, min, max);
        go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();

        var labelGo = Panel(go, "Label", Vector2.zero, Vector2.one);
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        SciFiUiKit.StyleButton(btn, tmp, 28);
        UIButtonRaycastFix.Apply(btn);
        return btn;
    }

    static GameObject Panel(GameObject parent, string name, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go;
    }

    static void AddToBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(s => s.path == ScenePath)) return;
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
