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
        yield return PlayTextureDoorCutscene(root, "museumdoor", "MUSEUM READY LEVEL 2",
            LevelCutsceneType.MuseumSecurityDoor);
    }

    static IEnumerator PlayClubDiscoBall(GameObject root)
    {
        yield return PlayTextureDoorCutscene(root, "undergroundoor", "UNDERGROUND READY LEVEL 3",
            LevelCutsceneType.ClubDiscoBall);
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
