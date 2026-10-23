using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Tools → Setup Dance Floor Characters: imports the Mixamo dance floor characters and dance
/// animations as Humanoid (so any dance plays on any character), loops the clips in place,
/// builds the crowd / DJ / bodyguard Animator Controllers and registers everything in
/// Resources/FxLibrary for the levels to spawn.
/// </summary>
[InitializeOnLoad]
public static class SetupDanceFloorCharacters
{
    // First load after an update: if the bouncer idle hasn't been generated yet, run the setup once.
    static SetupDanceFloorCharacters()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(OutDir + "/BouncerIdle.anim") != null) return;
            if (!AssetDatabase.IsValidFolder(CharactersDir)) return;
            Setup();
        };
    }

    const string CharactersDir = "Assets/Animations/characters";
    const string DancesDir = "Assets/Animations/dancefloorcharacterAnimations";
    const string OutDir = "Assets/Animations/DanceFloor";
    const string LibraryPath = "Assets/Resources/FxLibrary.asset";

    [MenuItem("Tools/Setup Dance Floor Characters")]
    public static void Setup()
    {
        if (!AssetDatabase.IsValidFolder(OutDir)) AssetDatabase.CreateFolder("Assets/Animations", "DanceFloor");

        var characterPaths = Directory.GetFiles(CharactersDir, "*.fbx").Select(p => p.Replace('\\', '/')).ToList();
        var dancePaths = Directory.GetFiles(DancesDir, "*.fbx").Select(p => p.Replace('\\', '/')).ToList();
        foreach (var p in characterPaths.Concat(dancePaths)) MakeHumanoid(p);
        foreach (var p in characterPaths) ExtractTextures(p);

        // Crowd dances (the Ch46 clips) and the DJ's own dance.
        var crowdClips = new List<AnimationClip>();
        AnimationClip djClip = null;
        foreach (var p in dancePaths)
        {
            var clip = FirstClip(p);
            if (clip == null) continue;
            if (Path.GetFileName(p).ToLowerInvariant().StartsWith("dj")) djClip = clip;
            else crowdClips.Add(clip);
        }

        var crowdCtrl = BuildController(OutDir + "/DanceCrowd.controller", crowdClips, out var crowdStates);
        var djCtrl = BuildController(OutDir + "/DJ.controller",
            new List<AnimationClip> { djClip ?? FirstClip(CharactersDir + "/dj.fbx") }.Where(c => c != null).ToList(), out _);
        var guardClip = FirstClip(CharactersDir + "/djbodyguard.fbx");
        if (guardClip == null || guardClip.length < 0.1f) guardClip = BuildBouncerIdle(OutDir + "/BouncerIdle.anim");
        var guardCtrl = BuildController(OutDir + "/Bodyguard.controller",
            new List<AnimationClip> { guardClip }.Where(c => c != null).ToList(), out _);

        var lib = AssetDatabase.LoadAssetAtPath<FxLibrary>(LibraryPath);
        if (lib == null)
        {
            lib = ScriptableObject.CreateInstance<FxLibrary>();
            AssetDatabase.CreateAsset(lib, LibraryPath);
        }
        lib.dancers = characterPaths
            .Where(p => Path.GetFileName(p).ToLowerInvariant().Contains("dancefloorcharacter"))
            .Select(p => AssetDatabase.LoadAssetAtPath<GameObject>(p)).Where(g => g != null).ToArray();
        lib.dj = AssetDatabase.LoadAssetAtPath<GameObject>(CharactersDir + "/dj.fbx");
        lib.bodyguard = AssetDatabase.LoadAssetAtPath<GameObject>(CharactersDir + "/djbodyguard.fbx");
        lib.crowdController = crowdCtrl;
        lib.djController = djCtrl;
        lib.bodyguardController = guardCtrl;
        lib.danceStates = crowdStates.ToArray();
        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        Debug.Log($"[DanceFloor] dancers={lib.dancers.Length} dances={crowdStates.Count} dj={(lib.dj != null)} djClip={(djClip != null)} bodyguard={(lib.bodyguard != null)} guardClip={(guardClip != null)}");
    }

    static void MakeHumanoid(string path)
    {
        var mi = AssetImporter.GetAtPath(path) as ModelImporter;
        if (mi == null) return;
        mi.animationType = ModelImporterAnimationType.Human;
        mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        var clips = mi.clipAnimations;
        if (clips == null || clips.Length == 0) clips = mi.defaultClipAnimations;
        foreach (var c in clips)
        {
            c.loopTime = true;
            c.loopPose = true;
            c.lockRootRotation = true;      // dance in place, facing where we put them
            c.keepOriginalOrientation = true;
            c.lockRootHeightY = true;
            c.keepOriginalPositionY = true;
            c.lockRootPositionXZ = true;
            c.keepOriginalPositionXZ = true;
        }
        mi.clipAnimations = clips;
        mi.SaveAndReimport();
    }

    /// <summary>
    /// The bodyguard FBX has no motion, so build a humanoid idle from muscle curves: wide stance,
    /// hands clasped in front, chest breathing and the head slowly scanning the crowd.
    /// </summary>
    static AnimationClip BuildBouncerIdle(string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        var clip = existing != null ? existing : new AnimationClip { name = "BouncerIdle" };
        clip.ClearCurves();
        const float len = 6f;
        void Pose(string muscle, float v) => Curve(muscle, AnimationCurve.Constant(0f, len, v));
        void Curve(string muscle, AnimationCurve c) =>
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), muscle), c);
        AnimationCurve Wave(float a, float b, int cycles)
        {
            var c = new AnimationCurve();
            int n = cycles * 2;
            for (int i = 0; i <= n; i++) c.AddKey(new Keyframe(len * i / n, i % 2 == 0 ? a : b));
            for (int i = 0; i < c.length; i++) c.SmoothTangents(i, 0f);
            return c;
        }

        foreach (var side in new[] { "Left", "Right" })
        {
            Pose(side + " Arm Down-Up", -0.45f);
            Pose(side + " Arm Front-Back", 0.55f);
            Pose(side + " Arm Twist In-Out", 0.1f);
            Pose(side + " Forearm Stretch", 0.15f);
            Pose(side + " Forearm Twist In-Out", 0.3f);
            Pose(side + " Hand Down-Up", 0f);
            Pose(side + " Upper Leg In-Out", 0.2f);
            Pose(side + " Upper Leg Front-Back", 0f);
            Pose(side + " Lower Leg Stretch", 0.95f);
            Pose(side + " Shoulder Down-Up", -0.1f);
        }
        // Body root: standing height (normalised), facing forward. Without it the hips sit on the floor.
        Pose("RootT.x", 0f); Pose("RootT.y", 1.0f); Pose("RootT.z", 0f);
        Pose("RootQ.x", 0f); Pose("RootQ.y", 0f); Pose("RootQ.z", 0f); Pose("RootQ.w", 1f);
        Curve("Chest Front-Back", Wave(0f, 0.12f, 3));
        Curve("Spine Front-Back", Wave(0f, 0.05f, 3));
        Curve("Head Turn Left-Right", Wave(-0.3f, 0.3f, 1));
        Curve("Head Nod Down-Up", Wave(-0.05f, 0.05f, 2));
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        if (existing == null) AssetDatabase.CreateAsset(clip, path);
        else EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        return clip;
    }

    [MenuItem("Tools/Rebuild Bouncer Idle")]
    static void RebuildBouncerIdle()
    {
        BuildBouncerIdle(OutDir + "/BouncerIdle.anim");
        Debug.Log("[DanceFloor] bouncer idle rebuilt");
    }

    /// <summary>Mixamo embeds the skin textures in the FBX; pull them out so the materials pick them up.</summary>
    [MenuItem("Tools/Extract Dance Floor Textures")]
    static void ExtractAll()
    {
        foreach (var p in Directory.GetFiles(CharactersDir, "*.fbx")) ExtractTextures(p.Replace('\\', '/'));
    }

    static void ExtractTextures(string path)
    {
        var mi = AssetImporter.GetAtPath(path) as ModelImporter;
        if (mi == null) return;
        string dir = CharactersDir + "/Textures/" + Path.GetFileNameWithoutExtension(path);
        if (AssetDatabase.IsValidFolder(dir)) return;
        Directory.CreateDirectory(dir);
        AssetDatabase.Refresh();
        if (mi.ExtractTextures(dir))
        {
            AssetDatabase.Refresh();
            mi.materialSearch = ModelImporterMaterialSearch.Everywhere;
            mi.SaveAndReimport();
            Debug.Log("[DanceFloor] extracted textures for " + Path.GetFileName(path));
        }
    }

    static AnimationClip FirstClip(string path) =>
        AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"));

    static AnimatorController BuildController(string path, List<AnimationClip> clips, out List<string> states)
    {
        states = new List<string>();
        AssetDatabase.DeleteAsset(path);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        var sm = ctrl.layers[0].stateMachine;
        int i = 0;
        foreach (var clip in clips)
        {
            string name = "Dance" + i;
            var st = sm.AddState(name, new Vector3(260, 60 + i * 60, 0));
            st.motion = clip;
            if (i == 0) sm.defaultState = st;
            states.Add(name);
            i++;
        }
        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }
}
