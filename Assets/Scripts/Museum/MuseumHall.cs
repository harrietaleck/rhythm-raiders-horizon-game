using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Builds the Level 2 night-museum interior from primitives and instrument models:
/// coffered ceiling with skylight, marble columns, lit paintings, chandeliers,
/// carpet runner, guide lights and glass exhibit cases. Decoration only — no colliders.
/// </summary>
public static class MuseumHall
{
    public const float HalfWidth = 12.5f;
    public const float CeilingY = 9f;
    public const float BayLength = 12f;
    const float LaneEdge = 9.3f;

    static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Mats.Clear();

    static readonly Color Marble = new Color(0.86f, 0.84f, 0.8f);
    static readonly Color Gold = new Color(0.78f, 0.6f, 0.26f);
    static readonly Color DarkWood = new Color(0.22f, 0.13f, 0.07f);
    static readonly Color Velvet = new Color(0.45f, 0.04f, 0.07f);
    static readonly Color WarmLight = new Color(1f, 0.82f, 0.55f);

    static readonly Color[] Canvases =
    {
        new Color(0.55f, 0.2f, 0.15f), new Color(0.15f, 0.25f, 0.45f), new Color(0.25f, 0.38f, 0.22f),
        new Color(0.5f, 0.4f, 0.18f), new Color(0.32f, 0.16f, 0.36f), new Color(0.12f, 0.32f, 0.36f),
    };

    // ── Materials ────────────────────────────────────────────────────────────

    public static Material Lit(Color color, float smoothness = 0.4f, float metallic = 0f, Texture texture = null, float tiling = 1f)
    {
        string key = $"{color}{smoothness}{metallic}{(texture != null ? texture.GetInstanceID() : 0)}{tiling}";
        if (Mats.TryGetValue(key, out var cached) && cached != null) return cached;

        var m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", smoothness);
        m.SetFloat("_Metallic", metallic);
        if (texture != null)
        {
            m.SetTexture("_BaseMap", texture);
            m.SetTextureScale("_BaseMap", Vector2.one * tiling);
        }
        Mats[key] = m;
        return m;
    }

    static GameObject Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 size, Material mat, Vector3 euler = default)
        => RooftopProps.Part(type, parent, pos, size, mat, false, euler);

    // ── Bay ─────────────────────────────────────────────────────────────────

    /// <summary>One 12 m bay of hall starting at <paramref name="z"/>.</summary>
    public static GameObject Bay(Transform parent, float z, int index, Texture ceilingTexture, IList<GameObject> instruments)
    {
        var root = RooftopProps.Root("MuseumBay_" + index, parent, new Vector3(0f, 0f, z));
        var t = root.transform;
        float mid = BayLength * 0.5f;
        var gold = Lit(Gold, 0.65f, 0.8f);
        var marble = Lit(Marble, 0.75f);

        // Coffered ceiling either side of a long skylight.
        var ceiling = Lit(ceilingTexture != null ? Color.white : new Color(0.75f, 0.7f, 0.62f), 0.2f, 0f, ceilingTexture, 2f);
        foreach (float side in new[] { -1f, 1f })
            Part(PrimitiveType.Cube, t, new Vector3(side * 7.75f, CeilingY + 0.2f, mid), new Vector3(9.5f, 0.4f, BayLength), ceiling);
        Part(PrimitiveType.Cube, t, new Vector3(0f, CeilingY + 0.9f, mid), new Vector3(6f, 0.05f, BayLength), RooftopProps.Glow(new Color(0.45f, 0.6f, 1f, 0.22f)));
        foreach (float side in new[] { -1f, 1f })
            Part(PrimitiveType.Cube, t, new Vector3(side * 3.05f, CeilingY + 0.55f, mid), new Vector3(0.18f, 0.9f, BayLength), Lit(DarkWood, 0.3f));
        for (int i = 0; i < 4; i++)
            Part(PrimitiveType.Cube, t, new Vector3(0f, CeilingY + 0.85f, i * 3f), new Vector3(6f, 0.08f, 0.08f), Lit(DarkWood, 0.3f));
        Part(PrimitiveType.Cube, t, new Vector3(0f, CeilingY - 0.15f, 0f), new Vector3(HalfWidth * 2f, 0.6f, 0.55f), gold);

        // Marble columns with gold base and capital against both walls.
        foreach (float side in new[] { -1f, 1f })
        {
            float x = side * (HalfWidth - 0.9f);
            Part(PrimitiveType.Cylinder, t, new Vector3(x, CeilingY * 0.5f, 0f), new Vector3(1.1f, CeilingY * 0.5f, 1.1f), marble);
            Part(PrimitiveType.Cube, t, new Vector3(x, 0.2f, 0f), new Vector3(1.6f, 0.4f, 1.6f), gold);
            Part(PrimitiveType.Cube, t, new Vector3(x, CeilingY - 0.3f, 0f), new Vector3(1.6f, 0.5f, 1.6f), gold);
        }

        // Lit painting on each wall between the columns.
        foreach (float side in new[] { -1f, 1f })
        {
            float x = side * (HalfWidth - 0.12f);
            var canvas = Canvases[(index * 2 + (side > 0 ? 1 : 0)) % Canvases.Length];
            Part(PrimitiveType.Cube, t, new Vector3(x, 4f, mid), new Vector3(0.12f, 2.8f, 3.8f), gold);
            Part(PrimitiveType.Cube, t, new Vector3(x - side * 0.08f, 4f, mid), new Vector3(0.04f, 2.3f, 3.3f), Lit(canvas, 0.1f));
            Part(PrimitiveType.Cube, t, new Vector3(x - side * 0.1f, 4f, mid), new Vector3(0.02f, 1.2f, 1.2f),
                 Lit(Color.Lerp(canvas, Color.white, 0.35f), 0.1f), new Vector3(45f, 0f, 0f));
            Part(PrimitiveType.Cube, t, new Vector3(x - side * 0.3f, 5.75f, mid), new Vector3(0.25f, 0.12f, 1.6f), gold);
            Part(PrimitiveType.Cube, t, new Vector3(x - side * 0.06f, 4.6f, mid), new Vector3(0.02f, 2.6f, 4.2f),
                 RooftopProps.Glow(new Color(1f, 0.85f, 0.55f, 0.12f)));
        }

        // Carpet runner over the lanes with gold trim, and cyan guide lights at the skirting.
        Part(PrimitiveType.Cube, t, new Vector3(0f, 0.012f, mid), new Vector3(LaneEdge * 2f, 0.02f, BayLength), Lit(Velvet, 0.15f));
        foreach (float side in new[] { -1f, 1f })
        {
            Part(PrimitiveType.Cube, t, new Vector3(side * LaneEdge, 0.02f, mid), new Vector3(0.25f, 0.025f, BayLength), gold);
            Part(PrimitiveType.Cube, t, new Vector3(side * (HalfWidth - 0.1f), 0.12f, mid), new Vector3(0.06f, 0.05f, BayLength),
                 RooftopProps.Glow(new Color(0.35f, 0.85f, 1f, 0.85f)));
        }

        // Glass exhibit case with an instrument, alternating sides.
        float exhibitSide = index % 2 == 0 ? -1f : 1f;
        Exhibit(t, new Vector3(exhibitSide * (HalfWidth - 2.1f), 0f, 3f), instruments, index);
        Exhibit(t, new Vector3(-exhibitSide * (HalfWidth - 2.1f), 0f, 9f), instruments, index + 7);

        if (index % 2 == 0)
            Chandelier(t, new Vector3(0f, CeilingY - 2.4f, mid));

        return root;
    }

    // ── Pieces ──────────────────────────────────────────────────────────────

    static void Exhibit(Transform parent, Vector3 pos, IList<GameObject> instruments, int seed)
    {
        var root = RooftopProps.Root("Exhibit", parent, parent.position + pos).transform;
        var gold = Lit(Gold, 0.65f, 0.8f);
        Part(PrimitiveType.Cube, root, new Vector3(0f, 0.55f, 0f), new Vector3(1.3f, 1.1f, 1.3f), Lit(Marble, 0.75f));
        Part(PrimitiveType.Cube, root, new Vector3(0f, 1.12f, 0f), new Vector3(1.4f, 0.06f, 1.4f), gold);
        Part(PrimitiveType.Cube, root, new Vector3(0f, 1.85f, 0f), new Vector3(1.2f, 1.4f, 1.2f), RooftopProps.Glow(new Color(0.7f, 0.9f, 1f, 0.1f)));
        Part(PrimitiveType.Cube, root, new Vector3(0f, 2.57f, 0f), new Vector3(1.26f, 0.05f, 1.26f), gold);
        Part(PrimitiveType.Cube, root, new Vector3(0f, 0.02f, 0f), new Vector3(2.6f, 0.01f, 2.6f), RooftopProps.Glow(new Color(1f, 0.85f, 0.55f, 0.1f)));

        var instrument = Instrument(root, seed, instruments);
        instrument.transform.localPosition = new Vector3(0f, 1.2f, 0f);

        // Velvet rope on the lane side.
        float laneSide = -Mathf.Sign(pos.x);
        foreach (float dz in new[] { -0.9f, 0.9f })
        {
            Part(PrimitiveType.Cylinder, root, new Vector3(laneSide * 1.25f, 0.45f, dz), new Vector3(0.08f, 0.45f, 0.08f), gold);
            Part(PrimitiveType.Sphere, root, new Vector3(laneSide * 1.25f, 0.92f, dz), Vector3.one * 0.14f, gold);
        }
        Part(PrimitiveType.Cube, root, new Vector3(laneSide * 1.25f, 0.75f, 0f), new Vector3(0.06f, 0.06f, 1.8f), Lit(Velvet, 0.2f));
    }

    /// <summary>An instrument model from the asset packs, or a built one, about 1 m tall.</summary>
    static GameObject Instrument(Transform parent, int seed, IList<GameObject> models)
    {
        int count = (models != null ? models.Count : 0) + 4;
        int pick = Mathf.Abs(seed * 7919) % count;
        GameObject go;
        if (models != null && pick < models.Count && models[pick] != null)
        {
            go = Object.Instantiate(models[pick], parent);
            go.name = models[pick].name;
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>()) Object.Destroy(mb);
            FitToSize(go, 1.1f);
        }
        else
        {
            go = new GameObject("Instrument");
            go.transform.SetParent(parent, false);
            switch ((pick - (models != null ? models.Count : 0)) % 4)
            {
                case 0: Violin(go.transform); break;
                case 1: Horn(go.transform); break;
                case 2: Harp(go.transform); break;
                default: Gramophone(go.transform); break;
            }
        }
        go.AddComponent<ExhibitSpin>();
        return go;
    }

    static void FitToSize(GameObject go, float size)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        var b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        float largest = Mathf.Max(b.size.x, b.size.y, b.size.z);
        if (largest > 0.001f) go.transform.localScale *= size / largest;
    }

    static void Violin(Transform t)
    {
        var wood = Lit(new Color(0.55f, 0.24f, 0.08f), 0.7f);
        var dark = Lit(new Color(0.08f, 0.05f, 0.04f), 0.5f);
        Part(PrimitiveType.Sphere, t, new Vector3(0f, 0.25f, 0f), new Vector3(0.42f, 0.36f, 0.12f), wood);
        Part(PrimitiveType.Sphere, t, new Vector3(0f, 0.55f, 0f), new Vector3(0.34f, 0.3f, 0.11f), wood);
        Part(PrimitiveType.Cube, t, new Vector3(0f, 0.95f, 0f), new Vector3(0.06f, 0.55f, 0.05f), dark);
        Part(PrimitiveType.Sphere, t, new Vector3(0f, 1.25f, 0f), new Vector3(0.09f, 0.1f, 0.07f), dark);
        Part(PrimitiveType.Cube, t, new Vector3(0f, 0.6f, 0.065f), new Vector3(0.05f, 0.9f, 0.005f), Lit(new Color(0.85f, 0.85f, 0.8f), 0.9f, 0.8f));
        t.localEulerAngles = new Vector3(0f, 0f, 12f);
    }

    static void Horn(Transform t)
    {
        var brass = Lit(new Color(0.85f, 0.65f, 0.25f), 0.85f, 0.9f);
        Part(PrimitiveType.Cylinder, t, new Vector3(0f, 0.5f, 0f), new Vector3(0.07f, 0.45f, 0.07f), brass, new Vector3(0f, 0f, 90f));
        for (int i = 0; i < 4; i++)
            Part(PrimitiveType.Cylinder, t, new Vector3(0.5f + i * 0.08f, 0.5f, 0f), new Vector3(0.12f + i * 0.09f, 0.04f, 0.12f + i * 0.09f), brass, new Vector3(0f, 0f, 90f));
        Part(PrimitiveType.Cylinder, t, new Vector3(-0.1f, 0.62f, 0f), new Vector3(0.05f, 0.1f, 0.05f), brass);
        Part(PrimitiveType.Cylinder, t, new Vector3(0.05f, 0.62f, 0f), new Vector3(0.05f, 0.1f, 0.05f), brass);
    }

    static void Harp(Transform t)
    {
        var gold = Lit(Gold, 0.7f, 0.8f);
        Part(PrimitiveType.Cube, t, new Vector3(-0.25f, 0.55f, 0f), new Vector3(0.08f, 1.1f, 0.08f), gold);
        Part(PrimitiveType.Cube, t, new Vector3(0.05f, 0.05f, 0f), new Vector3(0.65f, 0.08f, 0.1f), gold);
        Part(PrimitiveType.Cube, t, new Vector3(0.05f, 0.62f, 0f), new Vector3(0.08f, 1.25f, 0.08f), gold, new Vector3(0f, 0f, -28f));
        var str = Lit(new Color(0.95f, 0.95f, 0.9f), 0.9f, 0.5f);
        for (int i = 0; i < 6; i++)
            Part(PrimitiveType.Cube, t, new Vector3(-0.18f + i * 0.07f, 0.45f + i * 0.08f, 0f), new Vector3(0.01f, 0.75f - i * 0.1f, 0.01f), str);
    }

    static void Gramophone(Transform t)
    {
        var wood = Lit(DarkWood, 0.6f);
        var brass = Lit(new Color(0.85f, 0.65f, 0.25f), 0.85f, 0.9f);
        Part(PrimitiveType.Cube, t, new Vector3(0f, 0.15f, 0f), new Vector3(0.6f, 0.3f, 0.6f), wood);
        Part(PrimitiveType.Cylinder, t, new Vector3(0f, 0.31f, 0f), new Vector3(0.5f, 0.01f, 0.5f), Lit(new Color(0.05f, 0.05f, 0.05f), 0.8f));
        Part(PrimitiveType.Cylinder, t, new Vector3(0.15f, 0.55f, 0f), new Vector3(0.04f, 0.25f, 0.04f), brass, new Vector3(0f, 0f, -20f));
        for (int i = 0; i < 4; i++)
            Part(PrimitiveType.Cylinder, t, new Vector3(0.25f + i * 0.04f, 0.8f + i * 0.06f, 0f), new Vector3(0.1f + i * 0.12f, 0.03f, 0.1f + i * 0.12f), brass, new Vector3(0f, 0f, -55f));
    }

    static void Chandelier(Transform parent, Vector3 pos)
    {
        var root = RooftopProps.Root("Chandelier", parent, parent.position + pos).transform;
        var gold = Lit(Gold, 0.7f, 0.85f);
        Part(PrimitiveType.Cylinder, root, new Vector3(0f, 1.2f, 0f), new Vector3(0.05f, 1.2f, 0.05f), gold);
        Part(PrimitiveType.Cylinder, root, Vector3.zero, new Vector3(2.4f, 0.05f, 2.4f), gold);
        Part(PrimitiveType.Cylinder, root, new Vector3(0f, 0.45f, 0f), new Vector3(1.3f, 0.05f, 1.3f), gold);
        Part(PrimitiveType.Sphere, root, new Vector3(0f, -0.35f, 0f), new Vector3(0.35f, 0.55f, 0.35f), RooftopProps.Glow(new Color(1f, 0.95f, 0.85f, 0.55f)));
        var flame = RooftopProps.Glow(new Color(1f, 0.85f, 0.5f, 1f));
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 0.25f;
            var p = new Vector3(Mathf.Cos(a) * 1.15f, 0.12f, Mathf.Sin(a) * 1.15f);
            Part(PrimitiveType.Cylinder, root, p, new Vector3(0.06f, 0.1f, 0.06f), Lit(new Color(0.95f, 0.92f, 0.85f), 0.3f));
            Part(PrimitiveType.Sphere, root, p + Vector3.up * 0.16f, Vector3.one * 0.1f, flame);
        }
        var light = new GameObject("Light").AddComponent<Light>();
        light.transform.SetParent(root, false);
        light.transform.localPosition = new Vector3(0f, -0.4f, 0f);
        light.type = LightType.Point;
        light.color = WarmLight;
        light.range = 17f;
        light.intensity = 3.2f;
        light.shadows = LightShadows.None;
    }

    /// <summary>Grand archway across the hall marking a new wing, with its name on a banner.</summary>
    public static GameObject WingArch(Transform parent, float z, string wingName, Color accent, TMP_FontAsset font)
    {
        var root = RooftopProps.Root("WingArch_" + wingName, parent, new Vector3(0f, 0f, z));
        var t = root.transform;
        var gold = Lit(Gold, 0.7f, 0.85f);
        foreach (float side in new[] { -1f, 1f })
        {
            Part(PrimitiveType.Cube, t, new Vector3(side * (HalfWidth - 1.1f), CeilingY * 0.5f, 0f), new Vector3(2.2f, CeilingY, 1.4f), Lit(Marble, 0.75f));
            Part(PrimitiveType.Cube, t, new Vector3(side * (HalfWidth - 2.25f), CeilingY * 0.5f, 0f), new Vector3(0.12f, CeilingY, 1.5f), gold);
        }
        Part(PrimitiveType.Cube, t, new Vector3(0f, CeilingY - 0.9f, 0f), new Vector3(HalfWidth * 2f, 1.8f, 1.4f), Lit(Marble, 0.75f));
        Part(PrimitiveType.Cube, t, new Vector3(0f, CeilingY - 1.85f, -0.05f), new Vector3(HalfWidth * 2f - 4.4f, 0.12f, 1.5f), gold);

        // Hanging banner with the wing name.
        Part(PrimitiveType.Cube, t, new Vector3(0f, CeilingY - 2.6f, -0.75f), new Vector3(9f, 1.3f, 0.05f), Lit(new Color(0.12f, 0.03f, 0.05f), 0.2f));
        Part(PrimitiveType.Cube, t, new Vector3(0f, CeilingY - 3.27f, -0.76f), new Vector3(9f, 0.06f, 0.05f), RooftopProps.Glow(accent));
        var textGo = new GameObject("WingName");
        textGo.transform.SetParent(t, false);
        textGo.transform.localPosition = new Vector3(0f, CeilingY - 2.6f, -0.8f);
        var text = textGo.AddComponent<TextMeshPro>();
        if (font != null) text.font = font;
        text.text = wingName;
        text.fontSize = 7f;
        text.characterSpacing = 8f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.88f, 0.55f);
        text.rectTransform.sizeDelta = new Vector2(9f, 1.3f);
        return root;
    }
}
