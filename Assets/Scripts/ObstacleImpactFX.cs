using UnityEngine;

/// <summary>
/// Plays a short, obstacle-specific reaction when the player bumps into it:
/// lasers spark and flicker, speakers blast sound rings, statues chip stone, water tanks splash,
/// signs short out, ropes bounce, drums boom... Every obstacle also wobbles from the hit.
/// Everything is built from primitives and cleans itself up.
/// </summary>
public static class ObstacleImpactFX
{
    enum Kind { Explosive, Laser, Speaker, Stone, Water, Electric, Metal, Cloth, Rope, Drum, Generic }

    public static void Play(GameObject obstacle, Vector3 point, Transform victim = null)
    {
        if (obstacle == null) return;
        string n = ObstacleName(obstacle).ToLowerInvariant();
        var kind = Classify(n);

        if (kind == Kind.Explosive)
        {
            // Planted grenade: it goes off right where it sat and is gone.
            Grenade.Explode(obstacle.transform.position);
            foreach (var r in obstacle.GetComponentsInChildren<Renderer>()) r.enabled = false;
            var blink = obstacle.GetComponent<PlantedGrenadeBlink>();
            if (blink != null) blink.enabled = false;
            return;
        }

        var wobble = obstacle.GetComponent<ObstacleWobble>() ?? obstacle.AddComponent<ObstacleWobble>();
        wobble.Hit(kind == Kind.Stone || kind == Kind.Metal ? 0.06f : kind == Kind.Laser ? 0.03f : 0.12f);

        var root = new GameObject("ImpactFX_" + kind);
        root.transform.position = point;
        Object.Destroy(root, 2f);

        switch (kind)
        {
            case Kind.Laser:
            {
                // Electric shock: arcs crawl along the beam and around the runner,
                // hot sparks spray off the contact point. No soft "bubble" debris.
                Color beam = LaserTint(obstacle);
                Color bolt = Color.Lerp(beam, new Color(0.75f, 0.9f, 1f), 0.55f);
                var b = BoundsOf(obstacle, point);
                float halfW = Mathf.Min(b.extents.x, 2.2f);
                ArcBetween(root, point, new Vector3(point.x - halfW, point.y, point.z), bolt, 0.3f);
                ArcBetween(root, point, new Vector3(point.x + halfW, point.y, point.z), bolt, 0.3f);
                for (int i = 0; i < 4; i++)
                    ArcOut(root, point, Random.Range(0.5f, 1.1f), Color.white, 0.22f);
                if (victim != null) ShockVictim(victim, bolt);
                Sparks(root, 22, new Color(1f, 0.95f, 0.7f), 7f, 2.5f, 0.35f);
                Sparks(root, 10, beam, 5f, 2f, 0.3f);
                Flash(root, bolt, 14f, 7f, 0.14f);
                obstacle.AddComponent<ObstacleFlicker>().duration = 0.5f;
                break;
            }
            case Kind.Speaker:
                for (int i = 0; i < 3; i++) Ring(root, new Color(0.35f, 0.85f, 1f), 0.4f + i * 0.12f, 6f + i * 2f, i * 0.08f);
                Burst(root, 8, new Color(0.6f, 0.9f, 1f), 0.08f, 0.14f, 3f, 2f, 0.6f, gravity: 0f);
                Flash(root, new Color(0.35f, 0.85f, 1f), 8f, 7f, 0.2f);
                break;
            case Kind.Stone:
                Burst(root, 18, new Color(0.62f, 0.6f, 0.56f), 0.07f, 0.18f, 4f, 4f, 0.9f, gravity: 14f, cubes: true);
                Burst(root, 8, new Color(0.75f, 0.73f, 0.7f, 0.5f), 0.4f, 0.7f, 1f, 1.2f, 0.8f, gravity: -0.5f);
                break;
            case Kind.Water:
                Burst(root, 30, new Color(0.45f, 0.75f, 1f, 0.85f), 0.06f, 0.14f, 4f, 5f, 0.8f, gravity: 14f);
                Ring(root, new Color(0.5f, 0.8f, 1f), 0.5f, 4f, 0f, flat: true);
                break;
            case Kind.Electric:
                for (int i = 0; i < 4; i++)
                    ArcOut(root, point, Random.Range(0.5f, 1.2f), new Color(0.75f, 0.9f, 1f), 0.25f);
                if (victim != null) ShockVictim(victim, new Color(0.75f, 0.9f, 1f));
                Sparks(root, 24, new Color(1f, 0.95f, 0.5f), 6f, 4f, 0.45f);
                Flash(root, new Color(0.6f, 0.8f, 1f), 12f, 7f, 0.12f);
                obstacle.AddComponent<ObstacleFlicker>().duration = 0.8f;
                break;
            case Kind.Metal:
                Burst(root, 16, new Color(1f, 0.7f, 0.3f), 0.04f, 0.08f, 6f, 3f, 0.4f, gravity: 12f);
                Burst(root, 5, new Color(0.8f, 0.8f, 0.8f, 0.45f), 0.35f, 0.6f, 1f, 1.5f, 0.9f, gravity: -0.4f);
                Flash(root, new Color(1f, 0.7f, 0.35f), 6f, 5f, 0.1f);
                break;
            case Kind.Cloth:
                Burst(root, 10, new Color(0.9f, 0.4f, 0.5f), 0.18f, 0.3f, 2.5f, 2.5f, 1.1f, gravity: 2.5f, cubes: true, flat: true);
                Burst(root, 8, new Color(0.4f, 0.7f, 1f), 0.18f, 0.3f, 2.5f, 2.5f, 1.1f, gravity: 2.5f, cubes: true, flat: true);
                break;
            case Kind.Rope:
                Burst(root, 10, new Color(0.7f, 0.08f, 0.12f), 0.05f, 0.1f, 2.5f, 2f, 0.6f, gravity: 8f);
                Burst(root, 6, new Color(0.85f, 0.78f, 0.7f, 0.45f), 0.3f, 0.5f, 1f, 1f, 0.7f, gravity: -0.3f);
                break;
            case Kind.Drum:
                for (int i = 0; i < 2; i++) Ring(root, new Color(1f, 0.75f, 0.35f), 0.5f + i * 0.15f, 5f + i * 2f, i * 0.1f);
                Burst(root, 10, new Color(1f, 0.8f, 0.4f), 0.06f, 0.12f, 4f, 3f, 0.5f, gravity: 9f);
                break;
            default:
                Burst(root, 14, new Color(0.8f, 0.78f, 0.75f), 0.06f, 0.14f, 4f, 3f, 0.6f, gravity: 10f, cubes: true);
                Burst(root, 6, new Color(0.8f, 0.8f, 0.8f, 0.45f), 0.35f, 0.6f, 1f, 1.2f, 0.8f, gravity: -0.4f);
                break;
        }
    }

    static string ObstacleName(GameObject go)
    {
        var h = go.GetComponent<HealthDecreaseObstacle>();
        return h != null ? h.DisplayName : go.name;
    }

    static Kind Classify(string n)
    {
        if (n.Contains("grenade")) return Kind.Explosive;
        if (n.Contains("taser")) return Kind.Electric;
        if (n.Contains("disco")) return Kind.Metal;
        if (n.Contains("booth")) return Kind.Speaker;
        if (n.Contains("laser")) return Kind.Laser;
        if (n.Contains("speaker") || n.Contains("bass")) return Kind.Speaker;
        if (n.Contains("statue") || n.Contains("stone") || n.Contains("pillar")) return Kind.Stone;
        if (n.Contains("water") || n.Contains("tank")) return Kind.Water;
        if (n.Contains("neon") || n.Contains("sign") || n.Contains("rig") || n.Contains("light")) return Kind.Electric;
        if (n.Contains("ac unit") || n.Contains("crane") || n.Contains("hook") || n.Contains("metal") || n.Contains("steel")) return Kind.Metal;
        if (n.Contains("laundry") || n.Contains("cloth") || n.Contains("curtain")) return Kind.Cloth;
        if (n.Contains("rope")) return Kind.Rope;
        if (n.Contains("drum")) return Kind.Drum;
        return Kind.Generic;
    }

    /// <summary>Grenade blast: white flash, fireball, shock ring, sparks, debris, rising smoke, scorch mark.</summary>
    public static void Explosion(Vector3 pos, float radius)
    {
        var floor = new Vector3(pos.x, pos.y, pos.z);
        var root = new GameObject("Explosion");
        root.transform.position = floor + Vector3.up * 0.4f;
        Object.Destroy(root, 3f);

        Flash(root, new Color(1f, 0.6f, 0.25f), 35f, radius * 5f, 0.4f);

        // Core flash.
        Puff(root, Vector3.zero, new Color(1f, 0.95f, 0.75f, 0.95f), 0.4f, radius * 1.1f, 0.14f, Vector3.zero);
        // Fireball lobes.
        for (int i = 0; i < 9; i++)
        {
            var off = Random.insideUnitSphere * radius * 0.3f; off.y = Mathf.Abs(off.y) * 0.8f;
            var col = Color.Lerp(new Color(1f, 0.75f, 0.2f, 0.9f), new Color(1f, 0.28f, 0.05f, 0.85f), Random.value);
            Puff(root, off, col, 0.3f, Random.Range(0.9f, 1.5f) * radius * 0.7f, Random.Range(0.3f, 0.5f), Vector3.up * 1.2f + off * 1.5f);
        }
        // Smoke.
        for (int i = 0; i < 10; i++)
        {
            var off = Random.insideUnitSphere * radius * 0.35f; off.y = Mathf.Abs(off.y);
            float g = Random.Range(0.12f, 0.22f);
            Puff(root, off, new Color(g, g, g, 0.6f), 0.6f, Random.Range(1.2f, 2f) * radius * 0.6f, Random.Range(1.2f, 1.9f),
                 Vector3.up * Random.Range(1.2f, 2.2f) + off * 0.6f, delay: Random.Range(0.05f, 0.18f));
        }
        Ring(root, new Color(1f, 0.8f, 0.55f), 0.35f, radius * 2.4f, 0f, flat: true);
        Sparks(root, 26, new Color(1f, 0.85f, 0.45f), 10f, 4f, 0.6f);
        Burst(root, 14, new Color(0.15f, 0.15f, 0.14f), 0.06f, 0.16f, 7f, 5f, 1f, gravity: 16f, cubes: true);

        // Scorch mark left on the floor.
        var scorch = RooftopProps.Part(PrimitiveType.Cylinder, null, Vector3.zero, new Vector3(radius * 1.1f, 0.004f, radius * 1.1f),
                                       RooftopProps.Glow(new Color(0.03f, 0.03f, 0.03f, 0.55f)), false);
        scorch.transform.position = floor + Vector3.up * 0.02f;
        scorch.name = "Scorch";
        var fade = scorch.AddComponent<FxPuff>();
        fade.Set(scorch.transform.localScale.x, scorch.transform.localScale.x, 5f, Vector3.zero, flat: true, fadeStart: 0.6f);
    }

    /// <summary>Small dust kick where a thrown grenade lands.</summary>
    public static void Thud(Vector3 pos)
    {
        var root = new GameObject("GrenadeThud");
        root.transform.position = pos + Vector3.up * 0.1f;
        Object.Destroy(root, 1.5f);
        Burst(root, 8, new Color(0.55f, 0.5f, 0.45f, 0.5f), 0.12f, 0.25f, 1.5f, 1f, 0.5f, gravity: 1f);
    }

    static void Puff(GameObject root, Vector3 offset, Color color, float startSize, float endSize, float life, Vector3 velocity, float delay = 0f)
    {
        var p = RooftopProps.Part(PrimitiveType.Sphere, root.transform, offset, Vector3.one * startSize, RooftopProps.Glow(color), false);
        var fx = p.AddComponent<FxPuff>();
        fx.Set(startSize, endSize, life, velocity);
        if (delay > 0f)
        {
            p.SetActive(false);
            root.AddComponent<FxDelayActivate>().Set(p, delay);
        }
    }

    // ── Building blocks ──────────────────────────────────────────────────────

    static Color LaserTint(GameObject obstacle)
    {
        string n = ObstacleName(obstacle).ToLowerInvariant();
        if (n.Contains("fence")) return new Color(1f, 0.3f, 0.85f);   // club magenta
        return new Color(1f, 0.2f, 0.12f);                            // museum / sweep red
    }

    static Bounds BoundsOf(GameObject go, Vector3 fallback)
    {
        var col = go.GetComponent<Collider>();
        if (col != null) return col.bounds;
        var r = go.GetComponentInChildren<Renderer>();
        return r != null ? r.bounds : new Bounds(fallback, Vector3.one);
    }

    /// <summary>Jagged lightning between two points that re-strikes for its lifetime.</summary>
    static void ArcBetween(GameObject root, Vector3 a, Vector3 b, Color color, float life)
    {
        var go = new GameObject("Arc");
        go.transform.SetParent(root.transform, false);
        var arc = go.AddComponent<FxArc>();
        arc.Set(a, b, color, life, Vector3.Distance(a, b) * 0.12f + 0.05f);
    }

    static void ArcOut(GameObject root, Vector3 from, float length, Color color, float life)
    {
        var dir = Random.onUnitSphere;
        dir.y = Mathf.Abs(dir.y) * 0.8f;
        ArcBetween(root, from, from + dir.normalized * length, color, life * Random.Range(0.7f, 1.1f));
    }

    /// <summary>Short arcs crackle over the runner's body, following them as they fall.</summary>
    static void ShockVictim(Transform victim, Color color)
    {
        var host = new GameObject("ShockArcs");
        host.transform.SetParent(victim, false);
        host.transform.localPosition = Vector3.zero;
        Object.Destroy(host, 0.5f);
        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject("Arc");
            go.transform.SetParent(host.transform, false);
            var arc = go.AddComponent<FxArc>();
            float y0 = Random.Range(0.3f, 1.5f);
            var a = new Vector3(Random.Range(-0.3f, 0.3f), y0, Random.Range(-0.25f, 0.25f));
            var b = new Vector3(Random.Range(-0.35f, 0.35f), y0 + Random.Range(-0.6f, 0.6f), Random.Range(-0.25f, 0.25f));
            arc.Set(a, b, color, Random.Range(0.25f, 0.45f), 0.08f, local: true);
        }
    }

    /// <summary>Thin, hot spark streaks that line up with their flight and fall.</summary>
    static void Sparks(GameObject root, int count, Color color, float speed, float up, float life)
    {
        var mat = RooftopProps.Glow(color);
        for (int i = 0; i < count; i++)
        {
            var p = RooftopProps.Part(PrimitiveType.Cube, root.transform, Vector3.zero,
                                      new Vector3(0.025f, 0.025f, Random.Range(0.15f, 0.32f)), mat, false, Vector3.zero);
            var dir = Random.insideUnitSphere;
            dir.y = Mathf.Abs(dir.y);
            var fx = p.AddComponent<FxParticle>();
            fx.velocity = dir.normalized * Random.Range(speed * 0.5f, speed) + Vector3.up * Random.Range(0f, up);
            fx.gravity = 12f;
            fx.life = life * Random.Range(0.6f, 1.2f);
            fx.alignToVelocity = true;
        }
    }

    static void Burst(GameObject root, int count, Color color, float minSize, float maxSize, float speed, float up, float life,
                      float gravity, bool cubes = false, bool flat = false)
    {
        var mat = RooftopProps.Glow(color);
        for (int i = 0; i < count; i++)
        {
            float s = Random.Range(minSize, maxSize);
            var size = flat ? new Vector3(s, s * 0.08f, s) : Vector3.one * s;
            var p = RooftopProps.Part(cubes ? PrimitiveType.Cube : PrimitiveType.Sphere, root.transform,
                                      Random.insideUnitSphere * 0.2f, size, mat, false, Random.rotation.eulerAngles);
            var dir = Random.insideUnitSphere;
            dir.y = Mathf.Abs(dir.y);
            var fx = p.AddComponent<FxParticle>();
            fx.velocity = dir.normalized * Random.Range(speed * 0.4f, speed) + Vector3.up * Random.Range(0f, up);
            fx.gravity = gravity;
            fx.life = life * Random.Range(0.7f, 1.2f);
            fx.spin = cubes || flat ? Random.insideUnitSphere * 500f : Vector3.zero;
        }
    }

    static void Ring(GameObject root, Color color, float life, float grow, float delay, bool flat = false)
    {
        var r = RooftopProps.Part(PrimitiveType.Cylinder, root.transform, Vector3.zero, new Vector3(0.4f, 0.01f, 0.4f),
                                  RooftopProps.Glow(new Color(color.r, color.g, color.b, 0.5f)), false,
                                  flat ? Vector3.zero : new Vector3(90f, 0f, 0f));
        var fx = r.AddComponent<FxRing>();
        fx.life = life;
        fx.grow = grow;
        fx.delay = delay;
        r.SetActive(delay <= 0f);
        if (delay > 0f) root.AddComponent<FxDelayActivate>().Set(r, delay);
    }

    static void Flash(GameObject root, Color color, float intensity, float range, float life)
    {
        var l = root.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        root.AddComponent<FxLightFade>().life = life;
    }
}

/// <summary>Moving, fading debris piece.</summary>
public class FxParticle : MonoBehaviour
{
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    public Vector3 velocity;
    public Vector3 spin;
    public float gravity = 9f;
    public float life = 0.6f;
    public bool alignToVelocity;
    float _age;
    Renderer _r;
    MaterialPropertyBlock _block;
    Color _c;

    void Start()
    {
        _r = GetComponent<Renderer>();
        _block = new MaterialPropertyBlock();
        _c = _r != null ? _r.sharedMaterial.GetColor(BaseColor) : Color.white;
        if (alignToVelocity && velocity.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(velocity);
    }

    void Update()
    {
        _age += Time.deltaTime;
        if (_age >= life) { Destroy(gameObject); return; }
        velocity += Vector3.down * gravity * Time.deltaTime;
        var p = transform.position + velocity * Time.deltaTime;
        if (p.y < 0.03f) { p.y = 0.03f; velocity = new Vector3(velocity.x * 0.5f, -velocity.y * 0.3f, velocity.z * 0.5f); }
        transform.position = p;
        if (spin != Vector3.zero) transform.Rotate(spin * Time.deltaTime, Space.Self);
        if (alignToVelocity && velocity.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(velocity);
        if (_r == null) return;
        float k = _age / life;
        _block.SetColor(BaseColor, new Color(_c.r, _c.g, _c.b, _c.a * (1f - k * k)));
        _r.SetPropertyBlock(_block);
    }
}

/// <summary>Expanding, fading ring (sound wave / splash).</summary>
public class FxRing : MonoBehaviour
{
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    public float life = 0.5f;
    public float grow = 6f;
    public float delay;
    float _age;
    Renderer _r;
    MaterialPropertyBlock _block;
    Color _c;

    void Start()
    {
        _r = GetComponent<Renderer>();
        _block = new MaterialPropertyBlock();
        _c = _r.sharedMaterial.GetColor(BaseColor);
    }

    void Update()
    {
        _age += Time.deltaTime;
        if (_age >= life) { Destroy(gameObject); return; }
        float k = _age / life;
        float s = 0.4f + grow * k;
        transform.localScale = new Vector3(s, 0.01f, s);
        _block.SetColor(BaseColor, new Color(_c.r, _c.g, _c.b, _c.a * (1f - k)));
        _r.SetPropertyBlock(_block);
    }
}

/// <summary>Flickering lightning bolt drawn with a LineRenderer; re-strikes every few hundredths of a second.</summary>
public class FxArc : MonoBehaviour
{
    const int Points = 9;
    Vector3 _a, _b;
    float _jitter, _life, _age, _nextStrike;
    LineRenderer _lr;
    Color _c;

    public void Set(Vector3 a, Vector3 b, Color color, float life, float jitter, bool local = false)
    {
        _a = a; _b = b; _c = color; _life = life; _jitter = jitter;
        _lr = gameObject.AddComponent<LineRenderer>();
        _lr.useWorldSpace = !local;
        _lr.positionCount = Points;
        _lr.sharedMaterial = LineMat();
        _lr.widthCurve = new AnimationCurve(new Keyframe(0f, 0.05f), new Keyframe(0.5f, 0.035f), new Keyframe(1f, 0.01f));
        _lr.numCapVertices = 2;
        _lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _lr.receiveShadows = false;
        Strike();
    }

    static Material _lineMat;
    // Sprites/Default honours LineRenderer vertex colours (URP Unlit does not).
    static Material LineMat()
    {
        if (_lineMat != null) return _lineMat;
        var sh = Shader.Find("Sprites/Default");
        _lineMat = sh != null ? new Material(sh) : RooftopProps.Glow(Color.white);
        return _lineMat;
    }

    void Strike()
    {
        Vector3 d = _b - _a;
        Vector3 side = Vector3.Cross(d.normalized, Vector3.up);
        if (side.sqrMagnitude < 0.01f) side = Vector3.right;
        Vector3 up2 = Vector3.Cross(d.normalized, side);
        for (int i = 0; i < Points; i++)
        {
            float t = i / (float)(Points - 1);
            float amp = (i == 0 || i == Points - 1) ? 0f : _jitter;
            _lr.SetPosition(i, _a + d * t + side * Random.Range(-amp, amp) + up2 * Random.Range(-amp, amp));
        }
        _nextStrike = _age + Random.Range(0.025f, 0.05f);
        _lr.enabled = Random.value > 0.15f;   // occasional blink, like real arcing
    }

    void Update()
    {
        if (_lr == null) return;
        _age += Time.deltaTime;
        if (_age >= _life) { Destroy(gameObject); return; }
        if (_age >= _nextStrike) Strike();
        float a = 1f - _age / _life;
        var c = new Color(_c.r, _c.g, _c.b, a);
        _lr.startColor = Color.Lerp(Color.white, c, 0.3f) * new Color(1, 1, 1, a);
        _lr.endColor = c;
    }
}

/// <summary>Grows, drifts and fades a puff (fireball lobe, smoke, scorch mark).</summary>
public class FxPuff : MonoBehaviour
{
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    float _s0, _s1, _life, _age, _fadeStart;
    Vector3 _vel;
    bool _flat;
    Renderer _r;
    MaterialPropertyBlock _block;
    Color _c;

    public void Set(float startSize, float endSize, float life, Vector3 velocity, bool flat = false, float fadeStart = 0f)
    {
        _s0 = startSize; _s1 = endSize; _life = life; _vel = velocity; _flat = flat; _fadeStart = fadeStart;
    }

    void Start()
    {
        _r = GetComponent<Renderer>();
        _block = new MaterialPropertyBlock();
        _c = _r != null ? _r.sharedMaterial.GetColor(BaseColor) : Color.white;
    }

    void Update()
    {
        _age += Time.deltaTime;
        if (_age >= _life) { Destroy(gameObject); return; }
        float k = _age / _life;
        float s = Mathf.Lerp(_s0, _s1, 1f - (1f - k) * (1f - k));
        transform.localScale = _flat ? new Vector3(s, transform.localScale.y, s) : Vector3.one * s;
        transform.position += _vel * Time.deltaTime;
        _vel *= 1f - Mathf.Min(1f, Time.deltaTime * 2.5f);
        if (_r == null) return;
        float a = k < _fadeStart ? 1f : 1f - (k - _fadeStart) / Mathf.Max(0.001f, 1f - _fadeStart);
        _block.SetColor(BaseColor, new Color(_c.r, _c.g, _c.b, _c.a * a));
        _r.SetPropertyBlock(_block);
    }
}

public class FxDelayActivate : MonoBehaviour
{
    GameObject _target;
    float _at;
    public void Set(GameObject target, float delay) { _target = target; _at = Time.time + delay; }
    void Update()
    {
        if (_target == null || Time.time < _at) return;
        _target.SetActive(true);
        Destroy(this);
    }
}

public class FxLightFade : MonoBehaviour
{
    public float life = 0.15f;
    Light _l;
    float _start, _age;
    void Start() { _l = GetComponent<Light>(); _start = _l != null ? _l.intensity : 0f; }
    void Update()
    {
        _age += Time.deltaTime;
        if (_l == null) return;
        _l.intensity = Mathf.Lerp(_start, 0f, _age / life);
        if (_age >= life) { _l.enabled = false; Destroy(this); }
    }
}

/// <summary>Squash-and-settle wobble on the obstacle that was hit.</summary>
public class ObstacleWobble : MonoBehaviour
{
    Vector3 _base;
    bool _hasBase;
    float _amount;
    float _t = 1f;

    public void Hit(float amount)
    {
        if (!_hasBase) { _base = transform.localScale; _hasBase = true; }
        _amount = amount;
        _t = 0f;
    }

    void Update()
    {
        if (!_hasBase || _t >= 1f) return;
        _t += Time.deltaTime / 0.45f;
        float w = Mathf.Sin(_t * Mathf.PI * 4f) * _amount * (1f - _t);
        transform.localScale = new Vector3(_base.x * (1f + w), _base.y * (1f - w), _base.z * (1f + w));
        if (_t >= 1f) transform.localScale = _base;
    }
}

/// <summary>Briefly flickers an obstacle's renderers (shorted neon, disrupted laser).</summary>
public class ObstacleFlicker : MonoBehaviour
{
    public float duration = 0.5f;
    Renderer[] _rs;
    float _age;

    void Start() => _rs = GetComponentsInChildren<Renderer>();

    void Update()
    {
        _age += Time.deltaTime;
        bool on = _age >= duration || Mathf.Repeat(_age * 22f, 1f) > 0.45f;
        foreach (var r in _rs) if (r != null) r.enabled = on;
        if (_age >= duration) Destroy(this);
    }
}
