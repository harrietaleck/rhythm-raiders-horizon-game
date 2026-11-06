using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    //Declare variables
    public Transform player;
    private float speed = 7f;
    private float baseSpeed = 7f;
    private float boostedSpeed = 12f;   // overridden by LevelBootstrap; 12 = manageable surge
    [Tooltip("When the player is slowed, guard speed = player speed + this margin.")]
    public float catchUpMargin = 1f;
    private PickupBase pickupBase;
    private PlayerMovement playerMovement;
    public float followDistance = 1f;
    public float groundOffset = 0.05f;
    public float groundRayHeight = 6f;
    public LayerMask groundMask;

    private bool  _surging      = false;
    private float _surgeTimer   = 0f;
    private float _surgeDuration = 0f;
    private bool  _caughtPlayer = false;
    private Collider _catchCollider;
    private float _pivotToFeet;

    void Start()
    {
        GuardGuns.Attach(transform, 0.9f);   // the chasing guard carries a rifle
        //Assign scripts to be used in the enemy base script
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null)
        {
            Debug.LogError("Player not found!");
            return;
        }
        player = playerGO.transform;
        playerMovement = player.GetComponent<PlayerMovement>();
        pickupBase = player.GetComponent<PickupBase>();

        if (pickupBase == null)
            Debug.LogError("PickupBase script not found on Player!");
        // currentHealth is owned by LevelBootstrap — do NOT overwrite it here

        if (playerMovement == null)
            Debug.LogError("PlayerMovement script not found on Player!");

        _catchCollider = GetComponent<Collider>();
        if (_catchCollider == null)
            _catchCollider = GetComponentInChildren<Collider>();
        if (_catchCollider != null)
            _catchCollider.isTrigger = true;

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (groundMask.value == 0)
            groundMask = LayerMask.GetMask("Ground");

        _pivotToFeet = ComputePivotToFeet();
        SnapToGround(true);
    }

    void Update()
    {
        if (!GameManager.GameStarted)
            return;

        // Surge timer — returns to base speed when surge expires
        if (_surging)
        {
            _surgeTimer += Time.deltaTime;
            if (_surgeTimer >= _surgeDuration)
            {
                speed    = baseSpeed;
                _surging = false;
                Debug.Log("Guard surge ended — returning to base speed.");
            }
        }

        //Calculate the target position to follow the player
        Vector3 targetPosition = new Vector3(player.position.x, transform.position.y, player.position.z - followDistance);

        float chaseSpeed = GetChaseSpeed();

        // Player tripped: sprint in to the lunge gap and hold it until they're back up.
        if (Time.time < _lungeUntil && !IsStunned)
        {
            targetPosition.z = player.position.z - _lungeGap;
            if (transform.position.z < targetPosition.z)
                chaseSpeed = Mathf.Max(chaseSpeed, lungeSpeed);
            else
                targetPosition.z = Mathf.Min(targetPosition.z, transform.position.z);   // never back away
        }

        //Move toward player
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, chaseSpeed * Time.deltaTime);
        SnapToGround(false);

        // Close enough to grab: the runner is caught (the trigger alone can miss a CharacterController).
        if (!IsStunned && player != null)
        {
            float gap = player.position.z - transform.position.z;
            float side = Mathf.Abs(player.position.x - transform.position.x);
            bool down = playerMovement != null && playerMovement.IsStumbling;
            // Close behind her, or near enough while she's on the floor after a fall: caught.
            if (gap > -1.5f && side < catchSideReach && (gap < catchDistance || (down && gap < downCatchDistance)))
                CatchPlayer();
        }
    }

    [Header("Catch")]
    [Tooltip("Guard catches the runner when he gets this close behind her (metres along the run).")]
    public float catchDistance = 1.8f;
    [Tooltip("...and is within this distance sideways.")]
    public float catchSideReach = 2.2f;
    [Tooltip("While the runner is down after a trip, the guard grabs her from this far.")]
    public float downCatchDistance = 5f;   // matches the HUD "CLOSE!" warning

    float GetChaseSpeed()
    {
        if (Time.time < _stunnedUntil)
            return 0f;

        if (playerMovement != null &&
            playerMovement.forwardSpeed < playerMovement.baseForwardSpeed - 0.1f)
            return Mathf.Max(1f, playerMovement.forwardSpeed + catchUpMargin);

        float result = speed * _speedMultiplier;
        if (rubberBand && !_surging && player != null)
        {
            float gap = player.position.z - transform.position.z;
            if (gap > rubberBandFar)
                result *= 1.3f;
            else if (gap < rubberBandNear)
                result *= 0.85f;
        }
        return result;
    }

    [Header("Balance")]
    public bool rubberBand = true;
    [Tooltip("Guard speeds up when further behind than this (metres).")]
    public float rubberBandFar = 14f;
    [Tooltip("Guard eases off when closer than this (metres).")]
    public float rubberBandNear = 4f;
    [Tooltip("Seconds of warning before a surge kicks in.")]
    public float surgeWarning = 1.2f;

    float _speedMultiplier = 1f;
    float _stunnedUntil;

    public bool IsWarning { get; private set; }
    public bool IsStunned => Time.time < _stunnedUntil;
    public static event System.Action SurgeWarned;

    public void SetSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = Mathf.Max(0.1f, multiplier);
    }

    [Header("Stumble pursuit")]
    [Tooltip("Metres the guard gains on the player each time they trip.")]
    public float stumbleGain = 5f;
    [Tooltip("Closest the guard gets from a single trip. Trip again while this close and you're caught.")]
    public float stumbleMinGap = 2.4f;
    [Tooltip("Guard sprint speed while closing in on a tripped player.")]
    public float lungeSpeed = 16f;

    float _lungeUntil;
    float _lungeGap;

    /// <summary>Distance from guard to player along the run (metres).</summary>
    public float Gap => player != null ? player.position.z - transform.position.z : 999f;

    /// <summary>
    /// The player tripped over something. The guard sprints in to steal
    /// <see cref="stumbleGain"/> metres. If they were already on the player's
    /// heels, the trip is fatal.
    /// </summary>
    public void ClosePursuitGap(PickupBase victim)
    {
        if (!enabled || !GameManager.GameStarted || player == null || IsStunned) return;
        if (victim != null && victim.isSneaking) return;

        float gap = Gap;
        if (gap <= stumbleMinGap + 0.6f)
        {
            _lungeGap = 0.4f;                       // dive onto the player — catch on contact
            _lungeUntil = Time.time + 1.5f;
            Invoke(nameof(CatchIfStillDown), 0.45f);
            return;
        }

        _lungeGap = Mathf.Max(stumbleMinGap, gap - stumbleGain);
        float duration = playerMovement != null ? playerMovement.stumbleDuration : 1.75f;
        _lungeUntil = Time.time + duration;
    }

    void CatchIfStillDown()
    {
        if (playerMovement != null && playerMovement.IsStumbling) CatchPlayer();
    }

    public void Stun(float seconds)
    {
        _stunnedUntil = Mathf.Max(_stunnedUntil, Time.time + seconds);
    }

    private void SnapToGround(bool instant)
    {
        LayerMask mask = groundMask.value != 0 ? groundMask : LayerMask.GetMask("Ground");

        Vector3 origin = transform.position + Vector3.up * groundRayHeight;
        float maxDist = groundRayHeight + _pivotToFeet + 6f;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDist, mask, QueryTriggerInteraction.Ignore))
            return;

        float targetY = hit.point.y + _pivotToFeet;
        if (instant || Mathf.Abs(transform.position.y - targetY) > 0.5f)
            transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
        else if (Mathf.Abs(transform.position.y - targetY) > 0.02f)
        {
            float smoothY = Mathf.Lerp(transform.position.y, targetY, Mathf.Min(1f, 15f * Time.deltaTime));
            transform.position = new Vector3(transform.position.x, smoothY, transform.position.z);
        }
    }

    float ComputePivotToFeet()
    {
        return GroundPlacementUtility.GetPivotToFeet(transform, groundOffset);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!PlayerColliderUtility.IsPlayer(other)) return;
        CatchPlayer();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!PlayerColliderUtility.IsPlayer(collision.collider)) return;
        CatchPlayer();
    }

    private void CatchPlayer()
    {
        if (_caughtPlayer || pickupBase == null) return;
        if (!enabled || !GameManager.GameStarted) return;   // disabled at victory: no late catches
        if (pickupBase.isSneaking) return;

        _caughtPlayer = true;
        Debug.Log("Security caught the player!");
        pickupBase.KillPlayer(DeathCause.Guard);
    }

    public void ApplyLevelTuning(float normalSpeed, float boostSpeed)
    {
        baseSpeed    = normalSpeed;
        speed        = normalSpeed;
        boostedSpeed = boostSpeed;
    }

    public bool IsSurging     => _surging;
    public float SurgeTimeLeft => _surging ? Mathf.Max(0f, _surgeDuration - _surgeTimer) : 0f;

    /// <summary>
    /// Called when the player collects their first artifact.
    /// Guard surges to boostedSpeed for <duration> seconds to add tension.
    /// </summary>
    public void Surge(float duration)
    {
        if (_surging || IsWarning) return;
        StartCoroutine(SurgeAfterWarning(duration));
    }

    System.Collections.IEnumerator SurgeAfterWarning(float duration)
    {
        IsWarning = true;
        SurgeWarned?.Invoke();
        yield return new WaitForSeconds(surgeWarning);
        IsWarning = false;
        BeginSurge(duration);
    }

    void BeginSurge(float duration)
    {
        speed         = boostedSpeed;
        _surgeDuration = duration;
        _surgeTimer   = 0f;
        _surging      = true;
        Debug.Log($"Guard surge! Speed → {boostedSpeed} for {duration}s.");
    }
}