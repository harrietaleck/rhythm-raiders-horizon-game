using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools → Setup FX Library (Fire, Plasma, Text Animator): registers the Stylized Fire pickup
/// flames, the Plasma Shader shield sphere and the Procedural Text Animator effect database
/// in Resources/FxLibrary so the game can use them at runtime.
/// </summary>
public static class SetupNewFxPacks
{
    const string LibraryPath = "Assets/Resources/FxLibrary.asset";
    const string Fire = "Assets/Vefects/Stylized Fire VFX URP/VFX/";

    [MenuItem("Tools/Setup FX Library (Fire, Plasma, Text Animator)")]
    public static void Setup()
    {
        var lib = AssetDatabase.LoadAssetAtPath<FxLibrary>(LibraryPath);
        if (lib == null) { Debug.LogError("[FxLibrary] missing " + LibraryPath); return; }

        lib.flameSpeed = Load<GameObject>(Fire + "Flames/Particles/VFX_Stylized_Fire_01.prefab");
        lib.flameSlow = Load<GameObject>(Fire + "Water/Particles/VFX_Stylized_Fire_01_Water.prefab");
        lib.flameJump = Load<GameObject>(Fire + "Electric/Particles/VFX_Stylized_Fire_01_Electric.prefab");
        lib.flameSneak = Load<GameObject>(Fire + "Mystic/Particles/VFX_Stylized_Fire_01_Mystic.prefab");
        lib.flameHealth = Load<GameObject>(Fire + "Poison/Particles/VFX_Stylized_Fire_01_Poison.prefab");
        lib.pickupFlameScale = 1.6f;
        lib.plasmaShield = Load<GameObject>("Assets/Houidisoft technology/Plasma Shader/Prefabs/Plasma Sphere (1).prefab");
        lib.textEffects = Load<ProceduralUIEffects.Lite.EffectDatabase>("Assets/Procedural Text Animator - LITE/Effect Assets/My Project Effects.asset");

        EditorUtility.SetDirty(lib);
        AssetDatabase.SaveAssets();
        Debug.Log($"[FxLibrary] flames speed={lib.flameSpeed != null} slow={lib.flameSlow != null} jump={lib.flameJump != null} " +
                  $"sneak={lib.flameSneak != null} health={lib.flameHealth != null} plasma={lib.plasmaShield != null} text={lib.textEffects != null}");
    }

    static T Load<T>(string path) where T : Object
    {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a == null) Debug.LogWarning("[FxLibrary] not found: " + path);
        return a;
    }
}
