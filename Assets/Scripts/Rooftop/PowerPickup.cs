using UnityEngine;

/// <summary>Rare rooftop power-ups: magnet, shield bubble, smoke bomb.</summary>
public class PowerPickup : MonoBehaviour
{
    public enum Kind { Magnet, Bubble, SmokeBomb }

    public Kind kind;
    public Transform visual;

    bool _taken;
    float _baseY;

    void Start()
    {
        _baseY = transform.position.y;
    }

    void Update()
    {
        if (visual != null)
            visual.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);
        var p = transform.position;
        p.y = _baseY + Mathf.Sin(Time.time * 3f) * 0.15f;
        transform.position = p;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_taken || !GameManager.GameStarted || !PlayerColliderUtility.IsPlayer(other)) return;
        _taken = true;
        var pickup = PlayerColliderUtility.GetPickup(other);
        if (kind == Kind.Bubble && pickup != null) pickup.AddShield();   // shield = run momentum
        RunDirector.Instance?.ApplyPower(kind, pickup);
        AudioManager.Instance?.PlaySpeedPickupSfx();
        Destroy(gameObject);
    }
}
