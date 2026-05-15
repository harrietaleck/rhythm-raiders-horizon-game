using System.Collections;
using UnityEngine;

public class SlowDownObstacle : MonoBehaviour
{
    [Tooltip("How much to reduce speed by")]
    public float slowAmount = 4f;
    [Tooltip("How long the slow lasts in seconds")]
    public float duration = 3f;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerMovement movement = other.GetComponent<PlayerMovement>();
        if (movement == null) return;

        StartCoroutine(ApplySlow(movement));
        Destroy(gameObject);
    }

    private IEnumerator ApplySlow(PlayerMovement movement)
    {
        movement.forwardSpeed = Mathf.Max(1f, movement.forwardSpeed - slowAmount);
        yield return new WaitForSeconds(duration);
        movement.forwardSpeed += slowAmount;
    }
}
