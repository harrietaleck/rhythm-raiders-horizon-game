using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Wires the imported stambleFall.fbx into the player's Animator:
/// a "Stumble" trigger from Any State that plays trip → fall → get up and
/// blends back into Run. The FBX uses "mixamorig:" bone names in metres while the
/// player model (Collada) uses "mixamorig_" names in centimetres, so the curves are
/// copied into a new clip with renamed paths and scaled positions.
/// </summary>
public static class SetupStumbleAnimation
{
    const string FbxPath = "Assets/Animations/stambleFall.fbx";
    const string ControllerPath = "Assets/Animations/PlayerAnimations/PlayerController.controller";
    const string RunModel = "Assets/Animations/PlayerAnimations/run/Running.dae";

    [MenuItem("Tools/Inspect Stumble Clip")]
    public static void Inspect()
    {
        var sb = new StringBuilder("[Stumble] ");
        var clip = LoadClip(FbxPath);
        if (clip == null) { Debug.LogError("[Stumble] no clip in " + FbxPath); return; }
        sb.Append($"clip '{clip.name}' len={clip.length:F2}s fps={clip.frameRate} loop={clip.isLooping}\n");
        var b = AnimationUtility.GetCurveBindings(clip);
        sb.Append("paths: " + string.Join(" | ", b.Select(x => x.path).Distinct().Take(3)) + "\n");
        foreach (var bb in b)
        {
            if (!bb.path.EndsWith("Hips") || !bb.propertyName.StartsWith("m_LocalPosition")) continue;
            var cv = AnimationUtility.GetEditorCurve(clip, bb);
            sb.Append(bb.propertyName + ":");
            for (int i = 0; i <= 16; i++) sb.Append(" " + cv.Evaluate(clip.length * i / 16f).ToString("F2"));
            sb.Append("\n");
        }
        var run = LoadClip(RunModel);
        if (run != null)
        {
            var rb = AnimationUtility.GetCurveBindings(run);
            sb.Append("run paths: " + string.Join(" | ", rb.Select(x => x.path).Distinct().Take(3)) + "\n");
            foreach (var bb in rb)
            {
                if (!bb.path.EndsWith("Hips") || bb.propertyName != "m_LocalPosition.y") continue;
                var cv = AnimationUtility.GetEditorCurve(run, bb);
                sb.Append("run hips y0=" + cv.Evaluate(0).ToString("F2") + "\n");
            }
        }
        Debug.Log(sb.ToString());
    }

    [MenuItem("Tools/Setup Stumble Animation")]
    public static void Setup()
    {
        var src = LoadClip(FbxPath);
        if (src == null) { Debug.LogError("[Stumble] no clip in " + FbxPath); return; }
        var clip = BuildRetargetedClip(src);
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (clip == null || ctrl == null) { Debug.LogError("[Stumble] clip or controller missing"); return; }

        if (!ctrl.parameters.Any(p => p.name == "Stumble"))
            ctrl.AddParameter("Stumble", AnimatorControllerParameterType.Trigger);

        var sm = ctrl.layers[0].stateMachine;
        var stumble = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == "Stumble");
        if (stumble == null) stumble = sm.AddState("Stumble", new Vector3(520, 260, 0));
        stumble.motion = clip;
        stumble.writeDefaultValues = true;
        stumble.speed = 1.5f;    // played from 0.45 s → ~1.25 s fall and get up

        foreach (var t in sm.anyStateTransitions.Where(t => t.destinationState == stumble).ToArray())
            sm.RemoveAnyStateTransition(t);
        var anyT = sm.AddAnyStateTransition(stumble);
        anyT.AddCondition(AnimatorConditionMode.If, 0, "Stumble");
        anyT.hasExitTime = false;
        anyT.duration = 0.06f;
        anyT.canTransitionToSelf = false;

        var run = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == "Run");
        foreach (var t in stumble.transitions.ToArray()) stumble.RemoveTransition(t);
        if (run != null)
        {
            var back = stumble.AddTransition(run);
            back.hasExitTime = true;
            back.exitTime = 0.92f;
            back.duration = 0.18f;
        }
        var dead = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == "Dead");
        if (dead != null)
        {
            var toDead = stumble.AddTransition(dead);
            toDead.hasExitTime = false;
            toDead.duration = 0.1f;
            toDead.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
        }

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Stumble] Stumble state wired. clip len={clip.length:F2}s");
    }

    const string OutClip = "Assets/Animations/PlayerAnimations/StumbleFall.anim";

    static AnimationClip BuildRetargetedClip(AnimationClip src)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(OutClip);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, OutClip);
        }
        clip.ClearCurves();
        clip.frameRate = src.frameRate;

        foreach (var b in AnimationUtility.GetCurveBindings(src))
        {
            var curve = AnimationUtility.GetEditorCurve(src, b);
            if (curve == null) continue;
            if (b.propertyName.StartsWith("m_LocalScale")) continue;   // rig scale differs; scale never animates
            if (b.propertyName.StartsWith("m_LocalPosition"))
            {
                var keys = curve.keys;
                for (int i = 0; i < keys.Length; i++)
                {
                    keys[i].value *= 100f;
                    keys[i].inTangent *= 100f;
                    keys[i].outTangent *= 100f;
                }
                curve.keys = keys;
            }
            var nb = new EditorCurveBinding
            {
                path = b.path.Replace("mixamorig:", "mixamorig_"),
                propertyName = b.propertyName,
                type = b.type
            };
            AnimationUtility.SetEditorCurve(clip, nb, curve);
        }
        clip.EnsureQuaternionContinuity();
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    static AnimationClip LoadClip(string path) =>
        AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview"));
}
