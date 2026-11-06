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
        if (!PlayerColliderUtility.IsPlayer(other)) return;
        ApplyToPlayer(PlayerColliderUtility.GetMovement(other));
    }

    public void ApplyToPlayer(PlayerMovement movement)
    {
        if (movement == null) return;

        var col = GetComponent<Collider>();
        if (col != null && !col.enabled) return;

        AudioManager.Instance?.PlaySlowDownObstacleSfx();

        if (col != null) col.enabled = false;
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;   // model and its flame

        StartCoroutine(ApplySlow(movement));
    }

    private IEnumerator ApplySlow(PlayerMovement movement)
    {
        movement.forwardSpeed = Mathf.Max(1f, movement.forwardSpeed - slowAmount);
        yield return new WaitForSeconds(duration);
        movement.forwardSpeed += slowAmount;
        Destroy(gameObject);
    }
}
