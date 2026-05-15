using UnityEngine;
using UnityEditor;

/// <summary>
/// Sets loop time correctly on all character .dae animation clips.
/// Run via Tools > Fix Animation Loop Settings.
/// </summary>
public class FixAnimationLoops
{
    [MenuItem("Tools/Fix Animation Loop Settings")]
    public static void FixLoops()
    {
        // (path, shouldLoop)
        var clips = new (string path, bool loop)[]
        {
            ("Assets/Animations/PlayerAnimations/idle/Ch03_nonPBR.dae",         true),
            ("Assets/Animations/PlayerAnimations/run/Running.dae",               true),
            ("Assets/Animations/PlayerAnimations/jump/Jump.dae",                 false),
            ("Assets/Animations/PlayerAnimations/Defeated/Defeated.dae",         false),
            ("Assets/Animations/SecurityGuardAnimations/Standing Aim Idle 02 Looking.dae", true),
        };

        foreach (var (path, loop) in clips)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[FixLoops] Could not find importer at: {path}");
                continue;
            }

            var anims = importer.clipAnimations;
            if (anims == null || anims.Length == 0)
            {
                // Use default clips and override loop
                var defaults = importer.defaultClipAnimations;
                if (defaults.Length > 0)
                {
                    defaults[0].loop         = loop;
                    defaults[0].loopTime     = loop;
                    defaults[0].loopPose     = loop;
                    importer.clipAnimations  = defaults;
                }
            }
            else
            {
                foreach (var clip in anims)
                {
                    clip.loop     = loop;
                    clip.loopTime = loop;
                    clip.loopPose = loop;
                }
                importer.clipAnimations = anims;
            }

            AssetDatabase.WriteImportSettingsIfDirty(path);
            importer.SaveAndReimport();
            Debug.Log($"[FixLoops] {System.IO.Path.GetFileName(path)} → loop={loop}");
        }

        AssetDatabase.Refresh();
        Debug.Log("[FixLoops] Done.");
    }
}
