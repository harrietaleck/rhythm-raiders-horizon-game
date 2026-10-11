using UnityEngine;

/// <summary>
/// Helicopter hovering over the street with a beam sweeping across the lanes.
/// Getting caught in the light (without sneak) makes the guard surge.
/// </summary>
public class Searchlight : MonoBehaviour
{
    public float sweepWidth = 6f;
    public float sweepSpeed = 0.55f;
    public float spotRadius = 1.7f;
    public float spotLength = 3f;
    public float lifetime = 14f;

    Transform _beam, _spot, _heli, _blink;
    Transform _player;
    float _roofY;
    float _age;
    bool _spotted;

    public static GameObject Spawn(float worldZ, float playerY)
    {
        var go = new GameObject("Searchlight");
        go.transform.position = new Vector3(0f, 0f, worldZ);
        var s = go.AddComponent<Searchlight>();
        s.Build(playerY);
        return go;
    }

    void Build(float playerY)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        _player = player != null ? player.transform : null;
        var cc = player != null ? player.GetComponent<CharacterController>() : null;
        _roofY = cc != null ? cc.bounds.min.y : playerY - 1f;

        var heli = new GameObject("Helicopter");
        heli.transform.SetParent(transform, false);
        heli.transform.localPosition = new Vector3(0f, _roofY + 16f, 6f);
        _heli = heli.transform;
        var body = RooftopProps.Mat(new Color(0.08f, 0.08f, 0.1f));
        RooftopProps.Part(PrimitiveType.Capsule, _heli, Vector3.zero, new Vector3(2.2f, 1.4f, 2.2f), body, false, new Vector3(90f, 0f, 0f));
        RooftopProps.Part(PrimitiveType.Cube, _heli, new Vector3(0f, 0.2f, 3f), new Vector3(0.35f, 0.35f, 3.5f), body, false);
        RooftopProps.Part(PrimitiveType.Cube, _heli, new Vector3(0f, 1.1f, 0f), new Vector3(7f, 0.05f, 0.3f), body, false);
        RooftopProps.Part(PrimitiveType.Cube, _heli, new Vector3(0f, 1.1f, 0f), new Vector3(0.3f, 0.05f, 7f), body, false);
        _blink = RooftopProps.Part(PrimitiveType.Sphere, _heli, new Vector3(0f, -0.8f, 0f), Vector3.one * 0.3f,
                                   RooftopProps.Mat(new Color(1f, 0.1f, 0.1f), 5f), false).transform;

        _beam = RooftopProps.Part(PrimitiveType.Cylinder, transform, Vector3.zero, Vector3.one,
                                  RooftopProps.Glow(new Color(1f, 0.92f, 0.6f, 0.16f)), false).transform;
        _spot = RooftopProps.Part(PrimitiveType.Cylinder, transform, new Vector3(0f, _roofY + 0.04f, 0f),
                                  new Vector3(spotRadius * 2f, 0.02f, spotLength * 2f),
                                  RooftopProps.Glow(new Color(1f, 0.9f, 0.55f, 0.5f)), false).transform;

        var light = _spot.gameObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.88f, 0.55f);
        light.range = 9f;
        light.intensity = 6f;
    }

    void Update()
    {
        _age += Time.deltaTime;
        if (_age > lifetime || (_player != null && _player.position.z > transform.position.z + 25f))
        {
            Destroy(gameObject);
            return;
        }

        float x = Mathf.Sin(_age * sweepSpeed * Mathf.PI) * sweepWidth;
        _spot.localPosition = new Vector3(x, _roofY + 0.04f, 0f);
        _heli.localPosition = new Vector3(Mathf.Sin(_age * 0.7f) * 1.5f, _roofY + 16f + Mathf.Sin(_age * 2f) * 0.3f, 6f);
        _heli.GetChild(2).localRotation = Quaternion.Euler(0f, _age * 900f, 0f);
        _heli.GetChild(3).localRotation = Quaternion.Euler(0f, _age * 900f, 0f);
        _blink.gameObject.SetActive(Mathf.Repeat(_age * 2f, 1f) > 0.5f);

        Vector3 from = _heli.position + Vector3.down * 0.8f;
        Vector3 to = _spot.position;
        Vector3 mid = (from + to) * 0.5f;
        float len = Vector3.Distance(from, to);
        _beam.position = mid;
        _beam.up = (from - to).normalized;
        _beam.localScale = new Vector3(spotRadius * 1.6f, len * 0.5f, spotRadius * 1.6f);

        if (_spotted || _player == null || !GameManager.GameStarted || PlayerCarrier.IsCarried) return;
        Vector3 p = _player.position;
        if (Mathf.Abs(p.x - _spot.position.x) < spotRadius && Mathf.Abs(p.z - _spot.position.z) < spotLength)
        {
            _spotted = true;
            RunDirector.Instance?.OnSpotted();
        }
    }
}
