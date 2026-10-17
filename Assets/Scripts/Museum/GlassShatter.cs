using UnityEngine;

/// <summary>
/// Glass-break burst: thin glass shards fly out of a display case, tumble, bounce off the floor
/// and fade, with a puff of glittering dust. Self-contained; destroys itself.
/// </summary>
public class GlassShatter : MonoBehaviour
{
    const float Life = 1.4f;
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    struct Shard
    {
        public Transform t;
        public Renderer r;
        public Vector3 vel;
        public Vector3 spin;
        public Color color;
    }

    Shard[] _shards;
    MaterialPropertyBlock _block;
    float _born;

    public static void Spawn(Bounds glass, int count = 34) => Spawn(glass, Color.white, count);

    public static void Spawn(Bounds glass, Color tint, int count = 34)
    {
        var go = new GameObject("Glass Shatter");
        go.transform.position = glass.center;
        var fx = go.AddComponent<GlassShatter>();
        fx.Build(glass, count, tint);
        AudioManager.Instance?.PlayObstacleHitSfx();
    }

    void Build(Bounds glass, int count, Color tint)
    {
        _born = Time.time;
        _block = new MaterialPropertyBlock();
        var shardColor = Color.Lerp(new Color(0.85f, 0.95f, 1f), tint, 0.35f);
        var clear = RooftopProps.Glow(new Color(shardColor.r, shardColor.g, shardColor.b, 0.55f));
        var bright = RooftopProps.Glow(new Color(1f, 1f, 1f, 0.85f));
        _shards = new Shard[count + 12];

        for (int i = 0; i < _shards.Length; i++)
        {
            bool dust = i >= count;
            // Start on the surface of the case so the pieces read as the case breaking apart.
            var local = new Vector3(Random.Range(-1f, 1f) * glass.extents.x, Random.Range(-1f, 1f) * glass.extents.y, Random.Range(-1f, 1f) * glass.extents.z);
            int axis = Random.Range(0, 3);
            local[axis] = Mathf.Sign(local[axis] == 0f ? 1f : local[axis]) * glass.extents[axis];

            var size = dust ? Vector3.one * Random.Range(0.03f, 0.06f)
                            : new Vector3(Random.Range(0.06f, 0.24f), Random.Range(0.08f, 0.3f), 0.012f);
            var part = RooftopProps.Part(dust ? PrimitiveType.Sphere : PrimitiveType.Cube, transform, local, size,
                                         dust ? bright : clear, false, Random.rotation.eulerAngles);

            var outward = new Vector3(local.x, 0f, local.z).normalized;
            if (outward.sqrMagnitude < 0.01f) outward = Random.insideUnitSphere.normalized;
            _shards[i] = new Shard
            {
                t = part.transform,
                r = part.GetComponent<Renderer>(),
                vel = outward * Random.Range(2.5f, 6f) + Vector3.up * Random.Range(1.5f, 4.5f),
                spin = Random.insideUnitSphere * Random.Range(360f, 900f),
                color = (dust ? bright : clear).GetColor(BaseColor),
            };
        }
    }

    void Update()
    {
        float age = Time.time - _born;
        if (age >= Life) { Destroy(gameObject); return; }
        float dt = Time.deltaTime;
        float fade = 1f - Mathf.Clamp01((age - Life * 0.55f) / (Life * 0.45f));

        for (int i = 0; i < _shards.Length; i++)
        {
            var s = _shards[i];
            if (s.t == null) continue;
            s.vel += Physics.gravity * dt;
            var p = s.t.position + s.vel * dt;
            if (p.y < 0.02f)
            {
                // Skitter along the floor.
                p.y = 0.02f;
                s.vel = new Vector3(s.vel.x * 0.5f, -s.vel.y * 0.25f, s.vel.z * 0.5f);
                s.spin *= 0.5f;
            }
            s.t.position = p;
            s.t.Rotate(s.spin * dt, Space.Self);
            _block.SetColor(BaseColor, new Color(s.color.r, s.color.g, s.color.b, s.color.a * fade));
            s.r.SetPropertyBlock(_block);
            _shards[i] = s;
        }
    }
}
