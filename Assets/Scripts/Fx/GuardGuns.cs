using UnityEngine;

/// <summary>
/// Puts a Low Poly Stylized Gun rifle in a guard's hands: the chasing guard, the museum's armed
/// guards and the rooftop bouncers. The rifle sits in the right hand, barrel pointing where the
/// guard faces, at a steady world size whatever the character's scale.
/// </summary>
public static class GuardGuns
{
    /// <summary>Attach a rifle. Returns its muzzle (the barrel's front end), or null if no gun was added.</summary>
    /// <param name="character">The guard (any object above his Animator / rig).</param>
    /// <param name="length">Rifle length in metres.</param>
    /// <param name="fallbackParent">Used when no right-hand bone is found.</param>
    public static Transform Attach(Transform character, float length = 0.85f, Transform fallbackParent = null,
                                   Vector3 fallbackLocal = default, int variant = -1)
    {
        if (character == null) return null;
        var lib = FxLibrary.Get();
        if (lib == null || lib.guardGuns == null || lib.guardGuns.Length == 0) return null;
        var old = FindDeep(character, "GuardRifle");
        if (old != null) return FindDeep(old, "Muzzle");

        if (variant < 0) variant = Mathf.Abs(character.GetInstanceID()) % lib.guardGuns.Length;
        var prefab = lib.guardGuns[Mathf.Clamp(variant, 0, lib.guardGuns.Length - 1)];
        if (prefab == null) return null;

        Transform hand = RightHand(character);
        Vector3 forward = character.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();

        // Build the rifle at the origin first so its natural size and long axis can be measured.
        var gun = Object.Instantiate(prefab);
        gun.name = "GuardRifle";
        foreach (var c in gun.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
        gun.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        gun.transform.localScale = Vector3.one;
        var rs = gun.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) { Object.Destroy(gun); return null; }
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);

        // Longest side = barrel direction. The pack models point their barrels along -X / +Z;
        // pick the axis and flip so the barrel faces forward.
        Vector3 axis = b.size.x >= b.size.z && b.size.x >= b.size.y ? Vector3.right
                     : b.size.z >= b.size.y ? Vector3.forward : Vector3.up;
        float longest = Vector3.Scale(b.size, axis).magnitude;
        Vector3 barrel = BarrelSign(rs, b, axis) * axis;
        float s = length / Mathf.Max(0.01f, longest);

        // Pivot holder: centre the rifle on it, scale it, then turn the barrel to face forward.
        var holder = new GameObject("GuardRifle").transform;
        gun.name = "Model";
        gun.transform.SetParent(holder, false);
        gun.transform.localPosition = -b.center;
        holder.localScale = Vector3.one * s;
        Vector3 modelUp = Mathf.Abs(Vector3.Dot(barrel, Vector3.up)) > 0.9f ? Vector3.forward : Vector3.up;
        // Barrel to the guard's forward, the model's top (sights) up.
        Quaternion aim = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Inverse(Quaternion.LookRotation(barrel, modelUp));
        holder.rotation = aim;

        var muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(holder, false);
        muzzle.localPosition = barrel * (longest * 0.5f);

        if (hand != null)
        {
            // Grip in the right hand, most of the rifle out in front, slightly lowered.
            holder.position = hand.position + forward * (length * 0.22f) + Vector3.down * 0.04f;
            holder.SetParent(hand, true);
        }
        else
        {
            holder.SetParent(fallbackParent != null ? fallbackParent : character, false);
            holder.localPosition = fallbackLocal;
            holder.rotation = aim;
        }
        foreach (var r in holder.GetComponentsInChildren<Renderer>())
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        return muzzle;
    }

    /// <summary>Which end of the long axis is the barrel: the thinner end (the stock and body are bulkier).</summary>
    static float BarrelSign(Renderer[] rs, Bounds b, Vector3 axis)
    {
        float plus = 0f, minus = 0f;
        foreach (var r in rs)
        {
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null || !mf.sharedMesh.isReadable) continue;
            var m = r.transform.localToWorldMatrix;
            foreach (var v in mf.sharedMesh.vertices)
            {
                var w = m.MultiplyPoint3x4(v) - b.center;
                float along = Vector3.Dot(w, axis) / Mathf.Max(0.001f, Vector3.Scale(b.extents, axis).magnitude);
                Vector3 off = w - axis * Vector3.Dot(w, axis);
                float thick = off.magnitude;
                if (along > 0.6f) plus = Mathf.Max(plus, thick);
                else if (along < -0.6f) minus = Mathf.Max(minus, thick);
            }
        }
        if (plus == 0f && minus == 0f) return 1f;
        return plus <= minus ? 1f : -1f;   // thinner end is the barrel
    }

    public static Transform RightHand(Transform character)
    {
        var anim = character.GetComponentInChildren<Animator>();
        if (anim != null && anim.isHuman)
        {
            var h = anim.GetBoneTransform(HumanBodyBones.RightHand);
            if (h != null) return h;
        }
        foreach (var t in character.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLowerInvariant().Replace(":", "_");
            if (n.EndsWith("righthand") || n.EndsWith("right_hand") || n.EndsWith("hand_r") || n == "r_hand") return t;
        }
        return null;
    }

    static Transform FindDeep(Transform root, string name)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }
}
