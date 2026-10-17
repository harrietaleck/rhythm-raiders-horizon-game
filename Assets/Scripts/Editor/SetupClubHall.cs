using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools → Setup Level 3 Club
/// Adds the streamed underground club (ClubHallStream + ClubBeat) and the ClubRun course
/// controller to Level 3. The old random scatter spawner is suppressed while ClubRun is present.
/// </summary>
public static class SetupClubHall
{
    const string ScenePath = "Assets/Scenes/Level3UndergroundDance.unity";

    [MenuItem("Tools/Setup Level 3 Club")]
    static void Run()
    {
        if (Application.isPlaying) { Debug.LogWarning("[Club] Stop Play mode first."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var stream = Object.FindAnyObjectByType<ClubHallStream>();
        if (stream == null)
            stream = new GameObject("ClubHall").AddComponent<ClubHallStream>();
        EditorUtility.SetDirty(stream);

        var run = Object.FindAnyObjectByType<ClubRun>();
        if (run == null)
            run = new GameObject("ClubRun").AddComponent<ClubRun>();
        EditorUtility.SetDirty(run);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Club] Level 3 club hall and run controller set up.");
    }
}
