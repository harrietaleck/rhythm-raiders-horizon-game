using TMPro;
using UnityEngine;

/// <summary>
/// Sci-Fi GUI skin references for HUD built at runtime. Lives at Resources/HudSkin
/// (created by Tools → Apply Sci-Fi Skin To Gameplay HUD).
/// </summary>
[CreateAssetMenu(menuName = "Rhythm Raiders/HUD Skin")]
public class HudSkin : ScriptableObject
{
    public Sprite window;
    public Sprite banner;
    public TMP_FontAsset font;

    public static readonly Color Cyan = new Color(0.35f, 0.85f, 1f, 1f);

    static HudSkin _instance;
    static bool _looked;

    public static HudSkin Get()
    {
        if (!_looked)
        {
            _looked = true;
            _instance = Resources.Load<HudSkin>("HudSkin");
        }
        return _instance;
    }
}
