using UnityEngine;

/// <summary>Flings the player forward in a high arc — clears gaps and obstacles.</summary>
public class LaunchRamp : MonoBehaviour
{
    public float distance = 16f;
    public float height = 4f;

    bool _used;

    void OnTriggerEnter(Collider other)
    {
        if (_used || !GameManager.GameStarted || !PlayerColliderUtility.IsPlayer(other)) return;
        var player = other.GetComponentInParent<PlayerMovement>();
        if (player == null) return;

        var cc = player.GetComponent<CharacterController>();
        float feetY = cc != null ? cc.bounds.min.y : player.transform.position.y - 1f;
        Vector3 end = new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z + distance);
        float duration = distance / Mathf.Max(8f, player.forwardSpeed * 1.3f);

        if (PlayerCarrier.For(player.gameObject).Carry(end, duration, height + (feetY - transform.position.y)))
        {
            _used = true;
            RunDirector.Instance?.OnStunt("BIG AIR!");
            AudioManager.Instance?.PlayJumpPickupSfx();
        }
    }
}
