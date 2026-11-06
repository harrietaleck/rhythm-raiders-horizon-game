using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class PickupBase : MonoBehaviour
{
    //Declare variables for the pickups
    PlayerMovement playerMovement;

    //Pickup variables
    private int healthIncreaseAmount = 20;
    private int healthDecreaseAmount = 10;
    public int currentHealth = 100;
    public int maxHealth = 100;
    public const int ArtifactsToWin = 2;

    public int artifactAmount;
    public int artifactMoneyTotal;
    public int moneyPerArtifact = 500;
    public bool isSneaking = false;
    public int hitCounter = 10;

    [Header("Run momentum (shield)")]
    [Tooltip("While running, one shield point burns off every this many seconds.")]
    public float shieldDrainSeconds = 6f;
    [Tooltip("Pace with no shield left, as a fraction of full speed.")]
    public float minMomentum = 0.7f;
    [Tooltip("Shield points a shield pickup gives back.")]
    public int shieldPickupAmount = 3;
    int _maxShield = 10;
    float _drainTimer;
    public int MaxShield => _maxShield;

    private bool _deathHandled;
    readonly Dictionary<int, float> _solidHitTimes = new Dictionary<int, float>();
    const float SolidHitCooldown = 0.45f;

    //Independent timers for each boost (sharing one timer causes them to expire early when two are active)
    private float jumpBoostTimer = 0f;
    private float speedBoostTimer = 0f;
    private float sneakTimer = 0f;
    private bool jumpBoostActive = false;
    private bool speedBoostActive = false;

    // ── Public boost state for HUD ────────────────────────────────────────────
    private const float BoostDuration = 5f;
    public bool  IsSpeedBoostActive  => speedBoostActive;
    public bool  IsJumpBoostActive   => jumpBoostActive;
    public float SpeedBoostTimeLeft  => speedBoostActive ? Mathf.Max(0f, BoostDuration - speedBoostTimer) : 0f;
    public float JumpBoostTimeLeft   => jumpBoostActive  ? Mathf.Max(0f, BoostDuration - jumpBoostTimer)  : 0f;
    public float SneakTimeLeft       => isSneaking       ? Mathf.Max(0f, BoostDuration - sneakTimer)      : 0f;
    //This class is for pickups
    private void Start()
    {
        //Find the player movement script
        playerMovement = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerMovement>();
        //Assign currnt health to max health
        currentHealth = maxHealth;
        _maxShield = Mathf.Max(1, hitCounter);   // LevelBootstrap set the level's shield in Awake
    }

    /// <summary>
    /// Shield is run momentum: it burns off while she runs and her pace drops with it;
    /// shield pickups top it back up and she speeds up again.
    /// </summary>
    void UpdateMomentum()
    {
        if (playerMovement == null) return;
        if (GameManager.GameStarted && !_deathHandled && playerMovement.enabled && hitCounter > 0)
        {
            _drainTimer += Time.deltaTime;
            if (_drainTimer >= shieldDrainSeconds)
            {
                _drainTimer = 0f;
                hitCounter--;
            }
        }
        float target = Mathf.Lerp(minMomentum, 1f, Mathf.Clamp01(hitCounter / (float)_maxShield));
        playerMovement.Momentum = Mathf.MoveTowards(playerMovement.Momentum, target, Time.deltaTime * 0.3f);
    }

    /// <summary>Small Vibe pickup: a little health back, up to full.</summary>
    public void AddVibe(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        AudioManager.Instance?.PlayHealthPickupSfx();
        Debug.Log($"Vibe pickup: +{amount} vibe (now {currentHealth})");
    }

    /// <summary>Shield pickup: refills shield (and with it run momentum), up to the level's full shield.</summary>
    public void AddShield(int amount = -1)
    {
        if (amount < 0) amount = shieldPickupAmount;
        hitCounter = Mathf.Min(_maxShield, hitCounter + amount);
        _drainTimer = 0f;
        AudioManager.Instance?.PlaySpeedPickupSfx();
        Debug.Log($"Shield pickup: +{amount} shield (now {hitCounter}), momentum up");
    }
    private void Update()
    {
        UpdateMomentum();

        //Each boost has its own independent timer so two active boosts don't interfere
        if (jumpBoostActive)
        {
            jumpBoostTimer += Time.deltaTime;
            if (jumpBoostTimer >= 5f)
            {
                playerMovement.jumpHeight = 3f;
                jumpBoostTimer = 0f;
                jumpBoostActive = false;
                Debug.Log("Jump boost has worn off!");
            }
        }
        if (speedBoostActive)
        {
            speedBoostTimer += Time.deltaTime;
            if (speedBoostTimer >= 5f)
            {
                playerMovement.forwardSpeed = playerMovement.baseForwardSpeed;
                playerMovement.sidewaySpeed = playerMovement.baseSidewaySpeed;
                speedBoostTimer = 0f;
                speedBoostActive = false;
                Debug.Log("Speed boost has worn off!");
            }
        }
        if (isSneaking)
        {
            sneakTimer += Time.deltaTime;
            if (sneakTimer >= 5f)   // 5 s — timing the sneak now matters
            {
                isSneaking = false;
                sneakTimer = 0f;
                Debug.Log("Sneaking has worn off!");
            }
        }

    }
    // Called both by Unity (if the CapsuleCollider trigger fires directly) and by
    // PickupTriggerProxy on the child "PickupTrigger" object.  The CharacterController
    // suppresses trigger events on the same GameObject, so the proxy child is the
    // reliable path for all pickup / obstacle interactions.
    public void HandleTrigger(Collider other)
    {
        OnTriggerEnter(other);
    }

    public void HandleSolidContact(Collider other)
    {
        if (!GameManager.GameStarted || other == null) return;
        if (PickupCollectUtility.IsPickupConsumed(other)) return;

        var hdo = other.GetComponentInParent<HealthDecreaseObstacle>();
        int rootId = hdo != null ? hdo.gameObject.GetInstanceID() : other.transform.root.GetInstanceID();
        if (_solidHitTimes.TryGetValue(rootId, out float lastHit)
            && Time.time - lastHit < SolidHitCooldown)
            return;

        if (HasTag(other, GameplayCollisionUtility.TagHealthDec))
        {
            _solidHitTimes[rootId] = Time.time;
            HitObstacle(other);
            return;
        }

        if (HasTag(other, GameplayCollisionUtility.TagSlowDown))
        {
            _solidHitTimes[rootId] = Time.time;
            var slow = other.GetComponentInParent<SlowDownObstacle>();
            if (slow != null)
                slow.ApplyToPlayer(playerMovement);
            else
                DestroyPickupRoot(other);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!GameManager.GameStarted) return;
        if (PickupCollectUtility.IsPickupConsumed(other)) return;

        if (HasTag(other, "HealthINC"))
        {
            Debug.Log("Player hit a health pickup and is healed!");
            AudioManager.Instance?.PlayHealthPickupSfx();
            HealthIncrease();
            DestroyPickupRoot(other);
        }
        // Solid obstacles are hit by the body itself (PlayerMovement), not by this wider
        // pickup trigger - it reaches past the body and tripped her before she could jump.
        if (HasTag(other, "HealthDEC") && other.isTrigger
            && (playerMovement == null || !playerMovement.ClearsLowObstacle(other)))
            HitObstacle(other);
        // Guard catch is handled exclusively by EnemyBase.CatchPlayer() to avoid double-trigger.
        if (HasTag(other, "Artifact"))
        {
            if (other.GetComponentInParent<MusicInstrument>() != null)
                return;

            var goal = other.GetComponentInParent<ArtifactGoal>();
            if (goal != null && !PickupCollectUtility.MarkConsumed(goal.gameObject))
                return;
            Debug.Log("Player collected an artifact!");
            Artifact();
            if (goal != null) goal.Collect();
            else DestroyPickupRoot(other);
        }
        if (HasTag(other, "JumpBoost"))
        {
            Debug.Log("Player hit a jump boost and increased their jump height!");
            AudioManager.Instance?.PlayJumpPickupSfx();
            JumpBoost();
            DestroyPickupRoot(other);
        }
        if (HasTag(other, "Sneak"))
        {
            Debug.Log("Player hit a sneak pickup and is now invisible to obstacles for 5 seconds!");
            AudioManager.Instance?.PlaySneakPickupSfx();
            Sneak();
            DestroyPickupRoot(other);
        }
        if (HasTag(other, "Speed"))
        {
            Debug.Log("Player hit a speed boost and is now faster for 5 seconds!");
            AudioManager.Instance?.PlaySpeedPickupSfx();
            SpeedBoost();
            DestroyPickupRoot(other);
        }
        if (HasTag(other, "SlowDown"))
        {
            var slow = other.GetComponentInParent<SlowDownObstacle>();
            if (slow != null)
                slow.ApplyToPlayer(playerMovement);
            else
                DestroyPickupRoot(other);
        }
    }

    // Obstacles already hit this run: each one hurts once, then stays put as solid scenery.
    readonly HashSet<int> _hitObstacles = new HashSet<int>();

    /// <summary>
    /// Bumping an obstacle: one hit of damage per obstacle, its own impact effect, and the
    /// obstacle stays where it is (solid) - the player has to jump or change lanes to get past.
    /// </summary>
    void HitObstacle(Collider other)
    {
        var obstacle = other.GetComponentInParent<HealthDecreaseObstacle>();
        var root = obstacle != null ? obstacle.gameObject : other.gameObject;
        var point = other.ClosestPoint(transform.position + Vector3.up * 0.8f);
        HitObstacleCore(root, obstacle, point, true);
    }

    /// <summary>Caught in a grenade blast: same cost as bumping an obstacle, explosion already shown.</summary>
    public void HitByBlast(HealthDecreaseObstacle source, Vector3 point)
    {
        if (!GameManager.GameStarted || source == null || isSneaking) return;
        HitObstacleCore(source.gameObject, source, point, false);
    }

    void HitObstacleCore(GameObject root, HealthDecreaseObstacle obstacle, Vector3 point, bool playFx)
    {
        if (!_hitObstacles.Add(root.GetInstanceID())) return;

        Debug.Log("Player hit an obstacle: " + (obstacle != null ? obstacle.DisplayName : root.name));
        AudioManager.Instance?.PlayObstacleHitSfx(root);
        if (playFx) ObstacleImpactFX.Play(root, point, transform);
        root.SendMessage("OnPlayerHit", point, SendMessageOptions.DontRequireReceiver);
        if (obstacle != null)
            ApplyObstacleDamage(obstacle);
        else
            HealthDecrease();

        // Still standing? Then the bump costs ground, not the run: trip, fall,
        // get up and dash — while the chasing guard closes in.
        var movement = GetComponent<PlayerMovement>();
        if (movement == null) return;
        if (IsInvulnerable) { movement.PassThrough(root); return; }   // powered up: barge straight through
        if (_deathHandled || currentHealth <= 0 || !GameManager.GameStarted) return;
        bool alreadyDown = movement.IsStumbling;
        movement.Stumble(root);
        if (!alreadyDown)
        {
            var guard = FindAnyObjectByType<EnemyBase>();
            if (guard != null) guard.ClosePursuitGap(this);
        }
    }

    static bool HasTag(Collider col, string tag)
    {
        var t = col.transform;
        while (t != null)
        {
            if (t.CompareTag(tag)) return true;
            t = t.parent;
        }
        return false;
    }

    static void DestroyPickupRoot(Collider col)
    {
        PickupCollectUtility.TryConsumeFromCollider(col);
    }

    public void HealthIncrease()
    {
        // Always clamp to maxHealth so display never shows e.g. 105/100
        currentHealth = Mathf.Min(currentHealth + healthIncreaseAmount, maxHealth);
    }
    public void HealthDecrease()
    {
        if (IsInvulnerable) return;
        RunStats.SetKiller(null);
        PlayerHit?.Invoke();
        ApplyDamage(healthDecreaseAmount);
    }

    /// <summary>Fired whenever an obstacle hit lands (after bubble/invulnerability checks).</summary>
    public static event System.Action PlayerHit;

    [HideInInspector] public float invulnerableUntil;
    public bool IsInvulnerable => Time.time < invulnerableUntil;

    public void ApplyObstacleDamage(HealthDecreaseObstacle obstacle)
    {
        if (obstacle == null) return;
        if (IsInvulnerable) return;
        RunStats.SetKiller(obstacle.DisplayName);
        PlayerHit?.Invoke();
        if (obstacle.instantKill)
        {
            KillPlayer(DeathCause.Obstacle);
            return;
        }

        if (obstacle.vibeDamage > 0)
        {
            currentHealth = Mathf.Max(0, currentHealth - obstacle.vibeDamage);
            if (currentHealth <= 0)
            {
                KillPlayer(DeathCause.Obstacle);
                return;
            }
        }

        ApplyDamage(obstacle.damage);
    }

    /// <summary>
    /// A hit always hurts: it knocks a shield point off (slowing her run) and takes Vibe (health).
    /// While some shield is left it softens the Vibe loss to half; with none, Vibe takes it all.
    /// </summary>
    void ApplyDamage(int amount)
    {
        int vibeLoss = amount;
        if (hitCounter > 0)
        {
            hitCounter = Mathf.Max(0, hitCounter - 1);
            vibeLoss = Mathf.CeilToInt(amount * 0.5f);
            Debug.Log($"Hit: -1 shield ({hitCounter} left), -{vibeLoss} vibe");
        }
        currentHealth = Mathf.Max(0, currentHealth - vibeLoss);
        if (currentHealth <= 0) KillPlayer(DeathCause.Obstacle);
    }

    public void ResetForNewRun()
    {
        currentHealth      = maxHealth;   // maxHealth is already set by LevelBootstrap
        // hitCounter intentionally NOT reset here — LevelBootstrap.Awake() owns that value
        artifactAmount     = 0;
        artifactMoneyTotal = 0;
        _deathHandled      = false;
        _hitObstacles.Clear();
        RunStats.SetKiller(null);
        GetComponent<PlayerMovement>()?.ResetStumble();
    }

    /// <summary>Instant game over — defeat animation then DeathScene.</summary>
    public void KillPlayer(DeathCause cause = DeathCause.Unknown)
    {
        if (_deathHandled) return;
        _deathHandled = true;
        currentHealth = 0;
        hitCounter = 0;
        RunStats.SetDeathCause(cause);
        Death();
    }
    void Artifact()
    {
        CollectArtifact(1);
    }

    public void CollectArtifact(int count = 1, int money = -1)
    {
        if (money < 0)
            money = moneyPerArtifact * count;

        artifactAmount += count;
        artifactMoneyTotal += money;
        AudioManager.Instance?.PlayArtifactPickupSfx();
        Debug.Log($"Artifact collected! Total: {artifactAmount}, Value: ${artifactMoneyTotal}");

        // First artifact collected → guard surges to add tension
        if (artifactAmount == 1)
        {
            var enemy = Object.FindAnyObjectByType<EnemyBase>();
            enemy?.Surge(3f);
        }

        TriggerArtifactDialogue();

        TryTriggerVictory();
    }

    void TriggerArtifactDialogue()
    {
        if (AIDialogueService.Instance == null) return;
        var def = LevelCatalog.Get(LevelProgress.CurrentLevel);
        AIDialogueService.Instance.Speak(new DialogueContext
        {
            speakerName = "Pulse",
            levelName = def.displayName,
            eventType = DialogueEvent.ArtifactCollected,
            detail = $"artifact {artifactAmount}",
            artifactsCollected = artifactAmount,
            artifactsRequired = def.artifactsToWin
        });
    }

    /// <summary>Returns true and loads victory if the player has collected enough artifacts.</summary>
    public bool TryTriggerVictory()
    {
        if (artifactAmount < ArtifactsToWin || currentHealth < 1)
            return false;
        if (GameManager.EndlessRun)
            return false;

        Debug.Log($"Player collected {artifactAmount} artifacts — victory!");
        Victory();
        return true;
    }
    void JumpBoost()
    {
        playerMovement.jumpHeight = 6f;
        jumpBoostTimer = 0f;
        jumpBoostActive = true;
        Debug.Log("Jump boost activated.");
    }
    public void SpeedBoost()
    {
        playerMovement.forwardSpeed = playerMovement.baseForwardSpeed + 4f;   // +4 not +12 — still fast, not disorienting
        playerMovement.sidewaySpeed = playerMovement.baseSidewaySpeed + 1.5f;
        speedBoostTimer = 0f;
        speedBoostActive = true;
        Debug.Log("Speed boost activated.");
    }
    public void Sneak()
    {
        isSneaking = true;
        sneakTimer = 0f;
        Debug.Log("Sneak activated.");
    }
    public void Victory()
    {
        Debug.Log("Victory, player won!");
        // Use GameManager so the victory sequence (fade, etc.) runs cleanly
        if (GameManager.Instance != null)
            GameManager.Instance.TriggerVictory();
        else
            SceneFader.LoadScene("VictoryScene");
    }
    public void Death()
    {
        Debug.Log("Defeat, player lost!");
        RunStats.SaveFrom(this);

        if (GameManager.Instance != null)
            GameManager.Instance.TriggerGameOver();
        else
        {
            RunStats.SaveFrom(this);
            GameOverOverlay.Show("GAME OVER\n\n" + RunStats.GetCauseHeadline());
            SceneFader.LoadScene("DeathScene");
        }
    }
}
