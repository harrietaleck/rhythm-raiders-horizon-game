using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    //Declare variables
    public Transform player;
    private float speed = 10f;
    private float boostedSpeed = 34f;
    private float speedBoostTime = 10f;
    public float boostDuration = 10f;
    public float timer;
    private PickupBase pickupBase;
    private PlayerMovement playerMovement;
    public float followDistance = 1f;
    private bool hasBoosted = false;
    private bool _caughtPlayer = false;
    private Collider _catchCollider;

    void Start()
    {
        //Assign scripts to be used in the enemy base script
        player = GameObject.FindGameObjectWithTag("Player").transform;
        playerMovement = player.GetComponent<PlayerMovement>();
        pickupBase = player.GetComponent<PickupBase>();
        pickupBase.currentHealth = 100;
        //Check if the scripts are attached 
        if (pickupBase == null)
        {
            Debug.LogError("PickupBase script not found on Player!");
        }

        if (playerMovement == null)
        {
            Debug.LogError("PlayerMovement script not found on Player!");
        }

        if (player == null)
        {
            Debug.LogError("Player not found!");
        }

        _catchCollider = GetComponent<Collider>();
        if (_catchCollider == null)
            _catchCollider = GetComponentInChildren<Collider>();
        if (_catchCollider != null)
            _catchCollider.isTrigger = true;
    }

    void Update()
    {
        //Increment the timer
        timer += Time.deltaTime;

        if (pickupBase != null && playerMovement != null)
        {
            //Check if the timer reached the boost time and if the boost has not been applied yet
            if (pickupBase.isSneaking || playerMovement.forwardSpeed == 20f || pickupBase.artifactAmount >= PickupBase.ArtifactsToWin)
            {
                if (timer >= speedBoostTime && !hasBoosted)
                {
                    speed = boostedSpeed;

                    Debug.Log("Security speed boosted to: " + speed);

                    hasBoosted = true;
                }
            }
        }
        //Return back to normal speed
        if (timer >= boostDuration && hasBoosted)
        {
            speed = 10f;

            hasBoosted = false;

            timer = 0f;

            Debug.Log("Security is in Normal Speed!");
        }
        //Calculate the target position to follow the player 
        Vector3 targetPosition = new Vector3(player.position.x, transform.position.y, player.position.z - followDistance);

        //Move toward player
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        CatchPlayer();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        CatchPlayer();
    }

    private void CatchPlayer()
    {
        if (_caughtPlayer || pickupBase == null) return;
        if (pickupBase.isSneaking) return;

        _caughtPlayer = true;
        Debug.Log("Security caught the player!");
        pickupBase.KillPlayer();
    }
}