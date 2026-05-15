using UnityEngine;
using UnityEditor;

/// <summary>
/// Directly stamps loopTime / loopBlend on every animation clip using
/// AnimationUtility.SetAnimationClipSettings — the only API that reliably
/// persists loop settings on clips embedded inside .dae / .fbx files.
/// ModelImporter alone does not always update the AnimationClip object.
/// Run via Tools/Fix Clip Loop Settings.
/// </summary>
public class FixClipLoopSettings
{
    [MenuItem("Tools/Fix Clip Loop Settings")]
    public static void Fix()
    {
        var clips = new (string path, bool loop)[]
        {
            ("Assets/Animations/PlayerAnimations/idle/Ch03_nonPBR.dae",                       true),
            ("Assets/Animations/PlayerAnimations/run/Running.dae",                             true),
            ("Assets/Animations/PlayerAnimations/jump/Jump.dae",                               false),
            ("Assets/Animations/PlayerAnimations/Defeated/Defeated.dae",                       false),
            ("Assets/Animations/SecurityGuardAnimations/Standing Aim Idle 02 Looking.dae",     true),
        };

        int fixed_ = 0;
        foreach (var (path, loop) in clips)
        {
            // Load ALL assets embedded in this file
            Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
            if (all == null || all.Length == 0)
            {
                Debug.LogWarning($"[FixClipLoop] Nothing found at: {path}");
                continue;
            }

            foreach (Object obj in all)
            {
                if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    AnimationClipSettings s = AnimationUtility.GetAnimationClipSettings(clip);

                    bool changed = s.loopTime != loop || s.loopBlend != loop;
                    s.loopTime  = loop;
                    s.loopBlend = loop;   // smooths the pose at the loop seam

                    AnimationUtility.SetAnimationClipSettings(clip, s);
                    EditorUtility.SetDirty(clip);

                    string status = changed ? "CHANGED" : "already ok";
                    Debug.Log($"[FixClipLoop] {clip.name} in {System.IO.Path.GetFileName(path)} " +
                              $"→ loopTime={loop}, loopBlend={loop}  ({status})");
                    fixed_++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[FixClipLoop] Done — processed {fixed_} clip(s).");
    }
}
