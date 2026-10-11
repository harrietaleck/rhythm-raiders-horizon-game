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
    }
    private void Update()
    {
        
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

        int rootId = other.transform.root.GetInstanceID();
        if (_solidHitTimes.TryGetValue(rootId, out float lastHit)
            && Time.time - lastHit < SolidHitCooldown)
            return;

        if (HasTag(other, GameplayCollisionUtility.TagHealthDec))
        {
            _solidHitTimes[rootId] = Time.time;
            Debug.Log("Player bumped a dangerous obstacle!");
            AudioManager.Instance?.PlayObstacleHitSfx();
            var obstacle = other.GetComponentInParent<HealthDecreaseObstacle>();
            if (obstacle != null)
                ApplyObstacleDamage(obstacle);
            else
                HealthDecrease();
            DestroyPickupRoot(other);
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
        if (HasTag(other, "HealthDEC"))
        {
            Debug.Log("Player hit a dangerous obstacle!");
            AudioManager.Instance?.PlayObstacleHitSfx();
            var obstacle = other.GetComponentInParent<HealthDecreaseObstacle>();
            if (obstacle != null)
                ApplyObstacleDamage(obstacle);
            else
                HealthDecrease();
            DestroyPickupRoot(other);
        }
        // Guard catch is handled exclusively by EnemyBase.CatchPlayer() to avoid double-trigger.
        if (HasTag(other, "Artifact"))
        {
            if (other.GetComponentInParent<MusicInstrument>() != null)
                return;

            Debug.Log("Player hit an artifac and has increased the amount!");
            Artifact();
            DestroyPickupRoot(other);
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

    void HealthIncrease()
    {
        // Always clamp to maxHealth so display never shows e.g. 105/100
        currentHealth = Mathf.Min(currentHealth + healthIncreaseAmount, maxHealth);
    }
    public void HealthDecrease()
    {
        if (IsInvulnerable) return;
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
    /// Damage absorption: Shield (hitCounter) absorbs hits first.
    /// Only when Shield reaches 0 does Vibe (currentHealth) take damage.
    /// </summary>
    void ApplyDamage(int amount)
    {
        if (hitCounter > 0)
        {
            hitCounter = Mathf.Max(0, hitCounter - 1);
            Debug.Log($"Shield absorbed hit — Shield remaining: {hitCounter}");
            // Shield fully depleted this hit — spill leftover into Vibe
            if (hitCounter == 0)
            {
                int spillover = amount - 1;   // 1 point consumed the last shield
                if (spillover > 0)
                {
                    currentHealth = Mathf.Max(0, currentHealth - spillover);
                    if (currentHealth <= 0) KillPlayer(DeathCause.Obstacle);
                }
            }
        }
        else
        {
            // No shield left — Vibe takes full damage
            currentHealth = Mathf.Max(0, currentHealth - amount);
            if (currentHealth <= 0) KillPlayer(DeathCause.Obstacle);
        }
    }

    public void ResetForNewRun()
    {
        currentHealth      = maxHealth;   // maxHealth is already set by LevelBootstrap
        // hitCounter intentionally NOT reset here — LevelBootstrap.Awake() owns that value
        artifactAmount     = 0;
        artifactMoneyTotal = 0;
        _deathHandled      = false;
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
    void SpeedBoost()
    {
        playerMovement.forwardSpeed = playerMovement.baseForwardSpeed + 4f;   // +4 not +12 — still fast, not disorienting
        playerMovement.sidewaySpeed = playerMovement.baseSidewaySpeed + 1.5f;
        speedBoostTimer = 0f;
        speedBoostActive = true;
        Debug.Log("Speed boost activated.");
    }
    void Sneak()
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
