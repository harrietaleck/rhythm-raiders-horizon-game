using UnityEngine;

/// <summary>Test helper: presses jump when the runner is a fair distance from the target, then logs the outcome.</summary>
public class JumpTester : MonoBehaviour
{
    public PlayerMovement player;
    public Transform target;
    public float leadSeconds = 0.3f;
    bool _jumped;
    float _end;

    void Update()
    {
        if (player == null || target == null) { Destroy(gameObject); return; }
        float speed = Mathf.Max(1f, player.forwardSpeed * player.Momentum);
        float dist = target.position.z - player.transform.position.z;
        if (!_jumped && dist <= speed * leadSeconds + 0.5f)
        {
            _jumped = true;
            player.RequestJump();
            _end = Time.time + 2f;
            Debug.Log($"[LaserJumpTest] jump pressed at {dist:F2} m, stumbling={player.IsStumbling}");
        }
        if (_jumped && Time.time > _end)
        {
            Debug.Log($"[LaserJumpTest] result: stumbling={player.IsStumbling}");
            Destroy(gameObject);
        }
    }
}
