using ProceduralUIEffects.Lite;
using TMPro;
using UnityEngine;

/// <summary>
/// Animates a UI text with the Procedural Text Animator (LITE) effects: wave, shake, pulse.
/// The game's scripts keep setting .text and .color as usual; this re-feeds the animator
/// whenever either changes. Text that uses rich-text tags is left as it is.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class TextFx : MonoBehaviour
{
    [Tooltip("wave, shake or pulse; empty = no animation.")]
    public string effect;

    TextMeshProUGUI _tmp;
    PUAP_TMP _anim;
    string _shown;
    Color _color;
    string _fed;

    /// <summary>Turn the effect on (or change it) for this text. Pass null/empty to stop.</summary>
    public static TextFx Use(TMP_Text text, string effect)
    {
        var ugui = text as TextMeshProUGUI;
        if (ugui == null) return null;
        var fx = ugui.GetComponent<TextFx>();
        if (fx == null)
        {
            if (string.IsNullOrEmpty(effect)) return null;
            fx = ugui.gameObject.AddComponent<TextFx>();
        }
        fx.effect = effect;
        return fx;
    }

    void Awake()
    {
        _tmp = GetComponent<TextMeshProUGUI>();
    }

    PUAP_TMP Anim()
    {
        if (_anim != null) return _anim;
        var lib = FxLibrary.Get();
        if (lib == null || lib.textEffects == null) return null;
        _anim = GetComponent<PUAP_TMP>();
        if (_anim == null) _anim = gameObject.AddComponent<PUAP_TMP>();
        _anim.effectDatabase = lib.textEffects;
        _anim.animationLoop = AnimationLoop.Manual;
        return _anim;
    }

    void LateUpdate()
    {
        if (_tmp == null) return;
        string text = _tmp.text ?? "";
        bool active = !string.IsNullOrEmpty(effect) && text.Length > 0 && text.IndexOf('<') < 0 && _tmp.enabled;
        if (!active)
        {
            if (_fed != null) { _fed = null; _tmp.ForceMeshUpdate(); }
            return;
        }
        var anim = Anim();
        if (anim == null) return;

        if (text != _shown || _tmp.color != _color || _fed != effect)
        {
            anim.SetText("<" + effect + ">" + text + "</" + effect + ">");
            _shown = _tmp.text;
            _color = _tmp.color;
            _fed = effect;
        }
        anim.Animate(Time.unscaledDeltaTime);
    }
}
