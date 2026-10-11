using UnityEngine;

/// <summary>Pendulum swing across the street (crane hook).</summary>
public class SwingingHazard : MonoBehaviour
{
    public Transform pivot;
    public float angle = 38f;
    public float period = 2.6f;

    float _phase;

    void Start()
    {
        _phase = Random.value * period;
    }

    void Update()
    {
        if (pivot == null) return;
        float a = Mathf.Sin((Time.time + _phase) / period * Mathf.PI * 2f) * angle;
        pivot.localRotation = Quaternion.Euler(0f, 0f, a);
    }
}
