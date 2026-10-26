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
        // Solid body: the runner can bump into dancers (and loses notes from her bag).
        AddBody(t, RunnerHeight() * (height / 1.75f));
        t.gameObject.AddComponent<DancerBump>();
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
        // The DJ is about the runner's height, like everyone else in the club.
        var t = Spawn(parent, lib.dj, feet, facing, 1.75f * DJScale, lib.djController, "DJ Character", alwaysAnimate: true);
        var anim = t != null ? t.GetComponentInChildren<Animator>() : null;
        if (anim != null) { anim.Play(0, 0, Random.value); anim.Update(0f); }
        return t;
    }

    public static Transform SpawnBodyguard(Transform parent, Vector3 feet, Vector3 facing)
    {
        var lib = FxLibrary.Get();
        if (lib == null || lib.bodyguard == null) return null;
        // A bodyguard is a little taller than the runner.
        var t = Spawn(parent, lib.bodyguard, feet, facing, 1.75f * BodyguardScale, lib.bodyguardController, "Bodyguard Character", alwaysAnimate: true);
        if (t == null) return null;
        var anim = t.GetComponentInChildren<Animator>();
        if (anim != null) { anim.Play(0, 0, Random.value); anim.Update(0f); }
        // Run into him and he grabs the runner: game over.
        AddBody(t, RunnerHeight() * BodyguardScale);
        t.gameObject.AddComponent<BodyguardCatch>();
        GameplayCollisionUtility.EnsureKinematicRigidbody(t.gameObject);
        return t;
    }

    /// <summary>Puts a pistol like the museum guards' in the character's right hand, pointing ahead.</summary>
    public static void AttachSidearm(Transform character)
    {
        var anim = character != null ? character.GetComponentInChildren<Animator>() : null;
        var hand = anim != null && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.RightHand) : null;
        if (hand == null) return;
        float s = RunnerHeight() / 1.75f;
        var gun = new GameObject("Sidearm").transform;
        gun.position = hand.position + character.forward * 0.08f * s;
        gun.rotation = Quaternion.LookRotation(character.forward, Vector3.up);
        gun.localScale = Vector3.one * s;
        var metal = RooftopProps.Mat(new Color(0.08f, 0.08f, 0.09f));
        RooftopProps.Part(PrimitiveType.Cube, gun, new Vector3(0f, 0f, 0.1f), new Vector3(0.06f, 0.08f, 0.26f), metal, false);
        RooftopProps.Part(PrimitiveType.Cube, gun, new Vector3(0f, -0.07f, 0f), new Vector3(0.05f, 0.12f, 0.06f), metal, false, new Vector3(-15f, 0f, 0f));
        gun.SetParent(hand, true);
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
        float h = MeasureHeight(model.transform);
        if (h > 0.01f)
        {
            model.transform.localScale *= target / h;
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

    /// <summary>Heights relative to the runner.</summary>
    public static float BodyguardScale = 1.06f;
    public static float DJScale = 1.0f;

    static void AddBody(Transform root, float h)
    {
        var cap = root.gameObject.AddComponent<CapsuleCollider>();
        cap.height = h;
        cap.radius = h * 0.13f;
        cap.center = new Vector3(0f, h * 0.5f, 0f);
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
        // Same measure as the other characters: top of the head over the feet.
        float m = MeasureHeight(p.transform);
        if (m > 0.3f) _runnerHeight = m;
        return _runnerHeight;
    }

    /// <summary>
    /// Standing height from the rig: the Mixamo top-of-head bone over the lowest foot bone (the
    /// ankle sits ~5% above the sole). Works the same for cartoon and realistic proportions.
    /// Returns 0 if the bones aren't found.
    /// </summary>
    public static float MeasureHeight(Transform root)
    {
        Transform top = null, head = null;
        float foot = float.MaxValue;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name;
            if (n.EndsWith("HeadTop_End")) top = t;
            else if (head == null && n.EndsWith("Head")) head = t;
            else if (n.EndsWith("LeftFoot") || n.EndsWith("RightFoot")) foot = Mathf.Min(foot, t.position.y);
        }
        if (foot == float.MaxValue) return 0f;
        if (top != null) return (top.position.y - foot) / 0.95f;
        if (head != null) return (head.position.y - foot) / 0.83f;
        return 0f;
    }
}
