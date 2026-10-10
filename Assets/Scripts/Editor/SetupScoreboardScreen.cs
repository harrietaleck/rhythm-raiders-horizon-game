using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Adds the scoreboard overlay to DeathScene. Run via Tools → Setup Scoreboard Screen.
/// </summary>
public class SetupScoreboardScreen
{
    public const string ScoreboardBgPath = "Assets/UI/Scoreboard/scoreboard_background.png";

    // White score panel on the art (1920×1080 reference).
    private static readonly Vector2 ScoreTextMin = new Vector2(0.18f, 0.30f);
    private static readonly Vector2 ScoreTextMax = new Vector2(0.82f, 0.68f);
    private static readonly Vector2 BackButtonMin = new Vector2(0.35f, 0.08f);
    private static readonly Vector2 BackButtonMax = new Vector2(0.65f, 0.18f);

    private static readonly Color ButtonFill      = new Color(0.96f, 0.92f, 0.86f, 1f);
    private static readonly Color ButtonHighlight = new Color(1f, 0.98f, 0.94f, 1f);
    private static readonly Color ButtonPressed   = new Color(0.88f, 0.82f, 0.74f, 1f);
    private static readonly Color ButtonLabel     = new Color(0.25f, 0.18f, 0.12f, 1f);

    [MenuItem("Tools/Setup Scoreboard Screen")]
    public static void Setup()
    {
        EnsureTextureImport();

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/DeathScene.unity", OpenSceneMode.Single);
        var canvas = GameObject.Find("MainCanvas");
        if (canvas == null)
        {
            Debug.LogError("[SetupScoreboard] MainCanvas not found in DeathScene.");
            return;
        }

        var panel = EnsurePanel(canvas);
        var scoreText = EnsureScoreText(panel);
        var backBtn = EnsureBackButton(panel);

        var scoreboard = canvas.GetComponent<ScoreboardHUD>() ?? canvas.AddComponent<ScoreboardHUD>();
        scoreboard.panel      = panel;
        scoreboard.scoreText  = scoreText;
        scoreboard.backButton = backBtn;

        var deathHud = canvas.GetComponent<DeathHUD>();
        if (deathHud != null)
            deathHud.scoreboard = scoreboard;

        panel.SetActive(false);
        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SetupScoreboard] Scoreboard overlay wired in DeathScene.");
    }

    private static GameObject EnsurePanel(GameObject canvas)
    {
        var existing = canvas.transform.Find("ScoreboardPanel");
        GameObject panel;

        if (existing != null)
            panel = existing.gameObject;
        else
        {
            panel = new GameObject("ScoreboardPanel");
            panel.transform.SetParent(canvas.transform, false);
            panel.AddComponent<Image>();
        }

        panel.transform.SetAsLastSibling();
        SetAnchors(panel, Vector2.zero, Vector2.one);

        var img = panel.GetComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = true;
        img.preserveAspect = false;
        SciFiUiKit.StyleBackground(img);

        var winTr = panel.transform.Find("ScoreWindow");
        var win = winTr != null ? winTr.gameObject : new GameObject("ScoreWindow", typeof(RectTransform), typeof(Image));
        win.transform.SetParent(panel.transform, false);
        win.transform.SetAsFirstSibling();
        SetAnchors(win, new Vector2(0.28f, 0.05f), new Vector2(0.72f, 0.75f));
        SciFiUiKit.StyleWindow(win.GetComponent<Image>());
        win.GetComponent<Image>().raycastTarget = false;

        return panel;
    }

    private static TextMeshProUGUI EnsureScoreText(GameObject panel)
    {
        var tr = panel.transform.Find("ScoreText");
        GameObject go;

        if (tr != null)
            go = tr.gameObject;
        else
        {
            go = new GameObject("ScoreText");
            go.transform.SetParent(panel.transform, false);
            go.AddComponent<TextMeshProUGUI>();
        }

        SetAnchors(go, ScoreTextMin, ScoreTextMax);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = RunStats.FormatScoreSummary();
        tmp.fontSize = 34;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        SciFiUiKit.StyleText(tmp, 34, SciFiUiKit.TextMain);
        return tmp;
    }

    private static Button EnsureBackButton(GameObject panel)
    {
        var tr = panel.transform.Find("BackButton");
        GameObject go;

        if (tr != null)
            go = tr.gameObject;
        else
        {
            go = new GameObject("BackButton");
            go.transform.SetParent(panel.transform, false);
            go.AddComponent<Image>();
            go.AddComponent<Button>();

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.AddComponent<TextMeshProUGUI>();
        }

        SetAnchors(go, BackButtonMin, BackButtonMax);

        var btn = go.GetComponent<Button>();
        var label = go.GetComponentInChildren<TextMeshProUGUI>();
        SciFiUiKit.StyleButton(btn, label, 28);
        if (label != null)
        {
            label.text = "BACK";

            var lrt = label.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        }

        return btn;
    }

    private static void EnsureTextureImport()
    {
        var importer = AssetImporter.GetAtPath(ScoreboardBgPath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
    }

    private static void ApplyFont(TextMeshProUGUI tmp)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Unity UI Samples/Fonts/Jupiter/Jupiter SDF.asset");
        if (font != null)
            tmp.font = font;
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
