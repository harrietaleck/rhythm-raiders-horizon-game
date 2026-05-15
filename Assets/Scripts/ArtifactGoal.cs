using UnityEngine;

public class ArtifactGoal : MonoBehaviour
{
    [Tooltip("Rotate the artifact for visual flair")]
    public float rotationSpeed = 60f;

    private bool _triggered = false;

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered || !other.CompareTag("Player")) return;

        PickupBase pickup = other.GetComponent<PickupBase>();
        if (pickup == null || pickup.artifactAmount < PickupBase.ArtifactsToWin)
            return;

        _triggered = true;
        pickup.TryTriggerVictory();
    }
}
