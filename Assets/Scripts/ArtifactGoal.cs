using UnityEngine;

/// <summary>
/// Collectible artifact. Collection is handled by PickupBase (tag "Artifact") which calls
/// <see cref="Collect"/>. This component presents it:
///  - the music note spins and bobs so it reads clearly as a pickup;
///  - the pillar (any "Pedestal" child) is solid - the player can't run through it;
///  - on pickup the note vanishes and the pillar sinks into the floor, clearing the lane.
/// </summary>
public class ArtifactGoal : MonoBehaviour
{
    public float spinDegreesPerSecond = 120f;
    public float bobHeight = 0.12f;
    public float sinkSeconds = 0.5f;

    Transform _note;
    Vector3 _noteBase;
    Transform[] _pillars = new Transform[0];
    Vector3[] _pillarBase;
    bool _collected;
    float _sinkT;
    float _seed;

    void Start()
    {
        _seed = Random.value * 10f;
        BuildSleekStand();
        var pillars = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Pedestal")) pillars.Add(child);
            else if (_note == null && child.name != "OldStand" && child.GetComponentInChildren<Renderer>(true) != null) _note = child;
        }
        _pillars = pillars.ToArray();
        _pillarBase = new Vector3[_pillars.Length];
        for (int i = 0; i < _pillars.Length; i++) _pillarBase[i] = _pillars[i].localPosition;
        if (_note != null) _noteBase = _note.localPosition;
        BuildPillarCollider();
    }

    /// <summary>
    /// Replaces the stacked-box stand with one slim gold piece: a thin base disc on a soft glowing
    /// ring, a narrow column with a light band, and a small top plate the note floats over.
    /// Same height as the old stand, so the note stays where it was.
    /// </summary>
    void BuildSleekStand()
    {
        bool built = transform.Find("Pedestal_Sleek") != null;   // a copy of an already-dressed artifact

        // Old stand pieces: the root's own mesh, any "Pedestal" child, and the stacked slabs and
        // column baked into the note model (Box001-006, Cylinder002). Only the gold note stays.
        var old = new System.Collections.Generic.List<Renderer>();
        var note = new System.Collections.Generic.List<Renderer>();
        var own = GetComponent<Renderer>();
        if (own != null) old.Add(own);
        foreach (Transform child in transform)
        {
            if (child.name == "Pedestal_Sleek") continue;
            bool pedestal = child.name.StartsWith("Pedestal") || child.name == "OldStand";
            foreach (var r in child.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                string n = r.gameObject.name;
                if (pedestal || n.StartsWith("Box") || n == "Cylinder002") old.Add(r);
                else note.Add(r);
            }
        }
        if (old.Count == 0) return;
        if (built)
        {
            foreach (var r in old) r.enabled = false;   // spawning re-enables every renderer: hide the old stand again
            return;
        }

        Bounds all = old[0].bounds;
        foreach (var r in old) all.Encapsulate(r.bounds);
        Bounds nb = note.Count > 0 ? note[0].bounds : all;
        foreach (var r in note) nb.Encapsulate(r.bounds);
        float floor = all.min.y;
        float h = Mathf.Clamp((note.Count > 0 ? nb.min.y : all.max.y) - floor - 0.05f, 0.5f, 2.2f);

        foreach (var r in old) r.enabled = false;
        foreach (Transform child in transform)
            if (child.name.StartsWith("Pedestal") && child.name != "Pedestal_Sleek") child.name = "OldStand";   // keep the old parts out of the pillar list

        var stand = new GameObject("Pedestal_Sleek").transform;
        stand.SetParent(transform, false);
        stand.position = new Vector3(nb.center.x, floor, nb.center.z);
        stand.rotation = Quaternion.identity;
        var ls = stand.lossyScale;
        stand.localScale = new Vector3(1f / Mathf.Max(0.001f, Mathf.Abs(ls.x)), 1f / Mathf.Max(0.001f, Mathf.Abs(ls.y)), 1f / Mathf.Max(0.001f, Mathf.Abs(ls.z)));

        var gold = RooftopProps.Mat(new Color(1f, 0.74f, 0.2f), 0.45f);
        var glow = RooftopProps.Mat(new Color(1f, 0.82f, 0.45f), 3f);
        void Disc(float y, float radius, float thick, Material m) =>
            RooftopProps.Part(PrimitiveType.Cylinder, stand, new Vector3(0f, y, 0f), new Vector3(radius * 2f, thick * 0.5f, radius * 2f), m, false);

        Disc(0.01f, 0.62f, 0.02f, glow);                              // soft light ring on the floor
        Disc(0.05f, 0.5f, 0.06f, gold);                               // base disc
        Disc(0.05f + (h - 0.14f) * 0.5f, 0.12f, h - 0.14f, gold);     // slim column
        Disc(h - 0.3f, 0.16f, 0.03f, glow);                           // light band near the top
        Disc(h - 0.04f, 0.38f, 0.05f, gold);                          // top plate the note floats over
        foreach (var r in stand.GetComponentsInChildren<Renderer>())
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    /// <summary>Solid box around the pillar meshes so the runner can't pass through them.</summary>
    void BuildPillarCollider()
    {
        if (transform.Find("PillarCollider") != null) return;   // copied from the template already
        Bounds b = default;
        bool any = false;
        foreach (var p in _pillars)
            foreach (var r in p.GetComponentsInChildren<Renderer>(true))
            {
                if (!any) { b = r.bounds; any = true; }
                else b.Encapsulate(r.bounds);
            }
        if (!any) return;

        var go = new GameObject("PillarCollider");
        go.transform.SetParent(transform, false);
        go.transform.position = b.center;
        go.transform.rotation = Quaternion.identity;
        var ls = transform.lossyScale;
        go.transform.localScale = new Vector3(1f / Mathf.Max(0.001f, ls.x), 1f / Mathf.Max(0.001f, ls.y), 1f / Mathf.Max(0.001f, ls.z));
        var box = go.AddComponent<BoxCollider>();
        box.size = b.size;
        box.isTrigger = false;
    }

    void Update()
    {
        if (!_collected)
        {
            if (_note != null)
            {
                _note.Rotate(0f, spinDegreesPerSecond * Time.deltaTime, 0f, Space.World);
                _note.localPosition = _noteBase + Vector3.up * Mathf.Sin((Time.time + _seed) * 2.5f) * bobHeight;
            }
            return;
        }

        // Pillar sinks into the floor after pickup.
        _sinkT += Time.deltaTime / Mathf.Max(0.05f, sinkSeconds);
        float k = Mathf.SmoothStep(0f, 1f, _sinkT);
        for (int i = 0; i < _pillars.Length; i++)
            if (_pillars[i] != null) _pillars[i].localPosition = _pillarBase[i] + Vector3.down * k * 3f;
        if (_sinkT >= 1f) Destroy(gameObject);
    }

    /// <summary>Called once when the player picks the artifact up.</summary>
    public void Collect()
    {
        if (_collected) return;
        _collected = true;
        foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = false;
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            bool isPillar = false;
            foreach (var p in _pillars) if (p != null && r.transform.IsChildOf(p)) { isPillar = true; break; }
            if (!isPillar) r.enabled = false;
        }
    }
}
