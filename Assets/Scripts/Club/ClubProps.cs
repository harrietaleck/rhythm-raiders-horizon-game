using UnityEngine;

/// <summary>Level 3 club hazards. Each reads clearly and has one way to beat it.</summary>
public static class ClubProps
{
    static readonly Color Magenta = new Color(1f, 0.15f, 0.85f);
    static readonly Color Red = new Color(1f, 0.12f, 0.1f);

    static Material Lit(Color c, float smooth = 0.4f, float metal = 0f) => MuseumHall.Lit(c, smooth, metal);
    static GameObject Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 size, Material mat, Vector3 euler = default)
        => RooftopProps.Part(type, parent, pos, size, mat, false, euler);

    static GameObject Hazard(GameObject root, string displayName, Vector3 center, Vector3 size, int damage, int vibe, bool trigger = false)
    {
        root.tag = "HealthDEC";
        var h = root.AddComponent<HealthDecreaseObstacle>();
        h.damage = damage;
        h.vibeDamage = vibe;
        h.displayName = displayName;
        var box = root.AddComponent<BoxCollider>();
        box.center = center;
        box.size = size;
        box.isTrigger = trigger;
        GameplayCollisionUtility.EnsureKinematicRigidbody(root);
        return root;
    }

    /// <summary>Knee-high magenta laser across one lane - jump it.</summary>
    public static GameObject LaserFence(Transform parent, Vector3 pos)
    {
        var root = RooftopProps.Root("Laser Fence", parent, pos);
        var t = root.transform;
        var post = Lit(new Color(0.06f, 0.06f, 0.08f), 0.6f, 0.6f);
        var beam = ClubBeat.PulseMat(Magenta, 0.75f, 0.25f);
        foreach (float side in new[] { -1f, 1f })
        {
            Part(PrimitiveType.Cylinder, t, new Vector3(side * 2.2f, 0.45f, 0f), new Vector3(0.3f, 0.45f, 0.3f), post);
            Part(PrimitiveType.Sphere, t, new Vector3(side * 2.2f, 0.95f, 0f), Vector3.one * 0.22f, beam);
        }
        Part(PrimitiveType.Cube, t, new Vector3(0f, 0.5f, 0f), new Vector3(4.4f, 0.05f, 0.05f), beam);
        Part(PrimitiveType.Cube, t, new Vector3(0f, 0.75f, 0f), new Vector3(4.4f, 0.04f, 0.04f), beam);
        Part(PrimitiveType.Cube, t, new Vector3(0f, 0.6f, 0f), new Vector3(4.4f, 0.5f, 0.12f), ClubBeat.PulseMat(Magenta, 0.06f, 0.14f));
        Part(PrimitiveType.Cube, t, new Vector3(0f, 0.03f, 0f), new Vector3(4.6f, 0.01f, 1.4f), ClubBeat.PulseMat(Magenta, 0.06f, 0.1f));
        return Hazard(root, "Laser Fence", new Vector3(0f, 0.55f, 0f), new Vector3(4.2f, 0.6f, 0.4f), 10, 4);
    }

    /// <summary>Tall speaker stack blocking a lane - change lanes.</summary>
    public static GameObject SpeakerStack(Transform parent, Vector3 pos)
    {
        var root = RooftopProps.Root("Speaker Stack", parent, pos);
        var t = root.transform;
        var body = Lit(new Color(0.04f, 0.04f, 0.05f), 0.35f);
        var rim = Lit(new Color(0.3f, 0.3f, 0.33f), 0.7f, 0.9f);
        var neon = ClubBeat.PulseMat(new Color(0.2f, 0.85f, 1f), 0.5f, 0.5f);
        var cones = new System.Collections.Generic.List<Transform>();
        for (int i = 0; i < 2; i++)
        {
            float y = 0.8f + i * 1.6f;
            Part(PrimitiveType.Cube, t, new Vector3(0f, y, 0f), new Vector3(2.6f, 1.55f, 1.4f), body);
            foreach (float x in new[] { -0.6f, 0.6f })
                cones.Add(Part(PrimitiveType.Cylinder, t, new Vector3(x, y, -0.71f), new Vector3(0.95f, 0.04f, 0.95f), rim, new Vector3(90f, 0f, 0f)).transform);
            Part(PrimitiveType.Cube, t, new Vector3(0f, y + 0.78f, -0.71f), new Vector3(2.6f, 0.05f, 0.04f), neon);
        }
        var pulse = root.AddComponent<SpeakerPulse>();
        pulse.cones = cones.ToArray();
        return Hazard(root, "Speaker Stack", new Vector3(0f, 1.6f, 0f), new Vector3(2.6f, 3.2f, 1.4f), 15, 5, trigger: false);
    }

    /// <summary>
    /// A wall of bass across one lane, fed by speakers either side. It is ON (bright, deadly) for two
    /// beats, flickers a warning, then goes OFF (dark, safe) for two beats. Cross when it's dark.
    /// </summary>
    public static GameObject BassWall(Transform parent, Vector3 pos, int phaseOffsetBeats)
    {
        var root = RooftopProps.Root("Bass Wall", parent, pos);
        var t = root.transform;
        var body = Lit(new Color(0.04f, 0.04f, 0.05f), 0.35f);
        var rim = Lit(new Color(0.3f, 0.3f, 0.33f), 0.7f, 0.9f);
        foreach (float side in new[] { -1f, 1f })
        {
            Part(PrimitiveType.Cube, t, new Vector3(side * 2.55f, 1.4f, 0f), new Vector3(0.7f, 2.8f, 1f), body);
            Part(PrimitiveType.Cylinder, t, new Vector3(side * 2.18f, 1.4f, 0f), new Vector3(0.75f, 0.04f, 0.75f), rim, new Vector3(0f, 0f, 90f));
        }
        var wall = Part(PrimitiveType.Cube, t, new Vector3(0f, 1.4f, 0f), new Vector3(4.4f, 2.8f, 0.1f), RooftopProps.Glow(new Color(1f, 0.15f, 0.6f, 0.4f)));
        var rings = new Transform[3];
        for (int i = 0; i < rings.Length; i++)
            rings[i] = Part(PrimitiveType.Cube, t, new Vector3(0f, 1.4f, 0f), new Vector3(4.4f, 2.8f, 0.04f), RooftopProps.Glow(new Color(1f, 0.3f, 0.8f, 0.2f))).transform;
        var floor = Part(PrimitiveType.Cube, t, new Vector3(0f, 0.03f, 0f), new Vector3(4.6f, 0.01f, 1.6f), RooftopProps.Glow(new Color(1f, 0.15f, 0.6f, 0.2f)));

        var h = root.AddComponent<HealthDecreaseObstacle>();
        h.damage = 12;
        h.vibeDamage = 5;
        h.displayName = "Bass Wall";
        var bw = root.AddComponent<BassWall>();
        bw.wall = wall.GetComponent<Renderer>();
        bw.floor = floor.GetComponent<Renderer>();
        bw.rings = rings;
        bw.phaseOffsetBeats = phaseOffsetBeats;
        return root;
    }

    /// <summary>A floor-to-ceiling red laser gate that slides between two lanes on the beat.</summary>
    public static GameObject SweepLaser(Transform parent, float z, float xA, float xB, float beatsPerSweep)
    {
        var root = RooftopProps.Root("Sweeping Laser", parent, new Vector3(xA, 0f, z));
        var t = root.transform;
        var head = Lit(new Color(0.08f, 0.08f, 0.1f), 0.6f, 0.7f);
        Part(PrimitiveType.Cube, t, new Vector3(0f, 0.15f, 0f), new Vector3(0.7f, 0.3f, 0.7f), head);
        Part(PrimitiveType.Cube, t, new Vector3(0f, ClubHall.CeilingY - 1.2f, 0f), new Vector3(0.7f, 0.3f, 0.7f), head);
        Part(PrimitiveType.Cube, t, new Vector3(0f, (ClubHall.CeilingY - 1.2f) * 0.5f, 0f), new Vector3(0.08f, ClubHall.CeilingY - 1.4f, 0.08f), RooftopProps.Glow(Red));
        Part(PrimitiveType.Cube, t, new Vector3(0f, (ClubHall.CeilingY - 1.2f) * 0.5f, 0f), new Vector3(0.5f, ClubHall.CeilingY - 1.4f, 0.5f), RooftopProps.Glow(new Color(1f, 0.1f, 0.08f, 0.15f)));
        Part(PrimitiveType.Cube, t, new Vector3(0f, 0.03f, 0f), new Vector3(1.6f, 0.01f, 1.6f), RooftopProps.Glow(new Color(1f, 0.1f, 0.08f, 0.25f)));
        // Track on the floor showing where it travels.
        var track = Part(PrimitiveType.Cube, parent, Vector3.zero, new Vector3(Mathf.Abs(xB - xA), 0.01f, 0.25f), RooftopProps.Glow(new Color(1f, 0.1f, 0.08f, 0.18f)));
        track.transform.position = new Vector3((xA + xB) * 0.5f, 0.025f, z);
        track.transform.SetParent(t, true);
        var sweep = root.AddComponent<SweepLaser>();
        sweep.xA = xA;
        sweep.xB = xB;
        sweep.beatsPerSweep = beatsPerSweep;
        sweep.track = track.transform;
        return Hazard(root, "Sweeping Laser", new Vector3(0f, 1.6f, 0f), new Vector3(0.9f, 3.2f, 0.9f), 14, 6);
    }

    /// <summary>A lighting truss that drops into a lane when the runner gets close (with a warning).</summary>
    public static GameObject FallingRig(Transform parent, Vector3 pos)
    {
        var root = RooftopProps.Root("Falling Light Rig", parent, pos);
        var t = root.transform;
        var rig = new GameObject("Rig").transform;
        rig.SetParent(t, false);
        rig.localPosition = new Vector3(0f, ClubHall.CeilingY - 1.6f, 0f);
        var metal = Lit(new Color(0.18f, 0.18f, 0.2f), 0.6f, 0.85f);
        Part(PrimitiveType.Cube, rig, new Vector3(0f, 0.6f, 0f), new Vector3(4.6f, 0.25f, 0.25f), metal);
        Part(PrimitiveType.Cube, rig, new Vector3(0f, 0f, 0f), new Vector3(4.6f, 0.25f, 0.25f), metal);
        for (int i = 0; i < 5; i++)
            Part(PrimitiveType.Cube, rig, new Vector3(-2.2f + i * 1.1f, 0.3f, 0f), new Vector3(0.1f, 0.6f, 0.1f), metal, new Vector3(0f, 0f, i % 2 == 0 ? 35f : -35f));
        foreach (float x in new[] { -1.5f, 0f, 1.5f })
        {
            Part(PrimitiveType.Cylinder, rig, new Vector3(x, -0.35f, 0f), new Vector3(0.45f, 0.3f, 0.45f), Lit(new Color(0.05f, 0.05f, 0.06f), 0.4f));
            Part(PrimitiveType.Sphere, rig, new Vector3(x, -0.6f, 0f), Vector3.one * 0.3f, RooftopProps.Glow(new Color(1f, 0.95f, 0.85f, 1f)));
        }
        var warn = Part(PrimitiveType.Cube, t, new Vector3(0f, 0.03f, 0f), new Vector3(4.4f, 0.01f, 1.8f), RooftopProps.Glow(new Color(1f, 0.15f, 0.1f, 0f)));

        var obstacle = Hazard(root, "Falling Light Rig", new Vector3(0f, 0.9f, 0f), new Vector3(4.4f, 1.8f, 1f), 15, 6, trigger: false);
        var col = root.GetComponent<BoxCollider>();
        col.enabled = false;
        var fall = root.AddComponent<FallingRig>();
        fall.rig = rig;
        fall.warning = warn.GetComponent<Renderer>();
        fall.hitBox = col;
        return obstacle;
    }
}
