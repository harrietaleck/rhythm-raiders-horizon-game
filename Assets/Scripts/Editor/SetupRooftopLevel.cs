using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-shot Level 1 rooftop pass for the Steal n Catch kit.
/// Converts kit materials to URP, builds a panoramic skybox, and lays a
/// walkable roof course in MainGameL1Opera. Does not touch PlayerMovement.
/// </summary>
public static class SetupRooftopLevel
{
    const string ScenePath = "Assets/Scenes/MainGameL1Opera.unity";
    const string KitRoot = "Assets/Steal n Catch";
    const int GroundLayer = 6;

    [MenuItem("Tools/Setup Rooftop Level 1")]
    public static void Run()
    {
        int mats = UpgradeKitMaterials();
        Material sky = CreateSkybox();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        BuildCourse(sky);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Rooftop] Level 1 course saved. URP materials upgraded: {mats}.");
    }

    static int UpgradeKitMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("[Rooftop] URP Lit shader not found.");
            return 0;
        }

        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { KitRoot }))
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (mat == null) continue;
            if (!FixURPMaterials.UpgradeMaterial(mat, urpLit)) continue;
            EditorUtility.SetDirty(mat);
            count++;
        }
        return count;
    }

    static Material CreateSkybox()
    {
        var fastSky = AssetDatabase.LoadAssetAtPath<Material>("Assets/FastSky/Materials/RealisticSky.mat");
        if (fastSky != null) return fastSky;

        const string path = KitRoot + "/Skybox/CliffSideSkybox.mat";
        return AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    static void BuildCourse(Material sky)
    {
        if (sky != null)
        {
            if (sky.HasProperty("_CloudDensity")) sky.SetFloat("_CloudDensity", 2.6f);
            if (sky.HasProperty("_CloudThickness")) sky.SetFloat("_CloudThickness", 0.22f);
            if (sky.HasProperty("_CloudScale")) sky.SetFloat("_CloudScale", 0.65f);
            if (sky.HasProperty("_CloudColour")) sky.SetColor("_CloudColour", new Color(1f, 0.78f, 0.62f));
            if (sky.HasProperty("_EveningColour")) sky.SetColor("_EveningColour", new Color(1f, 0.42f, 0.28f));
            if (sky.HasProperty("_SkyColour")) sky.SetColor("_SkyColour", new Color(0.55f, 0.62f, 0.85f));
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
            DynamicGI.UpdateEnvironment();
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.95f, 0.48f, 0.28f);
        RenderSettings.ambientEquatorColor = new Color(0.72f, 0.38f, 0.28f);
        RenderSettings.ambientGroundColor = new Color(0.18f, 0.10f, 0.12f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.85f, 0.42f, 0.28f);
        RenderSettings.fogStartDistance = 40f;
        RenderSettings.fogEndDistance = 120f;

        var sun = GameObject.Find("Directional Light");
        if (sun != null)
        {
            var light = sun.GetComponent<Light>();
            if (light != null)
            {
                light.color = new Color(1f, 0.55f, 0.28f);
                light.intensity = 1.35f;
            }
            sun.transform.rotation = Quaternion.Euler(12f, -40f, 0f);
            var sunColor = sun.GetComponent<FastSky_Sun_Color>();
            if (sunColor == null) sunColor = sun.AddComponent<FastSky_Sun_Color>();
            var sunSerialized = new SerializedObject(sunColor);
            sunSerialized.FindProperty("sunIntensity").floatValue = 3.2f;
            sunSerialized.FindProperty("dayColour").colorValue = new Color(1f, 0.78f, 0.5f);
            sunSerialized.FindProperty("eveningColour").colorValue = new Color(1f, 0.45f, 0.22f);
            sunSerialized.ApplyModifiedPropertiesWithoutUndo();
            if (light != null) RenderSettings.sun = light;
        }

        SetActiveIfFound("RunLengthController", false);
        SetActiveIfFound("Ground", false);
        SetActiveIfFound("pIanopuzzle", false);
        RemoveByName("spawnObjects", "Curtain", "Wall1", "wall2", "wall2 (1)");

        var old = GameObject.Find("RooftopCourse");
        if (old != null) Object.DestroyImmediate(old);
        foreach (var goal in Object.FindObjectsByType<ArtifactGoal>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (goal != null && goal.gameObject.name.StartsWith("BeatFragment"))
                Object.DestroyImmediate(goal.gameObject);
        }

        var playerCam = Object.FindAnyObjectByType<Camera>();
        if (playerCam != null)
        {
            playerCam.clearFlags = CameraClearFlags.Skybox;
            playerCam.backgroundColor = new Color(0.45f, 0.7f, 0.95f);
        }

        var root = new GameObject("RooftopCourse");
        var stream = root.AddComponent<RooftopRunStream>();
        stream.buildingPrefabs = new[]
        {
            LoadPrefab("Building01"),
            LoadPrefab("Building02"),
            LoadPrefab("Building03"),
            LoadPrefab("Building04")
        };
        BuildStreet(root.transform);
    }

    static void BuildStreet(Transform root)
    {
        string[] names = { "Building01", "Building02", "Building03", "Building04" };
        const float sideGap = 2.4f;
        const int rows = 1;

        float cursorZ = 0f;
        Bounds firstPath = default;
        Bounds lastPath = default;

        for (int i = 0; i < rows; i++)
        {
            var path = Place(LoadPrefab("Pathway"), root, new Vector3(0f, 0f, cursorZ), Vector3.one, "Path_" + (i + 1));
            SnapBottomTo(path, 0.02f);
            AlignMinZ(path, cursorZ);
            Bounds pathBounds = WorldBounds(path);

            var ground = Place(LoadPrefab("Ground"), root, pathBounds.center, Vector3.one, "StreetGround_" + (i + 1));
            FitGroundUnderStreet(ground, pathBounds, sideGap);

            LineBuildings(root, pathBounds, names, sideGap);
            TilePath(path);

            if (i == 0) firstPath = pathBounds;
            lastPath = WorldBounds(path);
            cursorZ = pathBounds.max.z;
        }

        var walk = new GameObject("StreetWalk");
        walk.layer = GroundLayer;
        walk.transform.SetParent(root, true);
        float length = lastPath.max.z - firstPath.min.z;
        walk.transform.position = new Vector3(0f, -0.4f, firstPath.min.z + length * 0.5f);
        var box = walk.AddComponent<BoxCollider>();
        box.size = new Vector3(firstPath.size.x + sideGap * 2f, 1.2f, length + 4f);
        box.isTrigger = false;

        var server = LoadPrefab("ServerMachine");
        var transmitter = Place(server, root, new Vector3(0f, 0f, lastPath.max.z - 2f), Vector3.one, "Transmitter");
        SnapBottomTo(transmitter, 0.05f);

        float surfaceY = firstPath.max.y;
        PlaceArtifact(0, new Vector3(-1.2f, surfaceY + 0.6f, firstPath.min.z + length * 0.45f));
        PlaceArtifact(1, new Vector3(1.1f, surfaceY + 0.6f, lastPath.center.z));

        float startY = surfaceY + 1.15f;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            player.transform.position = new Vector3(0f, startY, firstPath.min.z + 6f);

        var enemy = GameObject.Find("Enemy");
        if (enemy != null)
            enemy.transform.position = new Vector3(0f, startY, firstPath.min.z + 1.5f);
    }

    static void TilePath(GameObject path)
    {
        if (path == null) return;
        foreach (var renderer in path.GetComponentsInChildren<Renderer>())
        {
            var mats = renderer.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null) continue;
                mats[i].SetTextureScale("_BaseMap", new Vector2(4f, 48f));
                mats[i].SetTextureScale("_MainTex", new Vector2(4f, 48f));
                if (mats[i].HasProperty("_BaseColor"))
                    mats[i].SetColor("_BaseColor", new Color(1f, 0.42f, 0.05f, 1f));
            }
            renderer.materials = mats;
        }
    }

    static void LineBuildings(Transform root, Bounds pathBounds, string[] names, float sideGap)
    {
        float cursorZ = pathBounds.min.z;
        int index = 0;
        while (cursorZ < pathBounds.max.z - 2f && index < 14)
        {
            var left = Place(LoadPrefab(names[index % names.Length]), root, Vector3.zero, Vector3.one, "BlockL_" + (index + 1));
            left.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            SnapBottomTo(left, 0f);
            AlignMinZ(left, cursorZ);
            Bounds leftBounds = WorldBounds(left);
            left.transform.position += new Vector3((pathBounds.min.x - sideGap) - leftBounds.max.x, 0f, 0f);

            var right = Place(LoadPrefab(names[(index + 2) % names.Length]), root, Vector3.zero, Vector3.one, "BlockR_" + (index + 1));
            right.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            SnapBottomTo(right, 0f);
            AlignMinZ(right, cursorZ);
            Bounds rightBounds = WorldBounds(right);
            right.transform.position += new Vector3((pathBounds.max.x + sideGap) - rightBounds.min.x, 0f, 0f);

            cursorZ += Mathf.Max(leftBounds.size.z, rightBounds.size.z);
            index++;
        }
    }

    static void FitGroundUnderStreet(GameObject ground, Bounds pathBounds, float sideGap)
    {
        if (ground == null) return;
        Bounds bounds = WorldBounds(ground);
        float needX = pathBounds.size.x + sideGap * 2f + 28f;
        float needZ = pathBounds.size.z + 1f;
        float scaleX = bounds.size.x > 0.01f ? needX / bounds.size.x : 1f;
        float scaleZ = bounds.size.z > 0.01f ? needZ / bounds.size.z : 1f;
        ground.transform.localScale = new Vector3(scaleX, 1f, scaleZ);
        bounds = WorldBounds(ground);
        ground.transform.position += new Vector3(pathBounds.center.x - bounds.center.x, 0f, pathBounds.center.z - bounds.center.z);
        SnapTopTo(ground, 0f);
    }

    static void RemoveByName(params string[] names)
    {
        var doomed = new System.Collections.Generic.List<GameObject>();
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null) continue;
            foreach (string name in names)
            {
                if (t.name != name) continue;
                doomed.Add(t.gameObject);
                break;
            }
        }

        foreach (var go in doomed)
        {
            if (go != null) Object.DestroyImmediate(go);
        }
    }

    static void AlignMinZ(GameObject go, float worldZ)
    {
        Bounds bounds = WorldBounds(go);
        go.transform.position += new Vector3(0f, 0f, worldZ - bounds.min.z);
    }

    static void AddRoofCollider(GameObject building)
    {
        Bounds bounds = WorldBounds(building);
        // Thick slab so the capsule cannot slip through a paper-thin roof.
        const float thickness = 3f;
        CreateSolid(
            "RoofWalk",
            building.transform,
            new Vector3(bounds.center.x, bounds.max.y - thickness * 0.5f, bounds.center.z),
            new Vector3(bounds.size.x, thickness, bounds.size.z));

        // Side rails stop strafing off the roof. The front and back stay open for jumps.
        const float railHeight = 2.4f;
        const float railThickness = 0.45f;
        float railY = bounds.max.y + railHeight * 0.5f;
        CreateSolid(
            "RailL",
            building.transform,
            new Vector3(bounds.min.x + railThickness * 0.5f, railY, bounds.center.z),
            new Vector3(railThickness, railHeight, bounds.size.z));
        CreateSolid(
            "RailR",
            building.transform,
            new Vector3(bounds.max.x - railThickness * 0.5f, railY, bounds.center.z),
            new Vector3(railThickness, railHeight, bounds.size.z));
    }

    static void CreateSolid(string name, Transform parent, Vector3 worldCenter, Vector3 size)
    {
        var go = new GameObject(name);
        go.layer = GroundLayer;
        go.transform.SetParent(parent, true);
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        go.transform.position = worldCenter;
        var box = go.AddComponent<BoxCollider>();
        box.size = size;
        box.center = Vector3.zero;
        box.isTrigger = false;
    }

    static Bounds WorldBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    static void PlaceArtifact(int index, Vector3 position)
    {
        ArtifactGoal template = null;
        foreach (var goal in Object.FindObjectsByType<ArtifactGoal>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (goal.gameObject.name.StartsWith("BeatFragment")) continue;
            template = goal;
            break;
        }
        if (template == null)
        {
            Debug.LogWarning("[Rooftop] Artifact template not found.");
            return;
        }

        var copy = Object.Instantiate(template.gameObject);
        copy.name = "BeatFragment_" + (index + 1);
        copy.SetActive(true);
        copy.transform.position = position;
        copy.transform.rotation = template.transform.rotation;
    }

    static GameObject Place(GameObject prefab, Transform parent, Vector3 position, Vector3 scale, string name)
    {
        if (prefab == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.transform.localScale = scale;
        go.SetActive(true);
        return go;
    }

    static void AddWalkCollider(GameObject go)
    {
        if (go == null) return;
        var meshFilter = go.GetComponentInChildren<MeshFilter>();
        var host = meshFilter != null ? meshFilter.gameObject : go;
        host.layer = GroundLayer;
        var box = host.GetComponent<BoxCollider>();
        if (box == null) box = host.AddComponent<BoxCollider>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            box.center = meshFilter.sharedMesh.bounds.center;
            box.size = meshFilter.sharedMesh.bounds.size;
        }
        box.isTrigger = false;
    }

    static float MeasureTopY(string prefabName)
    {
        var prefab = LoadPrefab(prefabName);
        var size = MeshSize(prefab);
        return size.y;
    }

    static Vector3 RendererSize(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return Vector3.one;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds.size;
    }

    static Vector3 MeshSize(GameObject prefab)
    {
        if (prefab == null) return Vector3.one;
        var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
        if (filters.Length == 0) return Vector3.one * 4f;
        Bounds bounds = filters[0].sharedMesh != null ? filters[0].sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one);
        for (int i = 1; i < filters.Length; i++)
        {
            if (filters[i].sharedMesh == null) continue;
            bounds.Encapsulate(filters[i].sharedMesh.bounds);
        }
        return bounds.size;
    }

    static GameObject LoadPrefab(string name)
    {
        string path = $"{KitRoot}/Prefab/{name}.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) Debug.LogWarning("[Rooftop] Missing prefab " + path);
        return prefab;
    }

    static void SnapTopTo(GameObject go, float worldY)
    {
        if (go == null) return;
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        float delta = worldY - bounds.max.y;
        go.transform.position += new Vector3(0f, delta, 0f);
    }

    static void SnapBottomTo(GameObject go, float worldY)
    {
        if (go == null) return;
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        float delta = worldY - bounds.min.y;
        go.transform.position += new Vector3(0f, delta, 0f);
    }

    static void SetActiveIfFound(string name, bool active)
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name != name) continue;
            root.SetActive(active);
            return;
        }
    }
}
