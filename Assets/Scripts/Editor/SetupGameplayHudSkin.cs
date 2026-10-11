using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tools → Apply Sci-Fi Skin To Gameplay HUD
/// Puts the gameplay HUD of all three levels on the Sci-Fi GUI skin: window frames
/// behind the stat panels, Zekton text, and skin sprites for the Level 1 run HUD.
/// </summary>
public static class SetupGameplayHudSkin
{
    static readonly string[] Scenes =
    {
        "Assets/Scenes/MainGameL1Opera.unity",
        "Assets/Scenes/Level2Museum.unity",
        "Assets/Scenes/Level3UndergroundDance.unity",
    };

    [MenuItem("Tools/Apply Sci-Fi Skin To Gameplay HUD")]
    static void Run()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("[HUD Skin] Stop Play mode first.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureSkinAsset();

        string original = EditorSceneManager.GetActiveScene().path;
        foreach (var path in Scenes)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int texts = SkinPanels();

            var director = Object.FindAnyObjectByType<RunDirector>();
            if (director != null)
            {
                director.panelSprite = SciFiUiKit.Window;
                director.bannerSprite = SciFiUiKit.Button;
                director.font = SciFiUiKit.TmpFont;
                EditorUtility.SetDirty(director);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[HUD Skin] {scene.name}: {texts} HUD labels restyled{(director != null ? ", run HUD sprites set" : "")}.");
        }

        if (!string.IsNullOrEmpty(original))
            EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
    }

    const string SkinAssetPath = "Assets/Resources/HudSkin.asset";

    static void EnsureSkinAsset()
    {
        var skin = AssetDatabase.LoadAssetAtPath<HudSkin>(SkinAssetPath);
        if (skin == null)
        {
            skin = ScriptableObject.CreateInstance<HudSkin>();
            AssetDatabase.CreateAsset(skin, SkinAssetPath);
        }
        skin.window = SciFiUiKit.Window;
        skin.banner = SciFiUiKit.Button;
        skin.font = SciFiUiKit.TmpFont;
        EditorUtility.SetDirty(skin);
        AssetDatabase.SaveAssets();
    }

    static int SkinPanels()
    {
        int count = 0;
        foreach (var name in new[] { "HUD_LeftPanel", "HUD_RightPanel" })
        {
            var panel = GameObject.Find(name);
            if (panel == null) continue;

            // The panel's own image stays invisible; a padded frame child carries the skin
            // so the icons and text sit inside the window border.
            var own = panel.GetComponent<Image>();
            if (own != null)
            {
                own.enabled = false;
                EditorUtility.SetDirty(own);
            }

            var frameT = panel.transform.Find("SkinFrame");
            var frame = frameT != null ? frameT.gameObject : new GameObject("SkinFrame", typeof(RectTransform), typeof(Image));
            frame.transform.SetParent(panel.transform, false);
            frame.transform.SetAsFirstSibling();
            var rt = frame.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            bool right = panel.GetComponent<RectTransform>().anchorMin.x > 0.5f;
            rt.offsetMin = new Vector2(-32f, -26f);
            rt.offsetMax = new Vector2(right ? 32f : 56f, 24f);
            var img = frame.GetComponent<Image>();
            img.sprite = SciFiUiKit.Window;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 3f;
            img.color = new Color(1f, 1f, 1f, 0.95f);
            img.raycastTarget = false;
            EditorUtility.SetDirty(frame);

            // Move the panel in so the padded frame stays on screen.
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchoredPosition = new Vector2(right ? -48f : 48f, -40f);
            EditorUtility.SetDirty(panelRt);

            foreach (var tmp in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                SciFiUiKit.StyleText(tmp, 26f, tmp.color);
                tmp.characterSpacing = 3f;
                EditorUtility.SetDirty(tmp);
                count++;
            }
        }
        return count;
    }
}
