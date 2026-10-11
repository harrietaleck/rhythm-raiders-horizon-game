using UnityEngine;

/// <summary>
/// Telegraphs a roof gap: chevrons pulse toward the edge, beacons blink, the pit glows,
/// and a callout fires once when the player gets close.
/// </summary>
public class GapWarning : MonoBehaviour
{
    public Renderer[] chevrons;
    public Renderer[] glows;
    public Renderer[] beacons;
    public Light pitLight;
    public Color color = new Color(1f, 0.45f, 0.1f);
    public float warnDistance = 24f;
    public string callout = "GAP AHEAD - JUMP!";

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    static Transform _player;

    MaterialPropertyBlock _block;
    float[] _sweep;
    bool _warned;

    void Update()
    {
        float t = Time.time;

        // Chevrons light up row by row, sweeping toward the gap.
        if (_sweep == null) BuildSweep();
        for (int i = 0; i < chevrons.Length; i++)
        {
            float phase = Mathf.Repeat(t * 1.6f - _sweep[i], 1f);
            Set(chevrons[i], Mathf.Lerp(0.55f, 1f, Mathf.Pow(1f - phase, 3f)));
        }

        float pulse = 0.55f + 0.45f * Mathf.Sin(t * 6f);
        foreach (var r in glows) Set(r, pulse);
        if (pitLight != null) pitLight.intensity = 2.5f + 2.5f * pulse;

        bool blink = Mathf.Repeat(t * 2.5f, 1f) > 0.45f;
        foreach (var r in beacons) if (r != null) r.enabled = blink;

        if (_warned || !GameManager.GameStarted) return;
        if (_player == null)
        {
            var movement = FindAnyObjectByType<PlayerMovement>();
            if (movement == null) return;
            _player = movement.transform;
        }
        float ahead = transform.position.z - _player.position.z;
        if (ahead > 0f && ahead < warnDistance)
        {
            _warned = true;
            RunDirector.Instance?.Flash(callout, color);
        }
    }

    void BuildSweep()
    {
        _sweep = new float[chevrons.Length];
        float min = float.MaxValue, max = float.MinValue;
        foreach (var r in chevrons) { float z = r.transform.parent.localPosition.z; min = Mathf.Min(min, z); max = Mathf.Max(max, z); }
        for (int i = 0; i < chevrons.Length; i++)
            _sweep[i] = max > min ? (chevrons[i].transform.parent.localPosition.z - min) / (max - min) * 0.6f : 0f;
    }

    void Set(Renderer r, float strength)
    {
        if (r == null) return;
        if (_block == null) _block = new MaterialPropertyBlock();
        var c = r.sharedMaterial.GetColor(BaseColor);
        r.GetPropertyBlock(_block);
        _block.SetColor(BaseColor, new Color(color.r * 2f, color.g * 2f, color.b * 2f, c.a * strength));
        r.SetPropertyBlock(_block);
    }
}
