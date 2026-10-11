using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds Level 1 rooftop hazards, pickups and set pieces from primitives at runtime.
/// Every builder returns the root so the caller can parent it to a street segment.
/// </summary>
public static class RooftopProps
{
    public const float LaneWidth = 4f;
    const float StreetHalfWidth = 8f;

    static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Mats.Clear();

    // ── Materials ────────────────────────────────────────────────────────────

    public static Material Mat(Color color, float emission = 0f, bool transparent = false)
    {
        string key = $"{color}{emission}{transparent}";
        if (Mats.TryGetValue(key, out var cached) && cached != null) return cached;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(shader);
        m.SetColor("_BaseColor", color);
        m.color = color;
        if (emission > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        if (transparent)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        Mats[key] = m;
        return m;
    }

    /// <summary>Unlit transparent glow; unaffected by sky reflections.</summary>
    public static Material Glow(Color color)
    {
        string key = $"glow{color}";
        if (Mats.TryGetValue(key, out var cached) && cached != null) return cached;

        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) return Mat(color, 2f, true);
        var m = new Material(shader);
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        Mats[key] = m;
        return m;
    }

    // ── Primitive helpers ────────────────────────────────────────────────────

    public static GameObject Root(string name, Transform parent, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
        return go;
    }

    public static GameObject Part(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 size,
                                  Material mat, bool keepCollider = true, Vector3 euler = default)
    {
        var go = GameObject.CreatePrimitive(type);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localEulerAngles = euler;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!keepCollider) Object.Destroy(go.GetComponent<Collider>());
        return go;
    }

    static GameObject MakeSolidHazard(GameObject root, int damage = 15)
    {
        root.tag = "HealthDEC";
        var h = root.AddComponent<HealthDecreaseObstacle>();
        h.damage = damage;
        h.vibeDamage = 5;
        foreach (var c in root.GetComponentsInChildren<Collider>()) c.isTrigger = false;
        GameplayCollisionUtility.EnsureKinematicRigidbody(root);
        return root;
    }

    static void Solid(GameObject root, Vector3 center, Vector3 size)
    {
        var box = root.AddComponent<BoxCollider>();
        box.center = center;
        box.size = size;
    }

    static BoxCollider Trigger(GameObject root, Vector3 center, Vector3 size)
    {
        var box = root.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = center;
        box.size = size;
        GameplayCollisionUtility.EnsureKinematicRigidbody(root);
        return box;
    }

    // ── Hazards (disadvantages) ──────────────────────────────────────────────

    /// <summary>Low air-conditioning unit — jump it.</summary>
    public static GameObject AcUnit(Transform parent, Vector3 pos)
    {
        var root = Root("AC Unit", parent, pos);
        Part(PrimitiveType.Cube, root.transform, new Vector3(0, 0.55f, 0), new Vector3(2.6f, 1.1f, 1.4f), Mat(new Color(0.62f, 0.64f, 0.66f)));
        Part(PrimitiveType.Cylinder, root.transform, new Vector3(0, 1.12f, 0), new Vector3(0.9f, 0.04f, 0.9f), Mat(new Color(0.15f, 0.15f, 0.17f)), false);
        Part(PrimitiveType.Cube, root.transform, new Vector3(0, 0.55f, -0.71f), new Vector3(2.2f, 0.7f, 0.02f), Mat(new Color(0.3f, 0.32f, 0.35f)), false);
        return MakeSolidHazard(root);
    }

    /// <summary>Tall water tank on legs — dodge it.</summary>
    public static GameObject WaterTank(Transform parent, Vector3 pos)
    {
        var root = Root("Water Tank", parent, pos);
        var wood = Mat(new Color(0.45f, 0.28f, 0.16f));
        Part(PrimitiveType.Cylinder, root.transform, new Vector3(0, 3.4f, 0), new Vector3(2.8f, 1.5f, 2.8f), wood);
        Part(PrimitiveType.Cylinder, root.transform, new Vector3(0, 5.1f, 0), new Vector3(3f, 0.5f, 3f), Mat(new Color(0.25f, 0.2f, 0.18f)), false);
        var metal = Mat(new Color(0.2f, 0.2f, 0.22f));
        foreach (var x in new[] { -1.1f, 1.1f })
            foreach (var z in new[] { -1.1f, 1.1f })
                Part(PrimitiveType.Cube, root.transform, new Vector3(x, 1f, z), new Vector3(0.22f, 2f, 0.22f), metal);
        Solid(root, new Vector3(0, 1f, 0), new Vector3(2.4f, 2f, 2.4f));
        return MakeSolidHazard(root, 20);
    }

    /// <summary>Washing line strung across one lane at body height — dodge it.</summary>
    public static GameObject LaundryLine(Transform parent, Vector3 pos)
    {
        var root = Root("Laundry Line", parent, pos);
        var pole = Mat(new Color(0.3f, 0.3f, 0.32f));
        Part(PrimitiveType.Cylinder, root.transform, new Vector3(-1.9f, 1.3f, 0), new Vector3(0.12f, 1.3f, 0.12f), pole);
        Part(PrimitiveType.Cylinder, root.transform, new Vector3(1.9f, 1.3f, 0), new Vector3(0.12f, 1.3f, 0.12f), pole);
        Part(PrimitiveType.Cube, root.transform, new Vector3(0, 2.4f, 0), new Vector3(3.8f, 0.03f, 0.03f), Mat(Color.white), false);
        Color[] cloth = { new Color(0.9f, 0.3f, 0.35f), new Color(0.3f, 0.6f, 0.95f), new Color(0.98f, 0.85f, 0.3f), new Color(0.95f, 0.95f, 0.95f) };
        for (int i = 0; i < 4; i++)
            Part(PrimitiveType.Cube, root.transform, new Vector3(-1.35f + i * 0.9f, 1.75f, 0), new Vector3(0.75f, 1.2f, 0.04f), Mat(cloth[i]), false);
        Solid(root, new Vector3(0, 1.3f, 0), new Vector3(3.6f, 2.6f, 0.4f));
        return MakeSolidHazard(root, 10);
    }

    /// <summary>Glowing billboard sign on legs — dodge it.</summary>
    public static GameObject NeonSign(Transform parent, Vector3 pos)
    {
        var root = Root("Neon Sign", parent, pos);
        Color[] neon = { new Color(1f, 0.2f, 0.75f), new Color(0.2f, 0.9f, 1f), new Color(0.7f, 0.35f, 1f) };
        var c = neon[Random.Range(0, neon.Length)];
        Part(PrimitiveType.Cube, root.transform, new Vector3(0, 1.9f, 0), new Vector3(3.2f, 1.6f, 0.3f), Mat(new Color(0.06f, 0.05f, 0.1f)));
        Part(PrimitiveType.Cube, root.transform, new Vector3(0, 1.9f, -0.16f), new Vector3(2.8f, 0.25f, 0.02f), Mat(c, 4f), false);
        Part(PrimitiveType.Cube, root.transform, new Vector3(0, 2.35f, -0.16f), new Vector3(2.2f, 0.15f, 0.02f), Mat(c, 4f), false);
        Part(PrimitiveType.Cube, root.transform, new Vector3(0, 1.45f, -0.16f), new Vector3(1.6f, 0.15f, 0.02f), Mat(c, 4f), false);
        var leg = Mat(new Color(0.15f, 0.15f, 0.18f));
        Part(PrimitiveType.Cube, root.transform, new Vector3(-1.3f, 0.55f, 0), new Vector3(0.15f, 1.1f, 0.15f), leg);
        Part(PrimitiveType.Cube, root.transform, new Vector3(1.3f, 0.55f, 0), new Vector3(0.15f, 1.1f, 0.15f), leg);
        var light = root.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = c;
        light.range = 6f;
        light.intensity = 2f;
        return MakeSolidHazard(root, 15);
    }

    /// <summary>Crane hook swinging across the street — time it.</summary>
    public static GameObject CraneHook(Transform parent, Vector3 pos)
    {
        var root = Root("Crane Hook", parent, pos + Vector3.up * 9f);
        var pivot = new GameObject("Pivot");
        pivot.transform.SetParent(root.transform, false);
        Part(PrimitiveType.Cube, pivot.transform, new Vector3(0, -4f, 0), new Vector3(0.06f, 8f, 0.06f), Mat(new Color(0.1f, 0.1f, 0.1f)), false);
        var hook = Part(PrimitiveType.Cube, pivot.transform, new Vector3(0, -8.1f, 0), new Vector3(1.4f, 1.4f, 1.4f), Mat(new Color(1f, 0.75f, 0.1f)));
        Part(PrimitiveType.Cube, pivot.transform, new Vector3(0, -8.1f, -0.71f), new Vector3(1f, 0.25f, 0.02f), Mat(Color.black), false);
        hook.name = "Hook";
        var swing = root.AddComponent<SwingingHazard>();
        swing.pivot = pivot.transform;
        MakeSolidHazard(root, 20);
        return root;
    }

    /// <summary>Vent that puffs steam on a rhythm and shoves the player sideways.</summary>
    public static GameObject SteamVent(Transform parent, Vector3 pos, float phase)
    {
        var root = Root("Steam Vent", parent, pos);
        Part(PrimitiveType.Cylinder, root.transform, new Vector3(0, 0.05f, 0), new Vector3(1.6f, 0.05f, 1.6f), Mat(new Color(0.18f, 0.18f, 0.2f)), false);
        Part(PrimitiveType.Cylinder, root.transform, new Vector3(0, 0.07f, 0), new Vector3(1.1f, 0.02f, 1.1f), Mat(new Color(1f, 0.4f, 0.1f), 2f), false);
        var plume = Part(PrimitiveType.Cylinder, root.transform, new Vector3(0, 1.6f, 0), new Vector3(1.3f, 1.6f, 1.3f), Mat(new Color(1f, 1f, 1f, 0.45f), 0, true), false);
        var vent = root.AddComponent<global::SteamVent>();
        vent.plume = plume;
        vent.phase = phase;
        Trigger(root, new Vector3(0, 1.5f, 0), new Vector3(2.4f, 3f, 2f));
        return root;
    }

    /// <summary>Wet puddle — strafing turns sluggish.</summary>
    public static GameObject WetPatch(Transform parent, Vector3 pos, float width)
    {
        var root = Root("Wet Patch", parent, pos);
        Part(PrimitiveType.Cube, root.transform, new Vector3(0, 0.02f, 0), new Vector3(width, 0.02f, 7f), Mat(new Color(0.25f, 0.45f, 0.7f, 0.55f), 0.6f, true), false);
        root.AddComponent<global::WetPatch>();
        Trigger(root, new Vector3(0, 1f, 0), new Vector3(width, 2f, 7f));
        return root;
    }

    /// <summary>Alarm pressure plate — stepping on it makes the guard surge.</summary>
    public static GameObject AlarmPanel(Transform parent, Vector3 pos)
    {
        var root = Root("Alarm Panel", parent, pos);
        Part(PrimitiveType.Cube, root.transform, new Vector3(0, 0.03f, 0), new Vector3(3.2f, 0.06f, 2.4f), Mat(new Color(0.12f, 0.02f, 0.02f)), false);
        var lamp = Part(PrimitiveType.Cube, root.transform, new Vector3(0, 0.07f, 0), new Vector3(2.6f, 0.02f, 1.8f), Mat(new Color(1f, 0.1f, 0.1f), 3f), false);
        var alarm = root.AddComponent<global::AlarmPanel>();
        alarm.lamp = lamp.GetComponent<Renderer>();
        Trigger(root, new Vector3(0, 1f, 0), new Vector3(3f, 2f, 2.2f));
        return root;
    }

    /// <summary>Full-width gap between buildings. Optional zip-line over one lane.</summary>
    public static GameObject Gap(Transform parent, Vector3 center, float length, bool zipLine, float zipLaneX, bool crumble = false)
    {
        var root = Root(crumble ? "Crumbling Roof" : "Roof Gap", parent, center);
        float width = crumble ? LaneWidth : StreetHalfWidth * 2f + 1f;

        var pit = Part(PrimitiveType.Cube, root.transform, new Vector3(0, 0.015f, 0), new Vector3(width, 0.03f, length),
                       Mat(crumble ? new Color(0.4f, 0.36f, 0.33f) : new Color(0.01f, 0.01f, 0.02f)), false);
        if (!crumble)
        {
            Part(PrimitiveType.Cube, root.transform, new Vector3(0, -6f, 0), new Vector3(width, 12f, length), Mat(new Color(0.02f, 0.02f, 0.04f)), false);
            var stripe = Mat(new Color(1f, 0.8f, 0.1f), 1.2f);
            Part(PrimitiveType.Cube, root.transform, new Vector3(0, 0.035f, -length * 0.5f - 0.2f), new Vector3(width, 0.04f, 0.4f), stripe, false);
            Part(PrimitiveType.Cube, root.transform, new Vector3(0, 0.035f, length * 0.5f + 0.2f), new Vector3(width, 0.04f, 0.4f), stripe, false);
        }
        else
        {
            var crack = Mat(new Color(0.12f, 0.1f, 0.09f));
            for (int i = 0; i < 4; i++)
                Part(PrimitiveType.Cube, root.transform, new Vector3(Random.Range(-1.4f, 1.4f), 0.035f, Random.Range(-1.4f, 1.4f)),
                     new Vector3(Random.Range(0.8f, 2f), 0.01f, 0.08f), crack, false, new Vector3(0, Random.Range(0f, 180f), 0));
        }

        var gap = root.AddComponent<RooftopGap>();
        gap.crumble = crumble;
        gap.surface = pit.transform;
        Trigger(root, new Vector3(0, 0.3f, 0), new Vector3(width - 0.2f, 0.6f, Mathf.Max(0.5f, length - 0.8f)));

        if (zipLine && !crumble)
            ZipLine(parent, new Vector3(center.x + zipLaneX, center.y, center.z - length * 0.5f - 3f),
                            new Vector3(center.x + zipLaneX, center.y, center.z + length * 0.5f + 3f));
        return root;
    }

    /// <summary>Zip-line between two posts. Running into the start handle carries the player.</summary>
    public static GameObject ZipLine(Transform parent, Vector3 startFeet, Vector3 endFeet)
    {
        var root = Root("Zip Line", parent, startFeet);
        var post = Mat(new Color(0.22f, 0.22f, 0.26f));
        var glow = Mat(new Color(0.35f, 0.85f, 1f), 3f);
        float topY = 3.6f;
        Vector3 end = endFeet - startFeet;

        Part(PrimitiveType.Cylinder, root.transform, new Vector3(-1.6f, topY * 0.5f, 0), new Vector3(0.18f, topY * 0.5f, 0.18f), post, false);
        Part(PrimitiveType.Cylinder, root.transform, end + new Vector3(-1.6f, topY * 0.5f, 0), new Vector3(0.18f, topY * 0.5f, 0.18f), post, false);
        Part(PrimitiveType.Cube, root.transform, new Vector3(-0.8f, topY, 0), new Vector3(1.6f, 0.12f, 0.12f), post, false);
        Part(PrimitiveType.Cube, root.transform, end + new Vector3(-0.8f, topY, 0), new Vector3(1.6f, 0.12f, 0.12f), post, false);

        float len = end.magnitude;
        Part(PrimitiveType.Cube, root.transform, new Vector3(0, topY, len * 0.5f), new Vector3(0.05f, 0.05f, len), glow, false);
        Part(PrimitiveType.Cube, root.transform, new Vector3(0, topY - 0.9f, 0.4f), new Vector3(0.8f, 0.08f, 0.08f), Mat(new Color(1f, 0.8f, 0.1f), 2f), false);
        Part(PrimitiveType.Cube, root.transform, new Vector3(0, topY - 0.45f, 0.4f), new Vector3(0.04f, 0.9f, 0.04f), post, false);

        var zip = root.AddComponent<global::ZipLine>();
        zip.endFeet = endFeet;
        Trigger(root, new Vector3(0, 1.4f, 0.4f), new Vector3(2.4f, 2.8f, 1.6f));
        return root;
    }

    /// <summary>Launch ramp — hitting it flings the player forward through the air.</summary>
    public static GameObject Ramp(Transform parent, Vector3 pos, float launchDistance)
    {
        var root = Root("Launch Ramp", parent, pos);
        Part(PrimitiveType.Cube, root.transform, new Vector3(0, 0.45f, 0), new Vector3(3.2f, 0.12f, 3.6f),
             Mat(new Color(0.85f, 0.45f, 0.1f)), false, new Vector3(-14f, 0, 0));
        Part(PrimitiveType.Cube, root.transform, new Vector3(0, 0.45f, 0.02f), new Vector3(0.5f, 0.13f, 3.4f),
             Mat(new Color(1f, 0.85f, 0.2f), 2f), false, new Vector3(-14f, 0, 0));
        Part(PrimitiveType.Cube, root.transform, new Vector3(0, 0.45f, 1.7f), new Vector3(3.2f, 0.9f, 0.2f), Mat(new Color(0.3f, 0.2f, 0.12f)), false);
        var ramp = root.AddComponent<LaunchRamp>();
        ramp.distance = launchDistance;
        Trigger(root, new Vector3(0, 1f, 0.6f), new Vector3(3f, 2f, 2f));
        return root;
    }

    /// <summary>Decorative pigeons that scatter as the player approaches.</summary>
    public static GameObject Pigeons(Transform parent, Vector3 pos)
    {
        var root = Root("Pigeons", parent, pos);
        var grey = Mat(new Color(0.55f, 0.56f, 0.6f));
        int count = Random.Range(4, 8);
        for (int i = 0; i < count; i++)
        {
            var bird = Part(PrimitiveType.Sphere, root.transform,
                new Vector3(Random.Range(-1.8f, 1.8f), 0.18f, Random.Range(-1.2f, 1.2f)), new Vector3(0.35f, 0.28f, 0.45f), grey, false);
            Part(PrimitiveType.Sphere, bird.transform, new Vector3(0, 0.55f, 0.55f), new Vector3(0.55f, 0.55f, 0.55f), Mat(new Color(0.3f, 0.32f, 0.38f)), false);
        }
        root.AddComponent<PigeonFlock>();
        return root;
    }

    // ── Pickups (advantages) ─────────────────────────────────────────────────

    public static GameObject Note(Transform parent, Vector3 pos)
    {
        var root = Root("Note", parent, pos);
        var gold = Mat(new Color(1f, 0.82f, 0.2f), 2.2f);
        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        Part(PrimitiveType.Sphere, visual.transform, new Vector3(-0.12f, 0, 0), new Vector3(0.42f, 0.34f, 0.2f), gold, false);
        Part(PrimitiveType.Cube, visual.transform, new Vector3(0.06f, 0.36f, 0), new Vector3(0.07f, 0.72f, 0.07f), gold, false);
        Part(PrimitiveType.Cube, visual.transform, new Vector3(0.2f, 0.64f, 0), new Vector3(0.3f, 0.09f, 0.07f), gold, false);
        root.AddComponent<RunNote>().visual = visual.transform;
        var sphere = root.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = 0.8f;
        GameplayCollisionUtility.EnsureKinematicRigidbody(root);
        return root;
    }

    public static GameObject Power(Transform parent, Vector3 pos, PowerPickup.Kind kind)
    {
        var root = Root(kind + " Pickup", parent, pos);
        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        switch (kind)
        {
            case PowerPickup.Kind.Magnet:
                var red = Mat(new Color(1f, 0.25f, 0.25f), 1.5f);
                Part(PrimitiveType.Cube, visual.transform, new Vector3(-0.3f, 0, 0), new Vector3(0.22f, 0.8f, 0.22f), red, false);
                Part(PrimitiveType.Cube, visual.transform, new Vector3(0.3f, 0, 0), new Vector3(0.22f, 0.8f, 0.22f), red, false);
                Part(PrimitiveType.Cube, visual.transform, new Vector3(0, 0.4f, 0), new Vector3(0.82f, 0.22f, 0.22f), red, false);
                Part(PrimitiveType.Cube, visual.transform, new Vector3(-0.3f, -0.45f, 0), new Vector3(0.23f, 0.15f, 0.23f), Mat(Color.white, 1f), false);
                Part(PrimitiveType.Cube, visual.transform, new Vector3(0.3f, -0.45f, 0), new Vector3(0.23f, 0.15f, 0.23f), Mat(Color.white, 1f), false);
                break;
            case PowerPickup.Kind.Bubble:
                Part(PrimitiveType.Sphere, visual.transform, Vector3.zero, Vector3.one * 0.9f, Mat(new Color(0.35f, 0.85f, 1f, 0.55f), 2f, true), false);
                Part(PrimitiveType.Sphere, visual.transform, Vector3.zero, Vector3.one * 0.4f, Mat(new Color(0.8f, 0.97f, 1f), 3f), false);
                break;
            default:
                Part(PrimitiveType.Sphere, visual.transform, Vector3.zero, Vector3.one * 0.7f, Mat(new Color(0.25f, 0.25f, 0.28f)), false);
                Part(PrimitiveType.Cylinder, visual.transform, new Vector3(0, 0.42f, 0), new Vector3(0.18f, 0.1f, 0.18f), Mat(new Color(0.5f, 0.5f, 0.5f)), false);
                Part(PrimitiveType.Cube, visual.transform, new Vector3(0, 0.6f, 0), new Vector3(0.05f, 0.2f, 0.05f), Mat(new Color(1f, 0.6f, 0.1f), 4f), false);
                break;
        }
        var power = root.AddComponent<PowerPickup>();
        power.kind = kind;
        power.visual = visual.transform;
        var sphere = root.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = 0.9f;
        GameplayCollisionUtility.EnsureKinematicRigidbody(root);
        return root;
    }
}