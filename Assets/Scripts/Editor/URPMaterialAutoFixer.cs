using UnityEngine;
using UnityEditor;

/// <summary>
/// Permanent pink-texture fix.
/// Runs automatically every time Unity imports or re-imports a model (.dae, .fbx, .obj).
/// Ensures all embedded materials are always on URP/Lit and textures are mapped correctly.
/// No manual intervention needed after this is in the project.
/// </summary>
public class URPMaterialAutoFixer : AssetPostprocessor
{
    // Shaders to skip — already correct or not 3D materials.
    // NOTE: do NOT skip "Hidden/" here — Hidden/InternalErrorShader is the pink
    // error shader Unity assigns when a shader is missing, and we must fix those.
    private static readonly string[] s_Skip = {
        "Universal Render Pipeline", "Unlit", "TextMeshPro",
        "TMPro", "Sprites/", "UI/"
    };

    /// <summary>Called for each material extracted from an imported model.</summary>
    void OnPostprocessMaterial(Material mat)
    {
        if (mat == null) return;
        if (ShouldSkip(mat.shader.name)) return;

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) return;

        // Cache textures & colours before switching shader
        Texture mainTex  = mat.HasProperty("_MainTex")    ? mat.GetTexture("_MainTex")    : null;
        Texture baseMap  = mat.HasProperty("_BaseMap")     ? mat.GetTexture("_BaseMap")    : null;
        Color   col      = mat.HasProperty("_Color")       ? mat.GetColor("_Color")        : Color.white;
        Color   baseCol  = mat.HasProperty("_BaseColor")   ? mat.GetColor("_BaseColor")    : Color.white;
        Texture bump     = mat.HasProperty("_BumpMap")     ? mat.GetTexture("_BumpMap")    : null;
        Texture emission = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap"): null;

        mat.shader = urpLit;

        Texture tex = mainTex ?? baseMap;
        if (tex != null)
            mat.SetTexture("_BaseMap", tex);

        Color finalCol = (col != Color.white) ? col : baseCol;
        mat.SetColor("_BaseColor", finalCol);

        if (bump != null && !IsCharacterModelAsset(assetPath))
        {
            mat.SetTexture("_BumpMap", bump);
            mat.EnableKeyword("_NORMALMAP");
        }
        else
        {
            mat.DisableKeyword("_NORMALMAP");
            if (mat.HasProperty("_BumpMap"))
                mat.SetTexture("_BumpMap", null);
        }

        if (emission != null) mat.SetTexture("_EmissionMap", emission);

        if (tex == null)
            TryAssignCharacterTexturesFromFolder(mat, assetPath);

        Debug.Log($"[URPAutoFix] Fixed material '{mat.name}' in {assetPath}");
    }

    private static bool ShouldSkip(string shaderName)
    {
        foreach (var s in s_Skip)
            if (shaderName.StartsWith(s)) return true;
        return false;
    }

    static bool IsCharacterModelAsset(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath)) return false;
        return assetPath.Contains("PlayerAnimations") || assetPath.Contains("SecurityGuardAnimations");
    }

    static void TryAssignCharacterTexturesFromFolder(Material mat, string modelAssetPath)
    {
        string dir = System.IO.Path.GetDirectoryName(modelAssetPath)?.Replace('\\', '/');
        if (string.IsNullOrEmpty(dir)) return;

        string texDir = dir + "/textures";
        if (!System.IO.Directory.Exists(texDir)) return;

        string body = mat.name.Replace(" ", "");
        string[] diffuseGuesses =
        {
            $"{texDir}/{body}_Diffuse.png",
            $"{texDir}/Ch03_1001_Diffuse.png",
            $"{texDir}/Ch15_1001_Diffuse.png",
            $"{texDir}/Ch15_1002_Diffuse.png",
        };

        foreach (string path in diffuseGuesses)
        {
            var diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (diffuse == null) continue;
            mat.SetTexture("_BaseMap", diffuse);
            mat.SetColor("_BaseColor", Color.white);
            break;
        }
    }
}
