using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds the four game scenes to Build Settings in the correct order.
/// Run via  Tools → Setup Build Scenes.
/// Build order:
///   0 - StartScene    (intro narration + PLAY button)
///   1 - MainGameL1    (main gameplay)
///   2 - DeathScene    (game over narration + RETRY)
///   3 - VictoryScene  (victory narration + PLAY AGAIN)
/// </summary>
public class SetupBuildScenes
{
    private static readonly string[] GameScenes = new string[]
    {
        "Assets/Scenes/StartScene.unity",
        "Assets/Scenes/MainGameL1.unity",
        "Assets/Scenes/DeathScene.unity",
        "Assets/Scenes/VictoryScene.unity",
    };

    [MenuItem("Tools/Setup Build Scenes")]
    public static void Setup()
    {
        var scenes = new EditorBuildSettingsScene[GameScenes.Length];
        for (int i = 0; i < GameScenes.Length; i++)
            scenes[i] = new EditorBuildSettingsScene(GameScenes[i], true);

        EditorBuildSettings.scenes = scenes;

        Debug.Log("[SetupBuild] Build Settings updated:\n" +
                  "  0 → StartScene\n" +
                  "  1 → MainGameL1\n" +
                  "  2 → DeathScene\n" +
                  "  3 → VictoryScene");
    }
}
