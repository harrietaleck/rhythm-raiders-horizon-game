using UnityEngine;

/// <summary>
/// Short-lived gun fire at a muzzle: a burst of flame tongues that flare forward and shrink away,
/// plus a few wisps of smoke that drift off. Destroys itself.
/// </summary>
public class MuzzleFire : MonoBehaviour
{
    const float Life = 0.14f;
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    Transform[] _flames;
    Vector3[] _sizes;
    Renderer[] _renderers;
    Color[] _colors;
    MaterialPropertyBlock _block;
    float _born;

    public static void Spawn(Transform muzzle)
    {
        var go = new GameObject("Muzzle Fire");
        go.transform.SetPositionAndRotation(muzzle.position, muzzle.rotation);
        go.AddComponent<MuzzleFire>();

        // Smoke wisps, left in the world as the guard turns.
        var smoke = RooftopProps.Glow(new Color(0.55f, 0.55f, 0.58f, 0.35f));
        for (int i = 0; i < 3; i++)
        {
            var puff = RooftopProps.Part(PrimitiveType.Sphere, null, Vector3.zero, Vector3.one * Random.Range(0.18f, 0.3f), smoke, false);
            puff.transform.position = muzzle.position + muzzle.forward * Random.Range(0.1f, 0.5f) + Random.insideUnitSphere * 0.08f;
            puff.AddComponent<SmokePuff>().life = 0.6f;
            Destroy(puff, 0.65f);
        }
    }

    void Start()
    {
        _born = Time.time;
        _block = new MaterialPropertyBlock();
        var hot = RooftopProps.Glow(new Color(1f, 0.95f, 0.7f, 1f));
        var mid = RooftopProps.Glow(new Color(1f, 0.65f, 0.15f, 0.9f));
        var outer = RooftopProps.Glow(new Color(1f, 0.3f, 0.05f, 0.6f));

        // Core flash, a forward flame tongue and side spikes.
        var specs = new (Vector3 pos, Vector3 size, Material mat, Vector3 euler)[]
        {
            (new Vector3(0f, 0f, 0.05f), new Vector3(0.22f, 0.22f, 0.22f), hot, Vector3.zero),
            (new Vector3(0f, 0f, 0.28f), new Vector3(0.2f, 0.2f, 0.55f), mid, Vector3.zero),
            (new Vector3(0f, 0f, 0.45f), new Vector3(0.14f, 0.14f, 0.45f), outer, Vector3.zero),
            (new Vector3(0.09f, 0f, 0.12f), new Vector3(0.07f, 0.07f, 0.3f), mid, new Vector3(0f, 45f, 0f)),
            (new Vector3(-0.09f, 0f, 0.12f), new Vector3(0.07f, 0.07f, 0.3f), mid, new Vector3(0f, -45f, 0f)),
            (new Vector3(0f, 0.09f, 0.12f), new Vector3(0.07f, 0.07f, 0.3f), outer, new Vector3(-45f, 0f, 0f)),
            (new Vector3(0f, -0.09f, 0.12f), new Vector3(0.07f, 0.07f, 0.3f), outer, new Vector3(45f, 0f, 0f)),
        };
        _flames = new Transform[specs.Length];
        _sizes = new Vector3[specs.Length];
        _renderers = new Renderer[specs.Length];
        _colors = new Color[specs.Length];
        for (int i = 0; i < specs.Length; i++)
        {
            var f = RooftopProps.Part(PrimitiveType.Sphere, transform, specs[i].pos, specs[i].size, specs[i].mat, false, specs[i].euler);
            f.transform.localRotation *= Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            _flames[i] = f.transform;
            _sizes[i] = specs[i].size;
            _renderers[i] = f.GetComponent<Renderer>();
            _colors[i] = specs[i].mat.GetColor(BaseColor);
        }
    }

    void Update()
    {
        float k = (Time.time - _born) / Life;
        if (k >= 1f) { Destroy(gameObject); return; }
        // Flare out fast, then shrink and fade.
        float grow = k < 0.3f ? Mathf.Lerp(0.6f, 1.25f, k / 0.3f) : Mathf.Lerp(1.25f, 0.4f, (k - 0.3f) / 0.7f);
        float fade = 1f - k * k;
        for (int i = 0; i < _flames.Length; i++)
        {
            _flames[i].localScale = _sizes[i] * grow;
            var c = _colors[i];
            _block.SetColor(BaseColor, new Color(c.r, c.g, c.b, c.a * fade));
            _renderers[i].SetPropertyBlock(_block);
        }
    }
}
