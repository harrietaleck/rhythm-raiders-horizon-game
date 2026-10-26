using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Level 3 run controller - the Neon Beat Vault. A readable course through three club sections
/// that ramp up: laser fences (jump), speaker stacks (dodge), bass walls (cross on the dark beat),
/// sweeping lasers and falling light rigs. Every row leaves a way through; rest rows give breathing
/// room; crossing bass walls in time ("in the pocket") builds a Groove that pulls in notes.
/// </summary>
[DefaultExecutionOrder(-200)]
public class ClubRun : MonoBehaviour
{
    public static ClubRun Instance { get; private set; }
    public const float MagnetRadius = 9f;

    [Header("Course")]
    public float[] lanes = { -7f, 0f, 7f };
    public float warmUp = 50f;
    public float lookAhead = 130f;
    public float keepBehind = 25f;
    public float[] artifactDistances = { 300f, 650f };

    [Header("Rewards")]
    public int notesForThirdStar = 110;
    public int pocketNotes = 3;
    public int grooveStreak = 3;
    public float grooveMagnetSeconds = 6f;

    public int Notes { get; private set; }
    public int Score { get; private set; }
    public bool MagnetActive => Time.time < _magnetUntil;
    public float Distance => _player != null ? _player.position.z - _origin : 0f;

    readonly List<GameObject> _spawned = new List<GameObject>();
    readonly List<float> _artifactZ = new List<float>();
    readonly HashSet<string> _intros = new HashSet<string>();
    readonly List<(float z, string key, string text, Color color)> _introPoints = new List<(float, string, string, Color)>();
    Transform _course;
    Transform _player;
    PickupBase _pickup;
    EnemyBase _enemy;
    ClubHallStream _hall;
    GameObject[] _pickupTemplates;
    GameObject _artifactTemplate;
    float _origin;
    float _nextRowZ;
    float _nextArtifactD;
    float _magnetUntil;
    int _rowIndex;
    int _section = -1;
    int _pocketStreak;
    int _pockets;
    bool _banked;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
        Spawner.Suppressed = true;
    }

    void OnEnable() => PickupBase.PlayerHit += OnPlayerHit;
    void OnDisable() => PickupBase.PlayerHit -= OnPlayerHit;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Spawner.Suppressed = false;
    }

    void Start()
    {
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
        {
            _player = playerGo.transform;
            _pickup = playerGo.GetComponent<PickupBase>();
        }
        _enemy = FindAnyObjectByType<EnemyBase>();
        _hall = FindAnyObjectByType<ClubHallStream>();
        _origin = _player != null ? _player.position.z : 0f;
        _nextRowZ = _origin + warmUp;
        _nextArtifactD = artifactDistances.Length > 0 ? artifactDistances[0] : 300f;
        _course = new GameObject("ClubCourse").transform;
        _course.SetParent(transform, false);

        RunStats.Notes = 0;
        ShopData.BeginRun();
        RunStats.Score = 0;
        RunStats.Stars = 0;
        RunStats.DailyCompleted = false;

        FindTemplates();
        NoteLine(lanes[1], _origin + 14f, 6, 5f);
        BuildHud();
        Fill();
    }

    void Update()
    {
        if (_player == null) return;
        Fill();
        Cleanup();
        UpdateCallouts();
        UpdateSection();
        UpdateHud();
        CheckFinish();
    }

    void OnPlayerHit()
    {
        if (!GameManager.GameStarted || _pickup == null) return;
        _pocketStreak = 0;
        string what = string.IsNullOrEmpty(RunStats.LastKiller) ? "OBSTACLE" : RunStats.LastKiller.ToUpperInvariant();
        string cost = _pickup.hitCounter > 0 ? $"-1 SHIELD ({_pickup.hitCounter - 1} LEFT)" : "-VIBE";
        Flash($"HIT BY {what}!  {cost}", new Color(1f, 0.35f, 0.3f), 1.3f);
    }

    /// <summary>Banks notes into the shop at the end of the run; a win earns 1-3 stars.</summary>
    void CheckFinish()
    {
        if (_banked || _pickup == null || !GameManager.GameStarted && _pickup.currentHealth > 0) return;
        bool won = _pickup.artifactAmount >= PickupBase.ArtifactsToWin;
        bool lost = _pickup.currentHealth <= 0;
        if (!won && !lost) return;
        _banked = true;
        if (won)
        {
            int stars = 1;
            if (_pickup.hitCounter > 0) stars++;
            if (Notes >= notesForThirdStar) stars++;
            RunStats.Stars = stars;
            string key = "Stars_L" + LevelProgress.CurrentLevel;
            if (stars > PlayerPrefs.GetInt(key, 0)) PlayerPrefs.SetInt(key, stars);
        }
        ShopData.BankRunNotes(Notes);
    }

    // ── Course ───────────────────────────────────────────────────────────────

    void FindTemplates()
    {
        var pickups = new List<GameObject>();
        foreach (var go in gameObject.scene.GetRootGameObjects())
        {
            switch (go.name.Trim())
            {
                case "Health - Pickup":
                case "Speed boost - Pickup":
                case "Jump Boost  - Pickup":
                case "Jump Boost - Pickup":
                case "Sneak - Pickup":
                    pickups.Add(go);
                    break;
                case "Artifact - Goal":
                    _artifactTemplate = go;
                    break;
            }
        }
        _pickupTemplates = pickups.ToArray();
    }

    void Fill()
    {
        float target = _player.position.z + lookAhead;
        while (_nextRowZ < target)
        {
            float z = _nextRowZ;
            _nextRowZ += PlaceRow(z);
            PlaceCrowd(z, _nextRowZ);
        }
    }

    // ---- Dance floor crowd ------------------------------------------------------------
    // The imported dance floor characters dance along both sides of the floor, outside the
    // lanes and clear of the side DJ booths. Thin in the Lobby, packed on the Main Dance Floor.
    readonly List<Vector3> _crowdBlocked = new List<Vector3>();   // x = side, y = zMin, z = zMax
    float _crowdZ = -1000f;
    float _crowdLightZ = -1000f;
    int _crowdLightIndex;
    static readonly Color[] CrowdLightColors =
    {
        new Color(1f, 0.35f, 0.85f), new Color(0.3f, 0.85f, 1f), new Color(0.65f, 0.4f, 1f), new Color(1f, 0.7f, 0.35f),
    };

    /// <summary>Soft coloured wash over the dancers so they read in the dark club.</summary>
    void CrowdLight(int side, float z, float edge)
    {
        var go = new GameObject("Crowd Light");
        go.transform.SetParent(_course, false);
        go.transform.position = new Vector3(side * (edge + 0.7f), 3.4f, z);
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = CrowdLightColors[_crowdLightIndex++ % CrowdLightColors.Length];
        l.range = 7.5f;
        l.intensity = 3.2f;
        l.shadows = LightShadows.None;
        l.renderMode = LightRenderMode.ForcePixel;
        Track(go);
    }

    void BlockCrowd(int side, float zMin, float zMax)
    {
        _crowdBlocked.Add(new Vector3(side, zMin, zMax));
        // Dancers already placed there (the crowd runs a little ahead of the rows) step aside.
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            var go = _spawned[i];
            if (go == null || go.name != "Dancer") continue;
            var p = go.transform.position;
            if ((p.x > 0f ? 1 : -1) == side && p.z > zMin && p.z < zMax) { Destroy(go); _spawned.RemoveAt(i); }
        }
    }

    bool CrowdFree(int side, float z)
    {
        for (int i = _crowdBlocked.Count - 1; i >= 0; i--)
        {
            var b = _crowdBlocked[i];
            if ((int)b.x == side && z > b.y && z < b.z) return false;
        }
        return true;
    }

    void PlaceCrowd(float zFrom, float zTo)
    {
        if (_crowdZ < zFrom) _crowdZ = zFrom;
        float edge = Mathf.Abs(lanes[2]) + 1.6f;
        while (_crowdZ < zTo)
        {
            float d = _crowdZ - _origin;
            int section = SectionIndex(d);
            float gap = section == 0 ? 7f : section == 1 ? 3.2f : 4f;
            if (d > 15f)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    if (section == 0 && Random.value < 0.5f) continue;
                    float z = _crowdZ + Random.Range(-0.8f, 0.8f);
                    if (!CrowdFree(side, z)) continue;
                    float x = side * Random.Range(edge, edge + 1.3f);
                    var facing = new Vector3(-side * Random.Range(0.4f, 1f), 0f, Random.Range(-1f, 0.3f));
                    var t = DanceFloorCharacters.SpawnDancer(_course, new Vector3(x, 0f, z), facing, Random.Range(1.62f, 1.85f));
                    if (t == null) return;
                    Track(t.gameObject);
                    if (z - _crowdLightZ > 7f)
                    {
                        CrowdLight(side, z, edge);
                        _crowdLightZ = z;
                    }
                }
            }
            _crowdZ += gap;
        }
        _crowdBlocked.RemoveAll(b => b.z < _player.position.z - 20f);
    }

    void Cleanup()
    {
        float limit = _player.position.z - keepBehind;
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            var go = _spawned[i];
            if (go == null) { _spawned.RemoveAt(i); continue; }
            if (go.transform.position.z < limit)
            {
                Destroy(go);
                _spawned.RemoveAt(i);
            }
        }
    }

    int SectionIndex(float d) => _hall != null ? _hall.SectionIndex(d) : d < 250f ? 0 : d < 550f ? 1 : 2;

    float _lastCenterDj = -1000f, _lastSideDj = -1000f;
    int _djSide = 1;

    /// <summary>
    /// Side DJ gig stations along the club walls from the Main Dance Floor: a booth with a
    /// bouncer and a smoke machine that blasts fog across the lanes as the runner arrives.
    /// Snapped to the start of a hall bay so they never overlap the wall speaker stacks.
    /// </summary>
    void MaybeSideDJ(float z, float d, int section)
    {
        if (section < 1 || d - _lastSideDj < 48f) return;
        _lastSideDj = d;
        var hall = FindAnyObjectByType<ClubHallStream>();
        float bayOrigin = _origin + (hall != null ? hall.startZ : -12f);
        float bayZ = bayOrigin + Mathf.Round((z - bayOrigin) / ClubHall.BayLength) * ClubHall.BayLength + 0.5f;
        float x = Mathf.Abs(lanes[2]) + 3.4f;
        Track(DJStation.Build(_course, new Vector3(_djSide * x, 0f, bayZ), _djSide, Mathf.RoundToInt(bayZ * 13f), Mathf.Abs(lanes[2])));
        BlockCrowd(_djSide, bayZ - 2.6f, bayZ + 3.2f);
        _djSide = -_djSide;
    }

    float PlaceRow(float z)
    {
        float d = z - _origin;
        bool needArtifact = _pickup == null || _pickup.artifactAmount + PendingArtifacts() < PickupBase.ArtifactsToWin;
        if (d >= _nextArtifactD && needArtifact)
        {
            PlaceArtifact(z);
            int placed = _artifactZ.Count;
            _nextArtifactD = placed < artifactDistances.Length ? artifactDistances[placed] : d + 300f;
            return 22f;
        }

        int section = SectionIndex(d);
        float spacing = section == 0 ? 17f : section == 1 ? 15f : 13f;
        _rowIndex++;

        // Breathing room: every fifth row is notes only.
        if (_rowIndex % 5 == 0)
        {
            NoteLine(lanes[Random.Range(0, lanes.Length)], z - 3f, 5, 2.4f);
            return spacing;
        }

        // Sweeping laser: slides between the two lanes next to a safe edge lane.
        if (section >= 1 && d > 260f && Random.value < (section == 1 ? 0.14f : 0.18f))
        {
            int safeEdge = Random.value < 0.5f ? 0 : 2;
            int far = safeEdge == 0 ? 2 : 0;
            Track(ClubProps.SweepLaser(_course, z, lanes[far], lanes[1], section == 1 ? 4f : 3f));
            NoteLine(lanes[safeEdge], z - 6f, 5, 2.4f);
            return spacing + 6f;
        }

        int safe = Random.Range(0, lanes.Length);
        var others = new List<int>();
        for (int i = 0; i < lanes.Length; i++) if (i != safe) others.Add(i);
        Shuffle(others);

        // DJ stage in the middle lane (Main Dance Floor on): a taser bodyguard guards one side
        // lane and the stage's smoke machine fogs the floor at random. The other side lane is clear.
        if (section >= 1 && d - _lastCenterDj > 120f && Random.value < (section == 1 ? 0.14f : 0.12f))
        {
            int guardSide = Random.value < 0.5f ? -1 : 1;
            Track(DJStation.BuildCenter(_course, new Vector3(lanes[1], 0f, z + 4f), guardSide, Mathf.RoundToInt(z * 7f),
                                        guardOffset: 4.6f, laneReach: 3f, smokeSpread: 9f));
            BlockCrowd(guardSide, z + 0.5f, z + 6f);   // keep the bodyguard's side clear for the taser
            int free = guardSide > 0 ? 0 : 2;
            NoteLine(lanes[free], z - 3f, 5, 2.4f);
            _lastCenterDj = d;
            _lastSideDj = d;   // keep the side booths away from the stage
            return spacing + 10f;
        }

        MaybeSideDJ(z, d, section);

        // Disco ball drop: lands in a lane ahead and rolls back toward the runner.
        if (section >= 1 && d > 230f && Random.value < (section == 1 ? 0.12f : 0.16f))
        {
            Track(ClubProps.DiscoBallDrop(_course, new Vector3(lanes[others[0]], 0f, z + 12f)));
            if (section >= 2) PlaceHazard(section, new Vector3(lanes[others[1]], 0f, z));
            NoteLine(lanes[safe], z - 4f, 4, 2.4f);
            return spacing + 6f;
        }

        // Falling light rig in the Bass Vault.
        if (section >= 2 && Random.value < 0.14f)
        {
            Track(ClubProps.FallingRig(_course, new Vector3(lanes[others[0]], 0f, z)));
            Track(ClubProps.LaserFence(_course, new Vector3(lanes[others[1]], 0f, z)));
            NoteLine(lanes[safe], z - 4f, 4, 2.4f);
            return spacing + 4f;
        }

        // Rhythm rows: bass walls. From the dance floor on, a "rhythm gate" puts two walls on
        // opposite beats - one is always dark, so read the beat and pick your lane.
        if (d > 100f && Random.value < (section == 0 ? 0.18f : section == 1 ? 0.25f : 0.3f))
        {
            if (section == 0)
            {
                Track(ClubProps.BassWall(_course, new Vector3(lanes[others[0]], 0f, z), Random.Range(0, 4)));
                NoteLine(lanes[safe], z - 4f, 3, 2.4f);
            }
            else
            {
                Track(ClubProps.BassWall(_course, new Vector3(lanes[safe], 0f, z), 0));
                Track(ClubProps.BassWall(_course, new Vector3(lanes[others[0]], 0f, z), 2));
                var third = new Vector3(lanes[others[1]], 0f, z);
                Track(section == 1 && Random.value < 0.5f ? ClubProps.LaserFence(_course, third) : ClubProps.SpeakerStack(_course, third));
                NoteLine(lanes[safe], z - 7f, 2, 2.4f);
                NoteLine(lanes[others[0]], z - 7f, 2, 2.4f);
                IntroOnce("gate", "RHYTHM GATE - ONE WALL IS ALWAYS DARK", new Color(1f, 0.4f, 0.85f), z);
            }
            return spacing + 2f;
        }

        int blocked = section == 0 ? 1 : Random.value < (section == 1 ? 0.4f : 0.6f) ? 2 : 1;
        for (int i = 0; i < blocked; i++)
            PlaceHazard(section, new Vector3(lanes[others[i]], 0f, z));

        NoteLine(lanes[safe], z - 4f, 3, 2.4f);
        if (Random.value < 0.12f && _pickupTemplates.Length > 0)
            PlacePickup(new Vector3(lanes[safe], 1.2f, z + 5f));
        return spacing;
    }

    void PlaceHazard(int section, Vector3 pos)
    {
        float r = Random.value;
        float fence = section == 0 ? 0.55f : section == 1 ? 0.45f : 0.4f;
        float stack = fence + (section == 0 ? 0.45f : 0.35f);
        if (r < fence)
        {
            Track(ClubProps.LaserFence(_course, pos));
            IntroOnce("fence", "LASER FENCE - JUMP IT!", new Color(1f, 0.4f, 0.85f), pos.z);
        }
        else if (r < stack)
        {
            Track(ClubProps.SpeakerStack(_course, pos));
            IntroOnce("stack", "SPEAKER STACK - SWITCH LANES", new Color(0.35f, 0.85f, 1f), pos.z);
        }
        else
            Track(ClubProps.BassWall(_course, pos, Random.Range(0, 4)));
    }

    void PlacePickup(Vector3 pos)
    {
        var template = _pickupTemplates[Random.Range(0, _pickupTemplates.Length)];
        var clone = Instantiate(template, pos, template.transform.rotation, _course);
        clone.transform.localScale = template.transform.localScale * 0.6f;
        Spawner.PrepareSpawnedClone(clone);
        Track(clone);
    }

    void PlaceArtifact(float z)
    {
        float lane = lanes[Random.Range(0, lanes.Length)];
        _artifactZ.Add(z);
        var display = RooftopProps.Root("ArtifactDisplay", _course, new Vector3(lane, 0f, z));
        RooftopProps.Part(PrimitiveType.Cylinder, display.transform, new Vector3(0f, 0.02f, 0f), new Vector3(3f, 0.02f, 3f),
                          RooftopProps.Glow(new Color(1f, 0.8f, 0.3f, 0.5f)), false);
        RooftopProps.Part(PrimitiveType.Cylinder, display.transform, new Vector3(0f, 4f, 0f), new Vector3(1.6f, 4f, 1.6f),
                          RooftopProps.Glow(new Color(1f, 0.85f, 0.4f, 0.1f)), false);
        var light = new GameObject("Light").AddComponent<Light>();
        light.transform.SetParent(display.transform, false);
        light.transform.localPosition = new Vector3(0f, 2.5f, 0f);
        light.type = LightType.Point;
        light.color = new Color(1f, 0.8f, 0.4f);
        light.range = 8f;
        light.intensity = 4f;
        Track(display);

        if (_artifactTemplate != null)
        {
            var clone = Instantiate(_artifactTemplate, new Vector3(lane, 1.2f, z), _artifactTemplate.transform.rotation, _course);
            clone.transform.localScale = _artifactTemplate.transform.localScale;
            Spawner.PrepareSpawnedClone(clone);
            Track(clone);
        }
        NoteLine(lane, z - 14f, 4, 2.6f);
    }

    int PendingArtifacts()
    {
        int pending = 0;
        foreach (float z in _artifactZ)
            if (_player != null && z > _player.position.z) pending++;
        return pending;
    }

    void NoteLine(float x, float z, int count, float gap)
    {
        for (int i = 0; i < count; i++)
            Track(RooftopProps.Note(_course, new Vector3(x, 1f, z + i * gap)));
    }

    void Track(GameObject go)
    {
        if (go != null) _spawned.Add(go);
    }

    static void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // ── Scoring, tips and rhythm rewards ─────────────────────────────────────

    public void AddNote() => AddNotes(1);

    /// <summary>Bumped into a dancer: a few notes are knocked out of the runner's bag.</summary>
    public void LoseNotes(int count, Transform player)
    {
        int lost = Mathf.Min(count, Notes);
        Notes -= lost;
        RunStats.Notes = Notes;
        Flash(lost > 0 ? $"BUMPED A DANCER!  -{lost} NOTES" : "BUMPED A DANCER!", new Color(1f, 0.45f, 0.75f), 1.1f);
        NoteFx.Spill(RunnerBackpack.Bag != null ? RunnerBackpack.Bag : player, lost);
    }

    void AddNotes(int count)
    {
        Notes += count;
        Score += count * 10;
        RunStats.Notes = Notes;
        RunStats.Score = Score;
    }

    /// <summary>Crossed a bass wall while it was dark.</summary>
    public void PocketBonus()
    {
        _pockets++;
        RunStats.Pockets = _pockets;
        _pocketStreak++;
        AddNotes(pocketNotes);
        if (_pocketStreak >= grooveStreak)
        {
            _pocketStreak = 0;
            _magnetUntil = Time.time + grooveMagnetSeconds;
            Flash($"GROOVE! NOTE MAGNET {grooveMagnetSeconds:0}s", new Color(0.35f, 1f, 0.6f), 2f);
        }
        else
            Flash($"IN THE POCKET! +{pocketNotes}   ({_pocketStreak}/{grooveStreak} GROOVE)", new Color(0.35f, 0.9f, 1f), 1.2f);
    }

    /// <summary>One-time tip for a new hazard; shown now, or when the runner nears <paramref name="atZ"/>.</summary>
    public void IntroOnce(string key, string text, Color color, float atZ = float.NaN)
    {
        if (_intros.Contains(key)) return;
        if (!float.IsNaN(atZ))
        {
            foreach (var p in _introPoints) if (p.key == key) return;
            _introPoints.Add((atZ, key, text, color));
            return;
        }
        _intros.Add(key);
        Flash(text, color, 2.4f);
    }

    void UpdateCallouts()
    {
        if (!GameManager.GameStarted) return;
        for (int i = _introPoints.Count - 1; i >= 0; i--)
        {
            var p = _introPoints[i];
            if (p.z - _player.position.z < 40f)
            {
                _introPoints.RemoveAt(i);
                IntroOnce(p.key, p.text, p.color);
            }
        }
        for (int i = _artifactZ.Count - 1; i >= 0; i--)
        {
            float ahead = _artifactZ[i] - _player.position.z;
            if (ahead > 0f && ahead < 45f)
            {
                Flash("ARTIFACT AHEAD - FOLLOW THE GOLD LIGHT", new Color(1f, 0.85f, 0.35f), 2f);
                _artifactZ[i] = -9999f;
            }
        }
    }

    void UpdateSection()
    {
        int section = SectionIndex(Distance);
        if (section == _section || !GameManager.GameStarted) return;
        _section = section;
        string name = _hall != null ? _hall.SectionAt(Distance).name : "CLUB";
        Flash(name, ClubHall.SectionAccent[Mathf.Clamp(section, 0, 2)], 2.5f);
    }

    // ── HUD ──────────────────────────────────────────────────────────────────

    TextMeshProUGUI _distanceText, _sectionText, _scoreText, _grooveText, _powerText, _popupText, _hint;
    Image _statsPlate, _popupPlate, _beatImage;
    RectTransform _popupGroup, _beatPip;
    float _popupUntil, _popupShownAt;

    public void Flash(string text, Color color, float seconds)
    {
        if (_popupText == null) return;
        _popupText.text = text;
        _popupText.color = color;
        _popupUntil = Time.time + seconds;
        _popupShownAt = Time.time;
    }

    void BuildHud()
    {
        var skin = HudSkin.Get();
        var canvasGo = new GameObject("ClubHUD", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        _statsPlate = Plate(canvasGo.transform, "StatsPlate", new Vector2(0.36f, 0.805f), new Vector2(0.64f, 0.99f), skin != null ? skin.window : null, 2.5f, new Color(1f, 1f, 1f, 0.92f));
        _distanceText = Text(canvasGo.transform, "Distance", new Vector2(0.38f, 0.925f), new Vector2(0.62f, 0.98f), 40, new Color(0.9f, 0.97f, 1f), skin);
        _sectionText = Text(canvasGo.transform, "Section", new Vector2(0.38f, 0.89f), new Vector2(0.62f, 0.925f), 22, new Color(1f, 0.55f, 0.95f), skin);
        _scoreText = Text(canvasGo.transform, "Score", new Vector2(0.38f, 0.86f), new Vector2(0.62f, 0.89f), 20, new Color(0.35f, 0.85f, 1f), skin);
        _grooveText = Text(canvasGo.transform, "Groove", new Vector2(0.38f, 0.83f), new Vector2(0.62f, 0.86f), 18, Color.white, skin);
        _grooveText.richText = true;
        _powerText = Text(canvasGo.transform, "Powers", new Vector2(0.75f, 0.705f), new Vector2(0.98f, 0.755f), 20, new Color(0.8f, 0.95f, 1f), skin);

        _popupGroup = new GameObject("CalloutGroup", typeof(RectTransform)).GetComponent<RectTransform>();
        _popupGroup.SetParent(canvasGo.transform, false);
        _popupGroup.anchorMin = new Vector2(0.68f, 0.6f);
        _popupGroup.anchorMax = new Vector2(0.985f, 0.685f);
        _popupGroup.offsetMin = _popupGroup.offsetMax = Vector2.zero;
        _popupPlate = Plate(_popupGroup, "PopupPlate", Vector2.zero, Vector2.one, skin != null ? skin.banner : null, 4f, Color.white);
        _popupText = Text(_popupGroup, "Popup", new Vector2(0.07f, 0.1f), new Vector2(0.93f, 0.9f), 28, Color.white, skin);
        _popupText.enableAutoSizing = true;
        _popupText.fontSizeMin = 10f;
        _popupText.fontSizeMax = 28f;
        _popupText.textWrappingMode = TextWrappingModes.NoWrap;
        _popupText.text = "";

        var pip = new GameObject("BeatPip", typeof(RectTransform));
        pip.transform.SetParent(canvasGo.transform, false);
        _beatPip = pip.GetComponent<RectTransform>();
        _beatPip.anchorMin = _beatPip.anchorMax = new Vector2(0.5f, 0.06f);
        _beatPip.sizeDelta = new Vector2(24f, 24f);
        _beatPip.localEulerAngles = new Vector3(0f, 0f, 45f);
        _beatImage = pip.AddComponent<Image>();
        _beatImage.raycastTarget = false;
        _hint = Text(canvasGo.transform, "Hint", new Vector2(0.25f, 0.085f), new Vector2(0.75f, 0.12f), 16, new Color(1f, 0.55f, 0.95f, 0.85f), skin);
        _hint.text = "JUMP LASER FENCES - DODGE SPEAKERS - CROSS BASS WALLS WHEN THEY GO DARK";
    }

    static Image Plate(Transform parent, string name, Vector2 min, Vector2 max, Sprite sprite, float ppu, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.raycastTarget = false;
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = ppu;
            img.color = color;
        }
        else img.color = new Color(0.02f, 0.03f, 0.07f, 0.75f);
        return img;
    }

    static TextMeshProUGUI Text(Transform parent, string name, Vector2 min, Vector2 max, float size, Color color, HudSkin skin)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (skin != null && skin.font != null) tmp.font = skin.font;
        tmp.fontSize = size;
        // Shrink to fit its box (never spill over the panel art), never wrap, never overflow.
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = Mathf.Min(10f, size);
        tmp.fontSizeMax = size;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = 4f;
        tmp.raycastTarget = false;
        return tmp;
    }

    void UpdateHud()
    {
        bool show = GameManager.GameStarted;
        _statsPlate.enabled = _distanceText.enabled = _sectionText.enabled = _scoreText.enabled = _grooveText.enabled = show;
        _beatImage.enabled = _hint.enabled = _powerText.enabled = show;
        if (!show)
        {
            _popupPlate.enabled = false;
            _popupText.text = "";
            return;
        }

        _distanceText.text = $"{Distance:0} m";
        _sectionText.text = _hall != null ? _hall.SectionAt(Distance).name : "NEON BEAT VAULT";
        _scoreText.text = $"NOTES {Notes}    SCORE {Score:N0}";
        _grooveText.text = $"<color=#5AFF9A>GROOVE {_pocketStreak}/{grooveStreak}</color>   <color=#5AD9FF>IN THE POCKET {_pockets}</color>";

        var powers = new List<string>();
        if (MagnetActive) powers.Add($"MAGNET {Mathf.CeilToInt(_magnetUntil - Time.time)}s");
        if (_enemy != null && _enemy.IsStunned) powers.Add("GUARD STUNNED");
        _powerText.text = string.Join("   ", powers);

        float a = Time.time < _popupUntil ? Mathf.Clamp01((_popupUntil - Time.time) * 3f) : 0f;
        var c = _popupText.color;
        _popupText.color = new Color(c.r, c.g, c.b, a);
        _popupPlate.enabled = a > 0.01f && _popupText.text.Length > 0;
        _popupPlate.color = new Color(1f, 1f, 1f, a);
        float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.time - _popupShownAt) * 6f));
        _popupGroup.anchoredPosition = new Vector2(480f * (1f - k), 0f);

        float pulse = ClubBeat.Kick;
        _beatPip.sizeDelta = Vector2.one * (20f + pulse * 22f);
        _beatImage.color = Color.Lerp(new Color(1f, 0.3f, 0.85f, 0.35f), new Color(1f, 0.45f, 0.95f, 1f), pulse);
    }
}
