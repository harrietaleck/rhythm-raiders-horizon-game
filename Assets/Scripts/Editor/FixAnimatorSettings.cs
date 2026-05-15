using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Fixes the "brief animation then T-pose" glitch on game start.
/// Causes:
///   1. Apply Root Motion ON  → Idle root motion fights CharacterController, snaps model back
///   2. Write Defaults ON     → bones not in current clip get reset to bind-pose (T-pose) each frame
///   3. Animator Update Mode  → AnimatePhysics can cause one-frame desync on startup
/// Run via Tools/Fix Animator Settings.
/// </summary>
public class FixAnimatorSettings
{
    [MenuItem("Tools/Fix Animator Settings")]
    public static void Fix()
    {
        // ── Player animator ───────────────────────────────────────────────
        GameObject player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("[FixAnim] Player not found."); return; }

        Transform ch03 = player.transform.Find("Ch03_nonPBR");
        Animator playerAnim = ch03 != null
            ? ch03.GetComponent<Animator>()
            : player.GetComponentInChildren<Animator>();

        if (playerAnim == null)
        {
            Debug.LogError("[FixAnim] No Animator found on Player/Ch03_nonPBR.");
            return;
        }

        FixAnimator(playerAnim, "Player");

        // ── Enemy animator ────────────────────────────────────────────────
        GameObject enemy = GameObject.Find("Enemy");
        if (enemy != null)
        {
            Transform guard = enemy.transform.Find("Standing Aim Idle 02 Looking");
            Animator enemyAnim = guard != null
                ? guard.GetComponent<Animator>()
                : enemy.GetComponentInChildren<Animator>();

            if (enemyAnim != null)
                FixAnimator(enemyAnim, "Enemy");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[FixAnim] Done — scene saved.");
    }

    static void FixAnimator(Animator anim, string label)
    {
        // 1. Root motion fights CharacterController → always off for CC characters
        anim.applyRootMotion = false;
        Debug.Log($"[FixAnim] {label}: applyRootMotion = false");

        // 2. Update mode: Normal (not AnimatePhysics) prevents 1-frame startup desync
        anim.updateMode = AnimatorUpdateMode.Normal;
        Debug.Log($"[FixAnim] {label}: updateMode = Normal");

        // 3. Culling: AlwaysAnimate so animation runs even when off-screen
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        Debug.Log($"[FixAnim] {label}: cullingMode = AlwaysAnimate");

        EditorUtility.SetDirty(anim);

        // 4. Write Defaults OFF on every state in the controller
        //    With Write Defaults ON, any bone NOT animated in the current state
        //    gets snapped back to its default transform (T-pose) each frame.
        //    Setting it OFF means "keep last value" — bones stay in the pose
        //    the previous state left them in.
        AnimatorController ctrl = anim.runtimeAnimatorController as AnimatorController;
        if (ctrl == null)
        {
            Debug.LogWarning($"[FixAnim] {label}: controller is not an AnimatorController asset — skipping Write Defaults fix.");
            return;
        }

        int stateCount = 0;
        foreach (var layer in ctrl.layers)
        {
            SetWriteDefaults(layer.stateMachine, ref stateCount);
        }

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        Debug.Log($"[FixAnim] {label}: Write Defaults set to FALSE on {stateCount} state(s).");
    }

    // Looping states (Idle, Run) need Write Defaults ON so every bone is written
    // on every frame — this prevents the T-pose flash at the clip loop seam.
    // One-shot states (Jump, Dead) keep Write Defaults OFF so non-animated bones
    // aren't snapped back to bind-pose when those short clips don't cover all bones.
    static readonly System.Collections.Generic.HashSet<string> s_LoopingStates =
        new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        { "Idle", "Run" };

    static void SetWriteDefaults(AnimatorStateMachine sm, ref int count)
    {
        foreach (var cs in sm.states)
        {
            bool isLooping = s_LoopingStates.Contains(cs.state.name);
            cs.state.writeDefaultValues = isLooping;   // true for Idle/Run, false for Jump/Dead
            count++;
            Debug.Log($"[FixAnim] State '{cs.state.name}' → writeDefaultValues = {isLooping}");
        }
        foreach (var child in sm.stateMachines)
        {
            SetWriteDefaults(child.stateMachine, ref count);
        }
    }
}
