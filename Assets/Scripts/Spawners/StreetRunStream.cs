using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Repeats the Level 1 street segment ahead of the player and destroys passed copies.
/// </summary>
public class StreetRunStream : MonoBehaviour
{
    public GameObject segment;
    public float segmentLength = 60f;
    public float segmentStartZ;
    public float lookAhead = 150f;
    public float keepBehind = 40f;

    readonly List<GameObject> _segments = new List<GameObject>();
    GameObject _template;
    Transform _player;
    Vector3 _origin;
    float _startZ;
    int _nextIndex = 1;

    void Start()
    {
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null) _player = playerGo.transform;
        if (segment == null || segmentLength < 1f) return;

        _origin = segment.transform.position;
        _startZ = segmentStartZ;
        _template = Instantiate(segment, transform);
        _template.name = "StreetSegTemplate";
        _template.SetActive(false);
        _segments.Add(segment);
        Fill();
    }

    void LateUpdate()
    {
        if (_template == null || _player == null) return;
        Fill();
        Prune();
    }

    void Fill()
    {
        float target = (_player != null ? _player.position.z : 0f) + lookAhead;
        int guard = 0;
        while (_startZ + _nextIndex * segmentLength < target && guard < 16)
        {
            var copy = Instantiate(_template, transform);
            copy.name = "StreetSeg_" + _nextIndex;
            copy.transform.position = _origin + Vector3.forward * (_nextIndex * segmentLength);
            copy.SetActive(true);
            _segments.Add(copy);
            _nextIndex++;
            guard++;
        }
    }

    void Prune()
    {
        float limit = _player.position.z - keepBehind;
        for (int i = _segments.Count - 1; i >= 0; i--)
        {
            var seg = _segments[i];
            if (seg == null)
            {
                _segments.RemoveAt(i);
                continue;
            }
            if (_segments.Count <= 2) break;

            float endZ = _startZ + (seg.transform.position.z - _origin.z) + segmentLength;
            if (endZ < limit)
            {
                Destroy(seg);
                _segments.RemoveAt(i);
            }
        }
    }
}
