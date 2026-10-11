using UnityEngine;

/// <summary>Pigeons that burst into the air when the player gets close.</summary>
public class PigeonFlock : MonoBehaviour
{
    public float triggerDistance = 14f;

    Transform _player;
    Vector3[] _dirs;
    bool _flying;
    float _t;

    void Start()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) _player = go.transform;
        _dirs = new Vector3[transform.childCount];
        for (int i = 0; i < _dirs.Length; i++)
            _dirs[i] = new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(1.5f, 2.5f), Random.Range(0.2f, 1.2f)).normalized;
    }

    void Update()
    {
        if (_player == null) return;
        if (!_flying)
        {
            float dz = transform.position.z - _player.position.z;
            if (dz < triggerDistance && dz > -2f) _flying = true;
            else return;
        }

        _t += Time.deltaTime;
        for (int i = 0; i < transform.childCount && i < _dirs.Length; i++)
        {
            var bird = transform.GetChild(i);
            bird.position += _dirs[i] * (6f + _t * 8f) * Time.deltaTime;
            float flap = Mathf.Sin(Time.time * 30f + i) * 0.15f;
            bird.localScale = new Vector3(0.35f + flap, 0.28f, 0.45f);
        }
        if (_t > 3f) Destroy(gameObject);
    }
}
