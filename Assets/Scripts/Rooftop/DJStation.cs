using TMPro;
using UnityEngine;

/// <summary>
/// Rooftop DJ gig station for the sides of the Level 1 run: a stage riser with a neon edge,
/// a DJ table with an LED front, two turntables and a mixer, speaker stacks, a truss with a
/// name banner and moving-head beams, and a DJ who bobs to the beat. Built from primitives,
/// sits beside the lanes (never in them) and pulses in time with the run's tempo.
/// </summary>
public static class DJStation
{
    static readonly string[] Names = { "DJ NOVA", "DJ KAYA", "MC ZURI", "DJ RIMA", "SOUNDWAVE", "BEAT LAB", "DJ SOL", "NIGHT SHIFT" };
    static readonly Color[] Palette =
    {
        new Color(0.35f, 0.85f, 1f),   // cyan
        new Color(1f, 0.3f, 0.8f),     // magenta
        new Color(1f, 0.8f, 0.25f),    // gold
        new Color(0.55f, 1f, 0.45f),   // lime
    };

    /// <summary>Builds a station at a floor point. side = +1 on the right of the run, -1 on the left.</summary>
    public static GameObject Build(Transform parent, Vector3 floor, int side, int seed, float laneOuter = 4f)
    {
        var rng = new System.Random(seed);
        Color a = Palette[rng.Next(Palette.Length)];
        Color b = Palette[(System.Array.IndexOf(Palette, a) + 1 + rng.Next(Palette.Length - 1)) % Palette.Length];
        string name = Names[rng.Next(Names.Length)];

        var root = RooftopProps.Root("DJ Gig Station", parent, floor);
        // Local +z faces the runway.
        root.transform.rotation = Quaternion.LookRotation(new Vector3(-side, 0f, 0f));
        var t = root.transform;

        var metal = RooftopProps.Mat(new Color(0.08f, 0.08f, 0.1f));
        var steel = RooftopProps.Mat(new Color(0.35f, 0.36f, 0.4f));
        var black = RooftopProps.Mat(new Color(0.03f, 0.03f, 0.035f));

        // Riser and table.
        P(PrimitiveType.Cube, t, new Vector3(0f, 0.175f, 0f), new Vector3(4.2f, 0.35f, 2.4f), metal);
        var edge = P(PrimitiveType.Cube, t, new Vector3(0f, 0.36f, 1.2f), new Vector3(4.2f, 0.06f, 0.06f), RooftopProps.Glow(a));
        P(PrimitiveType.Cube, t, new Vector3(0f, 0.825f, 0.55f), new Vector3(2.8f, 0.95f, 0.7f), black);
        var led = P(PrimitiveType.Cube, t, new Vector3(0f, 0.83f, 0.91f), new Vector3(2.6f, 0.7f, 0.02f), RooftopProps.Glow(new Color(a.r, a.g, a.b, 0.85f)));

        // Decks: two turntables and a mixer.
        var platters = new Transform[2];
        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? -0.85f : 0.85f;
            P(PrimitiveType.Cylinder, t, new Vector3(x, 1.31f, 0.55f), new Vector3(0.6f, 0.02f, 0.6f), steel);
            var platter = P(PrimitiveType.Cylinder, t, new Vector3(x, 1.335f, 0.55f), new Vector3(0.5f, 0.012f, 0.5f), black).transform;
            P(PrimitiveType.Cube, platter, new Vector3(0.3f, 0.6f, 0f), new Vector3(0.18f, 1f, 0.06f), RooftopProps.Glow(b));   // label stripe so the spin reads
            P(PrimitiveType.Cube, t, new Vector3(x + 0.22f, 1.36f, 0.4f), new Vector3(0.03f, 0.03f, 0.32f), steel, new Vector3(0f, -25f, 0f));   // tone arm
            platters[i] = platter;
        }
        P(PrimitiveType.Cube, t, new Vector3(0f, 1.34f, 0.55f), new Vector3(0.55f, 0.07f, 0.45f), metal);
        for (int k = 0; k < 6; k++)
            P(PrimitiveType.Sphere, t, new Vector3(-0.18f + (k % 3) * 0.18f, 1.385f, 0.45f + (k / 3) * 0.18f), Vector3.one * 0.05f, RooftopProps.Glow(k % 2 == 0 ? a : b));

        // The DJ: bobbing body, head with headphones, one arm pumping.
        var dj = new GameObject("DJ").transform;
        dj.SetParent(t, false);
        dj.localPosition = new Vector3(0f, 0.35f, -0.25f);
        var shirt = RooftopProps.Mat(Color.Lerp(b, Color.black, 0.45f));
        var skin = RooftopProps.Mat(new Color(0.42f, 0.28f, 0.2f));
        P(PrimitiveType.Capsule, dj, new Vector3(0f, 0.95f, 0f), new Vector3(0.5f, 0.55f, 0.36f), shirt);
        var head = P(PrimitiveType.Sphere, dj, new Vector3(0f, 1.62f, 0.02f), Vector3.one * 0.3f, skin).transform;
        P(PrimitiveType.Cube, head, new Vector3(0f, 0.45f, 0f), new Vector3(1.15f, 0.12f, 0.2f), black);
        P(PrimitiveType.Sphere, head, new Vector3(-0.55f, 0.05f, 0f), new Vector3(0.3f, 0.45f, 0.45f), RooftopProps.Glow(a));
        P(PrimitiveType.Sphere, head, new Vector3(0.55f, 0.05f, 0f), new Vector3(0.3f, 0.45f, 0.45f), RooftopProps.Glow(a));
        var arm = new GameObject("ArmPivot").transform;
        arm.SetParent(dj, false);
        arm.localPosition = new Vector3(0.28f, 1.3f, 0f);
        P(PrimitiveType.Capsule, arm, new Vector3(0f, 0.3f, 0f), new Vector3(0.13f, 0.32f, 0.13f), shirt);
        P(PrimitiveType.Capsule, dj, new Vector3(-0.3f, 1.05f, 0.25f), new Vector3(0.13f, 0.28f, 0.13f), shirt, new Vector3(60f, 0f, 0f));   // hand on the deck
        UseDJCharacter(dj, t);

        // Speaker stacks.
        var cones = new Transform[4];
        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? -1.85f : 1.85f;
            P(PrimitiveType.Cube, t, new Vector3(x, 1.05f, 0.35f), new Vector3(0.6f, 1.4f, 0.6f), black);
            cones[i * 2] = P(PrimitiveType.Cylinder, t, new Vector3(x, 0.8f, 0.66f), new Vector3(0.44f, 0.02f, 0.44f), metal, new Vector3(90f, 0f, 0f)).transform;
            cones[i * 2 + 1] = P(PrimitiveType.Cylinder, t, new Vector3(x, 1.45f, 0.66f), new Vector3(0.22f, 0.02f, 0.22f), metal, new Vector3(90f, 0f, 0f)).transform;
            P(PrimitiveType.Cube, t, new Vector3(x, 1.77f, 0.66f), new Vector3(0.5f, 0.03f, 0.02f), RooftopProps.Glow(a));
        }

        // Truss, name banner and moving heads.
        for (int i = 0; i < 2; i++)
        {
            P(PrimitiveType.Cube, t, new Vector3(i == 0 ? -2f : 2f, 1.95f, -0.9f), new Vector3(0.12f, 3.2f, 0.12f), steel);
            P(PrimitiveType.Cube, t, new Vector3(i == 0 ? -2f : 2f, 1.95f, -0.82f), new Vector3(0.04f, 3.1f, 0.04f), RooftopProps.Glow(b));   // neon strip
        }
        P(PrimitiveType.Cube, t, new Vector3(0f, 3.45f, -0.8f), new Vector3(4.1f, 0.05f, 0.05f), RooftopProps.Glow(b));
        P(PrimitiveType.Cube, t, new Vector3(0f, 3.55f, -0.9f), new Vector3(4.2f, 0.14f, 0.14f), steel);
        P(PrimitiveType.Cube, t, new Vector3(0f, 3.05f, -0.86f), new Vector3(2.7f, 0.6f, 0.05f), black);
        var banner = P(PrimitiveType.Cube, t, new Vector3(0f, 3.05f, -0.83f), new Vector3(2.75f, 0.65f, 0.01f), RooftopProps.Glow(new Color(a.r, a.g, a.b, 0.25f)));
        var label = new GameObject("Name", typeof(RectTransform)).AddComponent<TextMeshPro>();
        label.transform.SetParent(t, false);
        label.transform.localPosition = new Vector3(0f, 3.05f, -0.8f);
        label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);   // readable from the runway side
        label.rectTransform.sizeDelta = new Vector2(2.6f, 0.6f);
        var hud = HudSkin.Get();
        if (hud != null && hud.font != null) label.font = hud.font;
        label.text = name;
        label.fontSize = 4.2f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 1f;
        label.fontSizeMax = 4.2f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.Lerp(a, Color.white, 0.35f);

        var heads = new Transform[2];
        for (int i = 0; i < 2; i++)
        {
            var h = new GameObject("MovingHead").transform;
            h.SetParent(t, false);
            h.localPosition = new Vector3(i == 0 ? -1.3f : 1.3f, 3.4f, -0.75f);
            P(PrimitiveType.Cube, h, Vector3.zero, new Vector3(0.22f, 0.22f, 0.22f), metal);
            var beam = P(PrimitiveType.Cylinder, h, new Vector3(0f, 0f, 2.4f), new Vector3(0.35f, 2.4f, 0.35f),
                         RooftopProps.Glow(new Color((i == 0 ? a : b).r, (i == 0 ? a : b).g, (i == 0 ? a : b).b, 0.1f)), new Vector3(90f, 0f, 0f));
            beam.name = "Beam";
            heads[i] = h;
        }

        var lightGo = new GameObject("StageLight");
        lightGo.transform.SetParent(t, false);
        lightGo.transform.localPosition = new Vector3(0f, 2.6f, 1.4f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = a;
        light.range = 12f;
        light.intensity = 5f;
        light.shadows = LightShadows.None;

        // Solid, so the runner can't pass through the booth (it never sits in a lane).
        var box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.8f, 0.1f);
        box.size = new Vector3(4.4f, 1.6f, 2.6f);

        // Crew: a bouncer at the front corner and a smoke machine aimed at the track.
        var front = t.TransformPoint(new Vector3(2.45f, 0f, 1.7f));
        GuardModels.Spawn(t, front, t.forward, "Bouncer");
        var machine = SmokeMachine(t, new Vector3(-2.35f, 0f, 1.55f), metal, a);
        var cannon = root.AddComponent<DJSmokeCannon>();
        cannon.nozzle = machine;
        cannon.side = side;
        cannon.laneOuter = laneOuter;
        cannon.tint = Color.Lerp(Color.white, a, 0.25f);

        var anim = root.AddComponent<DJStationAnimator>();
        anim.Set(dj, arm, platters, cones, heads, led.GetComponent<Renderer>(), edge.GetComponent<Renderer>(),
                 banner.GetComponent<Renderer>(), light, a, b, rng.Next(1000) * 0.01f);
        return root;
    }

    /// <summary>Box smoke machine with a nozzle; returns the nozzle.</summary>
    static Transform SmokeMachine(Transform t, Vector3 at, Material metal, Color accent)
    {
        var m = new GameObject("Smoke Machine").transform;
        m.SetParent(t, false);
        m.localPosition = at;
        P(PrimitiveType.Cube, m, new Vector3(0f, 0.3f, 0f), new Vector3(0.7f, 0.6f, 0.55f), metal);
        P(PrimitiveType.Cylinder, m, new Vector3(0f, 0.42f, 0.38f), new Vector3(0.2f, 0.12f, 0.2f), metal, new Vector3(90f, 0f, 0f));
        P(PrimitiveType.Cube, m, new Vector3(0.22f, 0.5f, 0.28f), new Vector3(0.08f, 0.08f, 0.02f), RooftopProps.Glow(accent));
        var nozzle = new GameObject("Nozzle").transform;
        nozzle.SetParent(m, false);
        nozzle.localPosition = new Vector3(0f, 0.42f, 0.55f);
        return nozzle;
    }

    // ── Centre-of-track DJ stage with bodyguards ─────────────────────────────

    /// <summary>
    /// A compact DJ stage in the middle lane (solid - bump it and you trip), with a taser-armed
    /// bodyguard on one side and a smoke machine that randomly floods the track.
    /// guardSide = -1 left, +1 right: that lane is guarded (jump the zap or take the other lane).
    /// </summary>
    /// <param name="guardOffset">How far from the centre the bodyguard stands (next to the guarded lane).</param>
    /// <param name="laneReach">Sideways reach of his taser from where he stands.</param>
    /// <param name="smokeSpread">Half-width of the track the random smoke covers.</param>
    /// <summary>Swaps the primitive DJ for the imported DJ character (if set up), dancing behind the decks.</summary>
    static void UseDJCharacter(Transform dj, Transform station)
    {
        var feet = station.TransformPoint(new Vector3(0f, 0f, -0.35f));
        feet.y = dj.position.y;
        var c = DanceFloorCharacters.SpawnDJ(dj, feet, station.forward);
        if (c == null) return;
        foreach (var r in dj.GetComponentsInChildren<Renderer>())
            if (!r.transform.IsChildOf(c)) r.enabled = false;
    }

    public static GameObject BuildCenter(Transform parent, Vector3 floor, int guardSide, int seed,
                                         float guardOffset = 2.75f, float laneReach = 2.1f, float smokeSpread = 4.5f)
    {
        var rng = new System.Random(seed);
        Color a = Palette[rng.Next(Palette.Length)];
        Color b = Palette[(System.Array.IndexOf(Palette, a) + 1) % Palette.Length];
        string name = Names[rng.Next(Names.Length)];

        var root = RooftopProps.Root("DJ Booth", parent, floor);
        root.transform.rotation = Quaternion.LookRotation(Vector3.back);   // faces the runner
        var t = root.transform;
        var metal = RooftopProps.Mat(new Color(0.08f, 0.08f, 0.1f));
        var steel = RooftopProps.Mat(new Color(0.35f, 0.36f, 0.4f));
        var black = RooftopProps.Mat(new Color(0.03f, 0.03f, 0.035f));

        P(PrimitiveType.Cube, t, new Vector3(0f, 0.15f, 0f), new Vector3(4.2f, 0.3f, 2.6f), metal);
        var edge = P(PrimitiveType.Cube, t, new Vector3(0f, 0.31f, 1.3f), new Vector3(4.2f, 0.06f, 0.06f), RooftopProps.Glow(a));
        P(PrimitiveType.Cube, t, new Vector3(0f, 0.8f, 0.6f), new Vector3(3f, 1f, 0.7f), black);
        var led = P(PrimitiveType.Cube, t, new Vector3(0f, 0.8f, 0.96f), new Vector3(2.8f, 0.75f, 0.02f), RooftopProps.Glow(new Color(a.r, a.g, a.b, 0.85f)));
        var platters = new Transform[2];
        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? -0.85f : 0.85f;
            P(PrimitiveType.Cylinder, t, new Vector3(x, 1.31f, 0.6f), new Vector3(0.6f, 0.02f, 0.6f), steel);
            platters[i] = P(PrimitiveType.Cylinder, t, new Vector3(x, 1.335f, 0.6f), new Vector3(0.5f, 0.012f, 0.5f), black).transform;
            P(PrimitiveType.Cube, platters[i], new Vector3(0.3f, 0.6f, 0f), new Vector3(0.18f, 1f, 0.06f), RooftopProps.Glow(b));
        }
        P(PrimitiveType.Cube, t, new Vector3(0f, 1.34f, 0.6f), new Vector3(0.55f, 0.07f, 0.45f), metal);

        var dj = new GameObject("DJ").transform;
        dj.SetParent(t, false);
        dj.localPosition = new Vector3(0f, 0.3f, -0.2f);
        var shirt = RooftopProps.Mat(Color.Lerp(b, Color.black, 0.45f));
        P(PrimitiveType.Capsule, dj, new Vector3(0f, 0.95f, 0f), new Vector3(0.5f, 0.55f, 0.36f), shirt);
        var head = P(PrimitiveType.Sphere, dj, new Vector3(0f, 1.62f, 0.02f), Vector3.one * 0.3f, RooftopProps.Mat(new Color(0.42f, 0.28f, 0.2f))).transform;
        P(PrimitiveType.Sphere, head, new Vector3(-0.55f, 0.05f, 0f), new Vector3(0.3f, 0.45f, 0.45f), RooftopProps.Glow(a));
        P(PrimitiveType.Sphere, head, new Vector3(0.55f, 0.05f, 0f), new Vector3(0.3f, 0.45f, 0.45f), RooftopProps.Glow(a));
        var arm = new GameObject("ArmPivot").transform;
        arm.SetParent(dj, false);
        arm.localPosition = new Vector3(0.28f, 1.3f, 0f);
        P(PrimitiveType.Capsule, arm, new Vector3(0f, 0.3f, 0f), new Vector3(0.13f, 0.32f, 0.13f), shirt);
        UseDJCharacter(dj, t);

        // Arch truss with the DJ's name over the stage.
        for (int i = 0; i < 2; i++)
            P(PrimitiveType.Cube, t, new Vector3(i == 0 ? -2f : 2f, 1.9f, -1f), new Vector3(0.12f, 3.2f, 0.12f), steel);
        P(PrimitiveType.Cube, t, new Vector3(0f, 3.5f, -1f), new Vector3(4.2f, 0.14f, 0.14f), steel);
        P(PrimitiveType.Cube, t, new Vector3(0f, 3.4f, -0.92f), new Vector3(4.1f, 0.05f, 0.05f), RooftopProps.Glow(b));
        var banner = P(PrimitiveType.Cube, t, new Vector3(0f, 3.0f, -0.95f), new Vector3(2.75f, 0.6f, 0.02f), RooftopProps.Glow(new Color(a.r, a.g, a.b, 0.25f)));
        var label = new GameObject("Name", typeof(RectTransform)).AddComponent<TMPro.TextMeshPro>();
        label.transform.SetParent(t, false);
        label.transform.localPosition = new Vector3(0f, 3.0f, -0.9f);
        label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        label.rectTransform.sizeDelta = new Vector2(2.6f, 0.6f);
        var hud = HudSkin.Get();
        if (hud != null && hud.font != null) label.font = hud.font;
        label.text = name;
        label.enableAutoSizing = true;
        label.fontSizeMin = 1f;
        label.fontSizeMax = 4.2f;
        label.alignment = TMPro.TextAlignmentOptions.Center;
        label.color = Color.Lerp(a, Color.white, 0.35f);

        var lightGo = new GameObject("StageLight");
        lightGo.transform.SetParent(t, false);
        lightGo.transform.localPosition = new Vector3(0f, 2.6f, 1.6f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = a;
        light.range = 12f;
        light.intensity = 5f;
        light.shadows = LightShadows.None;

        // Solid hazard: running into the booth trips you like any obstacle.
        root.tag = "HealthDEC";
        var h = root.AddComponent<HealthDecreaseObstacle>();
        h.damage = 10;
        h.vibeDamage = 4;
        h.displayName = "DJ Booth";
        var box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.8f, 0f);
        box.size = new Vector3(4.2f, 1.6f, 2.6f);
        GameplayCollisionUtility.EnsureKinematicRigidbody(root);

        // Bodyguard with a taser beside the stage, toward the runner.
        var guardFeet = floor + new Vector3(guardSide * guardOffset, 0f, -1.1f);
        var guard = GuardModels.Spawn(t, guardFeet, Vector3.back, "Bodyguard");
        var taser = guard.gameObject.AddComponent<TaserGuard>();
        taser.laneReach = laneReach;
        taser.Build(guardSide);

        // Smoke machine on the stage front that randomly floods the track.
        var machine = SmokeMachine(t, new Vector3(guardSide > 0 ? 1.6f : -1.6f, 0.3f, 1.05f), metal, a);
        var smoke = root.AddComponent<DJSmokeRandom>();
        smoke.nozzle = machine;
        smoke.tint = Color.Lerp(Color.white, a, 0.25f);
        smoke.spread = smokeSpread;

        var animator = root.AddComponent<DJStationAnimator>();
        animator.Set(dj, arm, platters, new Transform[0], new Transform[0], led.GetComponent<Renderer>(), edge.GetComponent<Renderer>(),
                     banner.GetComponent<Renderer>(), light, a, b, rng.Next(1000) * 0.01f);
        return root;
    }

    static GameObject P(PrimitiveType type, Transform parent, Vector3 pos, Vector3 size, Material mat, Vector3 euler = default)
        => RooftopProps.Part(type, parent, pos, size, mat, false, euler);
}

/// <summary>Drives a DJ station on the beat: DJ bob and fist pump, spinning decks, speaker kicks, LED and beam sweeps.</summary>
public class DJStationAnimator : MonoBehaviour
{
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    Transform _dj, _arm;
    Transform[] _platters, _cones, _heads;
    Renderer _led, _edge, _banner;
    Light _light;
    Color _a, _b;
    float _offset;
    Vector3 _djBase;
    MaterialPropertyBlock _block;
    Transform _player;

    public void Set(Transform dj, Transform arm, Transform[] platters, Transform[] cones, Transform[] heads,
                    Renderer led, Renderer edge, Renderer banner, Light light, Color a, Color b, float offset)
    {
        _dj = dj; _arm = arm; _platters = platters; _cones = cones; _heads = heads;
        _led = led; _edge = edge; _banner = banner; _light = light; _a = a; _b = b; _offset = offset;
        _djBase = dj.localPosition;
        _block = new MaterialPropertyBlock();
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
    }

    void Update()
    {
        // Only animate when near the runner; far stations sit still (cheap).
        if (_player != null && Mathf.Abs(transform.position.z - _player.position.z) > 90f) return;

        float bpm = RunDirector.Instance != null ? RunDirector.Instance.bpm : ClubRun.Instance != null ? 124f : 110f;
        float beats = Time.time * bpm / 60f + _offset;
        float frac = beats - Mathf.Floor(beats);
        float kick = Mathf.Exp(-frac * 7f);
        int bar = Mathf.FloorToInt(beats) % 4;

        _dj.localPosition = _djBase + Vector3.up * (kick * 0.08f);
        _dj.localRotation = Quaternion.Euler(kick * 8f, Mathf.Sin(beats * Mathf.PI * 0.5f) * 10f, 0f);
        _arm.localRotation = Quaternion.Euler(0f, 0f, -20f - kick * 25f - (bar == 3 ? 20f : 0f));

        foreach (var p in _platters) p.Rotate(0f, 200f * Time.deltaTime, 0f, Space.Self);
        for (int i = 0; i < _cones.Length; i++)
        {
            float s = 1f + kick * (i % 2 == 0 ? 0.16f : 0.08f);
            var ls = _cones[i].localScale;
            _cones[i].localScale = new Vector3(i % 2 == 0 ? 0.44f * s : 0.22f * s, ls.y, i % 2 == 0 ? 0.44f * s : 0.22f * s);
        }
        for (int i = 0; i < _heads.Length; i++)
        {
            float sweep = Mathf.Sin(beats * Mathf.PI * 0.25f + i * 1.7f);
            _heads[i].localRotation = Quaternion.Euler(-25f + sweep * 12f, (i == 0 ? -1f : 1f) * (20f + sweep * 25f), 0f);
        }

        Color c = bar % 2 == 0 ? _a : _b;
        Tint(_led, new Color(c.r, c.g, c.b, 0.35f + kick * 0.6f));
        Tint(_edge, new Color(c.r, c.g, c.b, 0.5f + kick * 0.5f));
        Tint(_banner, new Color(_a.r, _a.g, _a.b, 0.15f + kick * 0.25f));
        if (_light != null)
        {
            _light.color = c;
            _light.intensity = 3f + kick * 7f;
        }
    }

    void Tint(Renderer r, Color c)
    {
        if (r == null) return;
        r.GetPropertyBlock(_block);
        _block.SetColor(BaseColor, c);
        r.SetPropertyBlock(_block);
    }
}
