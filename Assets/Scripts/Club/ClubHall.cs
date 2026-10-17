using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Level 3: builds 12 m bays of the underground club. Section 0 is the backstreet service
/// corridor, section 1 the LED dance floor, section 2 the red bass vault. Everything that glows
/// uses ClubBeat's shared pulse materials so the room breathes with the music.
/// </summary>
public static class ClubHall
{
    public const float HalfWidth = 12.5f;
    public const float CeilingY = 8f;
    public const float BayLength = 12f;
    public const float LaneEdge = 9.6f;

    static readonly Color Wall = new Color(0.05f, 0.04f, 0.07f);
    static readonly Color Metal = new Color(0.16f, 0.16f, 0.18f);

    public static readonly Color[] SectionAccent =
    {
        new Color(0.2f, 0.85f, 1f),   // backstreet: cyan
        new Color(1f, 0.2f, 0.8f),    // dance floor: magenta
        new Color(1f, 0.15f, 0.12f),  // bass vault: red
    };

    static Material Lit(Color c, float smooth = 0.4f, float metal = 0f) => MuseumHall.Lit(c, smooth, metal);
    static GameObject Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 size, Material mat, Vector3 euler = default)
        => RooftopProps.Part(type, parent, pos, size, mat, false, euler);

    public static GameObject Bay(Transform parent, float z, int index, int section)
    {
        section = Mathf.Clamp(section, 0, 2);
        var root = RooftopProps.Root("ClubBay_" + index, parent, new Vector3(0f, 0f, z));
        var t = root.transform;
        float mid = BayLength * 0.5f;
        var accent = SectionAccent[section];
        var neon = ClubBeat.PulseMat(accent, 0.45f, 0.55f);
        var neonSoft = ClubBeat.PulseMat(accent, 0.05f, 0.12f);

        // Floor.
        var floorColor = section == 0 ? new Color(0.09f, 0.09f, 0.1f) : section == 1 ? new Color(0.03f, 0.03f, 0.05f) : new Color(0.06f, 0.04f, 0.05f);
        Part(PrimitiveType.Cube, t, new Vector3(0f, 0.005f, mid), new Vector3(HalfWidth * 2f, 0.02f, BayLength), Lit(floorColor, section == 1 ? 0.9f : 0.55f, section == 2 ? 0.5f : 0f));
        if (section == 1)
        {
            // LED dance floor: tiles light up in groups on the beat.
            const float tile = 3.2f;
            for (int ix = 0; ix < 6; ix++)
                for (int iz = 0; iz < 4; iz++)
                {
                    float x = -LaneEdge + tile * 0.5f + ix * tile;
                    Part(PrimitiveType.Cube, t, new Vector3(x, 0.02f, tile * 0.5f + iz * (BayLength / 4f)), new Vector3(tile - 0.12f, 0.01f, BayLength / 4f - 0.12f),
                         ClubBeat.FloorMat(ix + iz * 2 + index));
                }
        }
        else
        {
            // Lane edge strips.
            foreach (float side in new[] { -1f, 1f })
                Part(PrimitiveType.Cube, t, new Vector3(side * LaneEdge, 0.025f, mid), new Vector3(0.12f, 0.02f, BayLength), neon);
            if (section == 2)
                Part(PrimitiveType.Cube, t, new Vector3(0f, 0.025f, mid), new Vector3(LaneEdge * 2f, 0.01f, 0.15f), neonSoft, new Vector3(0f, 90f, 0f));
        }

        // Walls with vertical neon tubes and a floor-level strip.
        foreach (float side in new[] { -1f, 1f })
        {
            Part(PrimitiveType.Cube, t, new Vector3(side * (HalfWidth + 0.2f), CeilingY * 0.5f, mid), new Vector3(0.4f, CeilingY, BayLength), Lit(Wall, 0.3f));
            Part(PrimitiveType.Cube, t, new Vector3(side * (HalfWidth - 0.05f), 0.35f, mid), new Vector3(0.06f, 0.06f, BayLength), neon);
            for (int i = 0; i < 3; i++)
                Part(PrimitiveType.Cube, t, new Vector3(side * (HalfWidth - 0.05f), 4.2f, 2f + i * 4f), new Vector3(0.08f, 4.6f, 0.08f), neon);
            Part(PrimitiveType.Cube, t, new Vector3(side * (HalfWidth - 0.08f), 4.2f, mid), new Vector3(0.02f, 6f, BayLength), neonSoft);
        }

        // Ceiling and lighting truss.
        Part(PrimitiveType.Cube, t, new Vector3(0f, CeilingY + 0.2f, mid), new Vector3(HalfWidth * 2f + 0.8f, 0.4f, BayLength), Lit(new Color(0.02f, 0.02f, 0.03f), 0.2f));
        var metal = Lit(Metal, 0.6f, 0.8f);
        Part(PrimitiveType.Cube, t, new Vector3(0f, CeilingY - 0.6f, 0f), new Vector3(HalfWidth * 2f, 0.35f, 0.35f), metal);
        Part(PrimitiveType.Cube, t, new Vector3(0f, CeilingY - 1.05f, 0f), new Vector3(HalfWidth * 2f, 0.12f, 0.12f), metal);
        if (index % 2 == 0)
        {
            MovingHead(t, new Vector3(-5f, CeilingY - 1.3f, 0f), accent, 0f);
            MovingHead(t, new Vector3(5f, CeilingY - 1.3f, 0f), section == 1 ? SectionAccent[0] : accent, 0.5f);
        }

        // Speaker stack on alternating sides, cones thumping on the beat.
        float sSide = index % 2 == 0 ? -1f : 1f;
        SpeakerStack(t, new Vector3(sSide * 11.1f, 0f, 6f), sSide);

        // One coloured light per bay, pulsing with the kick.
        var light = new GameObject("BeatLight").AddComponent<Light>();
        light.transform.SetParent(t, false);
        light.transform.localPosition = new Vector3(0f, CeilingY - 2f, mid);
        light.type = LightType.Point;
        light.color = accent;
        light.range = 16f;
        light.shadows = LightShadows.None;
        light.gameObject.AddComponent<BeatLight>().baseIntensity = section == 0 ? 1.4f : 2.2f;

        if (section == 1 && index % 3 == 0)
            DiscoBall(t, new Vector3(0f, CeilingY - 2.2f, mid));
        if (section == 0 && index % 2 == 1)
        {
            // Service pipes along the backstreet ceiling.
            foreach (float px in new[] { -8f, -7.3f, 7.6f })
                Part(PrimitiveType.Cylinder, t, new Vector3(px, CeilingY - 0.4f, mid), new Vector3(0.3f, BayLength * 0.5f, 0.3f), Lit(new Color(0.2f, 0.18f, 0.16f), 0.5f, 0.6f), new Vector3(90f, 0f, 0f));
        }
        return root;
    }

    static void SpeakerStack(Transform parent, Vector3 pos, float side)
    {
        var root = RooftopProps.Root("SpeakerStack", parent, parent.position + pos).transform;
        var body = Lit(new Color(0.04f, 0.04f, 0.05f), 0.35f);
        var rim = Lit(new Color(0.25f, 0.25f, 0.28f), 0.7f, 0.9f);
        var cones = new List<Transform>();
        for (int i = 0; i < 2; i++)
        {
            float y = 0.85f + i * 1.75f;
            Part(PrimitiveType.Cube, root, new Vector3(0f, y, 0f), new Vector3(1.4f, 1.7f, 1.5f), body);
            var cone = Part(PrimitiveType.Cylinder, root, new Vector3(-side * 0.72f, y, 0f), new Vector3(1.05f, 0.04f, 1.05f), rim, new Vector3(0f, 0f, 90f));
            Part(PrimitiveType.Cylinder, cone.transform, new Vector3(0f, -0.8f, 0f), new Vector3(0.45f, 1f, 0.45f), Lit(new Color(0.08f, 0.08f, 0.09f), 0.5f));
            cones.Add(cone.transform);
        }
        var pulse = root.gameObject.AddComponent<SpeakerPulse>();
        pulse.cones = cones.ToArray();
    }

    static void MovingHead(Transform parent, Vector3 pos, Color color, float offset)
    {
        var root = RooftopProps.Root("MovingHead", parent, parent.position + pos).transform;
        Part(PrimitiveType.Cube, root, new Vector3(0f, 0.15f, 0f), new Vector3(0.5f, 0.3f, 0.5f), Lit(Metal, 0.6f, 0.8f));
        var head = new GameObject("Head").transform;
        head.SetParent(root, false);
        Part(PrimitiveType.Cylinder, head, new Vector3(0f, -0.25f, 0f), new Vector3(0.45f, 0.25f, 0.45f), Lit(new Color(0.05f, 0.05f, 0.06f), 0.5f));
        Part(PrimitiveType.Sphere, head, new Vector3(0f, -0.5f, 0f), Vector3.one * 0.32f, RooftopProps.Glow(new Color(color.r, color.g, color.b, 1f)));
        var beamGo = new GameObject("Beam");
        beamGo.transform.SetParent(head, false);
        beamGo.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        beamGo.AddComponent<MeshFilter>().sharedMesh = BeamMesh();
        var r = beamGo.AddComponent<MeshRenderer>();
        r.sharedMaterial = ClubBeat.PulseMat(color, 0.05f, 0.08f);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var mover = root.gameObject.AddComponent<MovingHeadSweep>();
        mover.head = head;
        mover.offset = offset;
    }

    static void DiscoBall(Transform parent, Vector3 pos)
    {
        var root = RooftopProps.Root("DiscoBall", parent, parent.position + pos).transform;
        Part(PrimitiveType.Cylinder, root, new Vector3(0f, 1f, 0f), new Vector3(0.04f, 1f, 0.04f), Lit(Metal, 0.5f, 0.8f));
        var ball = Part(PrimitiveType.Sphere, root, Vector3.zero, Vector3.one * 1.3f, Lit(new Color(0.8f, 0.8f, 0.85f), 0.98f, 1f));
        Part(PrimitiveType.Sphere, root, Vector3.zero, Vector3.one * 2.2f, ClubBeat.PulseMat(new Color(0.9f, 0.85f, 1f), 0.03f, 0.1f));
        ball.AddComponent<ExhibitSpin>().degreesPerSecond = 40f;
    }

    static Mesh _beam;

    /// <summary>Downward light cone, 7 m long.</summary>
    static Mesh BeamMesh()
    {
        if (_beam != null) return _beam;
        const int seg = 20;
        const float r0 = 0.12f, r1 = 1.6f, h = 7f;
        var verts = new List<Vector3>();
        var tris = new List<int>();
        for (int i = 0; i <= seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            verts.Add(d * r0);
            verts.Add(d * r1 + Vector3.down * h);
        }
        for (int i = 0; i < seg; i++)
        {
            int a = i * 2, b = a + 1, c = a + 2, e = a + 3;
            tris.AddRange(new[] { a, c, b, b, c, e, a, b, c, b, e, c });
        }
        _beam = new Mesh { name = "ClubBeam" };
        _beam.SetVertices(verts);
        _beam.SetTriangles(tris, 0);
        _beam.RecalculateNormals();
        _beam.RecalculateBounds();
        return _beam;
    }

    /// <summary>Neon gateway marking the start of a club section, with its name.</summary>
    public static GameObject SectionArch(Transform parent, float z, string name, int section, TMP_FontAsset font)
    {
        var root = RooftopProps.Root("SectionArch_" + name, parent, new Vector3(0f, 0f, z));
        var t = root.transform;
        var accent = SectionAccent[Mathf.Clamp(section, 0, 2)];
        var neon = ClubBeat.PulseMat(accent, 0.6f, 0.4f);
        var dark = Lit(Wall, 0.3f);
        foreach (float side in new[] { -1f, 1f })
        {
            Part(PrimitiveType.Cube, t, new Vector3(side * (HalfWidth - 1f), CeilingY * 0.5f, 0f), new Vector3(2f, CeilingY, 1.2f), dark);
            Part(PrimitiveType.Cube, t, new Vector3(side * (HalfWidth - 2.05f), CeilingY * 0.5f, -0.62f), new Vector3(0.1f, CeilingY, 0.06f), neon);
        }
        Part(PrimitiveType.Cube, t, new Vector3(0f, CeilingY - 0.9f, 0f), new Vector3(HalfWidth * 2f, 1.8f, 1.2f), dark);
        Part(PrimitiveType.Cube, t, new Vector3(0f, CeilingY - 1.85f, -0.62f), new Vector3(HalfWidth * 2f - 4.2f, 0.1f, 0.06f), neon);

        var textGo = new GameObject("SectionName");
        textGo.transform.SetParent(t, false);
        textGo.transform.localPosition = new Vector3(0f, CeilingY - 0.9f, -0.65f);
        var text = textGo.AddComponent<TextMeshPro>();
        if (font != null) text.font = font;
        text.text = name;
        text.fontSize = 8f;
        text.characterSpacing = 10f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.Lerp(accent, Color.white, 0.35f);
        text.rectTransform.sizeDelta = new Vector2(18f, 1.6f);
        return root;
    }
}

/// <summary>Speaker cones punch out on every kick.</summary>
public class SpeakerPulse : MonoBehaviour
{
    public Transform[] cones;
    Vector3[] _base;

    void Start()
    {
        _base = new Vector3[cones.Length];
        for (int i = 0; i < cones.Length; i++) _base[i] = cones[i].localScale;
    }

    void Update()
    {
        if (_base == null) return;
        float k = ClubBeat.Kick;
        for (int i = 0; i < cones.Length; i++)
            if (cones[i] != null) cones[i].localScale = new Vector3(_base[i].x * (1f + k * 0.12f), _base[i].y * (1f + k * 2.5f), _base[i].z * (1f + k * 0.12f));
    }
}

/// <summary>Light intensity kicks with the beat.</summary>
public class BeatLight : MonoBehaviour
{
    public float baseIntensity = 2f;
    Light _light;

    void Awake() => _light = GetComponent<Light>();

    void Update()
    {
        if (_light != null) _light.intensity = baseIntensity * (0.55f + 0.9f * ClubBeat.Kick);
    }
}

/// <summary>Moving-head stage light: sweeps its beam across the floor in time with the music.</summary>
public class MovingHeadSweep : MonoBehaviour
{
    public Transform head;
    public float offset;

    void Update()
    {
        if (head == null) return;
        float beats = (Time.time / ClubBeat.BeatLength) * 0.25f + offset;
        float pan = Mathf.Sin(beats * Mathf.PI) * 35f;
        float tilt = Mathf.Cos(beats * Mathf.PI * 0.5f) * 25f;
        head.localRotation = Quaternion.Euler(tilt, 0f, pan);
    }
}
