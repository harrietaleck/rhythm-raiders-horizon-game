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
        var pillars = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Pedestal")) pillars.Add(child);
            else if (_note == null && child.GetComponentInChildren<Renderer>(true) != null) _note = child;
        }
        _pillars = pillars.ToArray();
        _pillarBase = new Vector3[_pillars.Length];
        for (int i = 0; i < _pillars.Length; i++) _pillarBase[i] = _pillars[i].localPosition;
        if (_note != null) _noteBase = _note.localPosition;
        BuildPillarCollider();
    }

    /// <summary>Solid box around the pillar meshes so the runner can't pass through them.</summary>
    void BuildPillarCollider()
    {
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
