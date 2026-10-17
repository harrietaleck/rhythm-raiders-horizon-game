using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full-screen UI cutscenes per level theme:
/// L1 Rooftop — sci-fi blast doors part onto the run (Opera curtains kept for legacy)
/// L2 Museum — ornate doors open (museumdoor texture)
/// L3 Club — underground neon door opens (undergroundoor texture)
/// </summary>
public static class LevelCutscenePlayer
{
    const int SortOrder = 500;

    public static IEnumerator PlayAndWait(LevelCutsceneType type)
    {
        if (type == LevelCutsceneType.None)
            yield break;

        var root = CreateOverlayRoot();
        PlayCutsceneAudio(type, 0);

        switch (type)
        {
            case LevelCutsceneType.OperaCurtain:
                yield return PlayOperaCurtain(root);
                break;
            case LevelCutsceneType.RooftopDoor:
                yield return PlayRooftopDoor(root);
                break;
            case LevelCutsceneType.MuseumSecurityDoor:
                yield return PlayMuseumSecurityDoor(root);
                break;
            case LevelCutsceneType.ClubDiscoBall:
                yield return PlayClubDiscoBall(root);
                break;
        }

        Object.Destroy(root);
    }

    static GameObject CreateOverlayRoot()
    {
        var go = new GameObject("LevelCutsceneOverlay");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortOrder;
        go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        go.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        go.AddComponent<GraphicRaycaster>();

        var blocker = CreatePanel(go.transform, "Blocker", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 1f));
        blocker.transform.SetAsFirstSibling();
        return go;
    }

    static IEnumerator PlayOperaCurtain(GameObject root)
    {
        // Try to load the real curtain texture (copied to Resources by SetupCurtainResource editor script)
        var curtainTex = Resources.Load<Texture2D>("curtain_texture");

        // Left curtain — slides in from the left
        var left  = CreateCurtainPanel(root.transform, "CurtainLeft",  curtainTex, flipH: false);
        var right = CreateCurtainPanel(root.transform, "CurtainRight", curtainTex, flipH: true);

        var leftRt  = left.GetComponent<RectTransform>();
        var rightRt = right.GetComponent<RectTransform>();

        // Start off-screen
        leftRt.anchorMin  = new Vector2(-0.52f, 0f); leftRt.anchorMax  = new Vector2(-0.02f, 1f);
        rightRt.anchorMin = new Vector2(1.02f,  0f); rightRt.anchorMax = new Vector2(1.52f,  1f);

        // Slide in to cover screen
        yield return AnimateAnchors(leftRt,  leftRt.anchorMin,  new Vector2(0f,   0f), 0.75f);
        yield return AnimateAnchors(rightRt, rightRt.anchorMin, new Vector2(0.5f, 0f), 0.75f);
        rightRt.anchorMax = new Vector2(1f, 1f);

        PlayCutsceneAudio(LevelCutsceneType.OperaCurtain, 1);

        // Show "Level 1 — Opera House" title over the closed curtains
        var title = CreateTitleCard(root.transform, "LEVEL 1", "OPERA HOUSE");
        yield return FadeInGraphic(title.GetComponentsInChildren<Graphic>(), 0.35f);
        yield return new WaitForSecondsRealtime(1.1f);
        yield return FadeOutGraphics(title.GetComponentsInChildren<Graphic>(), 0.25f);

        PlayCutsceneAudio(LevelCutsceneType.OperaCurtain, 2);

        // Curtains part — slide off screen
        yield return AnimateAnchors(leftRt,  new Vector2(0f,   0f), new Vector2(-0.52f, 0f), 0.8f);
        yield return AnimateAnchors(rightRt, new Vector2(0.5f, 0f), new Vector2(1.02f,  0f), 0.8f);
        yield return FadeOut(root, 0.4f);
    }

    static IEnumerator PlayRooftopDoor(GameObject root)
    {
        // Sci-fi blast doors over the live scene: title card, unlock, doors part onto the run.
        var left  = CreateBlastDoor(root.transform, "DoorLeft",  seamOnRight: true,  out var leftLight,  out var leftSeam);
        var right = CreateBlastDoor(root.transform, "DoorRight", seamOnRight: false, out var rightLight, out var rightSeam);
        var leftRt  = left.GetComponent<RectTransform>();
        var rightRt = right.GetComponent<RectTransform>();
        leftRt.anchorMin  = new Vector2(0f, 0f);   leftRt.anchorMax  = new Vector2(0.5f, 1f);
        rightRt.anchorMin = new Vector2(0.5f, 0f); rightRt.anchorMax = new Vector2(1f, 1f);

        var title = CreateTitleCard(root.transform, "LEVEL 1", "ROOFTOP");
        yield return FadeInGraphic(title.GetComponentsInChildren<Graphic>(), 0.35f);
        yield return new WaitForSecondsRealtime(1.1f);
        yield return FadeOutGraphics(title.GetComponentsInChildren<Graphic>(), 0.25f);

        // Unlock: status lights go green, the seam flares and the doors jolt.
        var green = new Color(0.25f, 1f, 0.45f, 1f);
        leftLight.color = rightLight.color = green;
        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            float k = t / 0.35f;
            var seam = new Color(0.55f, 0.95f, 1f, Mathf.Lerp(0.6f, 1f, k));
            leftSeam.color = rightSeam.color = seam;
            float jolt = Mathf.Sin(t * 90f) * 4f * (1f - k);
            leftRt.anchoredPosition = new Vector2(-jolt, 0f);
            rightRt.anchorMin = new Vector2(0.5f, 0f);
            rightRt.anchoredPosition = new Vector2(jolt, 0f);
            yield return null;
        }
        leftRt.anchoredPosition = rightRt.anchoredPosition = Vector2.zero;

        // Reveal the scene behind the doors and part them together.
        var blocker = root.transform.Find("Blocker");
        if (blocker != null) blocker.gameObject.SetActive(false);
        var flash = CreatePanel(root.transform, "OpenFlash", new Vector2(0.4f, 0f), new Vector2(0.6f, 1f), new Color(0.6f, 0.95f, 1f, 0.55f));
        var flashImg = flash.GetComponent<Image>();

        const float openTime = 0.75f;
        for (float t = 0f; t < openTime; t += Time.unscaledDeltaTime)
        {
            float k = t / openTime;
            float ease = k * k * (3f - 2f * k);
            leftRt.anchorMin  = new Vector2(-0.5f * ease, 0f);
            leftRt.anchorMax  = new Vector2(0.5f - 0.5f * ease, 1f);
            rightRt.anchorMin = new Vector2(0.5f + 0.5f * ease, 0f);
            rightRt.anchorMax = new Vector2(1f + 0.5f * ease, 1f);
            var rt = flash.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f - 0.5f * ease, 0f);
            rt.anchorMax = new Vector2(0.5f + 0.5f * ease, 1f);
            flashImg.color = new Color(0.6f, 0.95f, 1f, 0.55f * (1f - k));
            yield return null;
        }
        Object.Destroy(flash);
    }

    static GameObject CreateBlastDoor(Transform parent, string name, bool seamOnRight, out Image statusLight, out Image seamGlow)
    {
        var door = CreatePanel(parent, name, Vector2.zero, Vector2.one, new Color(0.08f, 0.09f, 0.11f, 1f));

        // Inset face with horizontal ribs.
        var face = CreatePanel(door.transform, "Face", new Vector2(0.025f, 0.025f), new Vector2(0.975f, 0.975f),
            new Color(0.15f, 0.16f, 0.2f, 1f));
        for (int i = 1; i <= 6; i++)
        {
            float y = 0.2f + i * 0.1f;
            CreatePanel(face.transform, "Rib", new Vector2(0.04f, y), new Vector2(0.96f, y + 0.012f), new Color(0.09f, 0.1f, 0.12f, 1f));
            CreatePanel(face.transform, "RibLight", new Vector2(0.04f, y + 0.012f), new Vector2(0.96f, y + 0.016f), new Color(0.26f, 0.28f, 0.33f, 1f));
        }

        // Yellow/black hazard band across the bottom.
        var band = CreatePanel(face.transform, "Hazard", new Vector2(0f, 0.05f), new Vector2(1f, 0.15f), new Color(0.95f, 0.72f, 0.1f, 1f));
        band.AddComponent<RectMask2D>();
        const int stripes = 22;
        for (int i = -1; i <= stripes; i++)
        {
            var stripe = CreatePanel(band.transform, "Stripe", new Vector2((i + 0.5f) / stripes, 0.5f), new Vector2((i + 0.5f) / stripes, 0.5f),
                new Color(0.06f, 0.06f, 0.07f, 1f));
            var srt = stripe.GetComponent<RectTransform>();
            srt.sizeDelta = new Vector2(20f, 260f);
            srt.localEulerAngles = new Vector3(0f, 0f, seamOnRight ? -38f : 38f);
        }

        // Stencilled label, using the Sci-Fi GUI font when available.
        var label = new GameObject("Label");
        label.transform.SetParent(face.transform, false);
        var lrt = label.AddComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0.08f, 0.86f);
        lrt.anchorMax = new Vector2(0.92f, 0.95f);
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var tmp = label.AddComponent<TextMeshProUGUI>();
        var skin = HudSkin.Get();
        if (skin != null && skin.font != null) tmp.font = skin.font;
        tmp.text = seamOnRight ? "ROOF ACCESS" : "SECTOR 01";
        tmp.fontSize = 34f;
        tmp.characterSpacing = 14f;
        tmp.color = new Color(0.5f, 0.55f, 0.62f, 1f);
        tmp.alignment = seamOnRight ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
        tmp.raycastTarget = false;

        // Cyan light strip along the seam and a status light beside it.
        float seamMin = seamOnRight ? 0.982f : 0f;
        float seamMax = seamOnRight ? 1f : 0.018f;
        seamGlow = CreatePanel(door.transform, "SeamGlow", new Vector2(seamMin, 0f), new Vector2(seamMax, 1f),
            new Color(0.35f, 0.85f, 1f, 0.6f)).GetComponent<Image>();
        float lx = seamOnRight ? 0.9f : 0.06f;
        CreatePanel(door.transform, "LightHousing", new Vector2(lx - 0.008f, 0.465f), new Vector2(lx + 0.048f, 0.535f), new Color(0.05f, 0.05f, 0.06f, 1f));
        statusLight = CreatePanel(door.transform, "StatusLight", new Vector2(lx, 0.475f), new Vector2(lx + 0.04f, 0.525f),
            new Color(1f, 0.2f, 0.15f, 1f)).GetComponent<Image>();
        return door;
    }

    // Creates a curtain panel using RawImage so the Texture2D can be applied directly.
    // flipH mirrors the UV so left and right curtains look like a matching pair.
    static GameObject CreateCurtainPanel(Transform parent, string name, Texture2D tex, bool flipH)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        if (tex != null)
        {
            var raw = go.AddComponent<RawImage>();
            raw.texture = tex;
            // Single UV rect (no tiling) — avoids the visible seam mid-curtain.
            // Right panel is horizontally flipped so both sides mirror each other.
            raw.uvRect  = flipH ? new Rect(1f, 0f, -1f, 1f) : new Rect(0f, 0f, 1f, 1f);
            raw.raycastTarget = false;
        }
        else
        {
            // Fallback: plain dark red if texture not found
            var img = go.AddComponent<Image>();
            img.color = new Color(0.5f, 0.05f, 0.08f, 1f);
            img.raycastTarget = false;
        }
        return go;
    }

    // Creates a centred title card with a large level number and subtitle.
    static GameObject CreateBannerTitle(Transform parent, string line)
    {
        var card = new GameObject("TitleCard");
        card.transform.SetParent(parent, false);
        var cardRt = card.AddComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.1f, 0.38f);
        cardRt.anchorMax = new Vector2(0.9f, 0.62f);
        cardRt.offsetMin = cardRt.offsetMax = Vector2.zero;

        var bg = card.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.5f);
        bg.raycastTarget = false;
        SkinCard(bg);

        AddTitleText(card.transform, line, fontSize: 52, yAnchorMin: 0.1f, yAnchorMax: 0.9f,
            color: new Color(1f, 0.92f, 0.55f, 1f));
        return card;
    }

    static GameObject CreateTitleCard(Transform parent, string levelLine, string subtitleLine)
    {
        var card = new GameObject("TitleCard");
        card.transform.SetParent(parent, false);
        var cardRt = card.AddComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.15f, 0.35f);
        cardRt.anchorMax = new Vector2(0.85f, 0.65f);
        cardRt.offsetMin = cardRt.offsetMax = Vector2.zero;

        // Sci-Fi GUI window behind the text (dark band if the skin is missing)
        var bg = card.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);
        bg.raycastTarget = false;
        bool skinned = SkinCard(bg);

        AddTitleText(card.transform, levelLine,   fontSize: 80, yAnchorMin: 0.5f, yAnchorMax: 0.9f,
                     color: skinned ? HudSkin.Cyan : new Color(1f, 0.92f, 0.55f, 1f));
        AddTitleText(card.transform, subtitleLine, fontSize: 44, yAnchorMin: 0.12f, yAnchorMax: 0.48f,
                     color: skinned ? new Color(1f, 0.85f, 0.4f, 1f) : Color.white);
        return card;
    }

    static bool SkinCard(Image bg)
    {
        var skin = HudSkin.Get();
        if (skin == null || skin.window == null) return false;
        bg.sprite = skin.window;
        bg.type = Image.Type.Sliced;
        bg.pixelsPerUnitMultiplier = 2f;
        bg.color = Color.white;
        return true;
    }

    static void AddTitleText(Transform parent, string text, int fontSize,
                             float yAnchorMin, float yAnchorMax, Color color)
    {
        var go = new GameObject("TitleText");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f, yAnchorMin);
        rt.anchorMax = new Vector2(0.95f, yAnchorMax);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        var skin = HudSkin.Get();
        if (skin != null && skin.font != null)
        {
            tmp.font = skin.font;
            tmp.characterSpacing = 12f;
        }
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = skin != null && skin.font != null ? FontStyles.Normal : FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = color;
    }

    static IEnumerator FadeInGraphic(Graphic[] graphics, float duration)
    {
        float t = 0f;
        foreach (var g in graphics) { var c = g.color; c.a = 0f; g.color = c; }
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            foreach (var g in graphics) { var c = g.color; c.a = p; g.color = c; }
            yield return null;
        }
    }

    static IEnumerator FadeOutGraphics(Graphic[] graphics, float duration)
    {
        float t = 0f;
        var startAlphas = new float[graphics.Length];
        for (int i = 0; i < graphics.Length; i++) startAlphas[i] = graphics[i].color.a;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            for (int i = 0; i < graphics.Length; i++)
            {
                var c = graphics[i].color;
                c.a = Mathf.Lerp(startAlphas[i], 0f, p);
                graphics[i].color = c;
            }
            yield return null;
        }
    }

    static IEnumerator PlayMuseumSecurityDoor(GameObject root)
    {
        // Night museum entrance: marble columns, sconces, carved lintel and the ornate doors,
        // unlocked by a keypad, swinging inward on light, then pushing through into the hall.
        var doorTex = Resources.Load<Texture2D>("museumdoor");
        var stage = CreatePanel(root.transform, "Stage", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0f));
        var wallBg = new Color(0.05f, 0.045f, 0.07f, 1f);
        var wallLeft = CreatePanel(stage.transform, "WallBack", Vector2.zero, new Vector2(0.5f, 1f), wallBg);
        var wallRight = CreatePanel(stage.transform, "WallBack", new Vector2(0.5f, 0f), Vector2.one, wallBg);
        var wallTop = CreatePanel(stage.transform, "WallBack", new Vector2(0.4f, 0.5f), new Vector2(0.6f, 1f), wallBg);
        var wallBottom = CreatePanel(stage.transform, "WallBack", new Vector2(0.4f, 0f), new Vector2(0.6f, 0.5f), wallBg);
        var stageRt = stage.GetComponent<RectTransform>();
        var gold = new Color(0.78f, 0.6f, 0.28f, 1f);
        var marble = new Color(0.42f, 0.4f, 0.37f, 1f);

        // Wall panelling with gold trim.
        for (int i = 0; i < 6; i++)
        {
            float x = 0.02f + i * 0.165f;
            if (x > 0.27f && x < 0.66f) continue;
            CreatePanel(stage.transform, "WallPanel", new Vector2(x, 0.2f), new Vector2(x + 0.13f, 0.85f), new Color(0.08f, 0.07f, 0.1f, 1f));
            CreatePanel(stage.transform, "PanelTrim", new Vector2(x, 0.85f), new Vector2(x + 0.13f, 0.856f), gold * 0.8f);
            CreatePanel(stage.transform, "PanelTrim", new Vector2(x, 0.194f), new Vector2(x + 0.13f, 0.2f), gold * 0.8f);
        }

        // Marble floor and red carpet.
        CreatePanel(stage.transform, "Floor", new Vector2(0f, 0f), new Vector2(1f, 0.1f), new Color(0.11f, 0.095f, 0.1f, 1f));
        CreatePanel(stage.transform, "FloorEdge", new Vector2(0f, 0.1f), new Vector2(1f, 0.106f), gold * 0.6f);
        CreatePanel(stage.transform, "Carpet", new Vector2(0.38f, 0f), new Vector2(0.62f, 0.1f), new Color(0.4f, 0.04f, 0.06f, 1f));
        CreatePanel(stage.transform, "CarpetTrim", new Vector2(0.375f, 0f), new Vector2(0.38f, 0.1f), gold);
        CreatePanel(stage.transform, "CarpetTrim", new Vector2(0.62f, 0f), new Vector2(0.625f, 0.1f), gold);

        // Marble columns either side of the doorway, with sconces.
        foreach (float cx in new[] { 0.23f, 0.77f })
        {
            CreatePanel(stage.transform, "Column", new Vector2(cx - 0.035f, 0.1f), new Vector2(cx + 0.035f, 0.88f), marble);
            CreatePanel(stage.transform, "ColumnShade", new Vector2(cx + 0.012f, 0.1f), new Vector2(cx + 0.035f, 0.88f), new Color(0.3f, 0.28f, 0.26f, 1f));
            CreatePanel(stage.transform, "Capital", new Vector2(cx - 0.045f, 0.86f), new Vector2(cx + 0.045f, 0.9f), gold);
            CreatePanel(stage.transform, "Base", new Vector2(cx - 0.045f, 0.1f), new Vector2(cx + 0.045f, 0.13f), gold);
            float sx = cx < 0.5f ? cx - 0.12f : cx + 0.12f;
            CreateSconce(stage.transform, sx, 0.62f);
        }

        // Door rectangle: crop the photo to the doors (no arch/background), keep its aspect.
        const float doorBottom = 0.1f, doorTop = 0.72f;
        Rect uv = new Rect(0.055f, 0.0f, 0.89f, 0.665f);
        float texAspect = doorTex != null ? (doorTex.width * uv.width) / (doorTex.height * uv.height) : 0.95f;
        float screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        float halfW = (doorTop - doorBottom) * texAspect / screenAspect * 0.5f;
        float dl = 0.5f - halfW, dr = 0.5f + halfW;

        SetAnchors(wallLeft, Vector2.zero, new Vector2(dl, 1f));
        SetAnchors(wallRight, new Vector2(dr, 0f), Vector2.one);
        SetAnchors(wallTop, new Vector2(dl, doorTop), new Vector2(dr, 1f));
        SetAnchors(wallBottom, new Vector2(dl, 0f), new Vector2(dr, doorBottom));

        // Marble surround, gold frame and carved lintel with the plaque.
        CreateBorder(stage.transform, "Surround", new Vector2(dl - 0.035f, doorBottom), new Vector2(dr + 0.035f, doorTop + 0.03f), dl, dr, doorTop, marble);
        CreatePanel(stage.transform, "Lintel", new Vector2(dl - 0.06f, doorTop + 0.03f), new Vector2(dr + 0.06f, doorTop + 0.13f), new Color(0.48f, 0.45f, 0.41f, 1f));
        CreatePanel(stage.transform, "LintelTrim", new Vector2(dl - 0.06f, doorTop + 0.03f), new Vector2(dr + 0.06f, doorTop + 0.037f), gold);
        CreatePanel(stage.transform, "LintelTrim", new Vector2(dl - 0.06f, doorTop + 0.123f), new Vector2(dr + 0.06f, doorTop + 0.13f), gold);
        CreatePanel(stage.transform, "Plaque", new Vector2(dl + 0.01f, doorTop + 0.05f), new Vector2(dr - 0.01f, doorTop + 0.11f), new Color(0.12f, 0.08f, 0.05f, 1f));
        var plaque = new GameObject("PlaqueText");
        plaque.transform.SetParent(stage.transform, false);
        var prt = plaque.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(dl + 0.01f, doorTop + 0.05f);
        prt.anchorMax = new Vector2(dr - 0.01f, doorTop + 0.11f);
        prt.offsetMin = prt.offsetMax = Vector2.zero;
        var ptxt = plaque.AddComponent<TextMeshProUGUI>();
        ptxt.text = "MUSEUM OF SOUND";
        ptxt.fontSize = 34f;
        ptxt.fontStyle = FontStyles.Bold;
        ptxt.characterSpacing = 18f;
        ptxt.alignment = TextAlignmentOptions.Center;
        ptxt.color = new Color(0.95f, 0.78f, 0.4f, 1f);
        ptxt.raycastTarget = false;
        CreateBorder(stage.transform, "Frame", new Vector2(dl - 0.008f, doorBottom), new Vector2(dr + 0.008f, doorTop + 0.012f), dl, dr, doorTop, gold);

        // Doorway: dark until the doors open, then fades to reveal the live hall behind.
        var doorway = CreatePanel(stage.transform, "Doorway", new Vector2(dl, doorBottom), new Vector2(dr, doorTop), new Color(0.03f, 0.02f, 0.02f, 1f));
        var doorwayImg = doorway.GetComponent<Image>();

        // Door leaves, hinged on the outer edges and tinted for night.
        var nightTint = new Color(0.78f, 0.7f, 0.6f, 1f);
        var left = CreateDoorLeaf(stage.transform, "DoorLeft", doorTex, new Rect(uv.x, uv.y, uv.width * 0.5f, uv.height),
            new Vector2(dl, doorBottom), new Vector2(0.5f, doorTop), new Vector2(0f, 0.5f), nightTint);
        var right = CreateDoorLeaf(stage.transform, "DoorRight", doorTex, new Rect(uv.x + uv.width * 0.5f, uv.y, uv.width * 0.5f, uv.height),
            new Vector2(0.5f, doorBottom), new Vector2(dr, doorTop), new Vector2(1f, 0.5f), nightTint);

        var seam = CreatePanel(stage.transform, "SeamGlow", new Vector2(0.5f - 0.004f, doorBottom), new Vector2(0.5f + 0.004f, doorTop), new Color(0.55f, 0.95f, 1f, 0f));
        var seamImg = seam.GetComponent<Image>();

        // Security keypad beside the door.
        CreatePanel(stage.transform, "Keypad", new Vector2(dr + 0.05f, 0.38f), new Vector2(dr + 0.075f, 0.47f), new Color(0.07f, 0.07f, 0.08f, 1f));
        CreatePanel(stage.transform, "KeypadFace", new Vector2(dr + 0.054f, 0.39f), new Vector2(dr + 0.071f, 0.44f), new Color(0.15f, 0.16f, 0.18f, 1f));
        var lockLight = CreatePanel(stage.transform, "LockLight", new Vector2(dr + 0.056f, 0.448f), new Vector2(dr + 0.069f, 0.462f),
            new Color(1f, 0.15f, 0.1f, 1f)).GetComponent<Image>();

        // Title card.
        var title = CreateTitleCard(root.transform, "LEVEL 2", "HALL OF HARMONY");
        yield return FadeInGraphic(title.GetComponentsInChildren<Graphic>(), 0.4f);
        PlayCutsceneAudio(LevelCutsceneType.MuseumSecurityDoor, 1);
        yield return new WaitForSecondsRealtime(1.2f);
        yield return FadeOutGraphics(title.GetComponentsInChildren<Graphic>(), 0.25f);

        // Unlock: keypad goes green, the doors settle.
        lockLight.color = new Color(0.25f, 1f, 0.45f, 1f);
        PlayCutsceneAudio(LevelCutsceneType.MuseumSecurityDoor, 2);
        var leftJolt = left.GetComponent<RectTransform>();
        var rightJolt = right.GetComponent<RectTransform>();
        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            float k = t / 0.35f;
            seamImg.color = new Color(0.55f, 0.95f, 1f, Mathf.Lerp(0.6f, 1f, k));
            float jolt = Mathf.Sin(t * 90f) * 3f * (1f - k);
            leftJolt.anchoredPosition = new Vector2(-jolt, 0f);
            rightJolt.anchoredPosition = new Vector2(jolt, 0f);
            yield return null;
        }
        leftJolt.anchoredPosition = rightJolt.anchoredPosition = Vector2.zero;

        // Reveal the hall behind the doors with the same cyan open flash as Level 1.
        var blockerEarly = root.transform.Find("Blocker");
        if (blockerEarly != null) blockerEarly.gameObject.SetActive(false);
        var flash = CreatePanel(stage.transform, "OpenFlash", new Vector2(0.5f, doorBottom), new Vector2(0.5f, doorTop), new Color(0.6f, 0.95f, 1f, 0.55f));
        flash.transform.SetSiblingIndex(seam.transform.GetSiblingIndex());
        var flashRt = flash.GetComponent<RectTransform>();
        var flashImg = flash.GetComponent<Image>();

        // Doors swing inward on their hinges onto the hall, like Level 1's blast doors.
        var leftRt = left.GetComponent<RectTransform>();
        var rightRt = right.GetComponent<RectTransform>();
        var leftImg = left.GetComponent<RawImage>();
        var rightImg = right.GetComponent<RawImage>();
        const float swing = 1.1f;
        for (float t = 0f; t < swing; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / swing);
            float sx = Mathf.Lerp(1f, 0.12f, k);
            leftRt.localScale = rightRt.localScale = new Vector3(sx, 1f, 1f);
            var shade = Color.Lerp(nightTint, new Color(0.3f, 0.25f, 0.2f, 1f), k);
            leftImg.color = rightImg.color = shade;
            doorwayImg.color = new Color(0.03f, 0.02f, 0.02f, 1f - k);
            float half = (dr - dl) * 0.5f * k;
            flashRt.anchorMin = new Vector2(0.5f - half, doorBottom);
            flashRt.anchorMax = new Vector2(0.5f + half, doorTop);
            flashImg.color = new Color(0.6f, 0.95f, 1f, 0.55f * (1f - k));
            seamImg.color = new Color(0.55f, 0.95f, 1f, 1f - k);
            yield return null;
        }

        // Push through the doorway into the live hall.
        var blocker = root.transform.Find("Blocker");
        if (blocker != null) blocker.gameObject.SetActive(false);
        var graphics = stage.GetComponentsInChildren<Graphic>();
        var start = new float[graphics.Length];
        for (int i = 0; i < graphics.Length; i++) start[i] = graphics[i].color.a;
        stageRt.pivot = new Vector2(0.5f, (doorBottom + doorTop) * 0.5f);
        const float push = 0.6f;
        for (float t = 0f; t < push; t += Time.unscaledDeltaTime)
        {
            float k = t / push;
            stageRt.localScale = Vector3.one * Mathf.Lerp(1f, 2.4f, k * k);
            for (int i = 0; i < graphics.Length; i++)
            {
                var c = graphics[i].color;
                c.a = Mathf.Lerp(start[i], 0f, k);
                graphics[i].color = c;
            }
            yield return null;
        }
    }

    static GameObject CreateDoorLeaf(Transform parent, string name, Texture2D tex, Rect uv, Vector2 min, Vector2 max, Vector2 pivot, Color tint)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = pivot;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var raw = go.AddComponent<RawImage>();
        raw.texture = tex;
        raw.uvRect = uv;
        raw.color = tex != null ? tint : new Color(0.55f, 0.5f, 0.42f, 1f);
        raw.raycastTarget = false;
        return go;
    }

    static IEnumerator PlayClubDiscoBall(GameObject root)
    {
        // Underground club entrance: the neon-framed door in the graffiti wall, bass thumping behind
        // it, a flickering NEON BEAT VAULT sign, a keypad that unlocks, then the door swings open
        // onto the live club with a magenta flash (the club's take on Level 1's blast-door reveal).
        var tex = Resources.Load<Texture2D>("undergroundoor");
        if (tex == null)
        {
            yield return PlayTextureDoorCutscene(root, "undergroundoor", "LEVEL 3", LevelCutsceneType.ClubDiscoBall);
            yield break;
        }

        var neon = new Color(1f, 0.25f, 0.85f, 1f);
        var stage = CreatePanel(root.transform, "Stage", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0f));
        var stageRt = stage.GetComponent<RectTransform>();

        // Photo fitted to the screen height and centred; the wall continues dark either side.
        float screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        float photoW = Mathf.Min(1f, ((float)tex.width / tex.height) / screenAspect);
        float px0 = 0.5f - photoW * 0.5f, px1 = 0.5f + photoW * 0.5f;
        var wallDark = new Color(0.07f, 0.04f, 0.11f, 1f);
        CreatePanel(stage.transform, "WallLeft", Vector2.zero, new Vector2(px0 + 0.002f, 1f), wallDark);
        CreatePanel(stage.transform, "WallRight", new Vector2(px1 - 0.002f, 0f), Vector2.one, wallDark);
        CreateGlow(stage.transform, "SideGlowL", new Vector2(px0 - 0.25f, -0.2f), new Vector2(px0 + 0.1f, 1.2f), new Color(0.25f, 0.35f, 1f, 0.35f));
        CreateGlow(stage.transform, "SideGlowR", new Vector2(px1 - 0.1f, -0.2f), new Vector2(px1 + 0.25f, 1.2f), new Color(1f, 0.25f, 0.8f, 0.35f));

        // Door rectangle inside the photo (UV), and its position on screen.
        Rect door = new Rect(0.387f, 0.185f, 0.231f, 0.618f);
        Vector2 ToScreen(float u, float v) => new Vector2(px0 + u * photoW, v);
        var dMin = ToScreen(door.xMin, door.yMin);
        var dMax = ToScreen(door.xMax, door.yMax);

        // The photo, split around the door so the doorway can open onto the live club.
        PhotoPiece(stage.transform, "PhotoLeft", tex, new Rect(0f, 0f, door.xMin, 1f), ToScreen(0f, 0f), ToScreen(door.xMin, 1f));
        PhotoPiece(stage.transform, "PhotoRight", tex, new Rect(door.xMax, 0f, 1f - door.xMax, 1f), ToScreen(door.xMax, 0f), ToScreen(1f, 1f));
        PhotoPiece(stage.transform, "PhotoTop", tex, new Rect(door.xMin, door.yMax, door.width, 1f - door.yMax), ToScreen(door.xMin, door.yMax), ToScreen(door.xMax, 1f));
        PhotoPiece(stage.transform, "PhotoBottom", tex, new Rect(door.xMin, 0f, door.width, door.yMin), ToScreen(door.xMin, 0f), ToScreen(door.xMax, door.yMin));

        // Dark doorway behind the door (fades to reveal the club), then the door leaf, hinged right.
        var doorway = CreatePanel(stage.transform, "Doorway", dMin, dMax, new Color(0.03f, 0.01f, 0.04f, 1f));
        var doorwayImg = doorway.GetComponent<Image>();
        var leaf = CreateDoorLeaf(stage.transform, "DoorLeaf", tex, door, dMin, dMax, new Vector2(1f, 0.5f), Color.white);
        var leafRt = leaf.GetComponent<RectTransform>();
        var leafImg = leaf.GetComponent<RawImage>();

        // Neon frame glow pulsing with the bass, and light leaking under the door.
        float fw = (dMax.x - dMin.x);
        var frameGlow = CreateGlow(stage.transform, "FrameGlow", new Vector2(dMin.x - fw * 0.35f, dMin.y - 0.06f), new Vector2(dMax.x + fw * 0.35f, dMax.y + 0.06f), new Color(neon.r, neon.g, neon.b, 0.25f)).GetComponent<Image>();
        var underGlow = CreateGlow(stage.transform, "UnderDoor", new Vector2(dMin.x - fw * 0.2f, dMin.y - 0.05f), new Vector2(dMax.x + fw * 0.2f, dMin.y + 0.035f), new Color(0.55f, 0.3f, 1f, 0.5f)).GetComponent<Image>();

        // Neon sign above the door.
        var signGo = new GameObject("NeonSign");
        signGo.transform.SetParent(stage.transform, false);
        var signRt = signGo.AddComponent<RectTransform>();
        signRt.anchorMin = new Vector2(dMin.x - fw * 0.9f, dMax.y + 0.03f);
        signRt.anchorMax = new Vector2(dMax.x + fw * 0.9f, dMax.y + 0.13f);
        signRt.offsetMin = signRt.offsetMax = Vector2.zero;
        var sign = signGo.AddComponent<TextMeshProUGUI>();
        sign.text = "NEON BEAT VAULT";
        sign.fontSize = 46f;
        sign.fontStyle = FontStyles.Bold;
        sign.characterSpacing = 14f;
        sign.alignment = TextAlignmentOptions.Center;
        sign.enableAutoSizing = true;
        sign.fontSizeMin = 18f;
        sign.fontSizeMax = 46f;
        sign.raycastTarget = false;
        var skin = HudSkin.Get();
        if (skin != null && skin.font != null) sign.font = skin.font;
        var signGlow = CreateGlow(stage.transform, "SignGlow", new Vector2(signRt.anchorMin.x - 0.03f, signRt.anchorMin.y - 0.04f), new Vector2(signRt.anchorMax.x + 0.03f, signRt.anchorMax.y + 0.04f), new Color(neon.r, neon.g, neon.b, 0.3f)).GetComponent<Image>();
        signGo.transform.SetAsLastSibling();

        // Keypad on the wall beside the handle side.
        var kx = dMin.x - fw * 0.28f;
        CreatePanel(stage.transform, "Keypad", new Vector2(kx - 0.012f, 0.42f), new Vector2(kx + 0.012f, 0.5f), new Color(0.06f, 0.06f, 0.08f, 1f));
        CreatePanel(stage.transform, "KeypadFace", new Vector2(kx - 0.009f, 0.43f), new Vector2(kx + 0.009f, 0.475f), new Color(0.14f, 0.15f, 0.18f, 1f));
        var lockLight = CreatePanel(stage.transform, "LockLight", new Vector2(kx - 0.006f, 0.481f), new Vector2(kx + 0.006f, 0.493f), new Color(1f, 0.15f, 0.1f, 1f)).GetComponent<Image>();

        const float bpm = 124f;
        float beatLen = 60f / bpm;
        float start = Time.unscaledTime;
        void Thump(float t)
        {
            // Bass from behind the door: the neon swells and the sign flickers on early on (the scene stays still).
            float phase = Mathf.Repeat(t / beatLen, 1f);
            float kick = Mathf.Exp(-phase * 7f);
            frameGlow.color = new Color(neon.r, neon.g, neon.b, 0.18f + kick * 0.35f);
            underGlow.color = new Color(0.55f, 0.3f, 1f, 0.3f + kick * 0.5f);
            bool flicker = t < 1.2f && Mathf.PerlinNoise(t * 9f, 0.3f) < 0.45f;
            float signA = flicker ? 0.25f : 1f;
            sign.color = new Color(1f, 0.55f, 0.95f, signA);
            signGlow.color = new Color(neon.r, neon.g, neon.b, (flicker ? 0.05f : 0.28f) + kick * 0.12f);
        }

        // Title card over the thumping door.
        var title = CreateTitleCard(root.transform, "LEVEL 3", "NEON BEAT VAULT");
        var titleGraphics = title.GetComponentsInChildren<Graphic>();
        PlayCutsceneAudio(LevelCutsceneType.ClubDiscoBall, 1);
        for (float t = 0f; t < 2.1f; t += Time.unscaledDeltaTime)
        {
            Thump(Time.unscaledTime - start);
            float a = t < 0.4f ? t / 0.4f : t > 1.85f ? 1f - (t - 1.85f) / 0.25f : 1f;
            foreach (var g in titleGraphics) { var c = g.color; c.a = Mathf.Clamp01(a); g.color = c; }
            yield return null;
        }
        Object.Destroy(title);

        // Unlock: keypad goes green, the frame flares and the door jolts.
        lockLight.color = new Color(0.25f, 1f, 0.45f, 1f);
        PlayCutsceneAudio(LevelCutsceneType.ClubDiscoBall, 2);
        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            float k = t / 0.35f;
            Thump(Time.unscaledTime - start);
            frameGlow.color = new Color(neon.r, neon.g, neon.b, Mathf.Lerp(0.5f, 0.9f, k));
            leafRt.anchoredPosition = new Vector2(Mathf.Sin(t * 90f) * 3f * (1f - k), 0f);
            yield return null;
        }
        leafRt.anchoredPosition = Vector2.zero;

        // Open: reveal the live club behind the doorway with a magenta flash.
        var blocker = root.transform.Find("Blocker");
        if (blocker != null) blocker.gameObject.SetActive(false);
        var flash = CreatePanel(stage.transform, "OpenFlash", new Vector2(dMax.x, dMin.y), new Vector2(dMax.x, dMax.y), new Color(1f, 0.4f, 0.9f, 0.6f));
        flash.transform.SetSiblingIndex(leaf.transform.GetSiblingIndex());
        var flashRt = flash.GetComponent<RectTransform>();
        var flashImg = flash.GetComponent<Image>();
        const float swing = 0.9f;
        for (float t = 0f; t < swing; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / swing);
            Thump(Time.unscaledTime - start);
            leafRt.localScale = new Vector3(Mathf.Lerp(1f, 0.1f, k), 1f, 1f);
            leafImg.color = Color.Lerp(Color.white, new Color(0.35f, 0.25f, 0.4f, 1f), k);
            doorwayImg.color = new Color(0.03f, 0.01f, 0.04f, 1f - k);
            flashRt.anchorMin = new Vector2(Mathf.Lerp(dMax.x, dMin.x, k), dMin.y);
            flashImg.color = new Color(1f, 0.4f, 0.9f, 0.6f * (1f - k));
            yield return null;
        }
        Object.Destroy(flash);

        // Push through the doorway into the club.
        var graphics = stage.GetComponentsInChildren<Graphic>();
        var startA = new float[graphics.Length];
        for (int i = 0; i < graphics.Length; i++) startA[i] = graphics[i].color.a;
        stageRt.pivot = new Vector2((dMin.x + dMax.x) * 0.5f, (dMin.y + dMax.y) * 0.5f);
        stageRt.anchoredPosition = Vector2.zero;
        const float push = 0.6f;
        for (float t = 0f; t < push; t += Time.unscaledDeltaTime)
        {
            float k = t / push;
            stageRt.localScale = Vector3.one * Mathf.Lerp(1f, 2.6f, k * k);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null) continue;
                var c = graphics[i].color;
                c.a = Mathf.Lerp(startA[i], 0f, k);
                graphics[i].color = c;
            }
            yield return null;
        }
    }

    static GameObject PhotoPiece(Transform parent, string name, Texture2D tex, Rect uv, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var raw = go.AddComponent<RawImage>();
        raw.texture = tex;
        raw.uvRect = uv;
        raw.raycastTarget = false;
        return go;
    }

    static IEnumerator PlayTextureDoorCutscene(GameObject root, string textureResource, string bannerText,
        LevelCutsceneType audioType)
    {
        var doorTex = Resources.Load<Texture2D>(textureResource);

        var frame = CreateAspectFitFrame(root.transform, "DoorFrame", doorTex);

        var left = CreateDoorPanel(frame.transform, "DoorLeft", doorTex, leftHalf: true);
        var right = CreateDoorPanel(frame.transform, "DoorRight", doorTex, leftHalf: false);

        var leftRt = left.GetComponent<RectTransform>();
        var rightRt = right.GetComponent<RectTransform>();

        leftRt.anchorMin = new Vector2(0f, 0f);
        leftRt.anchorMax = new Vector2(0.5f, 1f);
        rightRt.anchorMin = new Vector2(0.5f, 0f);
        rightRt.anchorMax = new Vector2(1f, 1f);
        leftRt.offsetMin = leftRt.offsetMax = Vector2.zero;
        rightRt.offsetMin = rightRt.offsetMax = Vector2.zero;

        var title = CreateBannerTitle(root.transform, bannerText);
        yield return FadeInGraphic(title.GetComponentsInChildren<Graphic>(), 0.4f);
        PlayCutsceneAudio(audioType, 1);
        yield return new WaitForSecondsRealtime(1.2f);

        PlayCutsceneAudio(audioType, 2);
        yield return FadeOutGraphics(title.GetComponentsInChildren<Graphic>(), 0.25f);

        yield return AnimateAnchors(leftRt, new Vector2(0f, 0f), new Vector2(-0.5f, 0f), 0.85f);
        yield return AnimateAnchors(rightRt, new Vector2(0.5f, 0f), new Vector2(1f, 0f), 0.85f);
        rightRt.anchorMax = new Vector2(1.5f, 1f);
        yield return FadeOutAllGraphics(root, 0.35f);
    }

    static GameObject CreateAspectFitFrame(Transform parent, string name, Texture2D tex)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        ApplyAspectFit(rt, tex);
        return go;
    }

    static void ApplyAspectFit(RectTransform rt, Texture2D tex)
    {
        if (tex == null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return;
        }

        float texAspect = (float)tex.width / tex.height;
        float screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);

        if (texAspect >= screenAspect)
        {
            float heightFrac = screenAspect / texAspect;
            float y = (1f - heightFrac) * 0.5f;
            rt.anchorMin = new Vector2(0f, y);
            rt.anchorMax = new Vector2(1f, y + heightFrac);
        }
        else
        {
            float widthFrac = texAspect / screenAspect;
            float x = (1f - widthFrac) * 0.5f;
            rt.anchorMin = new Vector2(x, 0f);
            rt.anchorMax = new Vector2(x + widthFrac, 1f);
        }

        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static GameObject CreateDoorPanel(Transform parent, string name, Texture2D tex, bool leftHalf)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        if (tex != null)
        {
            var raw = go.AddComponent<RawImage>();
            raw.texture = tex;
            raw.uvRect = leftHalf
                ? new Rect(0f, 0f, 0.5f, 1f)
                : new Rect(0.5f, 0f, 0.5f, 1f);
            raw.raycastTarget = false;
        }
        else
        {
            var img = go.AddComponent<Image>();
            img.color = new Color(0.9f, 0.9f, 0.88f, 1f);
            img.raycastTarget = false;
        }

        return go;
    }

    static Sprite _radial;

    /// <summary>Soft round falloff sprite used for realistic light glows.</summary>
    static Sprite RadialSprite()
    {
        if (_radial != null) return _radial;
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                float a = Mathf.Pow(1f - d, 2.2f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        _radial = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _radial;
    }

    static GameObject CreateGlow(Transform parent, string name, Vector2 min, Vector2 max, Color color)
    {
        var go = CreatePanel(parent, name, min, max, color);
        go.GetComponent<Image>().sprite = RadialSprite();
        return go;
    }

    /// <summary>Brass wall sconce with a frosted glass shade and a soft warm-white glow on the wall.</summary>
    static void CreateSconce(Transform parent, float x, float y)
    {
        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        float rx = 0.07f, ry = rx * aspect;
        var brass = new Color(0.62f, 0.5f, 0.3f, 1f);
        CreateGlow(parent, "SconceWash", new Vector2(x - rx * 1.6f, y - ry * 1.4f), new Vector2(x + rx * 1.6f, y + ry * 1.8f), new Color(1f, 0.93f, 0.8f, 0.22f));
        CreateGlow(parent, "SconceGlow", new Vector2(x - rx * 0.6f, y - ry * 0.45f), new Vector2(x + rx * 0.6f, y + ry * 0.75f), new Color(1f, 0.95f, 0.85f, 0.45f));
        CreatePanel(parent, "SconcePlate", new Vector2(x - 0.006f, y - 0.05f), new Vector2(x + 0.006f, y + 0.01f), brass);
        CreatePanel(parent, "SconceArm", new Vector2(x - 0.002f, y - 0.03f), new Vector2(x + 0.002f, y + 0.02f), brass * 0.85f);
        CreatePanel(parent, "SconceCup", new Vector2(x - 0.01f, y + 0.012f), new Vector2(x + 0.01f, y + 0.02f), brass);
        CreatePanel(parent, "SconceShade", new Vector2(x - 0.012f, y + 0.02f), new Vector2(x + 0.012f, y + 0.07f), new Color(0.97f, 0.94f, 0.87f, 0.92f));
        CreateGlow(parent, "SconceBulb", new Vector2(x - 0.01f, y + 0.025f), new Vector2(x + 0.01f, y + 0.065f), new Color(1f, 1f, 0.97f, 0.9f));
        CreatePanel(parent, "SconceRim", new Vector2(x - 0.013f, y + 0.068f), new Vector2(x + 0.013f, y + 0.073f), brass);
    }

    /// <summary>Left, right and top strips of a frame around an opening (open at the bottom/floor).</summary>
    static void CreateBorder(Transform parent, string name, Vector2 outerMin, Vector2 outerMax, float innerLeft, float innerRight, float innerTop, Color color)
    {
        CreatePanel(parent, name, outerMin, new Vector2(innerLeft, outerMax.y), color);
        CreatePanel(parent, name, new Vector2(innerRight, outerMin.y), outerMax, color);
        CreatePanel(parent, name, new Vector2(innerLeft, innerTop), new Vector2(innerRight, outerMax.y), color);
    }

    static void SetAnchors(GameObject go, Vector2 min, Vector2 max)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static GameObject CreatePanel(Transform parent, string name, Vector2 min, Vector2 max, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return go;
    }

    static void AddStripe(Transform door, Color stripeColor)
    {
        var stripe = CreatePanel(door, "WarningStripe",
            new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.55f), stripeColor);
        stripe.transform.SetAsLastSibling();
    }

    static IEnumerator FadeOutAllGraphics(GameObject root, float duration)
    {
        var graphics = root.GetComponentsInChildren<Graphic>();
        float t = 0f;
        var startAlphas = new float[graphics.Length];
        for (int i = 0; i < graphics.Length; i++)
            startAlphas[i] = graphics[i].color.a;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / duration;
            for (int i = 0; i < graphics.Length; i++)
            {
                var c = graphics[i].color;
                c.a = Mathf.Lerp(startAlphas[i], 0f, p);
                graphics[i].color = c;
            }
            yield return null;
        }
    }

    static IEnumerator AnimateAnchors(RectTransform rt, Vector2 fromMin, Vector2 toMin, float duration)
    {
        Vector2 fromMax = rt.anchorMax;
        Vector2 toMax = new Vector2(toMin.x + (fromMax.x - fromMin.x), toMin.y + (fromMax.y - fromMin.y));
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / duration);
            rt.anchorMin = Vector2.Lerp(fromMin, toMin, p);
            rt.anchorMax = Vector2.Lerp(fromMax, toMax, p);
            yield return null;
        }
        rt.anchorMin = toMin;
        rt.anchorMax = toMax;
    }

    static IEnumerator FadeOut(GameObject root, float duration)
    {
        var images = root.GetComponentsInChildren<Image>();
        float t = 0f;
        var startAlphas = new float[images.Length];
        for (int i = 0; i < images.Length; i++)
            startAlphas[i] = images[i].color.a;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / duration;
            for (int i = 0; i < images.Length; i++)
            {
                var c = images[i].color;
                c.a = Mathf.Lerp(startAlphas[i], 0f, p);
                images[i].color = c;
            }
            yield return null;
        }
    }

    static void PlayCutsceneAudio(LevelCutsceneType type, int phase)
    {
        AudioManager.Instance?.PlayCutsceneStinger(type, phase);
    }
}
