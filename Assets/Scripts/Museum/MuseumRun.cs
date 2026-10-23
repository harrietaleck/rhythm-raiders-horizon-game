using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Level 2 run controller. Replaces the old random scatter with a readable course:
/// rows of museum hazards that always leave a safe lane, note lines along the best route,
/// rest rows, two fixed artifact displays, interactive exhibits and a skinned run HUD.
/// </summary>
[DefaultExecutionOrder(-200)]
public class MuseumRun : MonoBehaviour
{
    public static MuseumRun Instance { get; private set; }
    public const float MagnetRadius = 9f;
    public const int TreasureHeal = 40;

    [Header("Course")]
    public float[] lanes = { -7f, 0f, 7f };
    public float warmUp = 50f;
    public float lookAhead = 120f;
    public float keepBehind = 25f;
    public float[] artifactDistances = { 280f, 600f };
    public float hazardWidthScale = 1.3f;
    public GameObject drumModel;

    [Header("Rhythm")]
    public float bpm = 110f;
    public float beatWindow = 0.14f;

    [Header("Rewards")]
    [Tooltip("Notes needed (with both artifacts) for the third star.")]
    public int notesForThirdStar = 100;

    public int Notes { get; private set; }
    public int Score { get; private set; }
    public bool MagnetActive => Time.time < _magnetUntil;
    public float Distance => _player != null ? _player.position.z - _origin : 0f;

    readonly List<GameObject> _spawned = new List<GameObject>();
    readonly List<float> _artifactZ = new List<float>();
    Transform _course;
    Transform _player;
    PickupBase _pickup;
    EnemyBase _enemy;
    MuseumHallStream _hall;
    GameObject[] _pickupTemplates;
    GameObject _artifactTemplate;
    GameObject _guardModel;
    float _guardModelScale = 1f;
    bool _guardsIntroduced;
    float _origin;
    float _nextRowZ;
    float _nextArtifactD;
    float _beatStart;
    float _magnetUntil;
    float _lastExhibitAt = -10f;
    float _slowUntil;
    bool _slowMo;
    int _rowIndex;
    int _chain;
    int _wing = -1;
    int _guardCount;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
        Spawner.Suppressed = true;
    }

    void OnEnable()
    {
        MuseumHall.GuardableExhibitBuilt += OnGuardableExhibit;
        PickupBase.PlayerHit += OnPlayerHit;
    }

    void OnDisable()
    {
        MuseumHall.GuardableExhibitBuilt -= OnGuardableExhibit;
        PickupBase.PlayerHit -= OnPlayerHit;
    }

    /// <summary>Names what just hit the player and what it cost, so shield loss is never a mystery.</summary>
    void OnPlayerHit()
    {
        if (!GameManager.GameStarted || _pickup == null) return;
        string what = string.IsNullOrEmpty(RunStats.LastKiller) ? "OBSTACLE" : RunStats.LastKiller.ToUpperInvariant();
        string cost = _pickup.hitCounter > 0 ? $"-1 SHIELD ({_pickup.hitCounter - 1} LEFT)" : "-VIBE";
        Flash($"HIT BY {what}!  {cost}", new Color(1f, 0.35f, 0.3f), 1.3f);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Spawner.Suppressed = false;
        if (_slowMo) Time.timeScale = 1f;
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
        _hall = FindAnyObjectByType<MuseumHallStream>();
        _origin = _player != null ? _player.position.z : 0f;
        _nextRowZ = _origin + warmUp;
        _nextArtifactD = artifactDistances.Length > 0 ? artifactDistances[0] : 300f;
        _beatStart = Time.time;
        _course = new GameObject("MuseumCourse").transform;
        _course.SetParent(transform, false);

        PlantedGrenadeBlink.IntroShown = false;
        PatrolGuard.ResetIntros();
        RunStats.Notes = 0;
        ShopData.BeginRun();
        RunStats.Score = 0;
        RunStats.Stars = 0;
        RunStats.DailyCompleted = false;

        FindTemplates();
        FindGuardModel();
        NoteLine(lanes[1], _origin + 14f, 6, 5f);
        BuildHud();
        Fill();
    }

    void Update()
    {
        if (_player == null) return;
        Fill();
        Cleanup();
        UpdateArtifactCallouts();
        UpdateWing();
        UpdateSlowMo();
        UpdateHud();
        CheckFinish();
    }

    bool _banked;

    /// <summary>
    /// End of run: copy notes and score to the results screen and bank the notes into the shop
    /// wallet (win or lose). A win also earns 1-3 stars: 1 for finishing, +1 with shields left,
    /// +1 for collecting enough notes.
    /// </summary>
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
            _nextRowZ += PlaceRow(_nextRowZ);
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

    float PlaceRow(float z)
    {
        float d = z - _origin;
        bool needArtifact = _pickup == null || _pickup.artifactAmount + PendingArtifacts() < PickupBase.ArtifactsToWin;
        if (d >= _nextArtifactD && needArtifact)
        {
            PlaceArtifact(z);
            int placed = _artifactZ.Count;
            _nextArtifactD = placed < artifactDistances.Length ? artifactDistances[placed] : d + 300f;
            return 20f;
        }

        int wing = WingIndex(d);
        float spacing = wing == 0 ? 17f : wing == 1 ? 15f : 13f;
        _rowIndex++;

        // Breathing room: every fifth row is notes only.
        if (_rowIndex % 5 == 0)
        {
            NoteLine(lanes[Random.Range(0, lanes.Length)], z - 3f, 5, 2.4f);
            return spacing;
        }

        int safe = Random.Range(0, lanes.Length);
        var others = new List<int>();
        for (int i = 0; i < lanes.Length; i++) if (i != safe) others.Add(i);
        Shuffle(others);

        // Showcase row: glass cases in the lanes - smash the cyan ones, dodge the red, gamble on purple.
        if (d > 110f && Random.value < (wing == 0 ? 0.14f : wing == 1 ? 0.18f : 0.2f))
            return PlaceShowcaseRow(z, wing, safe, others) + spacing;

        // Set piece: a drum rolls down one lane.
        if (wing >= 1 && Random.value < 0.14f)
        {
            Track(MuseumProps.RollingDrum(_course, new Vector3(lanes[others[0]], 0f, z + 8f), drumModel));
            NoteLine(lanes[safe], z - 2f, 4, 3f);
            return spacing + 6f;
        }

        int blocked = wing == 0 ? 1 : Random.value < (wing == 1 ? 0.4f : 0.6f) ? 2 : 1;
        for (int i = 0; i < blocked; i++)
            PlaceHazard(wing, new Vector3(lanes[others[i]], 0f, z));

        NoteLine(lanes[safe], z - 4f, 3, 2.4f);
        if (Random.value < 0.12f && _pickupTemplates.Length > 0)
            PlacePickup(new Vector3(lanes[safe], 1.2f, z + 5f));
        return spacing;
    }

    /// <summary>
    /// Lane cases. Every pattern leaves a clean lane with notes, so smashing is always a choice:
    ///  Choice  - a cyan boost and a red curse side by side; read the colours and pick.
    ///  Gamble  - a purple mystery case next to a hazard.
    ///  Bait    - a cyan boost guarded by a low laser just in front (jump it to claim it).
    /// Curses get more common in the later wings.
    /// </summary>
    float PlaceShowcaseRow(float z, int wing, int safe, List<int> others)
    {
        float r = Random.value;
        int seed = Random.Range(0, 1000);
        if (r < 0.45f)
        {
            PlaceCase(lanes[others[0]], z, seed, ExhibitMood.Blessing);
            PlaceCase(lanes[others[1]], z, seed + 3, ExhibitMood.Curse);
        }
        else if (r < 0.75f || wing == 0)
        {
            PlaceCase(lanes[others[0]], z, seed, ExhibitMood.Mystery);
            if (wing >= 1) PlaceHazard(wing, new Vector3(lanes[others[1]], 0f, z));
            else PlaceCase(lanes[others[1]], z, seed + 5, ExhibitMood.Curse);
        }
        else
        {
            Track(MuseumProps.LowLaser(_course, new Vector3(lanes[others[0]], 0f, z - 6f)));
            PlaceCase(lanes[others[0]], z, seed, ExhibitMood.Blessing);
            PlaceCase(lanes[others[1]], z, seed + 9, wing >= 2 ? ExhibitMood.Curse : ExhibitMood.Mystery);
        }
        NoteLine(lanes[safe], z - 4f, 4, 2.4f);
        return 4f;
    }

    void PlaceCase(float x, float z, int seed, ExhibitMood mood)
    {
        var models = _hall != null ? _hall.instrumentModels : null;
        Track(MuseumHall.PathCase(_course, new Vector3(x, 0f, z), models, seed, mood));
    }

    void PlaceHazard(int wing, Vector3 pos)
    {
        float r = Random.value;
        GameObject go;
        if (Random.value < (wing == 0 ? 0.08f : wing == 1 ? 0.16f : 0.2f))
        {
            Track(MuseumProps.PlantedGrenade(_course, pos));
            return;
        }
        if (wing == 0)
            go = r < 0.4f ? MuseumProps.RopeBarrier(_course, pos) : r < 0.75f ? MuseumProps.LowLaser(_course, pos) : MuseumProps.Statue(_course, pos);
        else if (wing == 1)
            go = r < 0.25f ? MuseumProps.RopeBarrier(_course, pos) : r < 0.5f ? MuseumProps.LowLaser(_course, pos) : r < 0.75f ? MuseumProps.LaserGrid(_course, pos) : MuseumProps.Statue(_course, pos);
        else
            go = r < 0.3f ? MuseumProps.LowLaser(_course, pos) : r < 0.7f ? MuseumProps.LaserGrid(_course, pos) : MuseumProps.Statue(_course, pos);

        if (!go.name.StartsWith("Statue"))
            go.transform.localScale = new Vector3(hazardWidthScale, 1f, 1f);
        Track(go);
    }

    void FindGuardModel()
    {
        var enemy = FindAnyObjectByType<EnemyBase>();
        if (enemy == null) return;
        var anim = enemy.GetComponentInChildren<Animator>(true);
        if (anim == null) return;
        _guardModel = anim.gameObject;
        _guardModelScale = anim.transform.lossyScale.y;
    }

    /// <summary>
    /// Posts a guard beside an exhibit case as the hall streams it in. Treasure cases are always
    /// guarded once guards are introduced; ordinary interactive cases more often in later wings.
    /// </summary>
    void OnGuardableExhibit(Vector3 casePos, bool treasure)
    {
        if (_guardModel == null || _course == null || _player == null) return;
        float d = casePos.z - _origin;
        if (d < 160f) return;
        int wing = WingIndex(d);
        if (!_guardsIntroduced) _guardsIntroduced = true;
        else if (!treasure && Random.value > (wing == 0 ? 0.35f : wing == 1 ? 0.55f : 0.75f)) return;

        // Stand between the case and the outer lane, a step toward the runner.
        float side = Mathf.Sign(casePos.x);
        var guard = SpawnGuard(new Vector3(side * 8.4f, 0f, casePos.z - 1.2f));
        _guardCount++;
        guard.homeX = side * 8.4f;
        if (treasure) guard.fireCooldown = 0.9f;
        // Some guards lob grenades into the runner's lane instead of shooting (never the first one).
        else if (_guardCount > 1 && Random.value < (wing == 0 ? 0.25f : wing == 1 ? 0.35f : 0.45f))
            guard.grenadier = true;
    }

    PatrolGuard SpawnGuard(Vector3 pos)
    {
        var root = RooftopProps.Root("Museum Guard", _course, pos);
        var model = Instantiate(_guardModel, root.transform);
        model.name = "Model";
        foreach (var mb in model.GetComponentsInChildren<MonoBehaviour>(true)) Destroy(mb);
        foreach (var c in model.GetComponentsInChildren<Collider>(true)) Destroy(c);
        foreach (var rb in model.GetComponentsInChildren<Rigidbody>(true)) Destroy(rb);
        foreach (var cc in model.GetComponentsInChildren<CharacterController>(true)) Destroy(cc);
        model.SetActive(true);
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one * _guardModelScale;
        model.transform.localPosition = Vector3.zero;

        // Stand the model on the floor.
        var renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            model.transform.position += Vector3.up * (pos.y - b.min.y);
        }

        var lamp = BuildGuardLamp(root.transform, out var beam);

        var grab = root.AddComponent<HealthDecreaseObstacle>();
        grab.damage = 4;
        grab.vibeDamage = 8;
        grab.displayName = "Museum Guard's Bullet";

        var guard = root.AddComponent<PatrolGuard>();
        guard.animator = model.GetComponent<Animator>();
        guard.model = model.transform;
        guard.ring = beam;
        guard.lamp = lamp;
        Track(root);
        return guard;
    }

    static Mesh _beamMesh;

    /// <summary>
    /// A hanging museum lamp above the guard: cord, brass shade, warm bulb, a soft light beam and a
    /// real spot light that pools yellow light on the floor around his post. Returns the spot light.
    /// </summary>
    Light BuildGuardLamp(Transform parent, out Renderer beam)
    {
        const float bulbY = 6.4f;
        var lampRoot = RooftopProps.Root("Guard Lamp", parent, parent.position).transform;
        lampRoot.localPosition = Vector3.zero;
        var brass = RooftopProps.Mat(new Color(0.55f, 0.42f, 0.18f), 0.15f);
        var dark = RooftopProps.Mat(new Color(0.06f, 0.06f, 0.07f));

        RooftopProps.Part(PrimitiveType.Cylinder, lampRoot, new Vector3(0f, (bulbY + MuseumHall.CeilingY) * 0.5f + 0.3f, 0f),
                          new Vector3(0.04f, (MuseumHall.CeilingY - bulbY) * 0.5f, 0.04f), dark, false);
        RooftopProps.Part(PrimitiveType.Cylinder, lampRoot, new Vector3(0f, bulbY + 0.35f, 0f), new Vector3(0.9f, 0.14f, 0.9f), brass, false);
        RooftopProps.Part(PrimitiveType.Cylinder, lampRoot, new Vector3(0f, bulbY + 0.55f, 0f), new Vector3(0.35f, 0.1f, 0.35f), brass, false);
        RooftopProps.Part(PrimitiveType.Sphere, lampRoot, new Vector3(0f, bulbY + 0.12f, 0f), Vector3.one * 0.42f,
                          RooftopProps.Glow(new Color(1f, 0.9f, 0.55f, 1f)), false);
        RooftopProps.Part(PrimitiveType.Sphere, lampRoot, new Vector3(0f, bulbY + 0.12f, 0f), Vector3.one * 0.9f,
                          RooftopProps.Glow(new Color(1f, 0.85f, 0.35f, 0.25f)), false);

        // Soft beam of light from the bulb down to the floor.
        if (_beamMesh == null) _beamMesh = BuildConeMesh(32, 0.3f, 2.3f, bulbY);
        var beamGo = new GameObject("Beam");
        beamGo.transform.SetParent(lampRoot, false);
        beamGo.transform.localPosition = new Vector3(0f, bulbY, 0f);
        beamGo.AddComponent<MeshFilter>().sharedMesh = _beamMesh;
        beam = beamGo.AddComponent<MeshRenderer>();
        beam.sharedMaterial = RooftopProps.Glow(new Color(1f, 0.85f, 0.35f, 0.08f));
        beam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var spot = new GameObject("Spot").AddComponent<Light>();
        spot.transform.SetParent(lampRoot, false);
        spot.transform.localPosition = new Vector3(0f, bulbY, 0f);
        spot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        spot.type = LightType.Spot;
        spot.color = new Color(1f, 0.82f, 0.45f);
        spot.spotAngle = 52f;
        spot.innerSpotAngle = 25f;
        spot.range = bulbY + 3f;
        spot.intensity = 14f;
        spot.shadows = LightShadows.None;
        return spot;
    }

    /// <summary>Open, double-sided cone: radius r0 at the top (y = 0) down to r1 at y = -height.</summary>
    static Mesh BuildConeMesh(int segments, float r0, float r1, float height)
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();
        for (int i = 0; i <= segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            verts.Add(dir * r0);
            verts.Add(dir * r1 + Vector3.down * height);
        }
        for (int i = 0; i < segments; i++)
        {
            int a = i * 2, b = a + 1, c = a + 2, d = a + 3;
            tris.AddRange(new[] { a, c, b, b, c, d });   // outside
            tris.AddRange(new[] { a, b, c, b, d, c });   // inside
        }
        var mesh = new Mesh { name = "GuardBeam" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
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

        // Golden display: plinth glow and a light beam so it reads from far away.
        var display = RooftopProps.Root("ArtifactDisplay", _course, new Vector3(lane, 0f, z));
        RooftopProps.Part(PrimitiveType.Cylinder, display.transform, new Vector3(0f, 0.02f, 0f), new Vector3(3f, 0.02f, 3f),
                          RooftopProps.Glow(new Color(1f, 0.8f, 0.3f, 0.5f)), false);
        RooftopProps.Part(PrimitiveType.Cylinder, display.transform, new Vector3(0f, 4.5f, 0f), new Vector3(1.6f, 4.5f, 1.6f),
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

    int WingIndex(float d) => d < 250f ? 0 : d < 550f ? 1 : 2;

    // ── Scoring and exhibits ──────────────────────────────────────────────────

    public void AddNote() => AddNotes(1);

    void AddNotes(int count)
    {
        Notes += count;
        Score += count * 10;
        SyncRunStats();
    }

    void SyncRunStats()
    {
        RunStats.Notes = Notes;
        RunStats.Score = Score;
    }

    bool OnBeat()
    {
        float phase = Mathf.Repeat((Time.time - _beatStart) * bpm / 60f, 1f);
        return Mathf.Min(phase, 1f - phase) * 60f / bpm < beatWindow;
    }

    public void PlayExhibit(InteractiveExhibit exhibit, PickupBase pickup)
    {
        var mood = exhibit.mood;
        bool gamble = mood == ExhibitMood.Mystery;
        if (gamble) mood = Random.value < 0.65f ? ExhibitMood.Blessing : ExhibitMood.Curse;
        if (mood == ExhibitMood.Curse)
        {
            RunStats.RecordCase(RunStats.CaseResult.Curse);
            PlayCurse(exhibit, pickup, gamble);
            return;
        }
        RunStats.RecordCase(exhibit.kind == ExhibitKind.Treasure ? RunStats.CaseResult.Treasure : RunStats.CaseResult.Boost);

        bool perfect = OnBeat();
        _chain = Time.time - _lastExhibitAt < 6f ? _chain + 1 : 1;
        _lastExhibitAt = Time.time;
        AddNotes(5 * (perfect ? 2 : 1) * _chain);

        string label;
        float pitch;
        switch (exhibit.kind)
        {
            case ExhibitKind.Drum:
                pickup?.SpeedBoost();
                label = "DRUM ROLL! SPEED UP"; pitch = 0.7f; break;
            case ExhibitKind.Horn:
                _enemy?.Stun(1.5f);
                if (_player != null) PatrolGuard.StunNear(_player.position, 60f, 4f);
                label = "FANFARE! GUARDS STUNNED"; pitch = 1.1f; break;
            case ExhibitKind.Treasure:
                // Big reward: a large Vibe heal, an extra shield and banked notes.
                if (pickup != null)
                {
                    pickup.currentHealth = Mathf.Min(pickup.maxHealth, pickup.currentHealth + TreasureHeal);
                    pickup.hitCounter += 1;
                }
                ShopData.AddNotes(40);
                AddNotes(20);
                label = $"TREASURE SMASHED! +{TreasureHeal} VIBE +1 SHIELD +40 NOTES"; pitch = 1.6f; break;
            case ExhibitKind.Violin:
                StartSlowMo(2f);
                label = "VIOLIN SOLO! SLOW-MO"; pitch = 1.5f; break;
            case ExhibitKind.Harp:
                pickup?.HealthIncrease();
                label = "HARP! +VIBE"; pitch = 1.8f; break;
            case ExhibitKind.Gramophone:
                _magnetUntil = Time.time + 6f;
                label = "GRAMOPHONE! NOTE MAGNET"; pitch = 0.9f; break;
            case ExhibitKind.Headset:
                pickup?.Sneak();
                label = "BASS DROP! SNEAK MODE"; pitch = 0.6f; break;
            default:
                AddNotes(10);
                label = "ENCORE! +10 NOTES"; pitch = 1.3f; break;
        }
        if (gamble) label = "IN TUNE!  " + label;
        if (perfect) label = "PERFECT CHORD!  " + label;
        if (_chain > 1) label += $"  x{_chain}";

        Flash(label, perfect ? new Color(1f, 0.85f, 0.3f) : new Color(0.4f, 0.9f, 1f), 1.8f);
        AudioManager.Instance?.PlayPickupSfx(pitch, 0.9f);
    }

    /// <summary>
    /// Red (or off-key purple) cases - a "sour note": a setback that stings but never ends the run on its own.
    /// Also breaks the exhibit chain.
    /// </summary>
    void PlayCurse(InteractiveExhibit exhibit, PickupBase pickup, bool gamble)
    {
        _chain = 0;
        _lastExhibitAt = -10f;
        string label;
        switch (Random.Range(0, 4))
        {
            case 0:
                _enemy?.Surge(3f);
                label = "SOUR NOTE: ALARM BELL! THE GUARD SURGES";
                break;
            case 1:
                int lost = Mathf.Min(Notes, 20);
                Notes -= lost;
                Score = Mathf.Max(0, Score - lost * 10);
                SyncRunStats();
                _magnetUntil = 0f;
                label = $"SOUR NOTE: BROKEN RECORD! -{lost} NOTES";
                break;
            case 2:
                if (pickup != null) pickup.currentHealth = Mathf.Max(5, pickup.currentHealth - 12);
                label = "SOUR NOTE: FEEDBACK SCREECH! -12 VIBE";
                break;
            default:
                if (pickup != null)
                {
                    if (pickup.hitCounter > 0) pickup.hitCounter -= 1;
                    else pickup.currentHealth = Mathf.Max(5, pickup.currentHealth - 8);
                }
                label = "SOUR NOTE: CRACKED CYMBAL! -1 SHIELD";
                break;
        }
        if (gamble) label = "OFF-KEY!  " + label;
        Flash(label, new Color(1f, 0.3f, 0.25f), 1.8f);
        AudioManager.Instance?.PlayObstacleHitSfx();
    }

    void StartSlowMo(float seconds)
    {
        _slowMo = true;
        _slowUntil = Time.unscaledTime + seconds;
        Time.timeScale = 0.6f;
    }

    void UpdateSlowMo()
    {
        if (!_slowMo || Time.unscaledTime < _slowUntil) return;
        _slowMo = false;
        if (Mathf.Approximately(Time.timeScale, 0.6f)) Time.timeScale = 1f;
    }

    void UpdateArtifactCallouts()
    {
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

    void UpdateWing()
    {
        int wing = WingIndex(Distance);
        if (wing == _wing || !GameManager.GameStarted) return;
        _wing = wing;
        string name = _hall != null ? _hall.WingAt(Distance).name : "MUSEUM";
        Flash(name, new Color(1f, 0.85f, 0.55f), 2.5f);
    }

    // ── HUD ──────────────────────────────────────────────────────────────────

    TextMeshProUGUI _distanceText, _wingText, _scoreText, _powerText, _popupText;
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
        var canvasGo = new GameObject("MuseumHUD", typeof(RectTransform));
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
        _wingText = Text(canvasGo.transform, "Wing", new Vector2(0.38f, 0.89f), new Vector2(0.62f, 0.925f), 22, new Color(1f, 0.85f, 0.55f), skin);
        _scoreText = Text(canvasGo.transform, "Score", new Vector2(0.38f, 0.86f), new Vector2(0.62f, 0.89f), 20, new Color(0.35f, 0.85f, 1f), skin);
        _caseText = Text(canvasGo.transform, "Cases", new Vector2(0.38f, 0.83f), new Vector2(0.62f, 0.86f), 18, Color.white, skin);
        _caseText.richText = true;
        _powerText = Text(canvasGo.transform, "Powers", new Vector2(0.75f, 0.705f), new Vector2(0.98f, 0.755f), 20, new Color(0.8f, 0.95f, 1f), skin);

        _popupGroup = new GameObject("CalloutGroup", typeof(RectTransform)).GetComponent<RectTransform>();
        _popupGroup.SetParent(canvasGo.transform, false);
        _popupGroup.anchorMin = new Vector2(0.68f, 0.6f);
        _popupGroup.anchorMax = new Vector2(0.985f, 0.685f);
        _popupGroup.offsetMin = _popupGroup.offsetMax = Vector2.zero;
        _popupPlate = Plate(_popupGroup, "PopupPlate", Vector2.zero, Vector2.one, skin != null ? skin.banner : null, 4f, Color.white);
        _popupText = Text(_popupGroup, "Popup", new Vector2(0.07f, 0.1f), new Vector2(0.93f, 0.9f), 28, Color.white, skin);
        _popupText.enableAutoSizing = true;
        _popupText.fontSizeMin = 14f;
        _popupText.fontSizeMax = 28f;
        _popupText.textWrappingMode = TextWrappingModes.NoWrap;

        var pip = new GameObject("BeatPip", typeof(RectTransform));
        pip.transform.SetParent(canvasGo.transform, false);
        _beatPip = pip.GetComponent<RectTransform>();
        _beatPip.anchorMin = _beatPip.anchorMax = new Vector2(0.5f, 0.06f);
        _beatPip.sizeDelta = new Vector2(24f, 24f);
        _beatPip.localEulerAngles = new Vector3(0f, 0f, 45f);
        _beatImage = pip.AddComponent<Image>();
        _beatImage.raycastTarget = false;
        var hint = Text(canvasGo.transform, "BeatHint", new Vector2(0.3f, 0.085f), new Vector2(0.7f, 0.12f), 16, new Color(0.35f, 0.85f, 1f, 0.85f), skin);
        hint.text = "SMASH CYAN CASES FOR BOOSTS - RED CASES HIT A SOUR NOTE - PURPLE IS A GAMBLE";
        _hint = hint;
        _popupText.text = "";
    }

    TextMeshProUGUI _hint;
    TextMeshProUGUI _caseText;

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
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.characterSpacing = 4f;
        tmp.raycastTarget = false;
        return tmp;
    }

    void UpdateHud()
    {
        bool show = GameManager.GameStarted;
        _statsPlate.enabled = _distanceText.enabled = _wingText.enabled = _scoreText.enabled = _caseText.enabled = show;
        _beatImage.enabled = _hint.enabled = _powerText.enabled = show;
        if (!show)
        {
            _popupPlate.enabled = false;
            _popupText.text = "";
            return;
        }

        _distanceText.text = $"{Distance:0} m";
        _wingText.text = _hall != null ? _hall.WingAt(Distance).name : "MUSEUM";
        _scoreText.text = $"NOTES {Notes}    SCORE {Score:N0}";
        _caseText.text = $"CASES   <color=#5AD9FF>BOOST {RunStats.CaseBoosts}</color>   <color=#FF6A5A>SOUR NOTE {RunStats.CaseCurses}</color>   <color=#E6EEFF>TREASURE {RunStats.CaseTreasures}</color>";

        var powers = new List<string>();
        if (MagnetActive) powers.Add($"MAGNET {Mathf.CeilToInt(_magnetUntil - Time.time)}s");
        if (_slowMo) powers.Add("SLOW-MO");
        if (_enemy != null && _enemy.IsStunned) powers.Add("GUARD STUNNED");
        _powerText.text = string.Join("   ", powers);

        float a = Time.time < _popupUntil ? Mathf.Clamp01((_popupUntil - Time.time) * 3f) : 0f;
        var c = _popupText.color;
        _popupText.color = new Color(c.r, c.g, c.b, a);
        _popupPlate.enabled = a > 0.01f && _popupText.text.Length > 0;
        _popupPlate.color = new Color(1f, 1f, 1f, a);
        float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.time - _popupShownAt) * 6f));
        _popupGroup.anchoredPosition = new Vector2(480f * (1f - k), 0f);

        float phase = Mathf.Repeat((Time.time - _beatStart) * bpm / 60f, 1f);
        float pulse = 1f - Mathf.Clamp01(Mathf.Min(phase, 1f - phase) * 6f);
        _beatPip.sizeDelta = Vector2.one * (20f + pulse * 20f);
        _beatImage.color = Color.Lerp(new Color(0.35f, 0.85f, 1f, 0.35f), new Color(0.35f, 0.9f, 1f, 1f), pulse);
    }
}
