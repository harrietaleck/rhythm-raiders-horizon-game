using System.Collections;
using UnityEngine;

public class JumpBoostPickup : MonoBehaviour
{
    [Tooltip("How much extra jump height to add")]
    public float jumpBonus = 5f;
    [Tooltip("How long the boost lasts in seconds")]
    public float duration = 5f;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerMovement movement = other.GetComponent<PlayerMovement>();
        if (movement == null) return;

        StartCoroutine(ApplyBoost(movement));
        Destroy(gameObject);
    }

    private IEnumerator ApplyBoost(PlayerMovement movement)
    {
        movement.jumpHeight += jumpBonus;
        yield return new WaitForSeconds(duration);
        movement.jumpHeight -= jumpBonus;
    }
}
