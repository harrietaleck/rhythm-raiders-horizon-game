using UnityEngine;

/// <summary>Billowing smoke sphere that drifts up and expands.</summary>
public class SmokePuff : MonoBehaviour
{
    Vector3 _drift;

    void Start()
    {
        _drift = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0.6f, 1.2f), Random.Range(-0.4f, 0.4f));
    }

    void Update()
    {
        transform.position += _drift * Time.deltaTime;
        transform.localScale += Vector3.one * (0.8f * Time.deltaTime);
    }
}
