using UnityEngine;

/// <summary>
/// Music-note fragments moving to and from the runner's backpack: collected notes fly into the
/// bag, and when the runner bumps into a dancer a few notes spill out of it.
/// </summary>
public static class NoteFx
{
    /// <summary>Detaches a collected note's visual and flies it into the backpack.</summary>
    public static void FlyToBag(Transform visual)
    {
        if (visual == null) return;
        var bag = RunnerBackpack.Bag;
        visual.SetParent(null, true);
        if (bag == null) { Object.Destroy(visual.gameObject); return; }
        var f = visual.gameObject.AddComponent<NoteFlight>();
        f.target = bag;
        f.duration = 0.32f;
        f.toBag = true;
    }

    /// <summary>Notes knocked out of the backpack: they burst out and fall away, gone.</summary>
    public static void Spill(Transform from, int count)
    {
        if (from == null || count <= 0) return;
        for (int i = 0; i < Mathf.Min(count, 8); i++)
        {
            var note = RooftopProps.Note(null, from.position);
            // Visual only: these can't be picked back up.
            var rn = note.GetComponent<RunNote>(); if (rn != null) rn.enabled = false;
            foreach (var c in note.GetComponentsInChildren<Collider>()) c.enabled = false;
            note.name = "Spilled Note";
            var f = note.AddComponent<NoteFlight>();
            f.velocity = new Vector3(Random.Range(-3.5f, 3.5f), Random.Range(4f, 6.5f), Random.Range(-3.5f, -1f));
            f.duration = 1.1f;
        }
    }
}

/// <summary>Moves a note visual: homing into a target (the bag) or flying ballistically, then removes it.</summary>
public class NoteFlight : MonoBehaviour
{
    public Transform target;
    public bool toBag;
    public float duration = 0.35f;
    public Vector3 velocity;

    Vector3 _start, _startScale;
    float _t;

    void Start()
    {
        _start = transform.position;
        _startScale = transform.localScale;
    }

    void Update()
    {
        _t += Time.deltaTime;
        float k = Mathf.Clamp01(_t / duration);
        if (target != null)
        {
            // Arc up and into the bag, shrinking as it goes in.
            var end = target.position;
            var p = Vector3.Lerp(_start, end, k * k);
            p.y += Mathf.Sin(k * Mathf.PI) * 0.6f;
            transform.position = p;
            transform.localScale = _startScale * Mathf.Lerp(1f, 0.15f, k);
            if (k >= 1f)
            {
                if (toBag) RunnerBackpack.Pulse();
                Destroy(gameObject);
            }
            return;
        }
        if (toBag) { Destroy(gameObject); return; }
        velocity += Physics.gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        transform.Rotate(0f, 540f * Time.deltaTime, 200f * Time.deltaTime, Space.World);
        transform.localScale = _startScale * Mathf.Lerp(1f, 0.3f, k);
        if (k >= 1f) Destroy(gameObject);
    }
}
