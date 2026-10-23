using UnityEngine;

/// <summary>Starts rolling toward the player when they get within range.</summary>
public class RollingDrum : MonoBehaviour
{
    public Transform body;
    public float triggerDistance = 38f;
    public float speed = 7f;

    Transform _player;
    bool _rolling;

    void Start()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) _player = go.transform;
    }

    void Update()
    {
        if (_player == null || !GameManager.GameStarted) return;
        float ahead = transform.position.z - _player.position.z;
        if (!_rolling && ahead < triggerDistance && ahead > 0f)
        {
            _rolling = true;
            MuseumRun.Instance?.Flash("ROLLING DRUM!", new Color(1f, 0.35f, 0.25f), 1f);
            var lib = FxLibrary.Get();
            if (lib != null) AudioManager.Instance?.PlayObstacleSfxAt(lib.drumHitSound, _player.position + Vector3.forward * 6f, 0.9f);
        }
        if (!_rolling) return;

        transform.position += Vector3.back * speed * Time.deltaTime;
        if (body != null) body.Rotate(Vector3.right, -speed * 60f * Time.deltaTime, Space.World);
        if (ahead < -25f) Destroy(gameObject);
    }
}
