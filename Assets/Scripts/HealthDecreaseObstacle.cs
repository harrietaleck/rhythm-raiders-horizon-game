using UnityEngine;

public class HealthDecreaseObstacle : MonoBehaviour
{
    [Tooltip("How much health to take away (ignored when instantKill is true)")]
    public int damage = 15;

    [Tooltip("High-danger obstacles end the run immediately.")]
    public bool instantKill = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PickupBase pickup = other.GetComponent<PickupBase>();
        if (pickup == null) return;

        if (instantKill)
        {
            pickup.KillPlayer();
        }
        else
        {
            pickup.currentHealth = Mathf.Max(0, pickup.currentHealth - damage);
            if (pickup.currentHealth <= 0)
                pickup.KillPlayer();
        }

        Destroy(gameObject);
    }
}
