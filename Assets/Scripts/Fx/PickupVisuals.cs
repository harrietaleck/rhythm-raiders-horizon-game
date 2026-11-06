using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Dresses power-up pickups with the Stylized Fire (Vefects) effects instead of their old models:
/// speed = orange flames, slow = water flames, jump boost = electric, sneak = mystic purple,
/// health = green. The pickup keeps its collider and tag, only the look changes.
/// Called for every spawned clone (Spawner.PrepareSpawnedClone) and for pickups already in a scene.
/// </summary>
public static class PickupVisuals
{
    const string FxName = "PickupFx";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        DressScene();
    }

    static void OnSceneLoaded(Scene s, LoadSceneMode m) => DressScene();

    static readonly string[] Tags = { "Speed", "SlowDown", "JumpBoost", "Sneak", "HealthINC", "Artifact" };

    static void DressScene()
    {
        foreach (var tag in Tags)
        {
            GameObject[] found;
            try { found = GameObject.FindGameObjectsWithTag(tag); }
            catch (UnityException) { continue; }
            foreach (var go in found) Dress(go);
        }
    }

    static GameObject FlameFor(string tag, FxLibrary lib)
    {
        switch (tag)
        {
            case "Speed": return lib.flameSpeed;
            case "SlowDown": return lib.flameSlow;
            case "JumpBoost": return lib.flameJump;
            case "Sneak": return lib.flameSneak;
            case "HealthINC": return lib.flameHealth;
        }
        return null;
    }

    /// <summary>Swap a pickup's model for its flame. Safe to call more than once.</summary>
    public static void Dress(GameObject root)
    {
        if (root == null) return;
        if (root.CompareTag("Artifact")) { ArtifactSparkle.Attach(root); return; }   // treasure gets glitter and notes
        var lib = FxLibrary.Get();
        if (lib == null) return;
        var prefab = FlameFor(root.tag, lib);
        if (prefab == null) return;

        // Where the old model sat (before it is hidden).
        Bounds b = default;
        bool have = false;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer || IsFx(r.transform, root.transform)) continue;
            if (!have) { b = r.bounds; have = true; } else b.Encapsulate(r.bounds);
            r.enabled = false;
        }
        if (!have) b = new Bounds(root.transform.position, Vector3.one);

        var fx = root.transform.Find(FxName);
        if (fx == null)
        {
            var go = Object.Instantiate(prefab, root.transform);
            go.name = FxName;
            fx = go.transform;
        }

        // Flame stands on the spot the model's base was, at a steady world size.
        fx.position = new Vector3(b.center.x, b.min.y, b.center.z);
        fx.rotation = Quaternion.identity;
        var ls = root.transform.lossyScale;
        float s = lib.pickupFlameScale;
        fx.localScale = new Vector3(s / Mathf.Max(0.0001f, Mathf.Abs(ls.x)),
                                    s / Mathf.Max(0.0001f, Mathf.Abs(ls.y)),
                                    s / Mathf.Max(0.0001f, Mathf.Abs(ls.z)));
        foreach (var r in fx.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
        foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            if (!ps.isPlaying) ps.Play();
        }
        AddGlow(root.transform, fx.position + Vector3.up * 0.9f, GlowFor(root.tag));
    }

    /// <summary>Each pickup's colour, used for the light it throws so it stands out on dark roofs and halls.</summary>
    public static Color GlowFor(string tag)
    {
        switch (tag)
        {
            case "Speed": return new Color(1f, 0.45f, 0.05f);     // orange
            case "SlowDown": return new Color(0.15f, 0.55f, 1f);  // water blue
            case "JumpBoost": return new Color(0.75f, 0.9f, 1f);  // electric white-blue
            case "Sneak": return new Color(0.7f, 0.3f, 1f);       // mystic purple
            case "HealthINC": return new Color(0.25f, 1f, 0.3f);  // green
        }
        return Color.white;
    }

    /// <summary>A coloured point light at the pickup so it is easy to spot from a distance.</summary>
    public static void AddGlow(Transform parent, Vector3 at, Color color, float intensity = 6f, float range = 5f)
    {
        var t = parent.Find("PickupGlow");
        var go = t != null ? t.gameObject : new GameObject("PickupGlow");
        go.transform.SetParent(parent, true);
        go.transform.position = at;
        var light = go.GetComponent<Light>();
        if (light == null) light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        light.enabled = true;
    }

    /// <summary>
    /// A small Vibe pickup (+5 health): a rainbow flame with its own light, run through to collect.
    /// </summary>
    public static GameObject SpawnVibe(Vector3 at, Transform parent = null)
    {
        var lib = FxLibrary.Get();
        var root = new GameObject("Vibe Pickup");
        root.transform.SetParent(parent, true);
        root.transform.position = at;
        if (lib != null && lib.flameVibe != null)
        {
            var fx = Object.Instantiate(lib.flameVibe, root.transform);
            fx.name = FxName;
            fx.transform.localPosition = Vector3.down * 0.6f;
            fx.transform.localRotation = Quaternion.identity;
            fx.transform.localScale = Vector3.one * 1.2f;
            foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            }
        }
        AddGlow(root.transform, at, new Color(1f, 0.4f, 0.85f), 5f, 4f);
        var col = root.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.9f;
        root.AddComponent<VibePickup>();
        GameplayCollisionUtility.EnsureKinematicRigidbody(root);
        return root;
    }

    /// <summary>Share of spawned power-ups that become shield orbs instead.</summary>
    public static float ShieldShare = 0.15f;

    /// <summary>
    /// Turns a freshly spawned power-up (speed, jump, sneak, health) into a plasma shield orb:
    /// the shield refills run momentum, so it needs to turn up regularly in every level.
    /// </summary>
    public static bool TryMakeShield(GameObject clone)
    {
        if (clone == null) return false;
        string t = clone.tag;
        if (t != "Speed" && t != "JumpBoost" && t != "Sneak" && t != "HealthINC") return false;
        if (Random.value > ShieldShare || !PickupLimiter.HasRoom("Shield")) return false;

        // Measure where the model stands before hiding it.
        var b = default(Bounds);
        bool have = false;
        foreach (var r in clone.GetComponentsInChildren<Renderer>(true))
        { if (!r.enabled) continue; if (!have) { b = r.bounds; have = true; } else b.Encapsulate(r.bounds); }
        Vector3 at = have ? new Vector3(b.center.x, b.min.y + 1.1f, b.center.z) : clone.transform.position + Vector3.up * 1.1f;

        clone.tag = "Untagged";
        foreach (var r in clone.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        foreach (var c in clone.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        foreach (var mb in clone.GetComponentsInChildren<MonoBehaviour>(true)) mb.enabled = false;
        var orb = RooftopProps.Power(clone.transform, at, PowerPickup.Kind.Bubble);
        orb.name = "Shield Pickup";
        PickupLimiter.Register(orb, "Shield");
        return true;
    }

    static bool IsFx(Transform t, Transform root)
    {
        for (; t != null && t != root; t = t.parent)
            if (t.name == FxName) return true;
        return false;
    }
}


/// <summary>
/// Keeps pickups sparse and apart: caps how many of each kind can be waiting ahead of the
/// runner (shield, speed and slow especially) and never lets two pickups land on the same spot.
/// </summary>
public static class PickupLimiter
{
    const float MinSpacing = 3.5f;   // metres between any two pickups

    static readonly Dictionary<string, int> Caps = new Dictionary<string, int>
    {
        { "Shield", 2 }, { "Speed", 1 }, { "SlowDown", 2 }, { "Vibe", 2 },
        { "JumpBoost", 2 }, { "Sneak", 2 }, { "HealthINC", 2 },
    };

    static readonly List<KeyValuePair<string, GameObject>> Live = new List<KeyValuePair<string, GameObject>>();
    static Transform _player;

    public static string KindOf(GameObject go)
    {
        if (go == null) return null;
        if (go.GetComponentInChildren<PowerPickup>() is PowerPickup p && p.kind == PowerPickup.Kind.Bubble) return "Shield";
        if (go.GetComponentInChildren<VibePickup>() != null) return "Vibe";
        return Caps.ContainsKey(go.tag) ? go.tag : null;
    }

    static float PlayerZ()
    {
        if (_player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            _player = p != null ? p.transform : null;
        }
        return _player != null ? _player.position.z : float.MinValue;
    }

    static void Prune()
    {
        float pz = PlayerZ();
        for (int i = Live.Count - 1; i >= 0; i--)
        {
            var go = Live[i].Value;
            if (go == null || !go.activeInHierarchy || go.transform.position.z < pz - 2f) Live.RemoveAt(i);
        }
    }

    /// <summary>How many of this kind are still waiting ahead of the runner.</summary>
    public static int Count(string kind)
    {
        Prune();
        int n = 0;
        foreach (var e in Live) if (e.Key == kind) n++;
        return n;
    }

    public static bool HasRoom(string kind) => kind == null || !Caps.TryGetValue(kind, out int cap) || Count(kind) < cap;

    /// <summary>True when another pickup is already sitting at (or right next to) this spot.</summary>
    public static bool Crowded(Vector3 pos, GameObject self = null)
    {
        Prune();
        foreach (var e in Live)
        {
            if (e.Value == self) continue;
            var d = e.Value.transform.position - pos;
            d.y = 0f;
            if (d.sqrMagnitude < MinSpacing * MinSpacing) return true;
        }
        return false;
    }

    public static void Register(GameObject go, string kind = null)
    {
        kind = kind ?? KindOf(go);
        if (kind == null || go == null) return;
        Prune();
        Live.Add(new KeyValuePair<string, GameObject>(kind, go));
    }

    /// <summary>Should this freshly spawned pickup be dropped (too many of its kind, or on top of another)?</summary>
    public static bool Reject(GameObject clone)
    {
        string kind = KindOf(clone);
        if (kind == null) return false;   // obstacles, artifacts, notes: not limited here
        return !HasRoom(kind) || Crowded(clone.transform.position, clone);
    }

    /// <summary>Removes a rejected pickup without breaking whoever spawned it.</summary>
    public static void Drop(GameObject clone)
    {
        clone.tag = "Untagged";
        clone.SetActive(false);
    }
}
