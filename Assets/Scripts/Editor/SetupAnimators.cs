using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;

public class SetupAnimators
{
    [MenuItem("Tools/Diagnose Animation Clips")]
    public static void DiagnoseClips()
    {
        string[] folders = { "Assets/Animations/PlayerAnimations", "Assets/Animations/SecurityGuardAnimations" };
        foreach (string folder in folders)
        {
            Debug.Log($"[Diag] Scanning: {folder}");
            string[] guids = AssetDatabase.FindAssets("", new[] { folder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (Object o in all)
                    if (o is AnimationClip ac)
                        Debug.Log($"[Diag]  CLIP: '{ac.name}'  at  {path}");
            }
        }
    }

    [MenuItem("Tools/Setup Player & Enemy Animators")]
    public static void SetupAll()
    {
        SetupPlayerAnimator();
        SetupEnemyAnimator();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SetupAnimators] Done — Player and Enemy Animator Controllers created and assigned.");
    }

    // Load the real (non-preview) Take 001 clip from a .dae file at an exact path
    static AnimationClip LoadClipFromDae(string assetPath)
    {
        Object[] all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (Object o in all)
            if (o is AnimationClip ac && !ac.name.StartsWith("__preview__"))
                return ac;
        return null;
    }

    static void SetupPlayerAnimator()
    {
        string controllerPath = "Assets/Animations/PlayerAnimations/PlayerController.controller";
        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // Parameters
        ctrl.AddParameter("Speed",  AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Jump",   AnimatorControllerParameterType.Trigger);   // one-shot trigger
        ctrl.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

        var root = ctrl.layers[0].stateMachine;

        // Find clips — all Mixamo .dae clips are named "Take 001" internally
        AnimationClip idle     = LoadClipFromDae("Assets/Animations/PlayerAnimations/idle/Ch03_nonPBR.dae");
        AnimationClip run      = LoadClipFromDae("Assets/Animations/PlayerAnimations/run/Running.dae");
        AnimationClip jump     = LoadClipFromDae("Assets/Animations/PlayerAnimations/jump/Jump.dae");
        AnimationClip defeated = LoadClipFromDae("Assets/Animations/PlayerAnimations/Defeated/Defeated.dae");

        Debug.Log($"[SetupAnimators] Player clips — idle:{idle != null} run:{run != null} jump:{jump != null} defeated:{defeated != null}");

        // States
        var idleState  = idle     != null ? root.AddState("Idle",     Vector3.zero)  : root.AddState("Idle");
        var runState   = run      != null ? root.AddState("Run",      new Vector3(200,0,0)) : root.AddState("Run");
        var jumpState  = jump     != null ? root.AddState("Jump",     new Vector3(400,0,0)) : root.AddState("Jump");
        AnimatorState deadState = null;

        if (idle)     idleState.motion  = idle;
        if (run)      runState.motion   = run;
        if (jump)     jumpState.motion  = jump;

        if (defeated != null)
        {
            deadState = root.AddState("Dead", new Vector3(200, -100, 0));
            deadState.motion = defeated;
        }

        root.defaultState = idleState;

        // Idle -> Run (Speed > 0.1)
        var t = idleState.AddTransition(runState);
        t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        t.hasExitTime = false;

        // Run -> Idle (Speed < 0.1)
        t = runState.AddTransition(idleState);
        t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        t.hasExitTime = false;

        // Any -> Jump (trigger fires once)
        var jt = ctrl.layers[0].stateMachine.AddAnyStateTransition(jumpState);
        jt.AddCondition(AnimatorConditionMode.If, 0, "Jump");
        jt.hasExitTime = false;
        jt.canTransitionToSelf = false;

        // Jump -> Idle (plays full clip then returns, no condition needed)
        t = jumpState.AddTransition(idleState);
        t.hasExitTime = true;
        t.exitTime    = 0.9f;   // transition near end of clip
        t.duration    = 0.1f;

        // Any -> Dead
        if (deadState != null)
        {
            var dt = ctrl.layers[0].stateMachine.AddAnyStateTransition(deadState);
            dt.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
            dt.hasExitTime = false;
        }

        // Assign to Player's character model
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            // Try Ch03_nonPBR child first, then Player itself
            Transform charTransform = player.transform.Find("Ch03_nonPBR") ?? player.transform;
            Animator anim = charTransform.GetComponent<Animator>();
            if (anim == null) anim = charTransform.gameObject.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            EditorUtility.SetDirty(charTransform.gameObject);
            Debug.Log($"[SetupAnimators] Player Animator assigned to {charTransform.name}");
        }
    }

    static void SetupEnemyAnimator()
    {
        string controllerPath = "Assets/Animations/SecurityGuardAnimations/EnemyController.controller";
        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Jump",  AnimatorControllerParameterType.Trigger);  // one-shot trigger

        var root = ctrl.layers[0].stateMachine;

        // Security guard idle from its own .dae; run + jump shared from PlayerAnimations
        AnimationClip idle = LoadClipFromDae("Assets/Animations/SecurityGuardAnimations/Standing Aim Idle 02 Looking.dae");
        AnimationClip run  = LoadClipFromDae("Assets/Animations/PlayerAnimations/run/Running.dae");
        AnimationClip jump = LoadClipFromDae("Assets/Animations/PlayerAnimations/jump/Jump.dae");

        Debug.Log($"[SetupAnimators] Enemy clips — idle:{idle != null} run:{run != null} jump:{jump != null}");

        var idleState = root.AddState("Idle", Vector3.zero);
        var runState  = root.AddState("Run",  new Vector3(200, 0, 0));
        var jumpState = root.AddState("Jump", new Vector3(400, 0, 0));

        if (idle) idleState.motion = idle;
        if (run)  runState.motion  = run;
        if (jump) jumpState.motion = jump;

        root.defaultState = idleState;

        var t = idleState.AddTransition(runState);
        t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        t.hasExitTime = false;

        t = runState.AddTransition(idleState);
        t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        t.hasExitTime = false;

        var jt = ctrl.layers[0].stateMachine.AddAnyStateTransition(jumpState);
        jt.AddCondition(AnimatorConditionMode.If, 0, "Jump");
        jt.hasExitTime = false;
        jt.canTransitionToSelf = false;

        t = jumpState.AddTransition(idleState);
        t.hasExitTime = true;
        t.exitTime    = 0.9f;
        t.duration    = 0.1f;

        // Assign to Enemy's character model
        GameObject enemy = GameObject.Find("Enemy");
        if (enemy != null)
        {
            Transform charTransform = enemy.transform.Find("Standing Aim Idle 02 Looking") ?? enemy.transform;
            Animator anim = charTransform.GetComponent<Animator>();
            if (anim == null) anim = charTransform.gameObject.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            EditorUtility.SetDirty(charTransform.gameObject);
            Debug.Log($"[SetupAnimators] Enemy Animator assigned to {charTransform.name}");
        }
    }
}
