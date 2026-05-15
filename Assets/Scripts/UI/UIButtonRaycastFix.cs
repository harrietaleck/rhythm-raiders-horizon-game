using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TMP labels on buttons block raycasts and swallow clicks unless disabled.
/// Narration text fields should not eat clicks meant for buttons below them.
/// </summary>
public static class UIButtonRaycastFix
{
    public static void Apply(Button button)
    {
        if (button == null) return;

        var target = button.targetGraphic;
        foreach (var graphic in button.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic != null && graphic != target)
                graphic.raycastTarget = false;
        }
    }

    public static void DisableTextRaycasts(GameObject root)
    {
        if (root == null) return;

        foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp.GetComponentInParent<Button>() != null)
                continue;
            tmp.raycastTarget = false;
        }
    }

    /// <summary>Reparent to the canvas root and draw above other HUD so clicks register.</summary>
    public static void BringToFront(Button button)
    {
        if (button == null) return;
        var canvas = button.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var t = button.transform;
        t.SetParent(canvas.transform, true);
        t.SetAsLastSibling();
    }
}
