using UnityEngine;

/// <summary>
/// Level 2 grenades. Shared model builder, the explosion, and the blast check.
///  - Thrown: grenadier guards lob one into the runner's lane. It lands, a red blast ring
///    marks the danger zone and fills as the fuse burns, then it goes off. Change lane.
///  - Planted: sits on the carpet with a blinking red light. Dodge it or jump it; touch it and it blows.
/// A blast costs a shield, some Vibe, and knocks the runner down (the guard closes in).
/// </summary>
public static class Grenade
{
    public const float BlastRadius = 2.6f;

    static readonly Color Olive = new Color(0.22f, 0.27f, 0.14f);
    static readonly Color Steel = new Color(0.45f, 0.46f, 0.48f);

    /// <summary>Builds a grenade model under parent; returns the blinking LED renderer.</summary>
    public static Renderer Build(Transform parent, Vector3 localPos, float scale = 1f)
    {
        var root = new GameObject("Grenade").transform;
        root.SetParent(parent, false);
        root.localPosition = localPos;
        root.localScale = Vector3.one * scale;
        var body = MuseumHall.Lit(Olive, 0.35f, 0.1f);
        var band = MuseumHall.Lit(new Color(0.12f, 0.14f, 0.08f), 0.3f, 0.1f);
        var steel = MuseumHall.Lit(Steel, 0.7f, 0.8f);
        RooftopProps.Part(PrimitiveType.Sphere, root, new Vector3(0f, 0.17f, 0f), new Vector3(0.24f, 0.3f, 0.24f), body, false);
        RooftopProps.Part(PrimitiveType.Cylinder, root, new Vector3(0f, 0.13f, 0f), new Vector3(0.245f, 0.008f, 0.245f), band, false);
        RooftopProps.Part(PrimitiveType.Cylinder, root, new Vector3(0f, 0.21f, 0f), new Vector3(0.235f, 0.008f, 0.235f), band, false);
        RooftopProps.Part(PrimitiveType.Cylinder, root, new Vector3(0f, 0.335f, 0f), new Vector3(0.09f, 0.03f, 0.09f), steel, false);
        RooftopProps.Part(PrimitiveType.Cube, root, new Vector3(0.06f, 0.27f, 0f), new Vector3(0.025f, 0.16f, 0.05f), steel, false, new Vector3(0f, 0f, -18f));
        RooftopProps.Part(PrimitiveType.Cylinder, root, new Vector3(-0.07f, 0.35f, 0f), new Vector3(0.07f, 0.004f, 0.07f), steel, false, new Vector3(0f, 0f, 90f));
        var led = RooftopProps.Part(PrimitiveType.Sphere, root, new Vector3(0f, 0.37f, 0f), Vector3.one * 0.05f,
                                    RooftopProps.Glow(new Color(1f, 0.1f, 0.05f, 1f)), false);
        return led.GetComponent<Renderer>();
    }

    /// <summary>Explosion visuals and sound at a floor point.</summary>
    public static void Explode(Vector3 pos)
    {
        ObstacleImpactFX.Explosion(pos, BlastRadius);
        SmokeCloud.Spawn(pos, new Vector3(3f, 1.6f, 3f), 0.4f, new Color(0.55f, 0.55f, 0.52f, 0.8f), Vector3.up * 0.3f);
        AudioManager.Instance?.PlayExplosionSfx();
    }

    /// <summary>Damages the runner if they are inside the blast.</summary>
    public static bool BlastPlayer(Vector3 pos, HealthDecreaseObstacle source)
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go == null) return false;
        var p = go.transform.position;
        var flat = new Vector2(p.x - pos.x, p.z - pos.z);
        if (flat.magnitude > BlastRadius || p.y - pos.y > 3f) return false;
        var pickup = go.GetComponent<PickupBase>();
        if (pickup == null) return false;
        pickup.HitByBlast(source, pos);
        return true;
    }
}

/// <summary>A grenade lobbed by a guard: arcs to its landing spot, shows its blast ring, then explodes.</summary>
public class GuardGrenade : MonoBehaviour
{
    Vector3 _from, _to;
    float _flight, _fuse, _age, _arc;
    bool _landed;
    Transform _model;
    Renderer _led;
    BlastRing _ring;
    HealthDecreaseObstacle _source;
    ParticleSystem _smoke;

    public static GuardGrenade Throw(Vector3 from, Vector3 landAt, float flight, float fuse)
    {
        var go = new GameObject("Guard's Grenade");
        go.transform.position = from;
        var g = go.AddComponent<GuardGrenade>();
        g._from = from;
        g._to = landAt;
        g._flight = flight;
        g._fuse = fuse;
        g._arc = Mathf.Clamp(Vector3.Distance(from, landAt) * 0.22f, 1.5f, 4.5f);
        g._source = go.AddComponent<HealthDecreaseObstacle>();
        g._source.damage = 10;
        g._source.vibeDamage = 10;
        g._source.displayName = "Guard's Grenade";
        g._led = Grenade.Build(go.transform, new Vector3(0f, -0.17f, 0f));
        g._model = go.transform.GetChild(0);

        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.25f;
        trail.startWidth = 0.12f;
        trail.endWidth = 0f;
        trail.material = RooftopProps.Glow(new Color(1f, 0.85f, 0.6f, 0.35f));
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        g._smoke = SmokeCloud.Trail(go.transform, 0.7f, 90f);   // smoke streams behind it in flight
        return g;
    }

    void Update()
    {
        _age += Time.deltaTime;
        if (!_landed)
        {
            float t = Mathf.Clamp01(_age / _flight);
            transform.position = Vector3.Lerp(_from, _to, t) + Vector3.up * (_arc * 4f * t * (1f - t));
            _model.Rotate(new Vector3(720f, 90f, 0f) * Time.deltaTime, Space.Self);
            if (t >= 1f) Land();
            return;
        }

        float f = Mathf.Clamp01((_age - _flight) / _fuse);
        // Skid to a stop, beep faster as the fuse runs out.
        transform.position = Vector3.MoveTowards(transform.position, _to + new Vector3(0f, 0f, 0.35f), Time.deltaTime * 1.5f);
        if (_led != null) _led.enabled = Mathf.Sin(Time.time * Mathf.Lerp(12f, 45f, f)) > 0f;
        if (_ring != null) _ring.Fill(f);
        if (f >= 1f) Detonate();
    }

    void Land()
    {
        _landed = true;
        _model.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 80f);
        _model.localPosition = new Vector3(0f, 0.12f, 0f);
        var trail = GetComponent<TrailRenderer>();
        if (trail != null) trail.emitting = false;
        _ring = BlastRing.Create(_to, Grenade.BlastRadius);
        if (_smoke != null)
        {
            // Lying on the floor the fuse keeps smoking, thicker as it burns down.
            var em = _smoke.emission;
            em.rateOverTime = 45f;
            var main = _smoke.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
        }
        ObstacleImpactFX.Thud(_to);
    }

    void Detonate()
    {
        SmokeCloud.Release(_smoke);
        Grenade.Explode(transform.position);
        if (GameManager.GameStarted) Grenade.BlastPlayer(transform.position, _source);
        if (_ring != null) Destroy(_ring.gameObject);
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (_ring != null) Destroy(_ring.gameObject);
    }
}

/// <summary>Red danger circle on the floor: outline at the blast radius, inner disc grows with the fuse.</summary>
public class BlastRing : MonoBehaviour
{
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    LineRenderer _edge;
    Transform _fill;
    Renderer _fillR;
    MaterialPropertyBlock _block;
    float _radius;

    public static BlastRing Create(Vector3 centre, float radius)
    {
        var go = new GameObject("Blast Ring");
        go.transform.position = new Vector3(centre.x, centre.y + 0.03f, centre.z);
        var r = go.AddComponent<BlastRing>();
        r._radius = radius;
        r._edge = go.AddComponent<LineRenderer>();
        r._edge.useWorldSpace = false;
        r._edge.loop = true;
        r._edge.positionCount = 48;
        for (int i = 0; i < 48; i++)
        {
            float a = i / 48f * Mathf.PI * 2f;
            r._edge.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
        }
        r._edge.widthMultiplier = 0.12f;
        r._edge.material = RooftopProps.Glow(new Color(1f, 0.12f, 0.06f, 0.9f));
        r._edge.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var fill = RooftopProps.Part(PrimitiveType.Cylinder, go.transform, Vector3.zero, new Vector3(0.1f, 0.005f, 0.1f),
                                     RooftopProps.Glow(new Color(1f, 0.1f, 0.05f, 0.3f)), false);
        r._fill = fill.transform;
        r._fillR = fill.GetComponent<Renderer>();
        r._block = new MaterialPropertyBlock();
        return r;
    }

    public void Fill(float t)
    {
        float d = Mathf.Lerp(0.2f, _radius * 2f, t);
        _fill.localScale = new Vector3(d, 0.005f, d);
        float blink = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.Lerp(10f, 40f, t));
        _edge.widthMultiplier = 0.09f + 0.06f * blink;
        _block.SetColor(BaseColor, new Color(1f, 0.1f, 0.05f, 0.18f + 0.22f * t));
        _fillR.SetPropertyBlock(_block);
    }
}

/// <summary>Blinks a planted grenade's light, faster as the runner closes in; announces the first one.</summary>
public class PlantedGrenadeBlink : MonoBehaviour
{
    public static bool IntroShown;
    public Renderer led;
    public Light warningLight;
    Transform _player;

    void Start()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) _player = go.transform;
    }

    void OnDisable()
    {
        if (warningLight != null) warningLight.enabled = false;
    }

    void Update()
    {
        if (led == null) return;
        float dz = _player != null ? transform.position.z - _player.position.z : 99f;
        float rate = dz < 0f ? 4f : Mathf.Lerp(30f, 5f, Mathf.Clamp01(dz / 30f));
        led.enabled = Mathf.Sin(Time.time * rate) > -0.2f;
        if (warningLight != null) warningLight.enabled = led.enabled;

        if (!IntroShown && GameManager.GameStarted && dz > 0f && dz < 38f)
        {
            IntroShown = true;
            MuseumRun.Instance?.Flash("PLANTED GRENADE - DODGE IT OR JUMP IT!", new Color(1f, 0.45f, 0.25f), 1.8f);
        }
    }
}
