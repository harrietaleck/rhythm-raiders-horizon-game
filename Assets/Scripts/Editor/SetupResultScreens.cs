using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Replaces the old baked Victory / Death backgrounds with the sunset rooftop art.
/// Run via Tools → Setup Result Screens.
/// </summary>
public static class SetupResultScreens
{
    private static readonly Color BoxColor      = new Color(0.06f, 0.05f, 0.12f, 1f);
    private static readonly Color ButtonNormal  = new Color(0.18f, 0.15f, 0.3f, 1f);
    private static readonly Color ButtonHover   = new Color(1f, 0.48f, 0.22f, 0.95f);
    private static readonly Color ButtonPressed = new Color(0.85f, 0.32f, 0.12f, 1f);
    private static readonly Color Accent        = new Color(1f, 0.55f, 0.25f, 1f);

    [MenuItem("Tools/Setup Result Screens")]
    public static void Run()
    {
        string current = EditorSceneManager.GetActiveScene().path;

        SetupVictory();
        SetupDeath();

        if (!string.IsNullOrEmpty(current))
            EditorSceneManager.OpenScene(current);
        Debug.Log("[SetupResultScreens] Victory and Death backgrounds replaced.");
    }

    private static void SetupVictory()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/VictoryScene.unity", OpenSceneMode.Single);
        var canvas = GameObject.Find("MainCanvas");
        if (canvas == null) return;

        var bg = ApplyBackground(canvas.transform);
        var box = EnsureBox(canvas.transform, bg, new Vector2(0.32f, 0.08f), new Vector2(0.68f, 0.56f));
        SciFiUiKit.StyleWindow(box.GetComponent<Image>());
        box.GetComponent<Image>().raycastTarget = false;
        EnsureHeading(box, "VICTORY");

        foreach (Transform child in canvas.transform)
        {
            if (child.name != "VictoryButtonRow") continue;
            if (child.childCount == 0)
            {
                Object.DestroyImmediate(child.gameObject);
                break;
            }
        }

        var row = canvas.transform.Find("VictoryButtonRow");
        if (row != null)
        {
            SetAnchors(row.gameObject, new Vector2(0.36f, 0.12f), new Vector2(0.64f, 0.44f));
            foreach (var btn in row.GetComponentsInChildren<Button>(true))
                StyleButton(btn);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SetupDeath()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/DeathScene.unity", OpenSceneMode.Single);
        var canvas = GameObject.Find("MainCanvas");
        if (canvas == null) return;

        var bgTr = canvas.transform.Find("Background");
        if (bgTr != null)
        {
            var bgImg = bgTr.GetComponent<Image>();
            if (bgImg != null)
            {
                SciFiUiKit.StyleBackground(bgImg);
                bgImg.raycastTarget = false;
                EditorUtility.SetDirty(bgImg);
            }
        }

        var box = EnsureBox(canvas.transform, bgTr, new Vector2(0.30f, 0.12f), new Vector2(0.70f, 0.80f));
        SciFiUiKit.StyleWindow(box.GetComponent<Image>());
        box.GetComponent<Image>().raycastTarget = false;

        var heading = EnsureHeadingText(box, "GAME OVER");
        SciFiUiKit.StyleText(heading, 60, SciFiUiKit.TextMain);
        heading.characterSpacing = 16;
        heading.alignment = TextAlignmentOptions.Center;

        var divTr = box.Find("Divider");
        var div = divTr != null ? divTr.gameObject : new GameObject("Divider", typeof(RectTransform), typeof(Image));
        div.transform.SetParent(box, false);
        SetAnchors(div, new Vector2(0.14f, 0.79f), new Vector2(0.86f, 0.795f));
        div.GetComponent<RectTransform>().offsetMin = new Vector2(0f, -1f);
        div.GetComponent<RectTransform>().offsetMax = new Vector2(0f, 1f);
        div.GetComponent<Image>().color = SciFiUiKit.Cyan;
        div.GetComponent<Image>().raycastTarget = false;

        foreach (var name in new[] { "RetryButton", "MainMenuButton" })
        {
            var tr = canvas.transform.Find(name);
            if (tr == null) continue;
            SetAnchors(tr.gameObject,
                name == "RetryButton" ? new Vector2(0.36f, 0.28f) : new Vector2(0.36f, 0.17f),
                name == "RetryButton" ? new Vector2(0.64f, 0.37f) : new Vector2(0.64f, 0.26f));
            var btn = tr.GetComponent<Button>();
            if (btn != null)
                SciFiUiKit.StyleButton(btn, btn.GetComponentInChildren<TextMeshProUGUI>(true), 30);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Transform ApplyBackground(Transform canvas)
    {
        var bg = canvas.Find("Background");
        if (bg == null) return null;

        var img = bg.GetComponent<Image>();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Splash/title_horizon.jpg");
        if (img != null && sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.color = Color.white;
            EditorUtility.SetDirty(img);
        }

        var raw = bg.GetComponent<RawImage>();
        if (raw != null && sprite != null)
        {
            raw.texture = sprite.texture;
            raw.color = Color.white;
            EditorUtility.SetDirty(raw);
        }
        return bg;
    }

    private static Transform EnsureBox(Transform canvas, Transform bg, Vector2 min, Vector2 max)
    {
        var tr = canvas.Find("ContentBox");
        var go = tr != null ? tr.gameObject : new GameObject("ContentBox", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvas, false);
        go.transform.SetSiblingIndex(bg != null ? bg.GetSiblingIndex() + 1 : 0);
        SetAnchors(go, min, max);

        var img = go.GetComponent<Image>();
        img.sprite = null;
        img.color = BoxColor;
        img.raycastTarget = false;
        return go.transform;
    }

    private static TextMeshProUGUI EnsureHeadingText(Transform box, string text)
    {
        var tr = box.Find("Heading");
        var go = tr != null ? tr.gameObject : new GameObject("Heading", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(box, false);
        SetAnchors(go, new Vector2(0f, 0.82f), new Vector2(1f, 0.97f));
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void EnsureHeading(Transform box, string text)
    {
        var tr = box.Find("Heading");
        var go = tr != null ? tr.gameObject : new GameObject("Heading", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(box, false);
        SetAnchors(go, new Vector2(0f, 0.8f), new Vector2(1f, 0.98f));

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        SciFiUiKit.StyleText(tmp, 56, SciFiUiKit.TextMain);
        tmp.characterSpacing = 16;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }

    private static void PlaceButton(Transform tr, Vector2 min, Vector2 max)
    {
        if (tr == null) return;
        SetAnchors(tr.gameObject, min, max);
        var btn = tr.GetComponent<Button>();
        if (btn != null) StyleButton(btn);
    }

    private static void StyleButton(Button btn)
    {
        SciFiUiKit.StyleButton(btn, btn.GetComponentInChildren<TextMeshProUGUI>(true), 28);
        btn.GetComponent<Image>().pixelsPerUnitMultiplier = 6f;

        var legacy = btn.GetComponentInChildren<Text>(true);
        if (legacy != null)
            legacy.color = Color.white;

        EditorUtility.SetDirty(btn);
    }

    private static void ApplyFont(TextMeshProUGUI tmp)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Fonts/Coiny SDF.asset");
        if (font == null) return;
        tmp.font = font;
        if (font.material != null)
            tmp.fontSharedMaterial = font.material;
    }

    private static void SetAnchors(GameObject go, Vector2 min, Vector2 max)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }
}
