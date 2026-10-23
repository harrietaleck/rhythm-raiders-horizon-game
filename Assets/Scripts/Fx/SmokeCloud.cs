using UnityEngine;

/// <summary>
/// A drifting smoke cloud built from the Fog Particles pack (URP material), used by DJ smoke
/// machines to hide the track for a few seconds. Falls back to soft puffs if the pack is missing.
/// </summary>
public static class SmokeCloud
{
    /// <param name="center">World centre of the cloud (at floor level).</param>
    /// <param name="size">Box the smoke fills (x across, y height, z along the run).</param>
    /// <param name="emitSeconds">How long the machine keeps pumping.</param>
    /// <param name="drift">World velocity the smoke drifts with (e.g. out across the lanes).</param>
    public static GameObject Spawn(Vector3 center, Vector3 size, float emitSeconds, Color tint, Vector3 drift)
    {
        var lib = FxLibrary.Get();
        GameObject go;
        if (lib != null && lib.fogPrefab != null)
        {
            go = Object.Instantiate(lib.fogPrefab);
            go.name = "Smoke Cloud";
            go.transform.position = center + Vector3.up * size.y * 0.5f;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>())
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.duration = Mathf.Max(0.1f, emitSeconds);
                main.loop = false;
                main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.4f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
                main.startSize = new ParticleSystem.MinMaxCurve(2.4f, 4.2f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(tint.r, tint.g, tint.b, tint.a * 0.7f), tint);
                main.maxParticles = 220;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.scalingMode = ParticleSystemScalingMode.Local;
                main.gravityModifier = -0.02f;

                var shape = ps.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = size;
                shape.position = Vector3.zero;

                var emission = ps.emission;
                emission.enabled = true;
                emission.rateOverTime = Mathf.Clamp(size.x * size.z * 0.9f, 18f, 70f);

                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.World;
                vel.x = drift.x; vel.y = drift.y + 0.15f; vel.z = drift.z;

                var col = ps.colorOverLifetime;
                col.enabled = true;
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                          new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.85f, 0.7f), new GradientAlphaKey(0f, 1f) });
                col.color = g;

                var r = ps.GetComponent<ParticleSystemRenderer>();
                if (r != null && lib.fogMaterial != null) r.sharedMaterial = lib.fogMaterial;
                ps.Play();
            }
            Object.Destroy(go, emitSeconds + 4f);
            return go;
        }

        // Fallback: soft grey puffs.
        go = new GameObject("Smoke Cloud");
        go.transform.position = center;
        var mat = RooftopProps.Glow(new Color(tint.r, tint.g, tint.b, 0.35f));
        int n = Mathf.Clamp(Mathf.RoundToInt(size.x * size.z / 3f), 6, 18);
        for (int i = 0; i < n; i++)
        {
            var p = RooftopProps.Part(PrimitiveType.Sphere, go.transform,
                new Vector3(Random.Range(-0.5f, 0.5f) * size.x, Random.Range(0.3f, 1f) * size.y, Random.Range(-0.5f, 0.5f) * size.z),
                Vector3.one * Random.Range(1.6f, 2.8f), mat, false);
            var fx = p.AddComponent<FxPuff>();
            fx.Set(1.2f, Random.Range(2.5f, 3.6f), emitSeconds + 2f, drift * 0.6f + Vector3.up * 0.2f, fadeStart: 0.5f);
        }
        Object.Destroy(go, emitSeconds + 3f);
        return go;
    }
    /// <summary>
    /// A smoke trail that follows <paramref name="parent"/> (thrown grenades, fuses). Particles live
    /// in world space so they hang in the air behind the object. Returns the particle system.
    /// </summary>
    public static ParticleSystem Trail(Transform parent, float size = 0.5f, float rate = 40f, Color? tint = null)
    {
        var c = tint ?? new Color(0.78f, 0.78f, 0.76f, 0.95f);
        var go = new GameObject("Smoke Trail");
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size * 1.2f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = c;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;
        main.gravityModifier = -0.04f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;
        var emission = ps.emission;
        emission.rateOverTime = rate;
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 2.4f));
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.7f, 0.7f, 0.7f), 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.1f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var r = go.GetComponent<ParticleSystemRenderer>();
        var lib = FxLibrary.Get();
        r.sharedMaterial = lib != null && lib.fogMaterial != null ? lib.fogMaterial : RooftopProps.Glow(new Color(0.7f, 0.7f, 0.7f, 0.4f));
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Play();
        return ps;
    }

    /// <summary>Unparents a trail so its smoke fades out naturally after the owner is destroyed.</summary>
    public static void Release(ParticleSystem trail, float fade = 2f)
    {
        if (trail == null) return;
        trail.transform.SetParent(null, true);
        trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Object.Destroy(trail.gameObject, fade);
    }
}
