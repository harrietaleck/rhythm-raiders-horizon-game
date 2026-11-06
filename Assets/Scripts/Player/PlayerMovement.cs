using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;
    PickupBase _pickup;

    public float baseForwardSpeed = 8f;
    public float baseSidewaySpeed = 10f;
    public float sidewaySpeed = 10f;
    public float forwardSpeed = 8f;
    [Tooltip("Run momentum from the shield (1 = full pace). Set by PickupBase as the shield drains or refills.")]
    public float Momentum = 1f;
    public float gravity = -20f;     // stronger gravity = snappier, less floaty jumps
    public float jumpHeight = 3f;
    public float footGroundOffset = 0.08f;

    //Ground variables
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    private bool isGrounded;

    //Vector variable
    private float velocity;
    public LayerMask Ground;

    private bool canJump = true;
    public bool DidJumpThisFrame { get; private set; }

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        _pickup = GetComponent<PickupBase>();
        forwardSpeed = baseForwardSpeed;
        sidewaySpeed = baseSidewaySpeed;
        EnsureFootGroundCheck();
        if (groundMask.value == 0)
            groundMask = GroundPlacementUtility.DefaultGroundMask;

        GroundPlacementUtility.SnapTransformFeetToGround(
            transform, groundMask, footGroundOffset);
    }

    /// <summary>
    /// Scene had groundCheck wired to the Ground platform — that made isGrounded always true.
    /// </summary>
    private void EnsureFootGroundCheck()
    {
        if (groundCheck != null && groundCheck.IsChildOf(transform))
            return;

        var existing = transform.Find("GroundCheck");
        if (existing != null)
        {
            groundCheck = existing;
            return;
        }

        var feet = new GameObject("GroundCheck");
        feet.transform.SetParent(transform, false);
        float footY = controller != null
            ? controller.center.y - controller.height * 0.5f + 0.05f
            : -0.95f;
        feet.transform.localPosition = new Vector3(0f, footY, 0f);
        groundCheck = feet.transform;
    }

    private void UpdateGrounded()
    {
        isGrounded = controller.isGrounded;

        if (groundCheck != null && groundCheck.IsChildOf(transform))
        {
            isGrounded |= Physics.CheckSphere(
                groundCheck.position,
                groundDistance,
                groundMask,
                QueryTriggerInteraction.Ignore);
        }
    }

    void Update()
    {
        DidJumpThisFrame = false;

        if (!enabled || !GameManager.GameStarted)
            return;

        // Skip cursor lock while paused — pause menu needs the cursor visible and free.
        if (Time.timeScale > 0f)
            UICursor.LockForGameplay();

        groundMask = Ground;
        UpdateGrounded();

        if (controller.isGrounded && velocity < 0f)
        {
            velocity = -2f;
            canJump = true;
        }

        float horizontal = _stumbleSteer ? 0f : Input.GetAxis("Horizontal");
        Vector3 move = new Vector3(horizontal * sidewaySpeed, 0, forwardSpeed * _stumbleSpeedMul * Momentum);
        if (_stumbleSteer)
        {
            move.x = _stumbleVelX;
            move.z += _stumbleVelZ;
        }

        // Jump buffer: a press just before landing still counts.
        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space)) _jumpPressedAt = Time.time;
        if (!_stumbleSteer && Time.time - _jumpPressedAt < 0.15f && canJump && isGrounded)
        {
            velocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            canJump = false;
            DidJumpThisFrame = true;
            _jumpPressedAt = -10f;
        }

        velocity += gravity * Time.deltaTime;
        move.y = velocity;
        ImpactProbe(move);
        controller.Move(move * Time.deltaTime);

        RecoverFromFall();
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!enabled || !GameManager.GameStarted || hit.collider == null) return;
        if (ReactToCharacter(hit.collider)) return;
        // Floor, walls and props are just solid: only real obstacles can be jumped through.
        // (Letting the floor through made her sink and bob up and down the whole run.)
        var obstacle = hit.collider.GetComponentInParent<HealthDecreaseObstacle>();
        if (obstacle == null || IsGround(hit.collider))
        {
            _pickup?.HandleSolidContact(hit.collider);
            return;
        }
        if (ClearsByJumping(hit.collider) || NotABump(hit))
        {
            // Grazed the top while jumping over: no trip, slide straight over it.
            PassThrough(obstacle.gameObject);
            return;
        }
        if (LogBumps)
            Debug.Log($"[Bump] {obstacle.name} feet={FeetY:F2} top={hit.collider.bounds.max.y:F2} n={hit.normal} grounded={isGrounded} vel={velocity:F2} stumbling={IsStumbling} z={transform.position.z:F2} colZ={hit.collider.bounds.center.z:F2}");
        _pickup?.HandleSolidContact(hit.collider);
    }

    public static bool LogBumps = false;

    readonly RaycastHit[] _probeHits = new RaycastHit[12];
    float _jumpPressedAt = -10f;

    /// <summary>Same as pressing jump (used by test tools).</summary>
    public void RequestJump() => _jumpPressedAt = Time.time;

    /// <summary>
    /// A low obstacle touched anywhere but its front face isn't a bump: landing on its top or
    /// catching its back edge on the way down after a jump must not trip her.
    /// </summary>
    bool NotABump(ControllerColliderHit hit)
    {
        var col = hit.collider;
        if (col.bounds.max.y - FeetY > 1.3f) return false;      // tall: every contact counts
        if (hit.normal.y > 0.5f) return true;                     // on top of it
        if (transform.position.z > col.bounds.center.z) return true;   // already past it
        return hit.normal.z > -0.3f;                              // not the face toward her
    }

    /// <summary>Height of the runner's soles in world space.</summary>
    float FeetY => controller != null ? transform.TransformPoint(controller.center).y - controller.height * 0.5f : transform.position.y;

    /// <summary>
    /// Low obstacles (lasers, fences) that the runner can jump: they only count when her body
    /// actually meets them. While she's in the air with her feet near or above the top, she clears it.
    /// </summary>
    public bool ClearsLowObstacle(Collider col) => ClearsByJumping(col);

    bool ClearsByJumping(Collider col)
    {
        float top = col.bounds.max.y;
        bool low = top - FeetY < 1.3f;   // jumpable
        if (!low) return false;
        bool airborne = !isGrounded || velocity > 0.1f || DidJumpThisFrame;
        return airborne && FeetY > top - 0.45f;
    }

    /// <summary>
    /// Hits register the moment the runner's body meets an obstacle: a short sweep of the body
    /// capsule just ahead of this frame's movement fires the impact (effect, sound, stumble)
    /// on contact, instead of a frame or two after the capsule has pushed into it.
    /// </summary>
    void ImpactProbe(Vector3 move)
    {
        if (controller == null || _pickup == null) return;
        var dir = new Vector3(move.x, 0f, move.z);
        float speed = dir.magnitude;
        if (speed < 0.1f) return;
        dir /= speed;
        float lead = Mathf.Clamp(speed * 0.04f, 0.15f, 0.6f) + speed * Time.deltaTime;
        var centre = transform.TransformPoint(controller.center);
        float half = Mathf.Max(0f, controller.height * 0.5f - controller.radius);
        var p1 = centre + Vector3.up * half;
        var p2 = centre - Vector3.up * (half - 0.15f);   // just above the feet so the floor isn't swept
        int n = Physics.CapsuleCastNonAlloc(p1, p2, controller.radius, dir, _probeHits, lead, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            var col = _probeHits[i].collider;
            if (col == null || col.transform.IsChildOf(transform)) continue;
            if (ReactToCharacter(col)) continue;
            if (col.GetComponentInParent<HealthDecreaseObstacle>() == null) continue;
            // Low, jumpable obstacles aren't hit early by the look-ahead: the runner still has
            // time to press jump. They count on real contact (OnControllerColliderHit).
            if (col.bounds.max.y - FeetY < 1.3f) continue;
            if (col.isTrigger) _pickup.HandleTrigger(col);
            else _pickup.HandleSolidContact(col);
        }
    }

    /// <summary>DJ bodyguards grab the runner; dancers get bumped (and knock notes out of her bag).</summary>
    bool ReactToCharacter(Collider col)
    {
        var guard = col.GetComponentInParent<BodyguardCatch>();
        if (guard != null) { guard.Catch(transform); return true; }
        var dancer = col.GetComponentInParent<DancerBump>();
        if (dancer != null) { dancer.Bump(transform); return true; }
        return false;
    }

    // ── Stumble ─────────────────────────────────────────────────────────────
    // Bumping an obstacle no longer stops the run. The player trips over (or is
    // knocked around) it, hits the floor, gets up and dashes on. The chasing
    // guard uses the lost seconds to close the gap.

    [Header("Stumble")]
    [Tooltip("Seconds from the trip until the player is back on their feet (matches the StumbleFall clip from 0.45 s at 1.5x).")]
    public float stumbleDuration = 1.25f;
    [Tooltip("Burst of speed after getting up, as a multiplier of forward speed.")]
    public float dashMultiplier = 1.35f;
    public float dashDuration = 0.7f;
    [Tooltip("Obstacles shorter than this (m) are tumbled over; taller ones knock the player aside.")]
    public float tumbleOverHeight = 1.7f;

    public bool IsStumbling { get; private set; }
    public bool IsDashing { get; private set; }
    /// <summary>Raised when a stumble starts. Argument: the obstacle that tripped the player.</summary>
    public static event System.Action<GameObject> Stumbled;

    float _stumbleSpeedMul = 1f;
    bool _stumbleSteer;
    float _stumbleVelX, _stumbleVelZ;
    Coroutine _stumbleRoutine;

    public void Stumble(GameObject obstacle)
    {
        if (!enabled || !GameManager.GameStarted || controller == null) return;

        var cols = PassThrough(obstacle);

        if (IsStumbling) return;   // already on the floor — the hit still counts, no second fall

        Bounds b = new Bounds(transform.position, Vector3.zero);
        bool haveBounds = false;
        foreach (var c in cols)
        {
            if (c == null) continue;
            if (!haveBounds) { b = c.bounds; haveBounds = true; } else b.Encapsulate(c.bounds);
        }
        if (!haveBounds && obstacle != null)
            foreach (var r in obstacle.GetComponentsInChildren<Renderer>())
            {
                if (!haveBounds) { b = r.bounds; haveBounds = true; } else b.Encapsulate(r.bounds);
            }

        if (_stumbleRoutine != null) StopCoroutine(_stumbleRoutine);
        // Kill momentum and start the fall pose right now, in the collision frame.
        IsStumbling = true;
        _stumbleSteer = true;
        _stumbleSpeedMul = 0.35f;
        GetComponent<PlayerAnimationController>()?.PlayStumble();
        _stumbleRoutine = StartCoroutine(StumbleRoutine(b, haveBounds));
        Stumbled?.Invoke(obstacle);
    }

    /// <summary>Never get stuck on an obstacle already hit: the capsule passes through it from now on.</summary>
    public Collider[] PassThrough(GameObject obstacle)
    {
        var cols = obstacle != null ? obstacle.GetComponentsInChildren<Collider>() : new Collider[0];
        if (controller == null) return cols;
        foreach (var c in cols)
            if (c != null && !c.isTrigger && !IsGround(c)) Physics.IgnoreCollision(controller, c, true);
        return cols;
    }

    bool IsGround(Collider c)
    {
        int mask = (Ground.value != 0 ? Ground.value : groundMask.value) | GroundPlacementUtility.DefaultGroundMask.value;
        return (mask & (1 << c.gameObject.layer)) != 0;
    }

    System.Collections.IEnumerator StumbleRoutine(Bounds b, bool haveBounds)
    {
        IsStumbling = true;
        IsDashing = false;
        _stumbleSteer = true;
        _stumbleVelX = 0f;
        _stumbleVelZ = 0f;

        float feetY = transform.position.y - (controller.height * 0.5f - controller.center.y);
        bool tall = haveBounds && b.max.y - feetY > tumbleOverHeight;
        float fallEnd = stumbleDuration * 0.38f;     // hips hit the floor ~38% in
        float riseStart = stumbleDuration * 0.58f;

        // Where should the body end up? Short obstacle → land on its far side.
        // Tall obstacle → bounced back a touch and shoved toward the nearest open side.
        float targetZOffset = 0f;
        float targetX = transform.position.x;
        if (haveBounds)
        {
            if (!tall)
            {
                float need = b.max.z + 0.6f - transform.position.z;
                targetZOffset = Mathf.Max(0f, need - forwardSpeed * 0.35f * fallEnd);
            }
            else
            {
                targetZOffset = -0.6f;
                float left = b.min.x - 0.9f, right = b.max.x + 0.9f;
                bool leftFree = SpotFree(new Vector3(left, transform.position.y, b.center.z));
                bool rightFree = SpotFree(new Vector3(right, transform.position.y, b.center.z));
                if (leftFree || rightFree)
                {
                    bool goLeft = leftFree && (!rightFree ||
                        Mathf.Abs(transform.position.x - left) <= Mathf.Abs(transform.position.x - right));
                    targetX = goLeft ? left : right;
                }
            }
        }

        float t = 0f;
        while (t < stumbleDuration)
        {
            if (!GameManager.GameStarted) break;
            float dt = Time.deltaTime;
            t += dt;

            if (t < fallEnd)
            {
                float k = t / fallEnd;
                _stumbleSpeedMul = Mathf.Lerp(0.35f, 0.05f, k);       // momentum bleeds off as they go down
                _stumbleVelZ = targetZOffset / fallEnd;
                _stumbleVelX = tall ? (targetX - transform.position.x) * 3f : 0f;
            }
            else if (t < riseStart)
            {
                _stumbleSpeedMul = 0f;                                 // on the floor
                _stumbleVelZ = 0f;
                _stumbleVelX = (targetX - transform.position.x) * 3f;
            }
            else
            {
                float k = (t - riseStart) / (stumbleDuration - riseStart);
                _stumbleSpeedMul = Mathf.Lerp(0f, 0.6f, k);            // scrambling up
                _stumbleVelZ = 0f;
                _stumbleVelX = (targetX - transform.position.x) * 3f;
            }
            yield return null;
        }

        _stumbleSteer = false;
        _stumbleVelX = _stumbleVelZ = 0f;
        IsStumbling = false;

        // Dash: shoot off the floor to claw some distance back from the guard.
        IsDashing = true;
        float d = 0f;
        while (d < dashDuration && GameManager.GameStarted)
        {
            d += Time.deltaTime;
            float k = d / dashDuration;
            _stumbleSpeedMul = k < 0.25f
                ? Mathf.Lerp(0.6f, dashMultiplier, k / 0.25f)
                : Mathf.Lerp(dashMultiplier, 1f, (k - 0.25f) / 0.75f);
            yield return null;
        }
        _stumbleSpeedMul = 1f;
        IsDashing = false;
        _stumbleRoutine = null;
    }

    bool SpotFree(Vector3 centre)
    {
        float r = controller.radius;
        Vector3 c = centre + controller.center;
        Vector3 up = Vector3.up * Mathf.Max(0f, controller.height * 0.5f - r);
        int mask = ~groundMask.value & ~Ground.value & ~(1 << gameObject.layer);
        if (Physics.CheckCapsule(c - up, c + up, r * 0.9f, mask, QueryTriggerInteraction.Ignore))
            return false;
        // Must still have floor underneath (don't shove the player off the side).
        return Physics.Raycast(centre + Vector3.up * 2f, Vector3.down, 6f, Ground.value != 0 ? Ground.value : groundMask.value,
            QueryTriggerInteraction.Ignore);
    }

    /// <summary>Cancel any stumble/dash (new run, death, victory).</summary>
    public void ResetStumble()
    {
        if (_stumbleRoutine != null) StopCoroutine(_stumbleRoutine);
        _stumbleRoutine = null;
        IsStumbling = IsDashing = false;
        _stumbleSteer = false;
        _stumbleSpeedMul = 1f;
        _stumbleVelX = _stumbleVelZ = 0f;
    }

    void RecoverFromFall()
    {
        UpdateGrounded();
        if (isGrounded) return;

        Vector3 origin = transform.position + Vector3.up * 2f;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 40f, groundMask, QueryTriggerInteraction.Ignore))
            return;

        float footY = hit.point.y + (controller != null
            ? controller.height * 0.5f - controller.center.y + footGroundOffset
            : 1f);

        // Only rescue her when she has sunk into the floor while coming down. (It used to snap her
        // back whenever she was within 35 cm above the floor - which cancelled most jumps a few
        // frames after take-off, so she ran straight into lasers she had jumped for.)
        if (velocity > 0f || DidJumpThisFrame || transform.position.y > footY - 0.02f)
            return;

        controller.enabled = false;
        transform.position = new Vector3(transform.position.x, footY, transform.position.z);
        controller.enabled = true;
        velocity = -2f;
    }
    // AlignFeetToGround removed — it fought CharacterController's native ground
    // detection each frame, causing the player to stutter and stop mid-run.
    // The CharacterController isGrounded + gravity=-20 handles ground contact cleanly.
}