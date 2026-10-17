using UnityEngine;

/// <summary>Subtle flicker on laser beams so they read as live.</summary>
public class LaserFlicker : MonoBehaviour
{
    Renderer[] _beams;
    float _offset;

    void Start()
    {
        _beams = GetComponentsInChildren<Renderer>();
        _offset = Random.value * 10f;
    }

    void Update()
    {
        bool on = Mathf.PerlinNoise(Time.time * 12f + _offset, 0f) > 0.15f;
        for (int i = 2; i < _beams.Length; i++)
            if (_beams[i] != null) _beams[i].enabled = on;
    }
}
