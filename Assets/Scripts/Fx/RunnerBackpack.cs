using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Straps the runner's backpack (LowPoly Fantasy Bags) onto the player's upper spine in every
/// scene that has a Player, so it moves with the run, jump and stumble animations.
/// </summary>
public static class RunnerBackpack
{
    public static float HeightPerTorso = 0.85f;   // bag height relative to the hips-to-head length
    public static float BackPerTorso = 0.75f;    // how far behind the spine (same units)
    public static Vector3 Rotation = new Vector3(0f, 180f, 0f);  // bag front (straps) against the back

    /// <summary>The runner's backpack in the current scene (notes fly into it).</summary>
    public static Transform Bag;

    /// <summary>Little squash on the bag when a note drops in.</summary>
    public static void Pulse()
    {
        if (Bag == null) return;
        var p = Bag.GetComponent<BagPulse>();
        if (p == null) p = Bag.gameObject.AddComponent<BagPulse>();
        p.Kick();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
        Attach();
    }

    static void OnLoaded(Scene s, LoadSceneMode m) => Attach();

    public static Transform Attach()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return null;
        var lib = FxLibrary.Get();
        if (lib == null || lib.backpack == null) return null;
        var existing = FindDeep(player.transform, "Runner Backpack");
        if (existing != null) { Bag = existing; return existing; }

        var spine = FindBone(player.transform, "Spine2") ?? FindBone(player.transform, "Spine1") ?? FindBone(player.transform, "Spine");
        if (spine == null) return null;

        var holder = new GameObject("Runner Backpack").transform;
        holder.SetParent(spine, false);
        var bag = Object.Instantiate(lib.backpack, holder);
        bag.name = "Bag";
        foreach (var c in bag.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
        foreach (var rb in bag.GetComponentsInChildren<Rigidbody>(true)) Object.Destroy(rb);

        // Size in world space, independent of the rig's (centimetre) scale.
        // The model faces the run direction (the Player root itself may be turned around).
        var anim = player.GetComponentInChildren<Animator>();
        var body = anim != null ? anim.transform : player.transform;
        holder.rotation = body.rotation;
        bag.transform.localPosition = Vector3.zero;
        bag.transform.localRotation = Quaternion.Euler(Rotation);
        // Size it from the rig: hips-to-head length tells how big this runner is.
        var hips = FindBone(player.transform, "Hips");
        var head = FindBone(player.transform, "Head");
        float torso = hips != null && head != null ? Mathf.Abs(head.position.y - hips.position.y) : 0.75f;
        if (torso < 0.2f) torso = 0.75f;
        var rs = bag.GetComponentsInChildren<Renderer>();
        if (rs.Length > 0)
        {
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            if (b.size.y > 0.0001f) holder.localScale *= HeightPerTorso * torso / b.size.y;
            b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            // Centre on the upper spine, then push it out behind the back.
            holder.position += spine.position - b.center;
            holder.position += body.rotation * new Vector3(0f, -0.12f * torso, -BackPerTorso * torso);
        }
        foreach (var r in rs) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        Bag = holder;
        return holder;
    }

    static Transform FindBone(Transform root, string suffix)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.EndsWith(suffix) && (t.name.Contains("mixamo") || t.name.Contains("Spine"))) return t;
        return null;
    }

    static Transform FindDeep(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
        return null;
    }
}

/// <summary>Quick squash-and-stretch on the backpack.</summary>
public class BagPulse : MonoBehaviour
{
    Vector3 _base;
    float _t = 1f;
    bool _init;

    public void Kick()
    {
        if (!_init) { _base = transform.localScale; _init = true; }
        _t = 0f;
    }

    void LateUpdate()
    {
        if (!_init || _t >= 1f) return;
        _t = Mathf.Min(1f, _t + Time.deltaTime / 0.18f);
        float s = 1f + Mathf.Sin(_t * Mathf.PI) * 0.14f;
        transform.localScale = _base * s;
    }
}
