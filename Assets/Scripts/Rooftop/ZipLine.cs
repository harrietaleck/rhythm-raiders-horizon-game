using UnityEngine;

/// <summary>Grab the handle by running into it; rides the cable to the far post.</summary>
public class ZipLine : MonoBehaviour
{
    public Vector3 endFeet;
    public float speed = 16f;

    bool _used;

    void OnTriggerEnter(Collider other)
    {
        if (_used || !GameManager.GameStarted || !PlayerColliderUtility.IsPlayer(other)) return;
        var player = other.GetComponentInParent<PlayerMovement>();
        if (player == null) return;

        float duration = Vector3.Distance(player.transform.position, endFeet) / speed;
        if (PlayerCarrier.For(player.gameObject).Carry(endFeet, duration, -0.5f, 1.6f))
        {
            _used = true;
            RunDirector.Instance?.OnStunt("ZIP-LINE!");
            AudioManager.Instance?.PlayJumpPickupSfx();
        }
    }
}
