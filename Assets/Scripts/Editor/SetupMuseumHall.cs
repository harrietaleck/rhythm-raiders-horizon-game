using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools → Setup Level 2 Museum Hall
/// Adds the streamed night-museum interior and the MuseumRun course controller to Level 2,
/// and assigns the ceiling texture and instrument models from the project's asset packs.
/// </summary>
public static class SetupMuseumHall
{
    const string ScenePath = "Assets/Scenes/Level2Museum.unity";
    const string CeilingPath = "Assets/Materials/Textures/ceiling.jpg";

    static readonly string[] InstrumentPaths =
    {
        "Assets/prefab/AfricanDrum- obstacle.fbx",
        "Assets/prefab/Head sets.fbx",
        "Assets/prefab/musicnote-artifact.fbx",
    };

    [MenuItem("Tools/Setup Level 2 Museum Hall")]
    static void Run()
    {
        if (Application.isPlaying) { Debug.LogWarning("[MuseumHall] Stop Play mode first."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var stream = Object.FindAnyObjectByType<MuseumHallStream>();
        if (stream == null)
            stream = new GameObject("MuseumHall").AddComponent<MuseumHallStream>();

        stream.ceilingTexture = AssetDatabase.LoadAssetAtPath<Texture>(CeilingPath);
        var models = new System.Collections.Generic.List<GameObject>();
        foreach (var path in InstrumentPaths)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null) models.Add(go);
            else Debug.LogWarning("[MuseumHall] Missing instrument model: " + path);
        }
        stream.instrumentModels = models.ToArray();
        EditorUtility.SetDirty(stream);

        var run = Object.FindAnyObjectByType<MuseumRun>();
        if (run == null)
            run = new GameObject("MuseumRun").AddComponent<MuseumRun>();
        run.drumModel = AssetDatabase.LoadAssetAtPath<GameObject>(InstrumentPaths[0]);
        EditorUtility.SetDirty(run);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[MuseumHall] Level 2 museum hall set up with {models.Count} instrument models.");
    }
}
