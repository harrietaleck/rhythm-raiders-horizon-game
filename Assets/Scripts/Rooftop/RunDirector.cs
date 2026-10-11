using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Level 1 run pacing: distance, speed ramp, districts, score/combo, rhythm jumps,
/// power-ups, searchlight helicopter, star rating and the run HUD.
/// </summary>
public class RunDirector : MonoBehaviour
{
    public static RunDirector Instance { get; private set; }

    [System.Serializable]
    public class District
    {
        public string name;
        public float startDistance;
        public Color fog;
        public Color sun;
        public Color ambient;
    }

    [Header("References")]
    public StreetRunStream stream;
    public TMP_FontAsset font;
    public Light sun;

    [Header("Speed ramp")]
    public float rampEvery = 150f;
    public float rampStep = 0.08f;
    public float rampMax = 1.4f;

    [Header("Rhythm")]
    public float bpm = 110f;
    public float beatWindow = 0.13f;
    public float beatBoost = 2.5f;

    [Header("Powers")]
    public float magnetSeconds = 6f;
    public float magnetRadius = 9f;
    public float bubbleSeconds = 5f;
    public float smokeSeconds = 3f;

    [Header("Searchlight")]
    public float searchlightFrom = 350f;
    public float searchlightEvery = 22f;

    [Header("Stars")]
    public int notesForThirdStar = 40;

    public District[] districts =
    {
        new District { name = "RESIDENTIAL ROOFTOPS", startDistance = 0f,
            fog = new Color(0.85f, 0.42f, 0.28f), sun = new Color(1f, 0.55f, 0.28f), ambient = new Color(0.95f, 0.48f, 0.28f) },
        new District { name = "NEON MARKET", startDistance = 300f,
            fog = new Color(0.45f, 0.2f, 0.55f), sun = new Color(0.85f, 0.45f, 1f), ambient = new Color(0.6f, 0.3f, 0.75f) },
        new District { name = "CONSTRUCTION ZONE", startDistance = 600f,
            fog = new Color(0.6f, 0.45f, 0.3f), sun = new Color(1f, 0.72f, 0.4f), ambient = new Color(0.7f, 0.52f, 0.36f) },
    };

    public float Distance { get; private set; }
    public int DistrictIndex { get; private set; }
    public bool MagnetActive => Time.time < _magnetUntil;
    public int Combo { get; private set; }
    public int Multiplier => 1 + Mathf.Min(Combo / 4, 4);

    Transform _player;
    PlayerMovement _movement;
    PickupBase _pickup;
    EnemyBase _enemy;
    float _startZ;
    bool _started;
    bool _finished;

    float _speedMult = 1f;
    float _baseForward;
    float _guardWarnUntil;

    int _notes;
    int _score;
    float _lastHit = -10f;
    readonly List<float> _hazardRows = new List<float>();

    float _magnetUntil, _bubbleUntil, _beatBoostUntil;
    GameObject _bubbleFx;

    float _nextSearchlight;
    GameObject _searchlight;

    // HUD
    TextMeshProUGUI _distanceText, _fragmentText, _scoreText, _powerText, _popupText, _warningText, _beatLabel;
    RectTransform _beatPip;
    Image _beatImage;
    float _popupUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        PickupBase.PlayerHit -= OnPlayerHit;
        EnemyBase.SurgeWarned -= OnSurgeWarned;
        SaveBest();
    }

    void Start()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null)
        {
            _player = go.transform;
            _movement = go.GetComponent<PlayerMovement>();
            _pickup = go.GetComponent<PickupBase>();
            PlayerCarrier.For(go);
        }
        _enemy = FindAnyObjectByType<EnemyBase>();
        if (sun == null) sun = GameObject.Find("Directional Light")?.GetComponent<Light>();

        PickupBase.PlayerHit += OnPlayerHit;
        EnemyBase.SurgeWarned += OnSurgeWarned;

        RunStats.Notes = 0;
        RunStats.Score = 0;
        RunStats.Stars = 0;
        RunStats.BestDistance = PlayerPrefs.GetFloat(BestKey, 0f);

        BuildHud();
    }

    string BestKey => "BestDistance_L" + LevelProgress.CurrentLevel;

    void Update()
    {
        if (_player == null) return;

        if (!_started)
        {
            if (!GameManager.GameStarted) { UpdateHud(); return; }
            _started = true;
            _startZ = _player.position.z;
            _baseForward = _movement != null ? _movement.baseForwardSpeed : 8f;
            _nextSearchlight = searchlightFrom;
            ShowPopup(districts[0].name, new Color(1f, 0.85f, 0.6f), 2.5f);
        }

        if (_finished) return;

        Distance = Mathf.Max(0f, _player.position.z - _startZ);
        UpdateSpeedRamp();
        UpdateDistrict();
        UpdateCombo();
        UpdatePowers();
        UpdateSearchlight();
        CheckFinish();
        UpdateHud();
    }

    void LateUpdate()
    {
        if (!_started || _finished || _movement == null) return;
        if (_movement.DidJumpThisFrame) CheckRhythmJump();

        if (_beatBoostUntil > 0f && Time.time > _beatBoostUntil)
        {
            _beatBoostUntil = 0f;
            _movement.forwardSpeed = Mathf.Max(1f, _movement.forwardSpeed - beatBoost);
        }
    }

    public float StartZ => _started ? _startZ : (_player != null ? _player.position.z : 0f);

    // ── Progression ──────────────────────────────────────────────────────────

    void UpdateSpeedRamp()
    {
        if (_movement == null) return;
        float target = Mathf.Min(rampMax, 1f + Mathf.Floor(Distance / rampEvery) * rampStep);
        if (target <= _speedMult + 0.001f) return;

        float delta = _baseForward * (target - _speedMult);
        _speedMult = target;
        _movement.baseForwardSpeed += delta;
        _movement.forwardSpeed += delta;
        _enemy?.SetSpeedMultiplier(_speedMult);
        ShowPopup("SPEED UP!", new Color(1f, 0.75f, 0.3f), 1.2f);
    }

    void UpdateDistrict()
    {
        int index = 0;
        for (int i = 0; i < districts.Length; i++)
            if (Distance >= districts[i].startDistance) index = i;

        if (index != DistrictIndex)
        {
            DistrictIndex = index;
            ShowPopup(districts[index].name, new Color(1f, 0.85f, 0.6f), 2.5f);
        }

        var d = districts[DistrictIndex];
        float k = Time.deltaTime * 0.6f;
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, d.fog, k);
        RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor, d.ambient, k);
        if (sun != null) sun.color = Color.Lerp(sun.color, d.sun, k);
    }

    public int DistrictAt(float distance)
    {
        int index = 0;
        for (int i = 0; i < districts.Length; i++)
            if (distance >= districts[i].startDistance) index = i;
        return index;
    }

    // ── Score / combo ────────────────────────────────────────────────────────

    public void RegisterHazardRow(float worldZ)
    {
        _hazardRows.Add(worldZ);
    }

    void UpdateCombo()
    {
        float pz = _player.position.z;
        for (int i = _hazardRows.Count - 1; i >= 0; i--)
        {
            if (_hazardRows[i] > pz - 1.5f) continue;
            _hazardRows.RemoveAt(i);
            if (Time.time - _lastHit > 0.8f)
            {
                int before = Multiplier;
                Combo++;
                if (Multiplier > before)
                    ShowPopup($"COMBO x{Multiplier}", new Color(0.35f, 0.85f, 1f), 1.2f);
            }
        }
    }

    void OnPlayerHit()
    {
        _lastHit = Time.time;
        if (Combo >= 4) ShowPopup("COMBO LOST", new Color(1f, 0.4f, 0.35f), 1f);
        Combo = 0;
    }

    public void AddNote()
    {
        _notes++;
        _score += 10 * Multiplier;
        RunStats.Notes = _notes;
        RunStats.Score = _score;
        AudioManager.Instance?.PlayHealthPickupSfx();
    }

    public void OnStunt(string label)
    {
        Debug.Log($"[Run] {label} at {Distance:0}m");
        _score += 50 * Multiplier;
        RunStats.Score = _score;
        Combo++;
        ShowPopup(label, new Color(0.35f, 0.85f, 1f), 1.2f);
    }

    public void Flash(string label, Color color) => ShowPopup(label, color, 0.9f);

    // ── Rhythm jumps ─────────────────────────────────────────────────────────

    float BeatPhase()
    {
        float beat = 60f / bpm;
        return Mathf.Repeat(Time.time, beat) / beat;
    }

    void CheckRhythmJump()
    {
        float beat = 60f / bpm;
        float t = Mathf.Repeat(Time.time, beat);
        float off = Mathf.Min(t, beat - t);
        if (off > beatWindow) return;

        if (_beatBoostUntil <= 0f)
            _movement.forwardSpeed += beatBoost;
        _beatBoostUntil = Time.time + 1.5f;
        _score += 20 * Multiplier;
        RunStats.Score = _score;
        ShowPopup("ON BEAT!", new Color(1f, 0.85f, 0.25f), 0.8f);
    }

    // ── Powers ───────────────────────────────────────────────────────────────

    public void ApplyPower(PowerPickup.Kind kind, PickupBase pickup)
    {
        switch (kind)
        {
            case PowerPickup.Kind.Magnet:
                _magnetUntil = Time.time + magnetSeconds;
                ShowPopup("MAGNET!", new Color(1f, 0.4f, 0.4f), 1.2f);
                break;
            case PowerPickup.Kind.Bubble:
                _bubbleUntil = Time.time + bubbleSeconds;
                if (pickup != null) pickup.invulnerableUntil = _bubbleUntil;
                ShowPopup("SHIELD BUBBLE!", new Color(0.35f, 0.85f, 1f), 1.2f);
                break;
            case PowerPickup.Kind.SmokeBomb:
                if (_enemy != null)
                {
                    _enemy.Stun(smokeSeconds);
                    SpawnSmoke(_enemy.transform.position);
                }
                ShowPopup("SMOKE BOMB! GUARD STUNNED", new Color(0.8f, 0.8f, 0.85f), 1.5f);
                break;
        }
    }

    void UpdatePowers()
    {
        bool bubble = Time.time < _bubbleUntil;
        if (bubble && _bubbleFx == null)
        {
            _bubbleFx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(_bubbleFx.GetComponent<Collider>());
            _bubbleFx.name = "ShieldBubble";
            _bubbleFx.GetComponent<Renderer>().sharedMaterial = RooftopProps.Mat(new Color(0.35f, 0.85f, 1f, 0.25f), 1.5f, true);
            _bubbleFx.transform.SetParent(_player, false);
            _bubbleFx.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            _bubbleFx.transform.localScale = Vector3.one * 2.6f;
        }
        else if (!bubble && _bubbleFx != null)
        {
            Destroy(_bubbleFx);
        }
        if (_bubbleFx != null)
            _bubbleFx.transform.localScale = Vector3.one * (2.5f + Mathf.Sin(Time.time * 6f) * 0.08f);
    }

    void SpawnSmoke(Vector3 at)
    {
        var root = new GameObject("Smoke");
        root.transform.position = at;
        for (int i = 0; i < 7; i++)
        {
            var puff = RooftopProps.Part(PrimitiveType.Sphere, root.transform,
                new Vector3(Random.Range(-1f, 1f), Random.Range(0.3f, 2f), Random.Range(-1f, 1f)),
                Vector3.one * Random.Range(1.2f, 2f), RooftopProps.Mat(new Color(0.7f, 0.7f, 0.72f, 0.6f), 0f, true), false);
            puff.AddComponent<SmokePuff>();
        }
        Destroy(root, smokeSeconds);
    }

    // ── Searchlight helicopter ───────────────────────────────────────────────

    void UpdateSearchlight()
    {
        if (Distance < _nextSearchlight || _searchlight != null) return;
        _nextSearchlight = Distance + searchlightEvery + Random.Range(0f, 10f);
        _searchlight = Searchlight.Spawn(_player.position.z + 45f, _player.position.y);
        ShowPopup("SEARCHLIGHT AHEAD!", new Color(1f, 0.95f, 0.6f), 1.5f);
    }

    public void OnSpotted()
    {
        if (_pickup != null && _pickup.isSneaking) return;
        _enemy?.Surge(2.5f);
        ShowPopup("SPOTTED!", new Color(1f, 0.35f, 0.3f), 1.2f);
    }

    void OnSurgeWarned()
    {
        _guardWarnUntil = Time.time + 1.2f;
    }

    // ── Finish / stars ───────────────────────────────────────────────────────

    void CheckFinish()
    {
        if (_pickup == null) return;
        if (_pickup.artifactAmount >= PickupBase.ArtifactsToWin)
        {
            _finished = true;
            int stars = 1;
            if (_pickup.hitCounter > 0) stars++;
            if (_notes >= notesForThirdStar) stars++;
            RunStats.Stars = stars;
            string key = "Stars_L" + LevelProgress.CurrentLevel;
            if (stars > PlayerPrefs.GetInt(key, 0)) PlayerPrefs.SetInt(key, stars);
            SaveBest();
        }
        else if (_pickup.currentHealth <= 0)
        {
            _finished = true;
            SaveBest();
        }
    }

    void SaveBest()
    {
        if (!_started) return;
        float best = Mathf.Max(PlayerPrefs.GetFloat(BestKey, 0f), Distance);
        PlayerPrefs.SetFloat(BestKey, best);
        PlayerPrefs.Save();
        RunStats.BestDistance = best;
    }

    // ── HUD ──────────────────────────────────────────────────────────────────

    void BuildHud()
    {
        var canvasGo = new GameObject("RunHUD", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        _distanceText = Text(canvasGo, "Distance", new Vector2(0.35f, 0.905f), new Vector2(0.65f, 0.985f), 46, Color.white);
        _fragmentText = Text(canvasGo, "Fragment", new Vector2(0.3f, 0.865f), new Vector2(0.7f, 0.91f), 26, new Color(0.35f, 0.85f, 1f));
        _scoreText = Text(canvasGo, "Score", new Vector2(0.3f, 0.825f), new Vector2(0.7f, 0.865f), 24, new Color(1f, 0.85f, 0.35f));
        _powerText = Text(canvasGo, "Powers", new Vector2(0.3f, 0.79f), new Vector2(0.7f, 0.825f), 22, new Color(0.8f, 0.95f, 1f));
        _warningText = Text(canvasGo, "Warning", new Vector2(0.2f, 0.66f), new Vector2(0.8f, 0.76f), 60, new Color(1f, 0.25f, 0.2f));
        _popupText = Text(canvasGo, "Popup", new Vector2(0.15f, 0.56f), new Vector2(0.85f, 0.66f), 48, Color.white);

        var pipGo = new GameObject("BeatPip", typeof(RectTransform));
        pipGo.transform.SetParent(canvasGo.transform, false);
        _beatPip = pipGo.GetComponent<RectTransform>();
        _beatPip.anchorMin = _beatPip.anchorMax = new Vector2(0.5f, 0.07f);
        _beatPip.sizeDelta = new Vector2(26f, 26f);
        _beatPip.localEulerAngles = new Vector3(0, 0, 45f);
        _beatImage = pipGo.AddComponent<Image>();
        _beatImage.raycastTarget = false;
        _beatLabel = Text(canvasGo, "BeatLabel", new Vector2(0.35f, 0.095f), new Vector2(0.65f, 0.13f), 18, new Color(1f, 1f, 1f, 0.7f));
        _beatLabel.text = "JUMP ON THE BEAT";

        foreach (var t in new[] { _warningText, _popupText }) t.text = "";
    }

    TextMeshProUGUI Text(GameObject parent, string name, Vector2 min, Vector2 max, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.characterSpacing = 4f;
        tmp.outlineWidth = 0.18f;
        tmp.outlineColor = new Color32(0, 0, 0, 200);
        return tmp;
    }

    void ShowPopup(string text, Color color, float seconds)
    {
        if (_popupText == null) return;
        _popupText.text = text;
        _popupText.color = color;
        _popupUntil = Time.time + seconds;
    }

    void UpdateHud()
    {
        if (_distanceText == null) return;
        bool show = GameManager.GameStarted && !_finished;
        foreach (var t in new[] { _distanceText, _fragmentText, _scoreText, _powerText, _beatLabel })
            t.enabled = show;
        _beatImage.enabled = show;

        if (!show)
        {
            _warningText.text = "";
            _popupText.text = "";
            return;
        }

        _distanceText.text = $"{Distance:0} m";
        _fragmentText.text = FragmentLine();
        _scoreText.text = $"NOTES {_notes}    SCORE {_score:N0}" + (Multiplier > 1 ? $"    <color=#5AD9FF>x{Multiplier}</color>" : "");

        var powers = new List<string>();
        if (MagnetActive) powers.Add($"MAGNET {_magnetUntil - Time.time:0}s");
        if (Time.time < _bubbleUntil) powers.Add($"BUBBLE {_bubbleUntil - Time.time:0}s");
        if (_enemy != null && _enemy.IsStunned) powers.Add("GUARD STUNNED");
        if (_pickup != null && _pickup.isSneaking) powers.Add($"SNEAK {_pickup.SneakTimeLeft:0}s");
        _powerText.text = string.Join("    ", powers);

        bool warn = Time.time < _guardWarnUntil || (_enemy != null && (_enemy.IsWarning || _enemy.IsSurging));
        _warningText.text = warn && Mathf.Repeat(Time.time * 4f, 1f) > 0.35f
            ? (_enemy != null && _enemy.IsSurging ? "GUARD SURGE!" : "GUARD INCOMING!")
            : "";

        float a = Time.time < _popupUntil ? Mathf.Clamp01((_popupUntil - Time.time) * 3f) : 0f;
        var c = _popupText.color;
        _popupText.color = new Color(c.r, c.g, c.b, a);

        float phase = BeatPhase();
        float pulse = 1f - Mathf.Clamp01(Mathf.Min(phase, 1f - phase) * 6f);
        _beatPip.sizeDelta = Vector2.one * (22f + pulse * 22f);
        _beatImage.color = Color.Lerp(new Color(1f, 1f, 1f, 0.35f), new Color(1f, 0.85f, 0.25f, 1f), pulse);
    }

    string FragmentLine()
    {
        if (_pickup == null || stream == null) return "";
        int got = _pickup.artifactAmount;
        float z = stream.NextFragmentZ();
        if (float.IsNaN(z)) return "";
        float remaining = z - _player.position.z;
        return remaining > 3f
            ? $"FRAGMENT {got + 1} IN {remaining:0} m"
            : $"FRAGMENT {got + 1} NEARBY";
    }
}
