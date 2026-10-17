using UnityEngine;

/// <summary>Level 2 museum hazards. Red means danger: every hazard carries red glow.</summary>
public static class MuseumProps
{
    static readonly Color Red = new Color(1f, 0.12f, 0.08f, 0.95f);

    static GameObject Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 size, Material mat, Vector3 euler = default)
        => RooftopProps.Part(type, parent, pos, size, mat, false, euler);

    static GameObject Hazard(GameObject root, Vector3 center, Vector3 size, int damage, int vibe, bool trigger = false)
    {
        root.tag = "HealthDEC";
        var h = root.AddComponent<HealthDecreaseObstacle>();
        h.damage = damage;
        h.vibeDamage = vibe;
        var box = root.AddComponent<BoxCollider>();
        box.center = center;
        box.size = size;
        box.isTrigger = trigger;
        GameplayCollisionUtility.EnsureKinematicRigidbody(root);
        return root;
    }

    /// <summary>A grenade planted on the carpet, red light blinking. Dodge or jump it - touch it and it blows.</summary>
    public static GameObject PlantedGrenade(Transform parent, Vector3 pos)
    {
        var root = RooftopProps.Root("Planted Grenade", parent, pos);
        var t = root.transform;
        // A little sandbag nest makes it read from a distance; the grenade sits in it.
        var bag = MuseumHall.Lit(new Color(0.42f, 0.36f, 0.24f), 0.15f);
        for (int i = 0; i < 5; i++)
        {
            float a = i / 5f * Mathf.PI * 2f + 0.3f;
            Part(PrimitiveType.Sphere, t, new Vector3(Mathf.Cos(a) * 0.5f, 0.09f, Mathf.Sin(a) * 0.5f),
                 new Vector3(0.46f, 0.18f, 0.28f), bag, new Vector3(0f, -a * Mathf.Rad2Deg, 0f));
        }
        var led = Grenade.Build(t, new Vector3(0f, 0.02f, 0f), 1.8f);
        led.transform.localScale = Vector3.one * 0.09f;
        var glow = new GameObject("Warning Light");
        glow.transform.SetParent(t, false);
        glow.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        var light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.12f, 0.05f);
        light.range = 3.2f;
        light.intensity = 4f;
        light.shadows = LightShadows.None;
        var blink = root.AddComponent<PlantedGrenadeBlink>();
        blink.led = led;
        blink.warningLight = light;
        var h = Hazard(root, new Vector3(0f, 0.35f, 0f), new Vector3(1.1f, 0.7f, 1.1f), 10, 8);
        h.GetComponent<HealthDecreaseObstacle>().displayName = "Planted Grenade";
        return h;
    }

    /// <summary>Low red laser across one lane — jump it.</summary>
    public static GameObject LowLaser(Transform parent, Vector3 pos)
    {
        var root = RooftopProps.Root("Low Laser", parent, pos);
        var t = root.transform;
        var post = MuseumHall.Lit(new Color(0.12f, 0.12f, 0.14f), 0.6f, 0.6f);
        foreach (float side in new[] { -1f, 1f })
        {
            Part(PrimitiveType.Cube, t, new Vector3(side * 1.7f, 0.4f, 0f), new Vector3(0.18f, 0.8f, 0.18f), post);
            Part(PrimitiveType.Sphere, t, new Vector3(side * 1.7f, 0.82f, 0f), Vector3.one * 0.16f, RooftopProps.Glow(Red));
        }
        Part(PrimitiveType.Cube, t, new Vector3(0f, 0.45f, 0f), new Vector3(3.4f, 0.05f, 0.05f), RooftopProps.Glow(Red));
        Part(PrimitiveType.Cube, t, new Vector3(0f, 0.45f, 0f), new Vector3(3.4f, 0.16f, 0.16f), RooftopProps.Glow(new Color(1f, 0.1f, 0.05f, 0.18f)));
        Part(PrimitiveType.Cube, t, new Vector3(0f, 0.015f, 0f), new Vector3(3.6f, 0.01f, 1.4f), RooftopProps.Glow(new Color(1f, 0.1f, 0.05f, 0.12f)));
        return Hazard(root, new Vector3(0f, 0.45f, 0f), new Vector3(3.2f, 0.5f, 0.4f), 10, 4);
    }

    /// <summary>Full-height laser grid filling one lane — dodge it.</summary>
    public static GameObject LaserGrid(Transform parent, Vector3 pos)
    {
        var root = RooftopProps.Root("Laser Grid", parent, pos);
        var t = root.transform;
        var post = MuseumHall.Lit(new Color(0.12f, 0.12f, 0.14f), 0.6f, 0.6f);
        foreach (float side in new[] { -1f, 1f })
            Part(PrimitiveType.Cube, t, new Vector3(side * 1.75f, 1.4f, 0f), new Vector3(0.2f, 2.8f, 0.2f), post);
        Part(PrimitiveType.Cube, t, new Vector3(0f, 2.85f, 0f), new Vector3(3.7f, 0.15f, 0.2f), post);
        var beam = RooftopProps.Glow(Red);
        for (int i = 0; i < 5; i++)
            Part(PrimitiveType.Cube, t, new Vector3(0f, 0.35f + i * 0.55f, 0f), new Vector3(3.4f, 0.04f, 0.04f), beam);
        Part(PrimitiveType.Cube, t, new Vector3(0f, 1.4f, 0f), new Vector3(3.4f, 2.7f, 0.05f), RooftopProps.Glow(new Color(1f, 0.08f, 0.05f, 0.12f)));
        root.AddComponent<LaserFlicker>();
        return Hazard(root, new Vector3(0f, 1.4f, 0f), new Vector3(3.2f, 2.8f, 0.5f), 18, 6);
    }

    /// <summary>Velvet-rope barrier across one lane — jump it (low damage).</summary>
    public static GameObject RopeBarrier(Transform parent, Vector3 pos)
    {
        var root = RooftopProps.Root("Rope Barrier", parent, pos);
        var t = root.transform;
        var gold = MuseumHall.Lit(new Color(0.78f, 0.6f, 0.26f), 0.65f, 0.8f);
        foreach (float side in new[] { -1f, 0f, 1f })
        {
            Part(PrimitiveType.Cylinder, t, new Vector3(side * 1.6f, 0.45f, 0f), new Vector3(0.12f, 0.45f, 0.12f), gold);
            Part(PrimitiveType.Sphere, t, new Vector3(side * 1.6f, 0.95f, 0f), Vector3.one * 0.18f, gold);
        }
        Part(PrimitiveType.Cube, t, new Vector3(0f, 0.72f, 0f), new Vector3(3.2f, 0.09f, 0.09f), MuseumHall.Lit(new Color(0.55f, 0.04f, 0.07f), 0.3f));
        Part(PrimitiveType.Cube, t, new Vector3(0f, 0.015f, 0f), new Vector3(3.6f, 0.01f, 1.2f), RooftopProps.Glow(new Color(1f, 0.15f, 0.08f, 0.1f)));
        return Hazard(root, new Vector3(0f, 0.55f, 0f), new Vector3(3.2f, 0.9f, 0.4f), 8, 3);
    }

    /// <summary>Marble statue on a plinth blocking a lane — dodge it.</summary>
    public static GameObject Statue(Transform parent, Vector3 pos)
    {
        var root = RooftopProps.Root("Statue", parent, pos);
        var t = root.transform;
        var marble = MuseumHall.Lit(new Color(0.86f, 0.84f, 0.8f), 0.75f);
        Part(PrimitiveType.Cube, t, new Vector3(0f, 0.5f, 0f), new Vector3(2.2f, 1f, 2.2f), marble);
        Part(PrimitiveType.Cube, t, new Vector3(0f, 1.02f, 0f), new Vector3(2.3f, 0.06f, 2.3f), RooftopProps.Glow(Red));
        Part(PrimitiveType.Capsule, t, new Vector3(0f, 2f, 0f), new Vector3(0.9f, 0.95f, 0.7f), marble);
        Part(PrimitiveType.Sphere, t, new Vector3(0f, 3.15f, 0f), Vector3.one * 0.6f, marble);
        Part(PrimitiveType.Cube, t, new Vector3(0f, 0.015f, 0f), new Vector3(3f, 0.01f, 3f), RooftopProps.Glow(new Color(1f, 0.12f, 0.08f, 0.15f)));
        return Hazard(root, new Vector3(0f, 1.6f, 0f), new Vector3(2.2f, 3.2f, 2.2f), 15, 5, trigger: false);
    }

    /// <summary>Drum that rolls down its lane toward the player once they are close.</summary>
    public static GameObject RollingDrum(Transform parent, Vector3 pos, GameObject model)
    {
        var root = RooftopProps.Root("Rolling Drum", parent, pos);
        var t = root.transform;
        var body = new GameObject("Body");
        body.transform.SetParent(t, false);
        body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        if (model != null)
        {
            var m = Object.Instantiate(model, body.transform);
            foreach (var c in m.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            foreach (var mb in m.GetComponentsInChildren<MonoBehaviour>()) Object.Destroy(mb);
            var rs = m.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                float largest = Mathf.Max(b.size.x, b.size.y, b.size.z);
                if (largest > 0.001f) m.transform.localScale *= 1.8f / largest;
            }
            m.transform.localPosition = Vector3.zero;
            body.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
        }
        else
        {
            Part(PrimitiveType.Cylinder, body.transform, Vector3.zero, new Vector3(1.7f, 0.8f, 1.7f), MuseumHall.Lit(new Color(0.5f, 0.12f, 0.08f), 0.5f), new Vector3(0f, 0f, 90f));
        }
        Part(PrimitiveType.Cylinder, t, new Vector3(0f, 0.02f, 0f), new Vector3(2.4f, 0.01f, 2.4f), RooftopProps.Glow(new Color(1f, 0.12f, 0.08f, 0.35f)));
        root.AddComponent<RollingDrum>().body = body.transform;
        return Hazard(root, new Vector3(0f, 0.9f, 0f), new Vector3(1.8f, 1.8f, 1.8f), 15, 5);
    }
}
