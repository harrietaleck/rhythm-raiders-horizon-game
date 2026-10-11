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

        //Move toward player
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, chaseSpeed * Time.deltaTime);
        SnapToGround(false);
    }

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
        if (!GameManager.GameStarted) return;
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