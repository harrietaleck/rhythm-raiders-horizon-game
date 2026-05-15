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
    public int currentHealth;
    public int maxHealth = 100;
    public const int ArtifactsToWin = 2;

    public int artifactAmount;
    public bool isSneaking = false;
    public int hitCounter = 10;

    //Time variable for boost
    private float timer;
    private bool jumpBoostActive = false;
    private bool speedBoostActive = false;
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
        
        //Caalculate the time
        if (jumpBoostActive)
        {
            timer += Time.deltaTime;
            //Reset jump height after 5 seconds
            if (timer >= 5f)
            {
                playerMovement.jumpHeight = 3f;
                //Reset timer
                timer = 0f;
                //Deactivate jump boost
                jumpBoostActive = false;
                Debug.Log("Jump boost has worn off!");
            }
        }
        if (speedBoostActive)
        {
            timer += Time.deltaTime;
            //Reset after 5 seconds
            if (timer >= 5f)
            {
                playerMovement.forwardSpeed = 10f;
                playerMovement.sidewaySpeed = 10f;
                //Reset timer
                timer = 0f;
                //Deactivate boost
                speedBoostActive = false;
                Debug.Log("Speed boost has worn off!");
            }
        }
        if(isSneaking == true)
        {
            timer += Time.deltaTime;
            //Reset after 5 seconds
            if (timer >= 7f)
            {
                isSneaking = false;
                //Reset timer
                timer = 0f;
                Debug.Log("Sneaking has worn off!");
            }
        }

    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HealthINC"))
        {
            Debug.Log("Player hit a health pickup and is healed!");
            HealthIncrease();
            Destroy(other.gameObject);
        }
        if (other.CompareTag("HealthDEC"))
        {
            Debug.Log("Player hit a dangerous obstacle!");
            if (other.GetComponent<HealthDecreaseObstacle>() != null)
                ApplyObstacleDamage(other.GetComponent<HealthDecreaseObstacle>());
            else
                HealthDecrease();
            Destroy(other.gameObject);
        }
        if (other.GetComponentInParent<EnemyBase>() != null)
        {
            if (!isSneaking)
                KillPlayer();
        }
        if (other.CompareTag("Artifact"))
        {
            Debug.Log("Player hit an artifac and has increased the amount!");
            Artifact();
            Destroy(other.gameObject);
        }
        if (other.CompareTag("JumpBoost"))
        {
            Debug.Log("Player hit a jump boost and increased their jump height!");
            JumpBoost();
            Destroy(other.gameObject);
        }
        if(other.CompareTag("Sneak"))
        {
            Debug.Log("Player hit a sneak pickup and is now invisible to obstacles for 5 seconds!");
            Sneak();
            Destroy(other.gameObject);
        }
        if (other.CompareTag("Speed"))
        {
            Debug.Log("Player hit a speed boost and is now faster for 5 seconds!");
            SpeedBoost();
            Destroy(other.gameObject);
        }
    }

    void HealthIncrease()
    {
        //Check if the player health is already at max
        if (currentHealth >= maxHealth)
        {
            //Set the players health to max
            currentHealth = maxHealth;
        }
        else
        {
            //Increase the players health
            currentHealth += healthIncreaseAmount;
        }
    }
    public void HealthDecrease()
    {
        currentHealth -= healthDecreaseAmount;
        if (currentHealth <= 0)
            KillPlayer();
    }

    public void ApplyObstacleDamage(HealthDecreaseObstacle obstacle)
    {
        if (obstacle == null) return;
        if (obstacle.instantKill)
            KillPlayer();
        else
        {
            currentHealth = Mathf.Max(0, currentHealth - obstacle.damage);
            if (currentHealth <= 0)
                KillPlayer();
        }
    }

    /// <summary>Instant game over — defeat animation then DeathScene.</summary>
    public void KillPlayer()
    {
        if (currentHealth <= 0 && hitCounter <= 0) return;
        currentHealth = 0;
        hitCounter = 0;
        Death();
    }
    void Artifact()
    {
        artifactAmount += 1;
        Debug.Log($"Artifact collected! Total: {artifactAmount}");
        TryTriggerVictory();
    }

    /// <summary>Returns true and loads victory if the player has collected enough artifacts.</summary>
    public bool TryTriggerVictory()
    {
        if (artifactAmount < ArtifactsToWin || currentHealth < 1)
            return false;

        Debug.Log($"Player collected {artifactAmount} artifacts — victory!");
        Victory();
        return true;
    }
    void JumpBoost()
    {
        //Increase jump height
        playerMovement.jumpHeight = 6f;
        //Activate timer
        jumpBoostActive = true;
        Debug.Log("Pickup jump timer started: " + timer);
        //Reset timer
        timer = 0f;

    }
    void SpeedBoost()
    {
        //Increase player speed
        playerMovement.forwardSpeed = 20f;
        playerMovement.sidewaySpeed = 10f;
        //Activate timer
        speedBoostActive = true;
        Debug.Log("Pickup speed timer started: " + timer);
        //Reset timer
        timer = 0f;
    }
    void Sneak()
    {
        //Activate sneak
        isSneaking = true;
        //Activate timer
        Debug.Log("Pickup sneak timer started: " + timer);
        //Reset timer
        timer = 0f;
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
        if (GameManager.Instance != null)
            GameManager.Instance.TriggerGameOver();
        else
            SceneFader.LoadScene("DeathScene");
    }
}
