using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools → Setup FX Library: makes the imported Fog Particles and Rolling Balls Sci-fi packs
/// render in URP and registers them in Resources/FxLibrary for runtime use.
///  - Fog: a URP Particles/Unlit material from the pack's smoke sprite sheet.
///  - Rolling balls: the pack's Standard materials are switched to URP Lit (textures kept).
/// </summary>
public static class SetupFxLibrary
{
    const string FogPrefab = "Assets/Fog Particles/Prefabs/Whitish Fog.prefab";
    const string FogTexture = "Assets/Fog Particles/Texture/Smoke Sprite Sheet.png";
    const string FogUrpMat = "Assets/Fog Particles/Material/Fog-Material URP.mat";
    const string BallsRoot = "Assets/Rolling_Balls-Sci-fi_Pack/Rolling_Balls-Sci-fi_Pack_SRP_Build";
    const string LibraryPath = "Assets/Resources/FxLibrary.asset";

    [MenuItem("Tools/Setup FX Library (Fog, Disco Balls)")]
    public static void Setup()
    {
        var lib = AssetDatabase.LoadAssetAtPath<FxLibrary>(LibraryPath);
        if (lib == null)
        {
            lib = ScriptableObject.CreateInstance<FxLibrary>();
            AssetDatabase.CreateAsset(lib, LibraryPath);
        }

        // Fog.
        lib.fogPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FogPrefab);
        var fogMat = AssetDatabase.LoadAssetAtPath<Material>(FogUrpMat);
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (fogMat == null && shader != null)
        {
            fogMat = new Material(shader);
            AssetDatabase.CreateAsset(fogMat, FogUrpMat);
        }
        if (fogMat != null)
        {
            if (shader != null) fogMat.shader = shader;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(FogTexture);
            fogMat.SetTexture("_BaseMap", tex);
            fogMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.75f));
            fogMat.SetFloat("_Surface", 1f);
            fogMat.SetFloat("_Blend", 0f);
            fogMat.SetFloat("_ZWrite", 0f);
            fogMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            fogMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            fogMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            fogMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(fogMat);
        }
        lib.fogMaterial = fogMat;

        // Rolling balls → URP Lit.
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        int converted = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { BallsRoot + "/Material" }))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (m == null || lit == null || m.shader == lit) continue;
            var baseMap = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
            if (baseMap == null && m.HasProperty("_MainTex")) baseMap = m.GetTexture("_MainTex");
            var bump = m.HasProperty("_BumpMap") ? m.GetTexture("_BumpMap") : null;
            var emis = m.HasProperty("_EmissionMap") ? m.GetTexture("_EmissionMap") : null;
            var emisColor = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;
            m.shader = lit;
            m.SetTexture("_BaseMap", baseMap);
            m.SetColor("_BaseColor", Color.white);
            if (bump != null) { m.SetTexture("_BumpMap", bump); m.EnableKeyword("_NORMALMAP"); }
            m.SetFloat("_Metallic", 0.85f);
            m.SetFloat("_Smoothness", 0.9f);
            if (emis != null)
            {
                m.SetTexture("_EmissionMap", emis);
                m.SetColor("_EmissionColor", emisColor.maxColorComponent > 0.01f ? emisColor : new Color(0.6f, 0.6f, 0.6f));
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(m);
            converted++;
        }

        var balls = new List<GameObject>();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { BallsRoot + "/Prefab" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith("_1.prefab") && !path.EndsWith("_3.prefab")) continue;   // a varied handful
            var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (p != null) balls.Add(p);
        }
        lib.discoBalls = balls.ToArray();

        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        Debug.Log($"[FxLibrary] fog={(lib.fogPrefab != null)} fogMat={(fogMat != null)} balls={balls.Count} materialsConverted={converted}");
    }
}
