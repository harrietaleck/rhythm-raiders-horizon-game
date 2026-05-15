using System.Collections;
using UnityEngine;

public class SneakPickup : MonoBehaviour
{
    [Tooltip("How long sneaking lasts in seconds")]
    public float duration = 8f;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PickupBase pickup = other.GetComponent<PickupBase>();
        if (pickup == null) return;

        StartCoroutine(ApplySneak(pickup));
        Destroy(gameObject);
    }

    private IEnumerator ApplySneak(PickupBase pickup)
    {
        pickup.isSneaking = true;
        yield return new WaitForSeconds(duration);
        pickup.isSneaking = false;
    }
}
