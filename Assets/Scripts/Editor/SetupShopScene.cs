using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Builds ShopScene (upgrades, trails, daily challenge) with the Sci-Fi GUI skin.
/// Run via Tools → Setup Shop Scene.
/// </summary>
public static class SetupShopScene
{
    public const string ScenePath = "Assets/Scenes/ShopScene.unity";

    [MenuItem("Tools/Setup Shop Scene")]
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

        var window = Panel(canvasGo, "ShopPanel", new Vector2(0.14f, 0.05f), new Vector2(0.86f, 0.95f));
        SciFiUiKit.StyleWindow(window.AddComponent<Image>());

        var title = Label(window, "Title", "SHOP", new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.97f), 60, SciFiUiKit.TextMain);
        title.characterSpacing = 16;

        var div = Panel(window, "Divider", new Vector2(0.14f, 0.865f), new Vector2(0.86f, 0.87f));
        var divImg = div.AddComponent<Image>();
        divImg.color = SciFiUiKit.Cyan;
        divImg.raycastTarget = false;

        var ui = canvasGo.AddComponent<ShopSceneUI>();

        ui.walletText = Label(window, "Wallet", "NOTES 0", new Vector2(0.08f, 0.80f), new Vector2(0.92f, 0.86f), 32, SciFiUiKit.TextMain);
        ui.dailyText = Label(window, "Daily", "", new Vector2(0.06f, 0.73f), new Vector2(0.94f, 0.79f), 22, SciFiUiKit.TextMain);

        Header(window, "UpgradesHeader", "UPGRADES", 0.66f, 0.72f);
        int n = ShopData.Upgrades.Length;
        ui.upgradeInfo = new TextMeshProUGUI[n];
        ui.upgradeButtons = new Button[n];
        ui.upgradeButtonLabels = new TextMeshProUGUI[n];
        for (int i = 0; i < n; i++)
        {
            float yMax = 0.655f - i * 0.085f;
            float yMin = yMax - 0.075f;
            var info = Label(window, "Upgrade" + i + "Info", "", new Vector2(0.10f, yMin), new Vector2(0.70f, yMax), 24, SciFiUiKit.TextMain);
            info.alignment = TextAlignmentOptions.MidlineLeft;
            ui.upgradeInfo[i] = info;
            ui.upgradeButtons[i] = MakeButton(window, "Upgrade" + i + "Button", "BUY",
                new Vector2(0.72f, yMin + 0.008f), new Vector2(0.90f, yMax - 0.008f), out ui.upgradeButtonLabels[i]);
        }

        Header(window, "TrailsHeader", "RUN TRAILS", 0.25f, 0.31f);
        int t = ShopData.Trails.Length;
        ui.trailButtons = new Button[t];
        ui.trailButtonLabels = new TextMeshProUGUI[t];
        for (int i = 0; i < t; i++)
        {
            float xMin = 0.10f + i * 0.205f;
            ui.trailButtons[i] = MakeButton(window, "Trail" + i + "Button", ShopData.Trails[i].name,
                new Vector2(xMin, 0.15f), new Vector2(xMin + 0.19f, 0.245f), out ui.trailButtonLabels[i]);
            ui.trailButtonLabels[i].fontSize = 20;
        }

        ui.statusText = Label(window, "Status", "", new Vector2(0.08f, 0.10f), new Vector2(0.60f, 0.145f), 22, SciFiUiKit.Cyan);
        ui.statusText.alignment = TextAlignmentOptions.MidlineLeft;
        ui.menuButton = MakeButton(window, "MainMenuButton", "MAIN MENU", new Vector2(0.62f, 0.03f), new Vector2(0.90f, 0.12f), out _);

        var faderGo = new GameObject("SceneFader");
        faderGo.AddComponent<SceneFader>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();
        Debug.Log("[SetupShopScene] ShopScene created.");
    }

    [MenuItem("Tools/Debug/Add 500 Shop Notes")]
    static void AddNotes() => ShopData.AddNotes(500);

    [MenuItem("Tools/Debug/Open Shop (Play Mode)")]
    static void OpenShopInPlayMode()
    {
        if (Application.isPlaying) UnityEngine.SceneManagement.SceneManager.LoadScene(ShopSceneUI.SceneName);
    }

    [MenuItem("Tools/Debug/Open Level 1 (Play Mode)")]
    static void OpenLevel1() => OpenLevel(1);

    [MenuItem("Tools/Debug/Open Level 2 (Play Mode)")]
    static void OpenLevel2() => OpenLevel(2);

    [MenuItem("Tools/Debug/Open Level 3 (Play Mode)")]
    static void OpenLevel3() => OpenLevel(3);

    static void OpenLevel(int level)
    {
        if (!Application.isPlaying) return;
        LevelProgress.SetLevel(level);
        UnityEngine.SceneManagement.SceneManager.LoadScene(LevelCatalog.GetGameplayScene(level));
    }

    static void Header(GameObject window, string name, string text, float yMin, float yMax)
    {
        var h = Label(window, name, text, new Vector2(0.10f, yMin), new Vector2(0.90f, yMax), 28, SciFiUiKit.Cyan);
        h.characterSpacing = 8;
        h.alignment = TextAlignmentOptions.MidlineLeft;
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

    static Button MakeButton(GameObject parent, string name, string label, Vector2 min, Vector2 max,
                             out TextMeshProUGUI labelText)
    {
        var go = Panel(parent, name, min, max);
        var img = go.AddComponent<Image>();
        var btn = go.AddComponent<Button>();

        var labelGo = Panel(go, "Label", Vector2.zero, Vector2.one);
        labelText = labelGo.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        SciFiUiKit.StyleButton(btn, labelText, 24);
        img.pixelsPerUnitMultiplier = 10f;
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
