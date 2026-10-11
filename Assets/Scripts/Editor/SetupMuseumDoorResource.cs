using UnityEditor;
using UnityEngine;

/// <summary>
/// Copies museumdoor.jpg into Assets/Resources for Level 2 door cutscene.
/// </summary>
[InitializeOnLoad]
public static class SetupMuseumDoorResource
{
    const string Src = "Assets/Materials/Textures/museumdoor.jpg";
    const string Dst = "Assets/Resources/museumdoor.jpg";

    static SetupMuseumDoorResource()
    {
        EditorApplication.delayCall += CopyIfMissing;
    }

    [MenuItem("Tools/Setup Museum Door Resource")]
    public static void CopyIfMissing()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        if (AssetDatabase.LoadAssetAtPath<Texture2D>(Src) == null)
        {
            Debug.LogWarning("[SetupMuseumDoorResource] Source not found: " + Src);
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<Texture2D>(Dst) != null)
            return;

        if (AssetDatabase.CopyAsset(Src, Dst))
        {
            AssetDatabase.Refresh();
            Debug.Log("[SetupMuseumDoorResource] Copied museumdoor to Assets/Resources/");
        }
    }
}
