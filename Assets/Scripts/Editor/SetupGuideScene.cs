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
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        string previous = EditorSceneManager.GetActiveScene().path;
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
        ui.pages = _pages;
        ui.tabButtons = _tabs;
        credits.SetActive(false);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();
        Debug.Log("[SetupGuideScene] GuideScene created.");
        if (!string.IsNullOrEmpty(previous) && previous != ScenePath) EditorSceneManager.OpenScene(previous);
    }

    static GameObject[] _pages;
    static Button[] _tabs;

    static string C(string t) => "<color=" + Hex + ">" + t + "</color>";
    static string W(string t) => "<color=#FF6B5A>" + t + "</color>";
    static string G(string t) => "<color=#FFD65A>" + t + "</color>";

    /// <summary>
    /// Paged How To Play: BASICS / SCREEN / LEVELS / POINTS &amp; SHOP tabs (also ← → keys).
    /// </summary>
    static GameObject BuildGuide(GameObject canvas, out Button creditsBtn, out Button menuBtn)
    {
        var window = Window(canvas, "GuidePanel", new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.96f), "HOW TO PLAY");

        string[] tabNames = { "BASICS", "SCREEN", "LEVELS", "POINTS & SHOP" };
        _tabs = new Button[tabNames.Length];
        _pages = new GameObject[tabNames.Length];
        for (int i = 0; i < tabNames.Length; i++)
        {
            float x0 = 0.06f + i * 0.225f;
            _tabs[i] = MakeButton(window, "Tab" + i, tabNames[i], new Vector2(x0, 0.735f), new Vector2(x0 + 0.205f, 0.815f));
            _pages[i] = Panel(window, "Page" + i, new Vector2(0.06f, 0.17f), new Vector2(0.94f, 0.72f));
        }

        // ── BASICS ──
        var p = _pages[0];
        Block(p, "Goal", "GOAL",
            "Grab the " + C("2 Beat Fragments") + " in each level and reach the end while the security guard chases you. " +
            "Win all three levels to enter the " + G("Hall of Fame") + ".", 0f, 0.5f, 0.66f, 1f);
        Block(p, "Controls", "CONTROLS",
            C("A / D") + " or " + C("ARROWS") + "  move between lanes\n" + C("SPACE") + "  jump\n" + C("ESC") + "  pause / settings", 0.52f, 1f, 0.66f, 1f);
        Block(p, "Hits", "BUMPING INTO THINGS",
            "You " + W("trip, fall, get up and dash") + " on. Each bump costs " + C("1 shield") + " and some vibe, and the " +
            "guard gains ground. Trip while he's right behind you and you're " + W("caught") + ".", 0f, 0.5f, 0.3f, 0.64f);
        Block(p, "Lose", "SHIELD, VIBE & GAME OVER",
            C("Shield") + " soaks up hits first. When it's gone, " + C("Vibe") + " drops. " +
            W("Game over") + " when Vibe hits 0, the guard catches you, or you fall off a roof. The Game Over screen names what got you.", 0.52f, 1f, 0.3f, 0.64f);
        Block(p, "Tips", "TIPS",
            "Jump " + C("on the beat") + " for a speed boost.  Watch the " + C("GUARD") + " distance - when it says " + W("CLOSE!") + " don't take risks.  " +
            "Notes lead you to the safe lane.", 0f, 1f, 0f, 0.28f);

        // ── SCREEN ──
        p = _pages[1];
        Block(p, "TopLeft", "TOP LEFT",
            C("VIBE") + "  your health\n" + C("SHIELD") + "  hits you can still take (flashes red when low)\n" +
            C("ARTIFACTS") + "  Beat Fragments found (0/2)\n" + C("GUARD") + "  how far behind he is: NEAR, " + W("CLOSE!") + ", " + W("SURGE") + " when he sprints", 0f, 0.5f, 0.5f, 1f);
        Block(p, "TopMid", "TOP CENTRE",
            "Distance run, how far to the next fragment, notes, score and your combo. Each level adds its own line " +
            "(museum glass cases, club groove).", 0.52f, 1f, 0.5f, 1f);
        Block(p, "Right", "RIGHT SIDE",
            "Area names and warnings slide in and out (" + W("SLIPPERY!") + ", " + W("SMOKE MACHINE!") + ", " + W("GRENADIER!") + "...). Power-up timers show here too.", 0f, 0.5f, 0.06f, 0.48f);
        Block(p, "Bottom", "BOTTOM",
            "Today's " + C("daily challenge") + " and a level tip. The " + C("diamond") + " pulses on the beat.  " +
            "Bottom right: your " + G("SOUND POINTS") + " (+ notes collected this run).", 0.52f, 1f, 0.06f, 0.48f);

        // ── LEVELS ──
        p = _pages[2];
        LevelBlock(p, 1, 0f, 0.32f,
            "Jump the " + W("roof gaps") + " (red edges and steel bars mark them) or ride a " + C("zip-line") + ". " +
            "Dodge AC units, water tanks and laundry lines. " + C("DJ stations") + " blast " + W("smoke") + " across the lanes. " +
            "Centre DJ stages have a " + W("taser bodyguard") + " on one side - jump him or take the other lane.");
        LevelBlock(p, 2, 0.34f, 0.66f,
            W("Armed guards") + " aim a red laser - strafe out of it. " + W("Grenadiers") + " throw grenades: when a red ring lands in your lane, switch lanes. " +
            "Jump " + W("low lasers") + " and " + W("planted grenades") + ", dodge laser grids. Smash glass cases: " +
            C("cyan") + " = boost, " + W("red") + " = sour note, purple = gamble.");
        LevelBlock(p, 3, 0.68f, 1f,
            "Jump " + W("laser fences") + ", dodge speaker stacks, cross " + W("bass walls") + " when they go dark. " +
            "Watch for sweeping lasers, falling light rigs and " + W("disco balls") + " that drop and roll down a lane. " +
            "DJ stages, bouncers and smoke on the dance floor.");

        // ── POINTS & SHOP ──
        p = _pages[3];
        Block(p, "Notes", "NOTES & SOUND POINTS",
            "Collect music " + C("notes") + " for score. When you finish a level they're banked as " + G("SOUND POINTS") + " for the Shop.", 0f, 0.5f, 0.55f, 1f);
        Block(p, "Stars", "STARS",
            "1 star for finishing, +1 if you still have shield left, +1 for collecting enough notes. Beat your best to see " + G("NEW BEST") + ".", 0.52f, 1f, 0.55f, 1f);
        Block(p, "Shop", "SHOP",
            "Upgrades: " + C("Magnet") + ", " + C("Bubble") + ", " + C("Smoke Bomb") + ", " + C("Head Start") + ". Trails for your runner. " +
            "Open the " + G("JUKEBOX") + " to buy tracks and pick the music you run to - your pick plays in every level.", 0f, 0.5f, 0.06f, 0.52f);
        Block(p, "Records", "RECORDS",
            "Enter your name on your first win (change it on the results screen). Each level saves your score, stars and time; " +
            "clear all three for the " + G("Hall of Fame") + ". Finish the " + C("daily challenge") + " for bonus notes.", 0.52f, 1f, 0.06f, 0.52f);

        creditsBtn = MakeButton(window, "CreditsButton", "CREDITS", new Vector2(0.12f, 0.04f), new Vector2(0.46f, 0.13f));
        menuBtn    = MakeButton(window, "MainMenuButton", "MAIN MENU", new Vector2(0.54f, 0.04f), new Vector2(0.88f, 0.13f));
        return window;
    }

    /// <summary>Header + body block inside a page, laid out in page-relative anchors.</summary>
    static void Block(GameObject page, string name, string header, string body, float xMin, float xMax, float yMin, float yMax)
    {
        float pad = 0.015f;
        float headH = Mathf.Min(0.12f, (yMax - yMin) * 0.3f);
        var h = Label(page, name + "Header", header, new Vector2(xMin + pad, yMax - headH), new Vector2(xMax - pad, yMax), 26, SciFiUiKit.Cyan);
        h.characterSpacing = 6;
        h.alignment = TextAlignmentOptions.MidlineLeft;
        var b = Label(page, name + "Body", body, new Vector2(xMin + pad, yMin), new Vector2(xMax - pad, yMax - headH), 22, SciFiUiKit.TextMain);
        b.alignment = TextAlignmentOptions.TopLeft;
        b.textWrappingMode = TextWrappingModes.Normal;
        b.enableAutoSizing = true;
        b.fontSizeMin = 14f;
        b.fontSizeMax = 22f;
        b.richText = true;
    }

    static void LevelBlock(GameObject page, int level, float xMin, float xMax, string howTo)
    {
        var def = LevelCatalog.Get(level);
        string difficulty = level == 1 ? "EASY" : level == 2 ? "MEDIUM" : "HARD";
        Block(page, "Level" + level, "LEVEL " + level + "  <size=75%><color=#9FB8D1>" + def.displayName.ToUpper() + "  -  " + difficulty + "</color></size>",
              howTo + "\n" + C(def.hitCount + " shield") + " hits before Vibe starts dropping.", xMin, xMax, 0f, 1f);
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
