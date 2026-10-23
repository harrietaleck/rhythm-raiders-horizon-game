using UnityEngine;

/// <summary>
/// Keeps an animated character's soles on the floor: after the Animator poses the body each
/// frame, the model is shifted up or down so its lowest foot / toe bone sits on <see cref="floorY"/>.
/// </summary>
[DefaultExecutionOrder(1000)]
public class FeetOnFloor : MonoBehaviour
{
    public float floorY;
    [Tooltip("Height of the foot bones above the sole, as a fraction of the character's height.")]
    public float soleOffset = 0.035f;

    Transform[] _feet;
    float _height = 1f;
    Renderer _renderer;

    void Awake() => Find();

    void Find()
    {
        var anim = GetComponent<Animator>();
        if (anim != null && anim.isHuman)
        {
            _feet = new[]
            {
                anim.GetBoneTransform(HumanBodyBones.LeftToes), anim.GetBoneTransform(HumanBodyBones.RightToes),
                anim.GetBoneTransform(HumanBodyBones.LeftFoot), anim.GetBoneTransform(HumanBodyBones.RightFoot),
            };
            var head = anim.GetBoneTransform(HumanBodyBones.Head);
            var hips = anim.GetBoneTransform(HumanBodyBones.Hips);
            if (head != null && hips != null) _height = Mathf.Max(0.1f, (head.position.y - hips.position.y) * 2.1f);
        }
        _renderer = GetComponentInChildren<Renderer>();
    }

    public void Snap()
    {
        if (_feet == null) Find();
        LateUpdate();
    }

    void LateUpdate()
    {
        if (_renderer != null && !_renderer.isVisible) return;
        if (_feet == null) return;
        float low = float.MaxValue;
        foreach (var f in _feet) if (f != null && f.position.y < low) low = f.position.y;
        if (low == float.MaxValue) return;
        float want = floorY + soleOffset * _height;
        transform.position += Vector3.up * (want - low);
    }
}
