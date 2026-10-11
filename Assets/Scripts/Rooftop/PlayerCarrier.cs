using System.Collections;
using UnityEngine;

/// <summary>
/// Moves the player along a scripted path (zip-line, ramp launch) by pausing
/// PlayerMovement and the CharacterController for the duration.
/// </summary>
public class PlayerCarrier : MonoBehaviour
{
    public static bool IsCarried { get; private set; }

    PlayerMovement _movement;
    CharacterController _controller;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => IsCarried = false;

    public static PlayerCarrier For(GameObject player)
    {
        if (player == null) return null;
        var c = player.GetComponent<PlayerCarrier>();
        return c != null ? c : player.AddComponent<PlayerCarrier>();
    }

    void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        _controller = GetComponent<CharacterController>();
    }

    void OnDisable()
    {
        IsCarried = false;
    }

    float FeetOffset()
    {
        if (_controller == null) return 1f;
        return transform.position.y - _controller.bounds.min.y;
    }

    /// <summary>Carry feet from current position to <paramref name="endFeet"/> along a curve.</summary>
    /// <param name="height">Peak height above the straight line (negative = sag, e.g. zip-line).</param>
    public bool Carry(Vector3 endFeet, float duration, float height, float hangHeight = 0f)
    {
        if (IsCarried || _movement == null || !GameManager.GameStarted) return false;
        StartCoroutine(CarryRoutine(endFeet, duration, height, hangHeight));
        return true;
    }

    IEnumerator CarryRoutine(Vector3 endFeet, float duration, float height, float hangHeight)
    {
        IsCarried = true;
        float offset = FeetOffset();
        Vector3 startFeet = transform.position - Vector3.up * offset;

        _movement.enabled = false;
        if (_controller != null) _controller.enabled = false;

        float t = 0f;
        while (t < 1f)
        {
            if (!GameManager.GameStarted) break;
            t += Time.deltaTime / Mathf.Max(0.05f, duration);
            float k = Mathf.Clamp01(t);
            Vector3 p = Vector3.Lerp(startFeet, endFeet, k);
            float ease = Mathf.Clamp01(k * 4f) * Mathf.Clamp01((1f - k) * 4f);
            p.y += 4f * height * k * (1f - k) + hangHeight * ease;
            transform.position = p + Vector3.up * offset;
            yield return null;
        }

        if (_controller != null) _controller.enabled = true;
        _movement.enabled = true;
        IsCarried = false;
    }
}
