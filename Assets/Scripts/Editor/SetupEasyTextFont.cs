using System.IO;
using Mkey;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies the MK Easy Text Coiny font to game UI.
/// Legacy UI Text also gets EasyTextDeformer. TMP labels use a Coiny font asset.
/// </summary>
public static class SetupEasyTextFont
{
    const string SourceFont = "Assets/SoftEffects/Fonts/Coiny/Coiny-Regular.ttf";
    const string TmpFontPath = "Assets/UI/Fonts/Coiny SDF.asset";

    static readonly string[] Scenes =
    {
        "Assets/Scenes/StartScene.unity",
        "Assets/Scenes/DeathScene.unity",
        "Assets/Scenes/VictoryScene.unity",
        "Assets/Scenes/SplashScene.unity",
        "Assets/Scenes/MainGameL1Opera.unity"
    };

    [MenuItem("Tools/Apply Easy Text Font")]
    public static void Run()
    {
        var font = AssetDatabase.LoadAssetAtPath<Font>(SourceFont);
        if (font == null)
        {
            Debug.LogError("[EasyText] Coiny font missing at " + SourceFont);
            return;
        }

        var tmpFont = EnsureTmpFont(font);
        int labels = 0;
        foreach (string scenePath in Scenes)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null) continue;
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            foreach (var text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                text.font = font;
                if (text.GetComponent<EasyTextDeformer>() == null)
                    text.gameObject.AddComponent<EasyTextDeformer>();
                labels++;
            }

            if (tmpFont != null)
            {
                foreach (var tmp in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    tmp.font = tmpFont;
                    labels++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[EasyText] Coiny applied to {labels} labels.");
    }

    static TMP_FontAsset EnsureTmpFont(Font font)
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontPath);
        if (existing != null) return existing;

        string dir = Path.GetDirectoryName(TmpFontPath);
        if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
        {
            if (!AssetDatabase.IsValidFolder("Assets/UI"))
                AssetDatabase.CreateFolder("Assets", "UI");
            AssetDatabase.CreateFolder("Assets/UI", "Fonts");
        }

        var created = TMP_FontAsset.CreateFontAsset(font);
        if (created == null)
        {
            Debug.LogError("[EasyText] Could not create Coiny TMP font.");
            return null;
        }

        created.name = "Coiny SDF";
        AssetDatabase.CreateAsset(created, TmpFontPath);
        if (created.atlasTextures != null)
        {
            foreach (var atlas in created.atlasTextures)
            {
                if (atlas != null) AssetDatabase.AddObjectToAsset(atlas, created);
            }
        }
        if (created.material != null)
            AssetDatabase.AddObjectToAsset(created.material, created);
        EditorUtility.SetDirty(created);
        AssetDatabase.SaveAssets();
        return created;
    }
}
