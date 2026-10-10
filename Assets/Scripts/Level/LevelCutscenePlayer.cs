using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full-screen UI cutscenes per level theme:
/// L1 Opera — red curtains part
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
        var glow = CreatePanel(root.transform, "SunsetGlow", Vector2.zero, Vector2.one,
            new Color(1f, 0.55f, 0.28f, 1f));
        glow.transform.SetAsLastSibling();

        var left  = CreateSteelDoor(root.transform, "DoorLeft",  handleOnRight: true);
        var right = CreateSteelDoor(root.transform, "DoorRight", handleOnRight: false);
        var leftRt  = left.GetComponent<RectTransform>();
        var rightRt = right.GetComponent<RectTransform>();
        leftRt.anchorMin  = new Vector2(0f, 0f);   leftRt.anchorMax  = new Vector2(0.5f, 1f);
        rightRt.anchorMin = new Vector2(0.5f, 0f); rightRt.anchorMax = new Vector2(1f, 1f);

        var title = CreateTitleCard(root.transform, "LEVEL 1", "ROOFTOP");
        yield return FadeInGraphic(title.GetComponentsInChildren<Graphic>(), 0.35f);
        yield return new WaitForSecondsRealtime(1.1f);
        yield return FadeOutGraphics(title.GetComponentsInChildren<Graphic>(), 0.25f);

        yield return AnimateAnchors(leftRt,  new Vector2(0f, 0f),   new Vector2(-0.5f, 0f), 0.8f);
        yield return AnimateAnchors(rightRt, new Vector2(0.5f, 0f), new Vector2(1f, 0f),    0.8f);
        yield return FadeOut(root, 0.4f);
    }

    static GameObject CreateSteelDoor(Transform parent, string name, bool handleOnRight)
    {
        var door = CreatePanel(parent, name, Vector2.zero, Vector2.one, new Color(0.42f, 0.44f, 0.47f, 1f));

        CreatePanel(door.transform, "Edge", new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Color(0.28f, 0.29f, 0.31f, 1f));
        CreatePanel(door.transform, "Face", new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.98f),
            new Color(0.5f, 0.52f, 0.55f, 1f));
        CreatePanel(door.transform, "PanelTop", new Vector2(0.12f, 0.56f), new Vector2(0.88f, 0.9f),
            new Color(0.45f, 0.47f, 0.5f, 1f));
        CreatePanel(door.transform, "PanelBottom", new Vector2(0.12f, 0.1f), new Vector2(0.88f, 0.44f),
            new Color(0.45f, 0.47f, 0.5f, 1f));

        float hx = handleOnRight ? 0.84f : 0.08f;
        CreatePanel(door.transform, "Handle", new Vector2(hx, 0.46f), new Vector2(hx + 0.08f, 0.54f),
            new Color(0.2f, 0.21f, 0.22f, 1f));
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

        // Semi-transparent dark band behind the text
        var bg = card.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);
        bg.raycastTarget = false;

        AddTitleText(card.transform, levelLine,   fontSize: 80, yAnchorMin: 0.52f, yAnchorMax: 0.95f,
                     color: new Color(1f, 0.92f, 0.55f, 1f));  // gold
        AddTitleText(card.transform, subtitleLine, fontSize: 44, yAnchorMin: 0.05f, yAnchorMax: 0.48f,
                     color: new Color(1f, 1f, 1f, 1f));
        return card;
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
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = FontStyles.Bold;
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
