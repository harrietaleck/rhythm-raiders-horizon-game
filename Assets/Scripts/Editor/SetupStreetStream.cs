using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Widens the Level 1 street and makes it repeat endlessly via StreetRunStream.
/// Safe to run more than once.
/// </summary>
public static class SetupStreetStream
{
    const string ScenePath = "Assets/Scenes/MainGameL1Opera.unity";
    const float TargetPathWidth = 12f;

    const string SteelBarPath = "Assets/EmaceArt/PixelDrift Metro Free/Prefabs/GroundDecor/Props/PAEA01_GroundDecor_Gate_02d_PRE.prefab";

    [MenuItem("Tools/Assign Gap Steel Bars (Level 1)")]
    static void AssignSteelBars()
    {
        if (Application.isPlaying) return;
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var stream = Object.FindAnyObjectByType<StreetRunStream>();
        if (stream == null) { Debug.LogWarning("[StreetStream] No StreetRunStream in Level 1."); return; }
        stream.steelBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SteelBarPath);
        EditorUtility.SetDirty(stream);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[StreetStream] Gap steel bars assigned: {stream.steelBarPrefab != null}");
    }

    [MenuItem("Tools/Setup Level 1 Street Stream")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var root = GameObject.Find("RooftopCourse");
        if (root == null)
        {
            Debug.LogError("[StreetStream] RooftopCourse not found.");
            return;
        }

        var segTr = root.transform.Find("StreetSeg_0");
        var seg = segTr != null ? segTr.gameObject : new GameObject("StreetSeg_0");
        seg.transform.SetParent(root.transform, false);
        seg.transform.position = Vector3.zero;

        foreach (var child in ChildrenOf(root.transform))
        {
            if (child == seg.transform) continue;
            if (child.name == "Transmitter")
            {
                child.gameObject.SetActive(false);
                continue;
            }
            child.SetParent(seg.transform, true);
        }

        var path = FindChildStartingWith(seg.transform, "Path_");
        if (path == null)
        {
            Debug.LogError("[StreetStream] Path_ not found.");
            return;
        }

        Bounds pathBounds = WorldBounds(path.gameObject);
        float oldWidth = pathBounds.size.x;
        float factor = TargetPathWidth / Mathf.Max(0.01f, oldWidth);
        if (factor > 1.01f)
        {
            float centerX = pathBounds.center.x;
            var s = path.localScale;
            path.localScale = new Vector3(s.x * factor, s.y, s.z);
            Bounds widened = WorldBounds(path.gameObject);
            path.position += new Vector3(centerX - widened.center.x, 0f, 0f);
            ScaleTiling(path.gameObject, factor);

            float delta = widened.size.x - oldWidth;
            foreach (Transform child in seg.transform)
            {
                if (child.name.StartsWith("BlockL_")) child.position += Vector3.left * (delta * 0.5f);
                else if (child.name.StartsWith("BlockR_")) child.position += Vector3.right * (delta * 0.5f);
                else if (child.name.StartsWith("StreetGround_"))
                {
                    float gw = WorldBounds(child.gameObject).size.x;
                    var gs = child.localScale;
                    child.localScale = new Vector3(gs.x * (gw + delta) / Mathf.Max(0.01f, gw), gs.y, gs.z);
                }
                else if (child.name == "StreetWalk")
                {
                    var box = child.GetComponent<BoxCollider>();
                    if (box != null) box.size += new Vector3(delta, 0f, 0f);
                }
            }
            pathBounds = WorldBounds(path.gameObject);
        }

        var stream = root.GetComponent<StreetRunStream>() ?? root.AddComponent<StreetRunStream>();
        stream.segment = seg;
        stream.segmentStartZ = pathBounds.min.z;
        stream.segmentLength = pathBounds.size.z;
        stream.advantages = FindAll("Health - Pickup", "Jump Boost  - Pickup", "Speed boost - Pickup", "Sneak - Pickup");
        stream.hazards = FindAll("Random Obstacle - health Decrease", "Random Obstacle - health Decrease", "Slow down  - Obstacle");
        var fragments = FindAll("BeatFragment_1", "BeatFragment_2");
        stream.fragmentTemplate = fragments.Length > 0 ? fragments[0] : null;
        foreach (var f in fragments) f.SetActive(false);
        stream.fragmentDistances = new[] { 250f, 500f };
        stream.steelBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SteelBarPath);
        EditorUtility.SetDirty(stream);

        var director = root.GetComponent<RunDirector>() ?? root.AddComponent<RunDirector>();
        director.stream = stream;
        director.font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/UI/Fonts/Zekton SDF.asset");
        director.sun = GameObject.Find("Directional Light")?.GetComponent<Light>();
        EditorUtility.SetDirty(director);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[StreetStream] Path width {oldWidth:F1} -> {pathBounds.size.x:F1}, segment length {pathBounds.size.z:F1}.");
    }

    static System.Collections.Generic.List<Transform> ChildrenOf(Transform t)
    {
        var list = new System.Collections.Generic.List<Transform>();
        foreach (Transform c in t) list.Add(c);
        return list;
    }

    static GameObject[] FindAll(params string[] names)
    {
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var result = new System.Collections.Generic.List<GameObject>();
        foreach (var n in names)
        {
            foreach (var t in all)
            {
                if (t.name != n) continue;
                result.Add(t.gameObject);
                break;
            }
        }
        if (result.Count < names.Length)
            Debug.LogWarning($"[StreetStream] Found {result.Count}/{names.Length} templates for {string.Join(", ", names)}");
        return result.ToArray();
    }

    static Transform FindChildStartingWith(Transform parent, string prefix)
    {
        foreach (Transform c in parent)
            if (c.name.StartsWith(prefix)) return c;
        return null;
    }

    static void ScaleTiling(GameObject go, float factor)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            foreach (var m in r.sharedMaterials)
            {
                if (m == null || AssetDatabase.Contains(m)) continue;
                foreach (var prop in new[] { "_BaseMap", "_MainTex" })
                {
                    if (!m.HasProperty(prop)) continue;
                    var sc = m.GetTextureScale(prop);
                    m.SetTextureScale(prop, new Vector2(sc.x * factor, sc.y));
                }
            }
        }
    }

    static Bounds WorldBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }
}
