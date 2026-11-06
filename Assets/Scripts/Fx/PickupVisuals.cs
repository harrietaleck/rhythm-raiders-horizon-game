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

    static readonly string[] Tags = { "Speed", "SlowDown", "JumpBoost", "Sneak", "HealthINC" };

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
    }

    static bool IsFx(Transform t, Transform root)
    {
        for (; t != null && t != root; t = t.parent)
            if (t.name == FxName) return true;
        return false;
    }
}
