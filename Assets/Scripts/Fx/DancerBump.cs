using System.Collections;
using UnityEngine;

/// <summary>
/// A Level 3 dancer the runner can bump into: the dancer staggers and a few music notes are
/// knocked out of the runner's bag.
/// </summary>
public class DancerBump : MonoBehaviour
{
    public int notesLost = 3;
    float _next;
    static float _globalNext;

    public void Bump(Transform player)
    {
        if (!GameManager.GameStarted || Time.time < _next || Time.time < _globalNext) return;
        _next = Time.time + 2f;
        _globalNext = Time.time + 0.8f;
        AudioManager.Instance?.PlayObstacleHitSfx();
        if (ClubRun.Instance != null) ClubRun.Instance.LoseNotes(notesLost, player);
        StartCoroutine(Stagger(player));
    }

    IEnumerator Stagger(Transform player)
    {
        // Quick lean away from the runner and back.
        var away = transform.position - player.position;
        away.y = 0f;
        var axis = away.sqrMagnitude > 0.001f ? Vector3.Cross(Vector3.up, away.normalized) : Vector3.right;
        var start = transform.rotation;
        for (float t = 0f; t < 0.45f; t += Time.deltaTime)
        {
            float a = Mathf.Sin(t / 0.45f * Mathf.PI) * 18f;
            transform.rotation = Quaternion.AngleAxis(a, axis) * start;
            yield return null;
        }
        transform.rotation = start;
    }
}
