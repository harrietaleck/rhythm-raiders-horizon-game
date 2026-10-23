using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools → Setup Museum Art And Backpack: converts the Greek Statue, HQ Lion Statue and
/// LowPoly Fantasy Bags materials to URP Lit (they ship with built-in shaders and render pink),
/// and registers the statues, oil paintings, runner backpack and laser / drum obstacle sounds
/// in Resources/FxLibrary.
/// </summary>
[InitializeOnLoad]
public static class SetupMuseumArt
{
    const string LibraryPath = "Assets/Resources/FxLibrary.asset";
    static readonly string[] ConvertRoots = { "Assets/Greek Statue", "Assets/NOT_Lonely/LionStatue", "Assets/LowPoly Fantasy Bags" };
    public const string BackpackPath = "Assets/LowPoly Fantasy Bags/Prefab/Bag.1.prefab";

    static SetupMuseumArt()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var lib = AssetDatabase.LoadAssetAtPath<FxLibrary>(LibraryPath);
            if (lib != null && lib.statues != null && lib.statues.Length > 0 && lib.laserHitSound != null) return;
            Setup();
        };
    }

    [MenuItem("Tools/Setup Museum Art And Backpack")]
    public static void Setup()
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        int converted = 0;
        foreach (var root in ConvertRoots)
        {
            if (!AssetDatabase.IsValidFolder(root)) continue;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { root }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (m == null || lit == null) continue;
                if (m.shader.name.StartsWith("Universal Render Pipeline") || m.shader.name.StartsWith("Shader Graphs")) continue;
                Texture Tex(string n) => m.HasProperty(n) ? m.GetTexture(n) : null;
                var baseMap = Tex("_MainTex") ?? Tex("_BaseMap");
                var color = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
                var bump = Tex("_BumpMap");
                var occ = Tex("_OcclusionMap");
                var metal = Tex("_MetallicGlossMap");
                float gloss = m.HasProperty("_Glossiness") ? m.GetFloat("_Glossiness") : 0.3f;
                float metallic = m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0f;
                m.shader = lit;
                m.SetTexture("_BaseMap", baseMap);
                m.SetColor("_BaseColor", color);
                if (bump != null) { m.SetTexture("_BumpMap", bump); m.EnableKeyword("_NORMALMAP"); }
                if (occ != null) { m.SetTexture("_OcclusionMap", occ); m.EnableKeyword("_OCCLUSIONMAP"); }
                if (metal != null) { m.SetTexture("_MetallicGlossMap", metal); m.EnableKeyword("_METALLICSPECGLOSSMAP"); }
                m.SetFloat("_Metallic", metallic);
                m.SetFloat("_Smoothness", gloss);
                EditorUtility.SetDirty(m);
                converted++;
            }
        }

        var lib = AssetDatabase.LoadAssetAtPath<FxLibrary>(LibraryPath);
        if (lib == null) { lib = ScriptableObject.CreateInstance<FxLibrary>(); AssetDatabase.CreateAsset(lib, LibraryPath); }

        var statues = new List<GameObject>
        {
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Greek Statue/GreekStatue.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/NOT_Lonely/LionStatue/Prefab/LionStatue.prefab"),
        };
        lib.statues = statues.Where(s => s != null).ToArray();
        lib.paintings = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/PolyKebap/OilPaintings/Prefabs" })
            .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(g => g != null).OrderBy(g => g.name).ToArray();
        lib.backpack = AssetDatabase.LoadAssetAtPath<GameObject>(BackpackPath);
        lib.laserHitSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audios/laser-shot-obstacle sound.mp3");
        lib.drumHitSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audios/bass-drum-obstacle sound.mp3");
        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        Debug.Log($"[MuseumArt] converted {converted} materials, statues={lib.statues.Length} paintings={lib.paintings.Length} backpack={(lib.backpack != null)} laserSfx={(lib.laserHitSound != null)} drumSfx={(lib.drumHitSound != null)}");
    }
}
