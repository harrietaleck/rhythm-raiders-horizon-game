using UnityEngine;

/// <summary>
/// A posted guard's bullet: a round glowing ball that flies straight at where the runner was heading.
/// Strafe out of its path to dodge it; a hit costs Vibe and a shield.
/// </summary>
public class GuardBullet : MonoBehaviour
{
    public Vector3 velocity;
    public float life = 1.2f;
    public float hitRadius = 1.0f;
    public HealthDecreaseObstacle damage;

    Transform _player;
    PickupBase _pickup;

    public static void Fire(Vector3 from, Vector3 velocity, HealthDecreaseObstacle damage)
    {
        var go = new GameObject("Guard Bullet");
        go.transform.position = from;
        go.transform.rotation = Quaternion.LookRotation(velocity);
        var b = go.AddComponent<GuardBullet>();
        b.velocity = velocity;
        b.damage = damage;

        // A round, glowing ball: hot core inside a softer halo.
        RooftopProps.Part(PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one * 0.32f,
                          RooftopProps.Glow(new Color(1f, 0.92f, 0.6f, 1f)), false);
        RooftopProps.Part(PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one * 0.6f,
                          RooftopProps.Glow(new Color(1f, 0.55f, 0.15f, 0.4f)), false);

        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.6f, 0.25f);
        light.range = 6f;
        light.intensity = 5f;
    }

    void Start()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null)
        {
            _player = go.transform;
            _pickup = go.GetComponent<PickupBase>();
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        var from = transform.position;
        var to = from + velocity * dt;
        transform.position = to;

        if (_player != null && _pickup != null)
        {
            // Closest point on this frame's path to the runner's body, so fast bullets can't skip through.
            var body = _player.position + Vector3.up * 0.2f;
            var seg = to - from;
            float k = seg.sqrMagnitude > 0f ? Mathf.Clamp01(Vector3.Dot(body - from, seg) / seg.sqrMagnitude) : 0f;
            if ((from + seg * k - body).sqrMagnitude < hitRadius * hitRadius)
            {
                Hit();
                return;
            }
        }

        life -= dt;
        if (life <= 0f) Destroy(gameObject);
    }

    void Hit()
    {
        if (!_pickup.isSneaking && damage != null)
        {
            _pickup.ApplyObstacleDamage(damage);
            AudioManager.Instance?.PlayObstacleHitSfx();
        }
        Destroy(gameObject);
    }
}
