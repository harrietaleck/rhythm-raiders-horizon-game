using UnityEngine;
using UnityEditor;

public class FixURPMaterials
{
    // ── Restore TMP shaders (run this once if TMP text looks wrong) ───────
    [MenuItem("Tools/Restore TextMeshPro Shaders")]
    public static void RestoreTMPShaders()
    {
        // TMP Distance Field shader is the standard one for most TMP materials
        Shader tmpDistField    = Shader.Find("TextMeshPro/Distance Field");
        Shader tmpDistFieldMob = Shader.Find("TextMeshPro/Mobile/Distance Field");
        Shader tmpBitmap       = Shader.Find("TextMeshPro/Bitmap");
        Shader tmpSprite       = Shader.Find("TextMeshPro/Sprite");

        if (tmpDistField == null) { Debug.LogError("[FixURP] TextMeshPro/Distance Field shader not found — is TMP installed?"); return; }

        // Find all materials in TextMesh Pro folders that were incorrectly converted
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/TextMesh Pro" });
        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            // If it was wrongly converted to URP Lit, restore to Distance Field
            if (mat.shader.name == "Universal Render Pipeline/Lit")
            {
                mat.shader = tmpDistField;
                EditorUtility.SetDirty(mat);
                count++;
                Debug.Log($"[FixURP] Restored TMP shader on: {path}");
            }
        }

        // Also fix font assets (.asset files with embedded materials)
        string[] fontGuids = AssetDatabase.FindAssets("t:Font", new[] { "Assets/TextMesh Pro" });
        // (font assets don't have a Material type directly, so the above material pass covers embedded mats)

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[FixURP] Restored {count} TMP material(s).");
    }


    // ── Full project fix ──────────────────────────────────────────────────
    [MenuItem("Tools/Fix URP Pink Materials")]
    public static void FixMaterials()
    {
        int fixed1 = FixAssetMaterials();
        int fixed2 = FixSceneMaterials();

        // Save the open scene so fixes survive restarts
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[FixURP] Done — upgraded {fixed1} asset material(s) and {fixed2} scene material(s) to URP Lit.");
    }

    // ── Characters only ──────────────────────────────────────────────────
    [MenuItem("Tools/Fix URP Pink Materials - Characters Only")]
    public static void FixCharacterMaterials()
    {
        FixCharacterURPMaterials.FixAll();
    }

    /// <summary>Public so FixCharacterURPMaterials can reuse upgrade logic.</summary>
    public static bool UpgradeMaterial(Material mat, Shader urpLit) => UpgradeMat(mat, urpLit);

    // ── Helpers ───────────────────────────────────────────────────────────

    /// Upgrade every .mat asset that isn't already URP/Unlit
    static int FixAssetMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) { Debug.LogError("[FixURP] URP Lit shader not found."); return 0; }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;
            if (UpgradeMat(mat, urpLit))
            {
                EditorUtility.SetDirty(mat);
                count++;
                Debug.Log($"[FixURP] Asset upgraded: {path}  shader was: {mat.shader.name}");
            }
        }
        return count;
    }

    /// Upgrade every SkinnedMeshRenderer material in the open scene
    static int FixSceneMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) { Debug.LogError("[FixURP] URP Lit shader not found."); return 0; }

        int count = 0;
        foreach (var smr in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
        {
            bool changed = false;
            Material[] mats = smr.sharedMaterials;
            foreach (var mat in mats)
            {
                if (mat == null) continue;
                if (UpgradeMat(mat, urpLit))
                {
                    EditorUtility.SetDirty(mat);
                    count++;
                    changed = true;
                    Debug.Log($"[FixURP] Scene fixed: '{mat.name}' on {smr.gameObject.name}  (was {mat.shader.name})");
                }
            }
            if (changed) EditorUtility.SetDirty(smr.gameObject);
        }
        return count;
    }

    /// Returns true if the material was changed
    static bool UpgradeMat(Material mat, Shader urpLit)
    {
        string sn = mat.shader.name;
        // Keep anything already correct URP, Unlit, or TextMeshPro
        // NOTE: do NOT skip Hidden/ — Hidden/InternalErrorShader is the pink error
        //       shader Unity uses when a shader is missing, and we need to fix those.
        if (sn.StartsWith("Universal Render Pipeline") ||
            sn.StartsWith("Unlit") ||
            sn.StartsWith("TextMeshPro") ||
            sn.StartsWith("TMPro"))
            return false;

        // Grab existing texture & colour before switching shader
        Texture mainTex  = mat.HasProperty("_MainTex")     ? mat.GetTexture("_MainTex")      : null;
        Texture baseMap  = mat.HasProperty("_BaseMap")      ? mat.GetTexture("_BaseMap")       : null;
        Color   col      = mat.HasProperty("_Color")        ? mat.GetColor("_Color")           : Color.white;
        Color   baseCol  = mat.HasProperty("_BaseColor")    ? mat.GetColor("_BaseColor")       : Color.white;
        Texture bump     = mat.HasProperty("_BumpMap")      ? mat.GetTexture("_BumpMap")       : null;
        Texture emission = mat.HasProperty("_EmissionMap")  ? mat.GetTexture("_EmissionMap")   : null;

        mat.shader = urpLit;

        // Map old properties → URP equivalents
        Texture tex = mainTex ?? baseMap;
        if (tex != null)      mat.SetTexture("_BaseMap",   tex);
        // Use the most opaque/brightest colour
        Color finalCol = (col == Color.white) ? baseCol : col;
        mat.SetColor("_BaseColor", finalCol);
        if (bump     != null) mat.SetTexture("_BumpMap",       bump);
        if (emission != null) mat.SetTexture("_EmissionMap",   emission);

        return true;
    }
}
