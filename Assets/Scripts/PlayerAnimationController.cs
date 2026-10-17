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
    public float fallDeathY = -4f;

    [Tooltip("Seconds without ground support before a void fall counts as death")]
    public float voidFallDeathDelay = 1.2f;

    private float smoothedSpeed = 0f;
    private bool  wasDead       = false;
    private float _voidFallTimer;

    private static readonly int SpeedHash  = Animator.StringToHash("Speed");
    private static readonly int JumpHash   = Animator.StringToHash("Jump");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");
    private static readonly int StumbleHash = Animator.StringToHash("Stumble");
    private static readonly int StumbleStateHash = Animator.StringToHash("Base Layer.Stumble");
    private bool _hasStumbleParam;

    [Tooltip("Seconds into the StumbleFall clip to start from — skips the slow upright lead-in so the body pitches forward on impact.")]
    public float stumbleClipOffset = 0.45f;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        pickup   = GetComponent<PickupBase>();

        if (characterAnimator == null)
        {
            Transform ch03 = transform.Find("Ch03_nonPBR");
            characterAnimator = ch03 != null
                ? ch03.GetComponent<Animator>()
                : GetComponentInChildren<Animator>();
        }

        if (characterAnimator != null)
        {
            characterAnimator.applyRootMotion = false;
            foreach (var p in characterAnimator.parameters)
                if (p.nameHash == StumbleHash) _hasStumbleParam = true;
        }
    }

    private void Update()
    {
        if (characterAnimator == null) return;

        if (!GameManager.GameStarted)
        {
            if (!GameManager.IsGameOver)
            {
                characterAnimator.SetBool(IsDeadHash, false);
                characterAnimator.SetFloat(SpeedHash, 0f);
                smoothedSpeed = 0f;
                wasDead = false;
                _voidFallTimer = 0f;
            }
            return;
        }

        bool isDead = pickup != null && pickup.currentHealth <= 0;

        if (!isDead)
            isDead = CheckVoidFall();

        float target = 0f;
        if (!isDead && movement != null && movement.enabled)
            target = Mathf.Max(movement.forwardSpeed, 1f);

        smoothedSpeed = Mathf.MoveTowards(smoothedSpeed, target, Time.deltaTime / speedDampTime);
        characterAnimator.SetFloat(SpeedHash, smoothedSpeed);

        if (!isDead && movement != null && movement.DidJumpThisFrame)
            characterAnimator.SetTrigger(JumpHash);

        characterAnimator.SetBool(IsDeadHash, isDead);

        if (isDead && !wasDead)
            TriggerDefeat();

        wasDead = isDead;
    }

    /// <summary>
    /// Called by PlayerMovement in the same frame as the collision so the trip
    /// reads instantly — no waiting a frame for a trigger or for the clip's lead-in.
    /// </summary>
    public void PlayStumble()
    {
        if (characterAnimator == null || !_hasStumbleParam) return;
        if (!characterAnimator.HasState(0, StumbleStateHash)) return;
        characterAnimator.ResetTrigger(JumpHash);
        characterAnimator.CrossFadeInFixedTime(StumbleStateHash, 0.02f, 0, stumbleClipOffset);
        characterAnimator.Update(0f);   // apply the first stumble pose this frame
    }

    bool CheckVoidFall()
    {
        if (transform.position.y < fallDeathY)
        {
            RunStats.SetDeathCause(DeathCause.Fall);
            return true;
        }

        int groundMask = movement != null && movement.Ground.value != 0
            ? movement.Ground.value
            : LayerMask.GetMask("Ground");

        Vector3 origin = transform.position + Vector3.up * 2f;
        bool hasGround = Physics.Raycast(origin, Vector3.down, 12f, groundMask, QueryTriggerInteraction.Ignore);

        if (hasGround || transform.position.y > 3f)
        {
            _voidFallTimer = 0f;
            return false;
        }

        _voidFallTimer += Time.deltaTime;
        if (_voidFallTimer < voidFallDeathDelay)
            return false;

        RunStats.SetDeathCause(DeathCause.Fall);
        return true;
    }

    void TriggerDefeat()
    {
        if (pickup != null && pickup.currentHealth > 0)
            pickup.KillPlayer(RunStats.LastDeathCause);
        else if (GameManager.Instance != null)
            GameManager.Instance.TriggerGameOver();
    }
}
