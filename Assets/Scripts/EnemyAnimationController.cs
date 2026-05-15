using UnityEngine;

/// <summary>
/// Drives the Enemy Animator. Attach to the Enemy root GameObject.
/// Measures movement via position-delta each frame so it works with
/// EnemyBase (which uses transform.position directly, not a Rigidbody).
/// </summary>
public class EnemyAnimationController : MonoBehaviour
{
    [Tooltip("Animator on the character model child. Auto-found if blank.")]
    public Animator characterAnimator;

    [Tooltip("Speed smoothing — lower = smoother, higher = more reactive")]
    public float speedDampTime = 0.1f;

    private Vector3 lastPosition;
    private float   smoothedSpeed;

    private void Awake()
    {
        if (characterAnimator == null)
            characterAnimator = GetComponentInChildren<Animator>();
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (characterAnimator == null) return;

        // Horizontal distance moved this frame → convert to units/sec
        Vector3 delta = transform.position - lastPosition;
        delta.y      = 0f;
        float raw    = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPosition = transform.position;

        smoothedSpeed = Mathf.MoveTowards(smoothedSpeed, raw, Time.deltaTime / speedDampTime);
        characterAnimator.SetFloat("Speed", smoothedSpeed);
    }
}
