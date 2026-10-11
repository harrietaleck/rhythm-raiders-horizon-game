using UnityEngine;

/// <summary>Billowing smoke sphere that drifts up, expands and fades out over its life.</summary>
public class SmokePuff : MonoBehaviour
{
    public float life = 3f;

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    Vector3 _drift;
    Renderer _renderer;
    MaterialPropertyBlock _block;
    Color _color;
    float _born;

    void Start()
    {
        _drift = new Vector3(Random.Range(-0.6f, 0.6f), Random.Range(0.6f, 1.4f), Random.Range(-0.6f, 0.6f));
        _renderer = GetComponent<Renderer>();
        _block = new MaterialPropertyBlock();
        _color = _renderer != null ? _renderer.sharedMaterial.GetColor(BaseColor) : Color.gray;
        _born = Time.time;
    }

    void Update()
    {
        transform.position += _drift * Time.deltaTime;
        transform.localScale += Vector3.one * (1.2f * Time.deltaTime);

        if (_renderer == null) return;
        float k = Mathf.Clamp01((Time.time - _born) / Mathf.Max(0.1f, life));
        _block.SetColor(BaseColor, new Color(_color.r, _color.g, _color.b, _color.a * (1f - k * k)));
        _renderer.SetPropertyBlock(_block);
    }
}
