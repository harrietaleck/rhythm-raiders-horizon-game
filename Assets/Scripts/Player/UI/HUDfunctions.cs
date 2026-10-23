using UnityEngine;
using TMPro;

/// <summary>
/// In-game HUD. Shows Vibe, Artifacts, Guard distance, active boost timer,
/// running score, and current level.
/// All text fields auto-create themselves if not assigned in the Inspector.
/// </summary>
public class HUDfunctions : MonoBehaviour
{
    [Header("Core HUD (assign in Inspector or auto-created)")]
    public TextMeshProUGUI healthText;       // Vibe
    public TextMeshProUGUI artifactText;     // Artifacts
    // sneakText removed — boostText already shows "SNEAK Xs" with countdown

    [Header("New HUD elements")]
    public TextMeshProUGUI guardDistText;    // Guard distance
    public TextMeshProUGUI boostText;        // Active boost + timer
    public TextMeshProUGUI scoreText;        // Running money total
    public TextMeshProUGUI levelText;        // Current level

    [Header("Legacy")]
    public TextMeshProUGUI hitText;          // Shield (hitCounter)

    [SerializeField] private int hudFontSize = 46;

    PickupBase  _pickup;
    EnemyBase   _enemy;
    Transform   _playerTransform;
    int         _maxShield = 10;   // cached from LevelBootstrap at Start

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        EnsureFont(healthText);
        EnsureFont(artifactText);
        EnsureFont(hitText);
        EnsureFont(guardDistText);
        EnsureFont(boostText);
        EnsureFont(scoreText);
        EnsureFont(levelText);
    }

    private void Start()
    {
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            _pickup          = playerGO.GetComponent<PickupBase>();
            _playerTransform = playerGO.transform;
        }

        _enemy     = Object.FindAnyObjectByType<EnemyBase>();
        _maxShield = _pickup != null ? _pickup.hitCounter : 10;   // snapshot from LevelBootstrap

        // Auto-create any text fields that weren't wired in the Inspector
        // HUDfunctions lives on the Player (not inside Canvas), so search the scene for the gameplay Canvas.
        var canvas = GetComponentInParent<Canvas>()
                  ?? GameObject.Find("Canvas")?.GetComponent<Canvas>()
                  ?? Object.FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            // ── Left side: place below the existing artifactText using the same
            //    local coordinate system (centre-anchor) so positions are canvas-size agnostic.
            var refRT     = artifactText?.rectTransform ?? healthText?.rectTransform;
            var refLP     = refRT != null ? (Vector2)refRT.localPosition : new Vector2(-647f, 229f);
            // Step down 90 units per row (font 46 + padding)
            var guardLP   = new Vector2(refLP.x, refLP.y - 90f);
            var boostLP   = new Vector2(refLP.x, refLP.y - 180f);

            guardDistText = guardDistText ?? CreateHudTextAtLocalPos(canvas.transform, "GuardDistText",
                guardLP, new Vector2(340f, 54f));
            boostText     = boostText     ?? CreateHudTextAtLocalPos(canvas.transform, "BoostText",
                boostLP, new Vector2(380f, 54f));

            // ── Right side: anchor to top-right corner (works correctly for that side)
            scoreText = scoreText ?? CreateHudText(canvas.transform, "ScoreText",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10f, -10f), new Vector2(260f, 50f));

            levelText = levelText ?? CreateHudText(canvas.transform, "LevelText",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10f, -70f), new Vector2(260f, 50f));
        }

        // Style existing fields
        ApplyStyle(healthText,   new Color(0.35f, 1f,    0.35f, 1f));   // green — vibe
        ApplyStyle(artifactText, new Color(1f,    0.85f, 0.1f,  1f));   // gold  — artifacts
        ApplyStyle(hitText,      new Color(0.4f,  0.75f, 1f,   1f));   // blue  — shield

        // Style new fields
        ApplyStyle(guardDistText, new Color(1f,   0.4f, 0.4f, 1f));    // red-ish — danger
        ApplyStyle(boostText,     new Color(1f,   0.9f, 0.3f, 1f));    // yellow  — power-up
        ApplyStyle(scoreText,     new Color(0.9f, 0.9f, 0.9f, 1f));    // white   — score
        ApplyStyle(levelText,     new Color(0.7f, 0.7f, 1f,   1f));    // soft blue — level

        CompactLayout();
    }

    [Header("Layout")]
    [Tooltip("Size of the stats panel (top-left). Smaller keeps the track in view.")]
    [Range(0.4f, 1f)] public float panelScale = 0.68f;

    /// <summary>
    /// Keeps the gameplay HUD out of the way: the stats panel shrinks into the top-left corner
    /// and goes slightly see-through, and the top-right panel is hidden — its level number and
    /// "$" total were redundant (each level's own banner shows score, notes and distance).
    /// </summary>
    void CompactLayout()
    {
        var right = GameObject.Find("HUD_RightPanel");
        if (right != null) right.SetActive(false);
        if (scoreText != null && (right == null || !scoreText.transform.IsChildOf(right.transform))) scoreText.gameObject.SetActive(false);
        if (levelText != null && (right == null || !levelText.transform.IsChildOf(right.transform))) levelText.gameObject.SetActive(false);

        var left = GameObject.Find("HUD_LeftPanel");
        if (left == null) return;
        var rt = left.GetComponent<RectTransform>();
        if (rt != null)
        {
            // Pivot on the top-left corner so shrinking keeps it tucked in the corner.
            var size = rt.rect.size;
            var shift = new Vector2(0f - rt.pivot.x, 1f - rt.pivot.y);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition += new Vector2(shift.x * size.x, shift.y * size.y);
            rt.localScale = Vector3.one * panelScale;
        }
        var bg = left.GetComponent<UnityEngine.UI.Image>();
        if (bg != null) { var c = bg.color; c.a = Mathf.Min(c.a, 0.8f); bg.color = c; }
    }

    // ── Update ────────────────────────────────────────────────────────────────
    private void Update()
    {
        if (_pickup == null) return;

        // Re-find enemy if it disappeared and re-spawned
        if (_enemy == null) _enemy = Object.FindAnyObjectByType<EnemyBase>();

        // ── Vibe ──────────────────────────────────────────────────────────────
        TmpUiUtility.SetSafeText(healthText,
            $"VIBE {_pickup.currentHealth}");

        // ── Artifacts ─────────────────────────────────────────────────────────
        TmpUiUtility.SetSafeText(artifactText,
            $"ARTIFACTS {_pickup.artifactAmount}/{PickupBase.ArtifactsToWin}");

        // ── Shield (hitCounter) ───────────────────────────────────────────────
        int shield          = _pickup.hitCounter;
        int dangerThreshold = Mathf.Max(1, Mathf.CeilToInt(_maxShield * 0.3f));
        if (hitText != null)
        {
            TmpUiUtility.SetSafeText(hitText, $"SHIELD {shield}");
            hitText.color = shield <= dangerThreshold
                ? Color.Lerp(Color.red, new Color(0.4f, 0.75f, 1f, 1f),
                             Mathf.PingPong(Time.time * 3f, 1f))
                : new Color(0.4f, 0.75f, 1f, 1f);
        }

        // ── Guard distance ────────────────────────────────────────────────────
        if (guardDistText != null)
        {
            if (_enemy != null && _playerTransform != null)
            {
                float dist = Mathf.Max(0f,
                    _playerTransform.position.z - _enemy.transform.position.z);

                bool surging = _enemy.IsSurging;
                string danger = surging      ? $" SURGE {_enemy.SurgeTimeLeft:F0}s"
                              : dist < 5f   ? " CLOSE!"
                              : dist < 10f  ? " NEAR"
                              : "";
                guardDistText.color = surging      ? Color.Lerp(Color.red, Color.yellow,
                                                        Mathf.PingPong(Time.time * 4f, 1f))
                                    : dist < 5f   ? Color.red
                                    : dist < 10f  ? new Color(1f, 0.6f, 0f)
                                    : new Color(1f, 0.4f, 0.4f);
                TmpUiUtility.SetSafeText(guardDistText, $"GUARD {dist:F0}m{danger}");
            }
            else
            {
                TmpUiUtility.SetSafeText(guardDistText, "");
            }
        }

        // ── Active boost ──────────────────────────────────────────────────────
        if (boostText != null)
        {
            string boostMsg = "";
            if (_pickup.IsSpeedBoostActive)
                boostMsg = $"SPEED  {_pickup.SpeedBoostTimeLeft:F0}s";
            else if (_pickup.IsJumpBoostActive)
                boostMsg = $"JUMP  {_pickup.JumpBoostTimeLeft:F0}s";
            else if (_pickup.isSneaking)
                boostMsg = $"SNEAK  {_pickup.SneakTimeLeft:F0}s";

            TmpUiUtility.SetSafeText(boostText, boostMsg);
            // Hide/show the entire row (icon + text) not just the text object
            var boostRow = boostText.transform.parent != null &&
                           boostText.transform.parent != boostText.transform
                           ? boostText.transform.parent.gameObject
                           : boostText.gameObject;
            boostRow.SetActive(boostMsg.Length > 0);
        }

        // ── Score ─────────────────────────────────────────────────────────────
        TmpUiUtility.SetSafeText(scoreText,
            _pickup.artifactMoneyTotal > 0
                ? $"${_pickup.artifactMoneyTotal:N0}"
                : "");

        // ── Level ─────────────────────────────────────────────────────────────
        TmpUiUtility.SetSafeText(levelText,
            "Lv " + LevelProgress.CurrentLevel);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    void ApplyStyle(TextMeshProUGUI text, Color color)
    {
        if (text == null) return;
        EnsureFont(text);
        text.fontSize  = hudFontSize;
        text.fontStyle = FontStyles.Bold;
        text.color     = color;
    }

    static void EnsureFont(TextMeshProUGUI text)
    {
        if (text != null) TmpUiUtility.EnsureFont(text);
    }

    /// <summary>
    /// Creates a TMP text using centre-anchor and sets localPosition directly —
    /// matches the coordinate system of the pre-existing Inspector-assigned text objects.
    /// </summary>
    static TextMeshProUGUI CreateHudTextAtLocalPos(Transform canvasTransform,
        string goName, Vector2 localPos, Vector2 sizeDelta)
    {
        var existing = canvasTransform.Find(goName);
        if (existing != null)
            return existing.GetComponent<TextMeshProUGUI>();

        var go = new GameObject(goName);
        go.transform.SetParent(canvasTransform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0f, 0.5f);   // left-aligned, vertically centred
        rt.localPosition    = localPos;
        rt.sizeDelta        = sizeDelta;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize  = 42f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = Color.white;
        TmpUiUtility.EnsureFont(tmp);

        return tmp;
    }

    /// <summary>Creates a new TMP text object anchored to the canvas.</summary>
    static TextMeshProUGUI CreateHudText(Transform canvasTransform,
        string goName, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        // Don't duplicate if one already exists
        var existing = canvasTransform.Find(goName);
        if (existing != null)
            return existing.GetComponent<TextMeshProUGUI>();

        var go = new GameObject(goName);
        go.transform.SetParent(canvasTransform, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = anchorMin;   // pivot matches anchor corner
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize  = 42f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = Color.white;
        TmpUiUtility.EnsureFont(tmp);

        return tmp;
    }
}
