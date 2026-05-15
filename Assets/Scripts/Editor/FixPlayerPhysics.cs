using UnityEngine;
using UnityEditor;

/// <summary>
/// Fixes the Player physics conflict:
///   - Rigidbody + CharacterController fight each other and cause falling-through-ground.
///   - Fix: make Rigidbody Kinematic (physics can't move it, but triggers still fire).
///   - CapsuleCollider must be isTrigger=true so it only detects pickups/obstacles,
///     not physics-push the player through the floor.
/// Run via Tools/Fix Player Physics.
/// </summary>
public class FixPlayerPhysics
{
    [MenuItem("Tools/Fix Player Physics")]
    public static void Fix()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("[FixPlayer] Player not found in scene."); return; }

        bool changed = false;

        // ── Rigidbody: make kinematic ─────────────────────────────────────
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic  = true;
            rb.useGravity   = false;
            rb.constraints  = RigidbodyConstraints.FreezeAll;
            EditorUtility.SetDirty(player);
            changed = true;
            Debug.Log("[FixPlayer] Rigidbody → isKinematic=true, useGravity=false, FreezeAll.");
        }
        else
        {
            Debug.Log("[FixPlayer] No Rigidbody found on Player (already removed or not present).");
        }

        // ── CapsuleCollider: make trigger-only ────────────────────────────
        // CharacterController has its own capsule for ground collision.
        // The separate CapsuleCollider should only be used for trigger zones.
        CapsuleCollider cap = player.GetComponent<CapsuleCollider>();
        if (cap != null)
        {
            cap.isTrigger = true;
            EditorUtility.SetDirty(player);
            changed = true;
            Debug.Log("[FixPlayer] CapsuleCollider → isTrigger=true (trigger-only, CC handles ground).");
        }
        else
        {
            Debug.Log("[FixPlayer] No CapsuleCollider on Player.");
        }

        // ── CharacterController: ensure sensible defaults ─────────────────
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            // Standard FPS/runner values — prevents clipping through thin floors
            if (cc.skinWidth    < 0.05f) cc.skinWidth    = 0.08f;
            if (cc.stepOffset   > 0.6f)  cc.stepOffset   = 0.4f;
            if (cc.slopeLimit   < 45f)   cc.slopeLimit   = 45f;
            if (cc.minMoveDistance < 0f) cc.minMoveDistance = 0.001f;
            EditorUtility.SetDirty(player);
            changed = true;
            Debug.Log($"[FixPlayer] CharacterController — skinWidth:{cc.skinWidth:F3} " +
                      $"stepOffset:{cc.stepOffset:F2} slopeLimit:{cc.slopeLimit}");
        }
        else
        {
            Debug.LogWarning("[FixPlayer] No CharacterController on Player!");
        }

        if (changed)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("[FixPlayer] Done — scene saved.");
        }
    }
}
