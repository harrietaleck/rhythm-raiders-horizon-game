using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tools → Setup Gameplay HUD (Sci-Fi Icons)
/// Rebuilds the gameplay Canvas HUD with icon + text rows using the
/// AIRIDev sci-fi icon pack. Wires all TMP references to HUDfunctions.
/// Run once per gameplay scene (MainGameL1, L2, L3).
/// </summary>
public static class SetupGameplayHUD
{
    const string ICONS     = "Assets/AIRIDev_Scifi_UI_Icons/Sprites/Icons/";
    const float  ROW_H    = 48f;
    const float  PAD_X    = 12f;
    const float  PAD_Y    = 10f;
    const float  SPACING  = 4f;
    const float  ICON_SIZE = 40f;

    // ── Entry point ──────────────────────────────────────────────────────────

    [MenuItem("Tools/Setup Gameplay HUD (Sci-Fi Icons)")]
    static void Run()
    {
        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) { Debug.LogError("[HUD] No Canvas found in scene."); return; }
        var ct = canvas.transform;

        // Remove old plain HUD text objects and any previous HUD panels/root
        foreach (var n in new[] { "health", "artifact", "Hitsleft",
                                   "HUD_LeftPanel", "HUD_RightPanel", "HUD_Root" })
            DestroyChild(ct, n);

        // ── HUD_Root — scale this in the Inspector to resize the whole HUD ──
        var rootGO = new GameObject("HUD_Root");
        rootGO.transform.SetParent(ct, false);
        var rootRT          = rootGO.AddComponent<RectTransform>();
        rootRT.anchorMin    = Vector2.zero;
        rootRT.anchorMax    = Vector2.one;
        rootRT.offsetMin    = Vector2.zero;
        rootRT.offsetMax    = Vector2.zero;
        rootGO.AddComponent<HUDScaler>();   // exposes Scale + IconSize sliders in Inspector
        var rootT = rootGO.transform;

        // ── Left panel — stats ───────────────────────────────────────────────
        // 5 rows: Vibe, Shield, Artifacts, Guard, Boost
        int leftRows     = 5;
        float leftHeight = PAD_Y + leftRows * ROW_H + (leftRows - 1) * SPACING + PAD_Y;
        var leftRT = MakePanel(rootT, "HUD_LeftPanel",
            anchorMin:       new Vector2(0f, 1f),
            anchorMax:       new Vector2(0f, 1f),
            anchoredPos:     new Vector2(16f, -16f),
            size:            new Vector2(360f, leftHeight));

        var vibeText     = MakeRow(leftRT, "VibeRow",     "Icon_Energy.png.png",  0, "Vibe");
        var shieldText   = MakeRow(leftRT, "ShieldRow",   "Icon_Shield.png.png",  1, "Shield");
        var artifactText = MakeRow(leftRT, "ArtifactRow", "Icon_Trophy.png.png",  2, "Artifacts");
        var guardText    = MakeRow(leftRT, "GuardRow",    "Icon_Drone.png.png",   3, "Guard");
        var boostText    = MakeRow(leftRT, "BoostRow",    "Icon_Boost.png.png",   4, "Boost");

        // Boost row starts hidden — HUDfunctions activates it when a boost is active
        boostText.transform.parent.gameObject.SetActive(false);

        // Apply tinted colours matching game theme
        StyleText(vibeText,     new Color(0.35f, 1f,    0.35f));   // green  — vibe
        StyleText(shieldText,   new Color(0.4f,  0.75f, 1f));      // blue   — shield
        StyleText(artifactText, new Color(1f,    0.85f, 0.1f));    // gold   — artifacts
        StyleText(guardText,    new Color(1f,    0.4f,  0.4f));    // red    — guard
        StyleText(boostText,    new Color(1f,    0.9f,  0.3f));    // yellow — boost

        // ── Right panel — score + level ──────────────────────────────────────
        int rightRows     = 2;
        float rightHeight = PAD_Y + rightRows * ROW_H + (rightRows - 1) * SPACING + PAD_Y;
        var rightRT = MakePanel(rootT, "HUD_RightPanel",
            anchorMin:   new Vector2(1f, 1f),
            anchorMax:   new Vector2(1f, 1f),
            anchoredPos: new Vector2(-16f, -16f),
            size:        new Vector2(250f, rightHeight));
        rightRT.pivot = Vector2.one;   // pivot top-right so anchoredPos is from top-right corner

        var scoreText = MakeRow(rightRT, "ScoreRow", "Icon_Coin.png.png", 0, "Score",  TextAlignmentOptions.Left);
        var levelText = MakeRow(rightRT, "LevelRow", null,                1, "Level",  TextAlignmentOptions.Left);
        StyleText(scoreText, new Color(0.9f, 0.9f, 0.9f));   // white
        StyleText(levelText, new Color(0.7f, 0.7f, 1f));     // soft blue

        // ── Wire to HUDfunctions ─────────────────────────────────────────────
        var player = GameObject.FindGameObjectWithTag("Player");
        var hud    = player != null ? player.GetComponent<HUDfunctions>() : null;
        if (hud != null)
        {
            hud.healthText    = vibeText;
            hud.hitText       = shieldText;
            hud.artifactText  = artifactText;
            hud.guardDistText = guardText;
            hud.boostText     = boostText;
            hud.scoreText     = scoreText;
            hud.levelText     = levelText;
            EditorUtility.SetDirty(hud);
            Debug.Log("[HUD] HUDfunctions wired successfully.");
        }
        else
        {
            Debug.LogWarning("[HUD] HUDfunctions not found on Player — wire manually.");
        }

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        Debug.Log("[HUD] Gameplay HUD rebuilt with sci-fi icons.");
    }

    [MenuItem("Tools/Fix HUD Panel Colors")]
    static void FixPanelColors()
    {
        // Dark navy 80% opaque — clearly visible on any background
        var col = new Color(0f, 0.04f, 0.18f, 0.82f);

        foreach (var panelName in new[] { "HUD_LeftPanel", "HUD_RightPanel" })
        {
            var go = GameObject.Find(panelName);
            if (go == null) { Debug.LogWarning($"[HUD] {panelName} not found."); continue; }

            var img = go.GetComponent<UnityEngine.UI.Image>();
            if (img == null)
            {
                // Add Image if somehow missing
                img = go.AddComponent<UnityEngine.UI.Image>();
                Debug.Log($"[HUD] Added Image to {panelName}");
            }

            img.enabled       = true;
            img.color         = col;
            img.raycastTarget = false;
            EditorUtility.SetDirty(go);
            Debug.Log($"[HUD] {panelName} → color={col}  alpha={col.a}  enabled={img.enabled}");
        }

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        // Save silently — no dialog
        EditorSceneManager.SaveScene(scene, scene.path);
        Debug.Log("[HUD] Scene saved.");
    }

    // ── Builders ─────────────────────────────────────────────────────────────

    /// <summary>Creates a semi-transparent dark panel anchored to a corner.</summary>
    static RectTransform MakePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img   = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0.08f, 0.72f);   // dark navy, 72% opaque
        img.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = anchorMin;      // pivot at same corner as anchor
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
        return rt;
    }

    /// <summary>
    /// Creates one icon + text row inside a panel.
    /// Rows are stacked top-to-bottom with fixed Y offsets.
    /// Returns the TextMeshProUGUI component on the text child.
    /// </summary>
    static TextMeshProUGUI MakeRow(RectTransform panel, string rowName,
        string iconFile, int rowIndex, string label = "",
        TextAlignmentOptions align = TextAlignmentOptions.Left)
    {
        float yOffset = -(PAD_Y + rowIndex * (ROW_H + SPACING));

        // ── Row container ────────────────────────────────────────────────────
        var rowGO = new GameObject(rowName);
        rowGO.transform.SetParent(panel, false);

        var rowRT = rowGO.AddComponent<RectTransform>();
        rowRT.anchorMin        = new Vector2(0f, 1f);   // stretch horizontally, top anchor
        rowRT.anchorMax        = new Vector2(1f, 1f);
        rowRT.pivot            = new Vector2(0f, 1f);   // top-left pivot
        rowRT.anchoredPosition = new Vector2(0f, yOffset);
        rowRT.sizeDelta        = new Vector2(-PAD_X * 2f, ROW_H);   // width = panel - 2×padding

        var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing              = 10f;
        hlg.childAlignment       = TextAnchor.MiddleLeft;
        hlg.childControlHeight   = false;
        hlg.childControlWidth    = false;
        hlg.childForceExpandHeight = false;
        hlg.childForceExpandWidth  = false;
        hlg.padding              = new RectOffset(PAD_X > 0 ? 0 : 0, 0, 0, 0);

        // ── Icon ─────────────────────────────────────────────────────────────
        if (!string.IsNullOrEmpty(iconFile))
        {
            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(rowGO.transform, false);

            var iconRT = iconGO.AddComponent<RectTransform>();
            iconRT.sizeDelta = new Vector2(ICON_SIZE, ICON_SIZE);

            var iconImg = iconGO.AddComponent<Image>();
            var spr     = AssetDatabase.LoadAssetAtPath<Sprite>(ICONS + iconFile);
            if (spr != null)
                iconImg.sprite = spr;
            else
                Debug.LogWarning($"[HUD] Icon not found: {ICONS + iconFile}");
            iconImg.preserveAspect = true;
            iconImg.raycastTarget  = false;

            var le = iconGO.AddComponent<LayoutElement>();
            le.preferredWidth  = ICON_SIZE;
            le.preferredHeight = ICON_SIZE;
            le.minWidth        = ICON_SIZE;
        }

        // ── Text ─────────────────────────────────────────────────────────────
        string textName = rowName.Replace("Row", "Text");
        var textGO = new GameObject(textName);
        textGO.transform.SetParent(rowGO.transform, false);

        var textRT = textGO.AddComponent<RectTransform>();
        // Text fills the rest of the row after the icon
        float textWidth = 280f;
        textRT.sizeDelta = new Vector2(textWidth, ROW_H);

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.fontSize            = 30f;
        tmp.fontStyle           = FontStyles.Bold;
        tmp.color               = Color.white;
        tmp.alignment           = align;
        tmp.text                = string.IsNullOrEmpty(label) ? "——" : label;
        tmp.raycastTarget       = false;
        tmp.textWrappingMode  = TextWrappingModes.NoWrap;
        tmp.overflowMode        = TextOverflowModes.Overflow;  // never clip label
        TmpUiUtility.EnsureFont(tmp);

        var textLE = textGO.AddComponent<LayoutElement>();
        textLE.preferredWidth  = textWidth;
        textLE.preferredHeight = ROW_H;
        textLE.flexibleWidth   = 1f;

        return tmp;
    }

    static void StyleText(TextMeshProUGUI tmp, Color color)
    {
        if (tmp == null) return;
        tmp.color = color;
    }

    static void DestroyChild(Transform parent, string childName)
    {
        var t = parent.Find(childName);
        if (t != null) Object.DestroyImmediate(t.gameObject);
    }
}
