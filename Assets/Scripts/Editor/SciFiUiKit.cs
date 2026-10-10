using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared styling built on the Sci-Fi GUI Skin (Assets/Sci-Fi UI/_SciFi_GUISkin_).
/// Sprites are imported as 9-sliced so windows and buttons stretch cleanly.
/// </summary>
public static class SciFiUiKit
{
    const string SkinRoot      = "Assets/Sci-Fi UI/_SciFi_GUISkin_/Skin_Assets/";
    // Dark-filled copies of the skin's window/button sprites (originals are ~30% opaque glass).
    const string DarkRoot      = "Assets/UI/SciFi/";
    const string WindowPath    = DarkRoot + "window_dark.png";
    const string ButtonPath    = DarkRoot + "button_dark.png";
    const string ButtonHotPath = DarkRoot + "button_hot.png";
    const string ButtonPushPath= DarkRoot + "button_pressed.png";
    const string ScreenBgPath  = DarkRoot + "screen_background.jpg";
    const string BarBackPath   = SkinRoot + "bars/progress_bar_background.png";
    const string BarFillPath   = SkinRoot + "bars/bar_blue.png";
    const string SourceFont    = SkinRoot + "font/zekton free.ttf";
    const string TmpFontPath   = "Assets/UI/Fonts/Zekton SDF.asset";

    public static readonly Color WindowTint  = Color.white;
    public static readonly Color Cyan        = new Color(0.35f, 0.85f, 1f, 1f);
    public static readonly Color TextMain    = new Color(0.94f, 0.97f, 1f, 1f);
    public static readonly Color TextDim     = new Color(0.62f, 0.72f, 0.82f, 1f);
    public static readonly Color Dim         = new Color(0.02f, 0.03f, 0.07f, 0.8f);

    public static Sprite Window     => LoadSliced(WindowPath,     new Vector4(150, 150, 150, 150));
    public static Sprite Button     => LoadSliced(ButtonPath,     new Vector4(250, 130, 250, 130));
    public static Sprite ButtonHot  => LoadSliced(ButtonHotPath,  new Vector4(250, 130, 250, 130));
    public static Sprite ButtonPush => LoadSliced(ButtonPushPath, new Vector4(250, 130, 250, 130));
    public static Sprite ScreenBackground => LoadSliced(ScreenBgPath, Vector4.zero);
    public static Sprite BarBack    => LoadSliced(BarBackPath,    Vector4.zero);
    public static Sprite BarFill    => LoadSliced(BarFillPath,    Vector4.zero);

    public static void StyleBackground(Image img)
    {
        img.sprite = ScreenBackground;
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
        img.color = Color.white;
        img.raycastTarget = true;
    }

    public static void StyleWindow(Image img)
    {
        img.sprite = Window;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 2.5f;
        img.color = WindowTint;
        img.raycastTarget = true;
    }

    public static void StyleButton(Button btn, TMP_Text label, float fontSize = 30f)
    {
        var img = btn.GetComponent<Image>() ?? btn.gameObject.AddComponent<Image>();
        img.sprite = Button;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 4f;
        img.color = Color.white;
        img.raycastTarget = true;
        btn.targetGraphic = img;

        btn.transition = Selectable.Transition.SpriteSwap;
        btn.spriteState = new SpriteState
        {
            highlightedSprite = ButtonHot,
            selectedSprite    = ButtonHot,
            pressedSprite     = ButtonPush,
            disabledSprite    = Button
        };

        if (label != null)
        {
            StyleText(label, fontSize, TextMain);
            label.characterSpacing = 6;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
        }
        EditorUtility.SetDirty(btn);
    }

    public static void StyleText(TMP_Text tmp, float size, Color color)
    {
        var font = TmpFont;
        if (font != null)
        {
            tmp.font = font;
            if (font.material != null) tmp.fontSharedMaterial = font.material;
        }
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Normal;
        tmp.color = color;
    }

    public static void StyleSlider(Slider slider)
    {
        var bg = slider.transform.Find("Background")?.GetComponent<Image>();
        if (bg != null)
        {
            bg.sprite = BarBack;
            bg.type = Image.Type.Simple;
            bg.color = Color.white;
        }

        var fill = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
        if (fill != null)
        {
            fill.sprite = BarFill;
            fill.type = Image.Type.Simple;
            fill.color = Color.white;
        }

        if (slider.handleRect != null)
        {
            var handle = slider.handleRect.GetComponent<Image>();
            if (handle != null) handle.color = Cyan;
        }
    }

    public static TMP_FontAsset TmpFont
    {
        get
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontPath);
            if (existing != null) return existing;

            var ttf = AssetDatabase.LoadAssetAtPath<UnityEngine.Font>(SourceFont);
            if (ttf == null) return null;

            string dir = Path.GetDirectoryName(TmpFontPath);
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/UI", "Fonts");

            var created = TMP_FontAsset.CreateFontAsset(ttf);
            if (created == null) return null;

            created.name = "Zekton SDF";
            AssetDatabase.CreateAsset(created, TmpFontPath);
            if (created.atlasTextures != null)
                foreach (var atlas in created.atlasTextures)
                    if (atlas != null) AssetDatabase.AddObjectToAsset(atlas, created);
            if (created.material != null)
                AssetDatabase.AddObjectToAsset(created.material, created);
            EditorUtility.SetDirty(created);
            AssetDatabase.SaveAssets();
            return created;
        }
    }

    static Sprite LoadSliced(string path, Vector4 border)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return null;

        if (importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single ||
            importer.spriteBorder != border ||
            importer.mipmapEnabled)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
