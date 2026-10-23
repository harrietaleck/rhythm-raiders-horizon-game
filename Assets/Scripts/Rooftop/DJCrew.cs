using UnityEngine;

/// <summary>Shared helpers for the rooftop DJ crews.</summary>
static class DJCrew
{
    static float _lastSmokeCall = -100f, _lastTaserCall = -100f;

    public static Transform Player()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        return p != null ? p.transform : null;
    }

    public static float Feet(Transform player)
    {
        var cc = player.GetComponent<CharacterController>();
        return cc != null ? player.position.y - (cc.height * 0.5f - cc.center.y) : player.position.y - 1f;
    }

    public static void CallSmoke()
    {
        if (Time.time - _lastSmokeCall < 40f) return;
        _lastSmokeCall = Time.time;
        Flash("SMOKE MACHINE! KEEP YOUR LANE", new Color(0.8f, 0.85f, 1f), 1.4f);
    }

    public static void CallTaser()
    {
        if (Time.time - _lastTaserCall < 40f) return;
        _lastTaserCall = Time.time;
        Flash("BODYGUARD TASER - JUMP IT OR SWITCH LANE", new Color(0.55f, 0.85f, 1f), 1.8f);
    }

    /// <summary>Callout through whichever level's run controller is active (rooftop or club).</summary>
    static void Flash(string text, Color color, float seconds)
    {
        if (RunDirector.Instance != null) RunDirector.Instance.Flash(text, color, seconds);
        else if (ClubRun.Instance != null) ClubRun.Instance.Flash(text, color, seconds);
    }
}

/// <summary>
/// Side DJ station smoke machine: as the runner approaches it blasts a cloud of fog out across
/// the lanes ahead, hiding what's coming for a couple of seconds. A distraction, not a hit.
/// </summary>
public class DJSmokeCannon : MonoBehaviour
{
    public Transform nozzle;
    public int side = 1;
    [Tooltip("Distance from the centre to the outer lane; the cloud covers the lanes on this side.")]
    public float laneOuter = 4f;
    public Color tint = Color.white;
    [Range(0f, 1f)] public float chance = 0.65f;

    Transform _player;
    bool _done;

    void Start() => _player = DJCrew.Player();

    void Update()
    {
        if (_done || _player == null || !GameManager.GameStarted) return;
        float ahead = transform.position.z - _player.position.z;
        if (ahead > 22f || ahead < 6f) return;
        _done = true;
        if (Random.value > chance) return;

        var floor = transform.position;
        SmokeCloud.Spawn(new Vector3(side * laneOuter * 0.55f, floor.y, floor.z - 3f), new Vector3(laneOuter * 1.75f, 2.4f, 7f), 2.6f, tint,
                         new Vector3(-side * 1.1f, 0f, -0.6f));
        if (nozzle != null)
            SmokeCloud.Spawn(nozzle.position - Vector3.up * 0.4f, new Vector3(0.6f, 0.5f, 0.6f), 1.2f, tint, Vector3.zero);
        AudioManager.Instance?.PlayPickupSfx(0.45f, 0.5f);
        DJCrew.CallSmoke();
    }
}

/// <summary>Centre DJ stage smoke machine: puffs fog over the track at random while the runner closes in.</summary>
public class DJSmokeRandom : MonoBehaviour
{
    public Transform nozzle;
    public Color tint = Color.white;
    [Tooltip("Half-width of the track the smoke can land on.")]
    public float spread = 4.5f;
    float _next;
    Transform _player;

    void Start()
    {
        _player = DJCrew.Player();
        _next = Time.time + Random.Range(0.5f, 2f);
    }

    void Update()
    {
        if (_player == null || !GameManager.GameStarted || Time.time < _next) return;
        float ahead = transform.position.z - _player.position.z;
        if (ahead > 45f || ahead < 4f) return;
        _next = Time.time + Random.Range(1.6f, 3.4f);
        if (Random.value > 0.55f) return;

        var floor = transform.position;
        float x = Random.Range(-spread, spread);
        SmokeCloud.Spawn(new Vector3(x, floor.y, floor.z - Random.Range(5f, 12f)), new Vector3(5.5f, 2.2f, 5f), 1.8f, tint,
                         new Vector3(Random.Range(-0.8f, 0.8f), 0f, -0.5f));
        if (nozzle != null)
            SmokeCloud.Spawn(nozzle.position - Vector3.up * 0.3f, new Vector3(0.5f, 0.4f, 0.5f), 0.8f, tint, Vector3.zero);
        DJCrew.CallSmoke();
    }
}

/// <summary>
/// Bodyguard beside a centre DJ stage. When the runner comes up his side lane he charges a
/// taser (crackling tip = warning) and zaps as they pass: shield + vibe damage and a stumble.
/// Jump as you pass him, or take the other lane.
/// </summary>
public class TaserGuard : MonoBehaviour
{
    public float laneReach = 2.1f;
    public float chargeDistance = 9f;
    public float strikeDistance = 1.4f;
    public float jumpClearance = 0.75f;

    Transform _player, _tip;
    Light _tipLight;
    Renderer _tipGlow;
    HealthDecreaseObstacle _source;
    bool _charging, _struck;
    float _nextArc;

    public void Build(int side)
    {
        _source = gameObject.AddComponent<HealthDecreaseObstacle>();
        _source.damage = 10;
        _source.vibeDamage = 6;
        _source.displayName = "Bodyguard's Taser";

        // Baton in the hand on the track side, pointing forward.
        var baton = new GameObject("Taser").transform;
        baton.SetParent(transform, false);
        baton.localPosition = new Vector3(-side * 0.42f, 1.05f, 0.35f);
        baton.localRotation = Quaternion.Euler(70f, 0f, 0f);
        RooftopProps.Part(PrimitiveType.Cylinder, baton, new Vector3(0f, 0f, 0f), new Vector3(0.06f, 0.28f, 0.06f), RooftopProps.Mat(new Color(0.05f, 0.05f, 0.06f)), false);
        _tip = new GameObject("TaserTip").transform;
        _tip.SetParent(baton, false);
        _tip.localPosition = new Vector3(0f, 0.3f, 0f);
        _tipGlow = RooftopProps.Part(PrimitiveType.Sphere, _tip, Vector3.zero, Vector3.one * 0.09f, RooftopProps.Glow(new Color(0.55f, 0.85f, 1f, 1f)), false).GetComponent<Renderer>();
        _tipLight = _tip.gameObject.AddComponent<Light>();
        _tipLight.type = LightType.Point;
        _tipLight.color = new Color(0.55f, 0.85f, 1f);
        _tipLight.range = 3f;
        _tipLight.intensity = 0f;
        _tipLight.shadows = LightShadows.None;
    }

    void Start() => _player = DJCrew.Player();

    void Update()
    {
        if (_player == null || _tip == null) return;
        float dx = Mathf.Abs(_player.position.x - transform.position.x);
        float ahead = transform.position.z - _player.position.z;
        bool inLane = dx < laneReach;

        if (ahead < -3f) { _charging = false; _tipLight.intensity = 0f; return; }
        if (!GameManager.GameStarted) return;

        if (!_charging && !_struck && inLane && ahead < chargeDistance && ahead > strikeDistance)
        {
            _charging = true;
            DJCrew.CallTaser();
        }

        if (_charging)
        {
            _tipLight.intensity = 2f + Mathf.PingPong(Time.time * 40f, 4f);
            _tipGlow.enabled = Mathf.Sin(Time.time * 60f) > -0.3f;
            if (Time.time >= _nextArc)
            {
                _nextArc = Time.time + 0.06f;
                var arc = new GameObject("Crackle").AddComponent<FxArc>();
                arc.transform.SetParent(_tip, false);
                var dir = Random.onUnitSphere * 0.35f;
                arc.Set(Vector3.zero, dir, new Color(0.7f, 0.9f, 1f), 0.08f, 0.06f, local: true);
            }

            if (ahead <= strikeDistance)
            {
                _charging = false;
                _struck = true;
                Strike(inLane);
            }
        }
        else
        {
            _tipLight.intensity = Mathf.MoveTowards(_tipLight.intensity, 0f, Time.deltaTime * 20f);
            _tipGlow.enabled = true;
        }
    }

    void Strike(bool inLane)
    {
        bool grounded = DJCrew.Feet(_player) - transform.position.y < jumpClearance;
        var target = _player.position + Vector3.up * 0.2f;
        var bolt = new GameObject("Zap").AddComponent<FxArc>();
        bolt.Set(_tip.position, inLane ? target : _tip.position + transform.forward * 1.6f, new Color(0.7f, 0.9f, 1f), 0.25f, 0.18f);
        AudioManager.Instance?.PlayGuardShotSfx();

        if (!inLane || !grounded) return;   // switched lanes or jumped it
        var pickup = _player.GetComponent<PickupBase>();
        if (pickup == null || pickup.isSneaking) return;
        ObstacleImpactFX.Play(_tip.gameObject, target, _player);
        pickup.HitByBlast(_source, target);
    }
}
