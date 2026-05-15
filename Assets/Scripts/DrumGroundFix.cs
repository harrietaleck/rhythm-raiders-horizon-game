using UnityEngine;

/// <summary>
/// Prevents the Drum (and any similarly-animated instrument) from visually
/// sinking through the ground during its path animation.
///
/// How the drum animation works:
///   • The Drummove clip drives localPosition.y between 1.9 (visible, on ground)
///     and large negative values (e.g. -8, -222 …) to hide the drum while it
///     teleports to a new Z position along the track.
///   • Unity Bezier interpolation creates a smooth curve between those keyframes,
///     so the drum gradually passes through ground level → it looks like it sinks.
///
/// Fix: in LateUpdate (after Animator has written the position) clamp Y so the
/// drum is either at or above surfaceY, or snapped to undergroundY.
/// The midrange "sinking" zone is never visible.
/// </summary>
[DefaultExecutionOrder(200)]   // run after Animator updates (order 0) and LateUpdate
public class DrumGroundFix : MonoBehaviour
{
    [Tooltip("Y position when the drum sits on the ground. " +
             "Match the y=1.9 value baked into Drummove.anim.")]
    public float surfaceY = 1.9f;

    [Tooltip("Below this Y the drum is considered to be entering/leaving the ground " +
             "and will be snapped underground immediately.")]
    public float groundThreshold = 1.0f;

    [Tooltip("Y position used while the drum is hidden underground between appearances. " +
             "Should be well below the camera view.")]
    public float undergroundY = -30f;

    // ── internal state ────────────────────────────────────────────────────────
    private bool  underground    = false;
    private float prevAnimatedY  = 0f;

    private void Start()
    {
        prevAnimatedY = transform.position.y;
        underground   = prevAnimatedY < groundThreshold;
    }

    private void LateUpdate()
    {
        Vector3 pos      = transform.position;
        float   animatedY = pos.y;          // value written by Animator this frame

        if (!underground)
        {
            // Drum is visible.  If animator tries to move it below the threshold,
            // snap immediately to underground so it never passes through the ground.
            if (animatedY < groundThreshold)
            {
                pos.y      = undergroundY;
                underground = true;
            }
            // else leave it where the animator put it (at or near surfaceY)
        }
        else
        {
            // Drum is underground.  Wait until the animator brings it back near
            // surfaceY before snapping it onto the surface again.
            if (animatedY >= groundThreshold)
            {
                pos.y      = surfaceY;
                underground = false;
            }
            else
            {
                // Keep it fully underground regardless of what the animator says
                pos.y = undergroundY;
            }
        }

        prevAnimatedY    = animatedY;
        transform.position = pos;
    }
}
