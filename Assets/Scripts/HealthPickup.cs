using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [Tooltip("How much health this pickup restores")]
    public int healAmount = 20;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PickupBase pickup = other.GetComponent<PickupBase>();
        if (pickup == null) return;

        pickup.currentHealth = Mathf.Min(pickup.currentHealth + healAmount, pickup.maxHealth);
        Destroy(gameObject);
    }
}
