using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dresses a music artifact (the fragments the runner collects to win) so it reads as treasure:
/// a warm golden glow, twinkling glitter drifting around it, and small coloured musical
/// notes orbiting and bobbing. Purely visual; the artifact's own collider and pickup logic are untouched.
/// </summary>
public class ArtifactSparkle : MonoBehaviour
{
    const string FxName = "ArtifactFx";
    static Material _glitterMat;

    Transform _fx;
    readonly List<Transform> _notes = new List<Transform>();
    float _seed, _radius = 1f;
    Vector3 _offset;
    Renderer _probe;

    /// <summary>Add the sparkle to an artifact (safe to call more than once).</summary>
    public static void Attach(GameObject artifact)
    {
        if (artifact == null || artifact.GetComponent<ArtifactSparkle>() != null) return;
        if (artifact.GetComponentInParent<MusicInstrument>() != null) return;   // instruments aren't treasure
        artifact.AddComponent<ArtifactSparkle>();
    }

    void Start()
    {
        _seed = Random.value * 10f;

        // Centre of the artifact's model, measured in world space.
        Bounds b = new Bounds(transform.position, Vector3.one);
        bool have = false;
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            if (IsStand(r)) continue;   // just the note itself
            if (!have) { b = r.bounds; have = true; _probe = r; } else b.Encapsulate(r.bounds);
        }
        _radius = Mathf.Clamp(Mathf.Max(b.extents.x, b.extents.z) + 0.35f, 1f, 3f);

        // The effect lives outside the artifact (so its odd model scale/rotation can't stretch it)
        // and follows it every frame.
        var fx = new GameObject(FxName);
        fx.transform.position = b.center;
        _offset = b.center - transform.position;
        _fx = fx.transform;

        // Warm golden glow.
        var light = new GameObject("Glow").AddComponent<Light>();
        light.transform.SetParent(_fx, false);
        light.type = LightType.Point;
        light.color = new Color(1f, 0.8f, 0.35f);
        light.intensity = 7f;
        light.range = _radius * 3f;
        light.shadows = LightShadows.None;

        BuildGlitter(_fx, _radius);

        // Small musical notes orbiting the artifact.
        var colors = new[] { new Color(1f, 0.82f, 0.2f), new Color(1f, 0.4f, 0.8f), new Color(0.35f, 0.9f, 1f), new Color(0.7f, 0.5f, 1f) };
        for (int i = 0; i < 4; i++)
        {
            var note = new GameObject("Note" + i).transform;
            note.SetParent(_fx, false);
            var mat = RooftopProps.Mat(colors[i], 1.3f);
            float s = 0.32f + 0.06f * _radius;   // small accents, not rivals to the artifact
            RooftopProps.Part(PrimitiveType.Sphere, note, new Vector3(-0.12f, 0, 0) * s, new Vector3(0.42f, 0.34f, 0.2f) * s, mat, false);
            RooftopProps.Part(PrimitiveType.Cube, note, new Vector3(0.06f, 0.36f, 0) * s, new Vector3(0.07f, 0.72f, 0.07f) * s, mat, false);
            RooftopProps.Part(PrimitiveType.Cube, note, new Vector3(0.2f, 0.64f, 0) * s, new Vector3(0.3f, 0.09f, 0.07f) * s, mat, false);
            foreach (var r in note.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _notes.Add(note);
        }
    }

    /// <summary>The stand: the artifact root's own mesh, or parts named pedestal / pillar / plinth / stand.</summary>
    bool IsStand(Renderer r)
    {
        if (r.gameObject == gameObject && GetComponentsInChildren<Renderer>().Length > 1) return true;
        if (r.gameObject.name.StartsWith("Box") || r.gameObject.name == "Cylinder002") return true;   // slabs baked into the note model
        for (var t = r.transform; t != null && t != transform; t = t.parent)
        {
            string n = t.name.ToLowerInvariant();
            if (n.Contains("pedestal") || n.Contains("pillar") || n.Contains("plinth") || n.Contains("stand")) return true;
        }
        return false;
    }

    /// <summary>The artifact's stand (pedestal / pillar) becomes polished gold.</summary>
    void GildStand()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (!IsStand(r)) continue;
            var mats = r.materials;
            foreach (var m in mats)
            {
                if (m == null) continue;
                var gold = new Color(1f, 0.76f, 0.22f);
                if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null);   // plain polished gold, no old texture
                if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", null);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", gold);
                if (m.HasProperty("_Color")) m.SetColor("_Color", gold);
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.55f);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.55f);
                if (m.HasProperty("_EmissionColor"))
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", gold * 0.7f);   // reads gold even under dark club light
                }
            }
            r.materials = mats;
        }
    }

    void OnDisable()
    {
        if (_fx != null) _fx.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (_fx != null) Destroy(_fx.gameObject);
    }

    void Update()
    {
        if (_fx == null) return;
        _fx.position = transform.position + _offset;
        bool show = _probe == null || _probe.enabled;   // collected: renderers switched off
        if (_fx.gameObject.activeSelf != show) _fx.gameObject.SetActive(show);
        float t = Time.time + _seed;
        for (int i = 0; i < _notes.Count; i++)
        {
            float a = t * 1.4f + i * Mathf.PI * 0.5f;
            float r = _radius + Mathf.Sin(t * 2f + i) * 0.1f;
            var n = _notes[i];
            n.localPosition = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(t * 2.6f + i * 1.7f) * 0.35f * _radius, Mathf.Sin(a) * r);
            n.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, Mathf.Sin(t * 3f + i) * 15f);
            n.localScale = Vector3.one * (0.9f + Mathf.Sin(t * 4f + i) * 0.12f);
        }
    }

    static void BuildGlitter(Transform parent, float radius)
    {
        var go = new GameObject("Glitter");
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.duration = 2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.35f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.5f), new Color(1f, 1f, 1f));
        main.gravityModifier = -0.05f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 120;

        var em = ps.emission;
        em.rateOverTime = 45f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;

        // Twinkle: fade in, flash, fade out.
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.85f, 0.4f), 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.4f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.3f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0.1f)));

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.material = GlitterMaterial();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Play();
    }

    static Material GlitterMaterial()
    {
        if (_glitterMat != null) return _glitterMat;
        // Soft four-point star texture, made once at runtime.
        const int N = 32;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float core = Mathf.Clamp01(1f - d * 2.2f);
                float star = Mathf.Clamp01(1f - Mathf.Abs(dx) * 14f) * Mathf.Clamp01(1f - Mathf.Abs(dy) * 1.1f)
                           + Mathf.Clamp01(1f - Mathf.Abs(dy) * 14f) * Mathf.Clamp01(1f - Mathf.Abs(dx) * 1.1f);
                float a = Mathf.Clamp01(core * core + star * 0.8f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
        var m = new Material(shader);
        m.SetTexture("_BaseMap", tex);
        m.mainTexture = tex;
        m.SetColor("_BaseColor", Color.white);
        if (m.HasProperty("_Surface"))
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 2f);   // additive
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.EnableKeyword("_BLENDMODE_ADD");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        _glitterMat = m;
        return m;
    }
}
