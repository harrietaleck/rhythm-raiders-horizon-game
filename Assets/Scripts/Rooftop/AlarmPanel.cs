using UnityEngine;

/// <summary>Pressure plate: stepping on it alerts the guard (surge).</summary>
public class AlarmPanel : MonoBehaviour
{
    public Renderer lamp;
    public float surgeSeconds = 2.5f;

    bool _tripped;

    void Update()
    {
        if (lamp != null)
            lamp.enabled = _tripped ? Mathf.Repeat(Time.time * 6f, 1f) > 0.5f : Mathf.Repeat(Time.time * 1.5f, 1f) > 0.3f;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_tripped || !GameManager.GameStarted || !PlayerColliderUtility.IsPlayer(other)) return;
        if (PlayerCarrier.IsCarried) return;

        var cc = other.GetComponentInParent<CharacterController>();
        if (cc != null && cc.bounds.min.y > transform.position.y + 0.6f) return;

        _tripped = true;
        var pickup = PlayerColliderUtility.GetPickup(other);
        if (pickup != null && pickup.isSneaking) return;

        Object.FindAnyObjectByType<EnemyBase>()?.Surge(surgeSeconds);
        RunDirector.Instance?.Flash("ALARM TRIPPED!", new Color(1f, 0.3f, 0.25f));
        AudioManager.Instance?.PlayObstacleHitSfx();
    }
}
