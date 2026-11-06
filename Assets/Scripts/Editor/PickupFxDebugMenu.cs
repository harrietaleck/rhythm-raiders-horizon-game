using UnityEditor;
using UnityEngine;

/// <summary>Play-mode preview: lines up every power-up (with its new flame / plasma look) ahead of the runner.</summary>
public static class PickupFxDebugMenu
{
    static readonly string[] Tags = { "Speed", "SlowDown", "JumpBoost", "Sneak", "HealthINC" };

    [MenuItem("Tools/Debug/Line Up Power-ups Ahead")]
    static void LineUp()
    {
        if (!Application.isPlaying) { Debug.LogWarning("Enter Play mode first."); return; }
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        var p = player.transform.position;
        float x = -4f;
        foreach (var tag in Tags)
        {
            GameObject src = null;
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
                if (t.gameObject.scene.IsValid() && t.CompareTag(tag)) { src = t.gameObject; break; }
            if (src == null) { Debug.Log("[PowerUps] no template for " + tag); x += 2f; continue; }
            var c = Object.Instantiate(src, new Vector3(x, p.y, p.z + 9f), src.transform.rotation);
            c.name = "Preview " + tag;
            Spawner.PrepareSpawnedClone(c);
            foreach (var col in c.GetComponentsInChildren<Collider>()) col.enabled = false;
            x += 2f;
        }
        var bubble = RooftopProps.Power(null, new Vector3(0f, p.y + 1.5f, p.z + 6f), PowerPickup.Kind.Bubble);
        foreach (var col in bubble.GetComponentsInChildren<Collider>()) col.enabled = false;
        Debug.Log("[PowerUps] lined up ahead of the runner");
    }
}
