using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Level 3 beat clock (124 BPM). Everything in the club - neon, dance floor, speakers, bass walls,
/// sweeping lasers and the HUD beat pip - reads its timing from here so the whole room moves together.
/// Also drives the shared "pulse" materials so hundreds of neon parts cost one colour update each.
/// </summary>
[DefaultExecutionOrder(-300)]
public class ClubBeat : MonoBehaviour
{
    public static ClubBeat Instance { get; private set; }
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    public float bpm = 124f;

    float _start;

    public static float BeatLength => 60f / (Instance != null ? Instance.bpm : 124f);
    static float Elapsed => Instance != null ? Time.time - Instance._start : Time.time;
    /// <summary>0 on the beat, rising to 1 just before the next beat.</summary>
    public static float Phase => Mathf.Repeat(Elapsed / BeatLength, 1f);
    public static int Beat => Mathf.FloorToInt(Elapsed / BeatLength);
    /// <summary>1 on the beat, decaying quickly - use for kicks and flashes.</summary>
    public static float Kick => Mathf.Exp(-Phase * 6f);
    /// <summary>True during the first half of every beat pair (beats 0, 2, 4 ...).</summary>
    public static bool DownBeat => Beat % 2 == 0;

    class Pulse { public Material mat; public Color color; public float min; public float amp; }
    static readonly List<Pulse> Pulses = new List<Pulse>();
    static readonly Material[] FloorMats = new Material[4];
    static readonly Color[] FloorColors =
    {
        new Color(1f, 0.2f, 0.8f), new Color(0.2f, 0.85f, 1f), new Color(0.6f, 0.25f, 1f), new Color(1f, 0.55f, 0.15f),
    };

    void Awake()
    {
        Instance = this;
        _start = Time.time;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Pulses.Clear();
        for (int i = 0; i < FloorMats.Length; i++) FloorMats[i] = null;
    }

    /// <summary>A glow material whose alpha kicks on every beat (min + amp * kick).</summary>
    public static Material PulseMat(Color color, float min, float amp)
    {
        foreach (var p in Pulses)
            if (p.color == color && Mathf.Approximately(p.min, min) && Mathf.Approximately(p.amp, amp)) return p.mat;
        var mat = new Material(RooftopProps.Glow(new Color(color.r, color.g, color.b, min)));
        Pulses.Add(new Pulse { mat = mat, color = color, min = min, amp = amp });
        return mat;
    }

    /// <summary>One of four dance-floor tile materials; a different group lights up on each beat.</summary>
    public static Material FloorMat(int group)
    {
        group = Mathf.Abs(group) % FloorMats.Length;
        if (FloorMats[group] == null)
            FloorMats[group] = new Material(RooftopProps.Glow(new Color(FloorColors[group].r, FloorColors[group].g, FloorColors[group].b, 0.1f)));
        return FloorMats[group];
    }

    void Update()
    {
        float kick = Kick;
        foreach (var p in Pulses)
            if (p.mat != null) p.mat.SetColor(BaseColor, new Color(p.color.r, p.color.g, p.color.b, Mathf.Clamp01(p.min + p.amp * kick)));

        int beat = Beat;
        for (int g = 0; g < FloorMats.Length; g++)
        {
            if (FloorMats[g] == null) continue;
            bool lit = (beat + g) % FloorMats.Length == 0;
            float a = lit ? 0.22f + 0.4f * kick : 0.06f;
            var c = FloorColors[g];
            FloorMats[g].SetColor(BaseColor, new Color(c.r, c.g, c.b, a));
        }
    }
}
