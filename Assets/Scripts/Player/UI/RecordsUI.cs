using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime-built record screens in the Sci-Fi HUD skin: the one-time player name prompt and the
/// Hall of Fame (this campaign's level-by-level scores plus the top-10 finished campaigns).
/// </summary>
public static class RecordsUI
{
    static readonly Color Cyan = new Color(0.35f, 0.85f, 1f, 1f);
    static readonly Color Gold = new Color(1f, 0.84f, 0.35f, 1f);
    static readonly Color Dim = new Color(0.72f, 0.8f, 0.9f, 1f);

    // ── Name prompt ──────────────────────────────────────────────────────────

    public static void ShowNamePrompt(Action onDone)
    {
        var root = Overlay("NamePrompt", 400);
        var panel = Panel(root.transform, new Vector2(0.3f, 0.32f), new Vector2(0.7f, 0.7f));
        Label(panel, "Title", "ENTER YOUR NAME", new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.92f), 40, Cyan);
        Label(panel, "Sub", "Your scores will be saved under this name", new Vector2(0.06f, 0.6f), new Vector2(0.94f, 0.72f), 20, Dim);

        var inputGo = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
        inputGo.name = "NameInput";
        inputGo.transform.SetParent(panel, false);
        var irt = inputGo.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.15f, 0.36f);
        irt.anchorMax = new Vector2(0.85f, 0.54f);
        irt.offsetMin = irt.offsetMax = Vector2.zero;
        inputGo.GetComponent<Image>().color = new Color(0.03f, 0.06f, 0.12f, 0.95f);
        var input = inputGo.GetComponent<TMP_InputField>();
        input.characterLimit = 14;
        // The HUD font only has capitals, so type in capitals (lowercase letters were invisible).
        input.onValidateInput = (text, index, c) =>
            char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_' ? char.ToUpperInvariant(c) : '\0';
        input.text = PlayerRecords.HasPlayerName ? PlayerRecords.PlayerName : "";
        var skin = HudSkin.Get();
        foreach (var t in inputGo.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (skin != null && skin.font != null) t.font = skin.font;
            t.fontSize = 32;
            t.alignment = TextAlignmentOptions.Center;
            t.color = t.gameObject.name.Contains("Placeholder") ? new Color(0.5f, 0.6f, 0.7f, 0.6f) : Color.white;
            if (t.gameObject.name.Contains("Placeholder")) t.text = "TYPE YOUR NAME";
        }

        var save = Button(panel, "SAVE", new Vector2(0.3f, 0.08f), new Vector2(0.7f, 0.26f));
        void Submit()
        {
            if (string.IsNullOrWhiteSpace(input.text)) { input.ActivateInputField(); return; }
            PlayerRecords.SetPlayerName(input.text);
            UnityEngine.Object.Destroy(root);
            onDone?.Invoke();
        }
        save.onClick.AddListener(Submit);
        input.onSubmit.AddListener(_ => Submit());
        input.ActivateInputField();
    }

    // ── Level results panel ──────────────────────────────────────────────────

    /// <summary>
    /// Results panel beside the Victory menu (the scene has no score text of its own).
    /// Returns the body text so the caller can refresh it (e.g. after the name is entered).
    /// </summary>
    public static TextMeshProUGUI ShowResultsPanel(string title, Action onChangeName = null)
    {
        var go = new GameObject("LevelResults", typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;   // above the narration overlay so CHANGE NAME is clickable
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var panel = Panel(go.transform, new Vector2(0.685f, 0.1f), new Vector2(0.975f, 0.9f));
        Label(panel, "Title", title, new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.97f), 34, Cyan);
        var body = Label(panel, "Body", "", new Vector2(0.08f, onChangeName != null ? 0.13f : 0.05f), new Vector2(0.92f, 0.86f), 24, Color.white);
        body.alignment = TextAlignmentOptions.Top;
        if (onChangeName != null)
        {
            go.AddComponent<GraphicRaycaster>();
            var change = Button(panel, "CHANGE NAME", new Vector2(0.25f, 0.03f), new Vector2(0.75f, 0.11f));
            change.onClick.AddListener(() => onChangeName());
            change.gameObject.AddComponent<ButtonSoundPlayer>();
        }
        return body;
    }

    // ── Hall of Fame ─────────────────────────────────────────────────────────

    public static void ShowHallOfFame(Action onClose)
    {
        var root = Overlay("HallOfFame", 300);
        var panel = Panel(root.transform, new Vector2(0.15f, 0.06f), new Vector2(0.85f, 0.95f));
        var campaign = PlayerRecords.LastCampaign;
        Label(panel, "Title", "HALL OF FAME", new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.97f), 48, Gold);
        string sub = PlayerRecords.LastCompletedCampaign
            ? $"ALL LEVELS CLEARED  -  {PlayerRecords.PlayerName}"
            : $"FINAL SCORES  -  {PlayerRecords.PlayerName}";
        Label(panel, "Sub", sub, new Vector2(0.05f, 0.83f), new Vector2(0.95f, 0.885f), 22, Cyan);

        // This campaign, level by level.
        var sb = new StringBuilder();
        sb.Append(Row("<b>LEVEL</b>", "<b>SCORE</b>", "<b>STARS</b>", "<b>NOTES</b>", "<b>TIME</b>")).Append('\n');
        int total = 0, stars = 0, notes = 0; float time = 0f;
        for (int l = 1; l <= LevelProgress.MaxLevel; l++)
        {
            var r = PlayerRecords.Latest(l);
            if (r == null) { sb.Append(Row($"{l}  -", "-", "-", "-", "-")).Append('\n'); continue; }
            sb.Append(Row($"{l}  {r.levelName.ToUpperInvariant()}", r.score.ToString("N0"), $"{r.stars}/3", r.notes.ToString(), PlayerRecords.FormatTime(r.timeSeconds))).Append('\n');
            total += r.score; stars += r.stars; notes += r.notes; time += r.timeSeconds;
        }
        sb.Append(Row("<color=#FFD65A><b>TOTAL</b></color>", $"<color=#FFD65A><b>{total:N0}</b></color>", $"{stars}/9", notes.ToString(), PlayerRecords.FormatTime(time)));
        var table = Label(panel, "Campaign", sb.ToString(), new Vector2(0.06f, 0.55f), new Vector2(0.94f, 0.82f), 24, Color.white);
        table.alignment = TextAlignmentOptions.TopLeft;

        int rank = PlayerRecords.RankOf(campaign);
        string rankLine = campaign == null
                            ? (PlayerRecords.LastCompletedCampaign ? "Campaign complete - not in the top 10 this time" : "Clear all three levels to enter the Hall of Fame")
                        : rank == 1 ? "NEW HIGH SCORE  -  RANK #1!"
                        : rank > 0 ? $"RANK #{rank} OF {PlayerRecords.HallOfFame.Count}" : "";
        Label(panel, "Rank", rankLine, new Vector2(0.05f, 0.49f), new Vector2(0.95f, 0.55f), 26, rank == 1 ? Gold : Cyan);

        // Top 10 campaigns.
        var top = new StringBuilder();
        top.Append(TopRow("<b>#</b>", "<b>NAME</b>", "<b>SCORE</b>", "<b>STARS</b>", "<b>DATE</b>")).Append('\n');
        var hof = PlayerRecords.HallOfFame;
        if (hof.Count == 0) top.Append("<color=#8899AA>No finished campaigns yet</color>");
        for (int i = 0; i < hof.Count; i++)
        {
            var c = hof[i];
            string date = c.date != null && c.date.Length >= 10 ? c.date.Substring(0, 10) : "";
            string line = TopRow((i + 1).ToString(), c.playerName, c.totalScore.ToString("N0"), $"{c.totalStars}/9", date);
            top.Append(c == campaign ? $"<color=#FFD65A>{line}</color>" : line).Append('\n');
        }
        var topText = Label(panel, "Top10", top.ToString(), new Vector2(0.06f, 0.14f), new Vector2(0.94f, 0.48f), 20, Dim);
        topText.alignment = TextAlignmentOptions.TopLeft;

        var close = Button(panel, "CONTINUE", new Vector2(0.36f, 0.03f), new Vector2(0.64f, 0.11f));
        close.onClick.AddListener(() => { UnityEngine.Object.Destroy(root); onClose?.Invoke(); });
    }

    static string Row(string a, string b, string c, string d, string e)
        => $"{a}<pos=46%>{b}<pos=63%>{c}<pos=76%>{d}<pos=88%>{e}";

    static string TopRow(string a, string b, string c, string d, string e)
        => $"{a}<pos=7%>{b}<pos=46%>{c}<pos=63%>{d}<pos=78%>{e}";

    // ── Building blocks ──────────────────────────────────────────────────────

    static GameObject Overlay(string name, int order)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        var dim = new GameObject("Dim", typeof(RectTransform)).AddComponent<Image>();
        dim.transform.SetParent(go.transform, false);
        Stretch(dim.rectTransform, Vector2.zero, Vector2.one);
        dim.color = new Color(0f, 0f, 0.02f, 0.72f);
        return go;
    }

    static Transform Panel(Transform parent, Vector2 min, Vector2 max)
    {
        var img = new GameObject("Panel", typeof(RectTransform)).AddComponent<Image>();
        img.transform.SetParent(parent, false);
        Stretch(img.rectTransform, min, max);
        var skin = HudSkin.Get();
        if (skin != null && skin.window != null)
        {
            img.sprite = skin.window;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 2f;
            img.color = new Color(1f, 1f, 1f, 0.97f);
        }
        else img.color = new Color(0.02f, 0.04f, 0.09f, 0.95f);
        return img.transform;
    }

    static TextMeshProUGUI Label(Transform parent, string name, string text, Vector2 min, Vector2 max, float size, Color color)
    {
        var tmp = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        tmp.transform.SetParent(parent, false);
        Stretch(tmp.rectTransform, min, max);
        var skin = HudSkin.Get();
        if (skin != null && skin.font != null) tmp.font = skin.font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 12f;
        tmp.fontSizeMax = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.richText = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    static Button Button(Transform parent, string text, Vector2 min, Vector2 max)
    {
        var img = new GameObject(text + "Button", typeof(RectTransform)).AddComponent<Image>();
        img.transform.SetParent(parent, false);
        Stretch(img.rectTransform, min, max);
        var skin = HudSkin.Get();
        if (skin != null && skin.banner != null)
        {
            img.sprite = skin.banner;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 4f;
        }
        else img.color = new Color(0.1f, 0.3f, 0.45f, 0.95f);
        var btn = img.gameObject.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.8f, 0.95f, 1f);
        btn.colors = colors;
        var label = Label(img.transform, "Label", text, Vector2.zero, Vector2.one, 28, Color.white);
        label.raycastTarget = false;
        return btn;
    }

    static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
