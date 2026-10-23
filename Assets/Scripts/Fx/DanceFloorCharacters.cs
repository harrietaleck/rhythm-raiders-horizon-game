using UnityEngine;

/// <summary>
/// Spawns the imported dance floor characters: crowd dancers (random dance, random start so a
/// group never moves in sync), the DJ and DJ bodyguards. Returns null if the library isn't set up.
/// </summary>
public static class DanceFloorCharacters
{
    public static Transform SpawnDancer(Transform parent, Vector3 feet, Vector3 facing, float height = 1.75f)
    {
        var lib = FxLibrary.Get();
        if (lib == null || lib.dancers == null || lib.dancers.Length == 0 || lib.crowdController == null) return null;
        var prefab = lib.dancers[Random.Range(0, lib.dancers.Length)];
        var t = Spawn(parent, prefab, feet, facing, height, lib.crowdController, "Dancer");
        if (t == null) return null;
        var anim = t.GetComponentInChildren<Animator>();
        if (anim != null && lib.danceStates != null && lib.danceStates.Length > 0)
        {
            anim.Play(lib.danceStates[Random.Range(0, lib.danceStates.Length)], 0, Random.value);
            anim.speed = Random.Range(0.9f, 1.1f);
            anim.Update(0f);
        }
        return t;
    }

    public static Transform SpawnDJ(Transform parent, Vector3 feet, Vector3 facing)
    {
        var lib = FxLibrary.Get();
        if (lib == null || lib.dj == null) return null;
        // The DJ works behind a fixed-size booth, so he keeps a real-world height rather than the runner's.
        var t = Spawn(parent, lib.dj, feet, facing, 1.95f, lib.djController, "DJ Character", absoluteHeight: true);
        var anim = t != null ? t.GetComponentInChildren<Animator>() : null;
        if (anim != null) { anim.Play(0, 0, Random.value); anim.Update(0f); }
        return t;
    }

    public static Transform SpawnBodyguard(Transform parent, Vector3 feet, Vector3 facing)
    {
        var lib = FxLibrary.Get();
        if (lib == null || lib.bodyguard == null) return null;
        return Spawn(parent, lib.bodyguard, feet, facing, 1.85f, lib.bodyguardController, "Bodyguard Character", alwaysAnimate: true);
    }

    static Transform Spawn(Transform parent, GameObject prefab, Vector3 feet, Vector3 facing, float height,
                           RuntimeAnimatorController controller, string name,
                           bool absoluteHeight = false, bool alwaysAnimate = false)
    {
        if (prefab == null) return null;
        var root = new GameObject(name).transform;
        root.SetParent(parent, true);
        root.position = feet;
        if (facing.sqrMagnitude > 0.001f) root.rotation = Quaternion.LookRotation(new Vector3(facing.x, 0f, facing.z));

        var model = Object.Instantiate(prefab, root);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        foreach (var c in model.GetComponentsInChildren<Collider>()) Object.Destroy(c);
        StripExtras(model);

        var anim = model.GetComponentInChildren<Animator>();
        if (anim == null) anim = model.AddComponent<Animator>();
        if (controller != null) anim.runtimeAnimatorController = controller;
        anim.applyRootMotion = false;
        // Crowd dancers stop animating off screen; the DJ and guards are few, so they always animate
        // (a culled one could be left standing in its bind T-pose when it first comes into view).
        anim.cullingMode = alwaysAnimate || absoluteHeight ? AnimatorCullingMode.AlwaysAnimate : AnimatorCullingMode.CullUpdateTransforms;

        // Size against the runner: the characters share the floor with the player, so they are
        // scaled to the runner's height (height is given for a 1.75 m reference person).
        float target = absoluteHeight ? height : RunnerHeight() * (height / 1.75f);
        var head = anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Head) : null;
        var lFoot = anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.LeftFoot) : null;
        var rFoot = anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.RightFoot) : null;
        if (head != null && lFoot != null && rFoot != null)
        {
            // Head bone sits at the base of the skull: ~92% of standing height above the soles.
            float h = (head.position.y - Mathf.Min(lFoot.position.y, rFoot.position.y)) / 0.88f;
            if (h > 0.01f) model.transform.localScale *= target / h;
        }
        else
        {
            var rs = model.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                if (b.size.y > 0.01f) model.transform.localScale *= target / b.size.y;
            }
        }
        // Keep the soles on the floor every frame (dances crouch, kick and dip).
        var grounder = anim.gameObject.AddComponent<FeetOnFloor>();
        grounder.floorY = feet.y;
        grounder.Snap();
        foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>()) smr.updateWhenOffscreen = false;
        return root;
    }

    /// <summary>
    /// Imported models can carry the scene camera and lights from the program they were exported
    /// from; a stray camera would take over the screen, so remove them.
    /// </summary>
    public static void StripExtras(GameObject model)
    {
        foreach (var c in model.GetComponentsInChildren<Camera>(true)) { c.enabled = false; Object.Destroy(c.gameObject == model ? (Object)c : c.gameObject); }
        foreach (var l in model.GetComponentsInChildren<Light>(true)) { l.enabled = false; Object.Destroy(l); }
        foreach (var a in model.GetComponentsInChildren<AudioListener>(true)) { a.enabled = false; Object.Destroy(a); }
    }

    static float _runnerHeight;
    static int _runnerScene = -1;

    /// <summary>Standing height of the player's runner in world units (cached per scene).</summary>
    public static float RunnerHeight()
    {
        int scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
        if (_runnerScene == scene && _runnerHeight > 0f) return _runnerHeight;
        _runnerScene = scene;
        _runnerHeight = 1.75f;
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return _runnerHeight;
        Transform head = null;
        foreach (var t in p.GetComponentsInChildren<Transform>(true))
            if (t.name.EndsWith("Head")) { head = t; break; }
        var cc = p.GetComponent<CharacterController>();
        float floor = cc != null ? cc.bounds.min.y : p.transform.position.y;
        if (head != null && head.position.y - floor > 0.3f) _runnerHeight = (head.position.y - floor) / 0.88f;
        return _runnerHeight;
    }
}
