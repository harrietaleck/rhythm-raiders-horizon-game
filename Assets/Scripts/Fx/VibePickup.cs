using UnityEngine;

/// <summary>Small Vibe pickup: +5 Vibe (health) when the runner passes through it.</summary>
public class VibePickup : MonoBehaviour
{
    public int amount = 5;
    bool _taken;
    float _baseY;

    void Start() => _baseY = transform.position.y;

    void Update()
    {
        var p = transform.position;
        p.y = _baseY + Mathf.Sin(Time.time * 3f + p.z) * 0.12f;
        transform.position = p;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_taken || !GameManager.GameStarted || !PlayerColliderUtility.IsPlayer(other)) return;
        var pickup = PlayerColliderUtility.GetPickup(other);
        if (pickup == null) return;
        _taken = true;
        pickup.AddVibe(amount);
        Destroy(gameObject);
    }
}
