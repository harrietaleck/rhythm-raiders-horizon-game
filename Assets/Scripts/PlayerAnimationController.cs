using UnityEngine;

/// <summary>
/// Drives the Player Animator. Attach to the Player root GameObject.
/// Speed is derived from PlayerMovement.forwardSpeed so it never glitches
/// from CharacterController.velocity reporting momentary zeros on collision.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PickupBase))]
public class PlayerAnimationController : MonoBehaviour
{
    [Tooltip("Animator on the character model child (Ch03_nonPBR). Auto-found if blank.")]
    public Animator characterAnimator;

    [Tooltip("How fast Speed blends — lower = smoother, higher = more snappy")]
    public float speedDampTime = 0.08f;

    private PlayerMovement movement;
    private PickupBase     pickup;

    [Tooltip("Y position below which the player is considered to have fallen to death")]
    public float fallDeathY = -5f;

    private float smoothedSpeed = 0f;
    private bool  wasDead       = false;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        pickup   = GetComponent<PickupBase>();

        if (characterAnimator == null)
            characterAnimator = GetComponentInChildren<Animator>();

        // Force the animator into Idle from the very first frame.
        // Without this, there is a 1-frame window where the animator
        // is in an undefined state, which can appear as a T-pose flash.
        if (characterAnimator != null)
        {
            characterAnimator.SetFloat("Speed", 0f);
            characterAnimator.Play("Idle", 0, 0f);
        }
    }

    private void Update()
    {
        if (characterAnimator == null) return;

        bool isDead = pickup != null && (pickup.currentHealth <= 0 || pickup.hitCounter <= 0);

        // ── Speed ─────────────────────────────────────────────────────────
        // Use forwardSpeed as the authoritative value instead of cc.velocity,
        // which can briefly read zero when the character controller hits any
        // collider (including slow-down obstacles).
        float target = 0f;
        if (!isDead && movement != null && movement.enabled)
            target = movement.forwardSpeed;   // 10 normally, ~6 when slowed — always > 0.1

        smoothedSpeed = Mathf.MoveTowards(smoothedSpeed, target, Time.deltaTime / speedDampTime);
        characterAnimator.SetFloat("Speed", smoothedSpeed);

        if (!isDead && movement != null && movement.DidJumpThisFrame)
            characterAnimator.SetTrigger("Jump");

        // ── Dead ──────────────────────────────────────────────────────────
        characterAnimator.SetBool("IsDead", isDead);

        // ── Fall-death: player dropped below the world ────────────────────
        // Catches both "fell off the edge" and "clipped through the floor"
        if (!isDead && transform.position.y < fallDeathY)
        {
            // Force health to zero so IsDead becomes true next frame
            if (pickup != null) pickup.currentHealth = 0;
            isDead = true;
        }

        // Notify GameManager the first time the player dies
        if (isDead && !wasDead)
        {
            GameManager.Instance?.TriggerGameOver();
        }
        wasDead = isDead;
    }
}
