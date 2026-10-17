using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hides and removes a pickup the same frame the player collects it.
/// </summary>
public static class PickupCollectUtility
{
    static readonly HashSet<int> Consumed = new HashSet<int>();

    static readonly string[] PickupTags =
    {
        "HealthINC", "HealthDEC", "Artifact", "JumpBoost", "Sneak", "Speed", "SlowDown"
    };

    public static bool IsConsumed(GameObject root)
    {
        return root != null && Consumed.Contains(root.GetInstanceID());
    }

    /// <summary>Marks a pickup as collected without hiding or destroying it (it animates itself away).</summary>
    public static bool MarkConsumed(GameObject root) => root != null && Consumed.Add(root.GetInstanceID());

    public static bool TryConsumeFromCollider(Collider col)
    {
        if (col == null) return false;

        var root = FindPickupRoot(col.transform);
        if (root != null)
            return TryConsume(root);

        return TryConsume(col.gameObject);
    }

    public static bool IsPickupConsumed(Collider col)
    {
        if (col == null) return false;
        var root = FindPickupRoot(col.transform);
        return root != null && IsConsumed(root);
    }

    public static bool TryConsume(GameObject root)
    {
        if (root == null) return false;

        int id = root.GetInstanceID();
        if (!Consumed.Add(id)) return false;

        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = false;

        foreach (var col in root.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        Object.Destroy(root);
        return true;
    }

    static GameObject FindPickupRoot(Transform t)
    {
        while (t != null)
        {
            foreach (var tag in PickupTags)
            {
                if (t.CompareTag(tag))
                    return t.gameObject;
            }
            t = t.parent;
        }

        return null;
    }
}
