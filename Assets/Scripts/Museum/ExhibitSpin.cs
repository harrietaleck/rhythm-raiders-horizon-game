using UnityEngine;

/// <summary>Slow turntable rotation for instruments in display cases.</summary>
public class ExhibitSpin : MonoBehaviour
{
    public float degreesPerSecond = 25f;

    void Update() => transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);
}
