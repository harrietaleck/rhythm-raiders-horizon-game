using UnityEditor;
using UnityEngine;

/// <summary>
/// Copies undergroundoor.jpg into Assets/Resources for Level 3 door cutscene.
/// </summary>
[InitializeOnLoad]
public static class SetupUndergroundDoorResource
{
    const string Src = "Assets/Materials/Textures/undergroundoor.jpg";
    const string Dst = "Assets/Resources/undergroundoor.jpg";

    static SetupUndergroundDoorResource()
    {
        EditorApplication.delayCall += CopyIfMissing;
    }

    [MenuItem("Tools/Setup Underground Door Resource")]
    public static void CopyIfMissing()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        if (AssetDatabase.LoadAssetAtPath<Texture2D>(Src) == null)
        {
            Debug.LogWarning("[SetupUndergroundDoorResource] Source not found: " + Src);
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<Texture2D>(Dst) != null)
            return;

        if (AssetDatabase.CopyAsset(Src, Dst))
        {
            AssetDatabase.Refresh();
            Debug.Log("[SetupUndergroundDoorResource] Copied undergroundoor to Assets/Resources/");
        }
    }
}
