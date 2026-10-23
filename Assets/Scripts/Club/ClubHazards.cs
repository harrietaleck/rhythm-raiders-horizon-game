using UnityEngine;

/// <summary>
/// Bass Wall: ON for two beats (bright, hurts), a flickering warning, then OFF for two beats
/// (dark, safe). Crossing while it's dark rewards an "IN THE POCKET" bonus.
/// Lanes in a rhythm row run opposite phases so one is always safe - read the beat and pick.
/// </summary>
public class BassWall : MonoBehaviour
{
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    public Renderer wall;
    public Renderer floor;
    public Transform[] rings;
    public int phaseOffsetBeats;
    public float laneHalfWidth = 2.3f;

    Transform _player;
    PickupBase _pickup;
    HealthDecreaseObstacle _damage;
    MaterialPropertyBlock _block;
    float _lastPlayerZ;
    bool _crossed;
    bool _warned;

    public bool IsOn
    {
        get
        {
            int b = ((ClubBeat.Beat + phaseOffsetBeats) % 4 + 4) % 4;
            return b == 0 || b == 1;
        }
    }

    bool Warning
    {
        get
        {
            int b = ((ClubBeat.Beat + phaseOffsetBeats) % 4 + 4) % 4;
            return b == 3 && ClubBeat.Phase > 0.45f;
        }
    }

    void Start()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null)
        {
            _player = go.transform;
            _pickup = go.GetComponent<PickupBase>();
            _lastPlayerZ = _player.position.z;
        }
        _damage = GetComponent<HealthDecreaseObstacle>();
        _block = new MaterialPropertyBlock();
    }

    void Update()
    {
        bool on = IsOn;
        bool warn = !on && Warning && Mathf.Repeat(Time.time * 16f, 1f) < 0.5f;
        float kick = ClubBeat.Kick;

        Color wc = on ? new Color(1f, 0.12f, 0.55f, 0.42f + kick * 0.35f)
                 : warn ? new Color(1f, 0.25f, 0.6f, 0.3f)
                 : new Color(0.3f, 0.6f, 1f, 0.05f);
        Color fc = on ? new Color(1f, 0.12f, 0.55f, 0.3f + kick * 0.2f) : new Color(0.3f, 1f, 0.6f, 0.12f);
        Set(wall, wc);
        Set(floor, fc);
        for (int i = 0; i < rings.Length; i++)
        {
            if (rings[i] == null) continue;
            rings[i].gameObject.SetActive(on);
            if (!on) continue;
            float k = Mathf.Repeat(ClubBeat.Phase + i / (float)rings.Length, 1f);
            rings[i].localPosition = new Vector3(0f, 1.4f, (i % 2 == 0 ? -1f : 1f) * k * 1.4f);
            rings[i].localScale = new Vector3(4.4f * (1f + k * 0.15f), 2.8f * (1f + k * 0.15f), 0.04f);
        }

        if (_player == null || _pickup == null || !GameManager.GameStarted) return;
        float z = transform.position.z;
        float pz = _player.position.z;
        if (!_warned && z - pz < 40f && z - pz > 0f)
        {
            _warned = true;
            ClubRun.Instance?.IntroOnce("bass", "BASS WALL - CROSS WHEN IT GOES DARK", new Color(1f, 0.4f, 0.85f));
        }
        if (!_crossed && _lastPlayerZ < z && pz >= z && Mathf.Abs(_player.position.x - transform.position.x) < laneHalfWidth)
        {
            _crossed = true;
            if (on) { if (_damage != null) _pickup.ApplyObstacleDamage(_damage); AudioManager.Instance?.PlayObstacleHitSfx(); }
            else ClubRun.Instance?.PocketBonus();
        }
        _lastPlayerZ = pz;
    }

    void Set(Renderer r, Color c)
    {
        if (r == null) return;
        r.GetPropertyBlock(_block);
        _block.SetColor(BaseColor, c);
        r.SetPropertyBlock(_block);
    }
}

/// <summary>Slides a laser gate between two lanes, easing in time with the beat.</summary>
public class SweepLaser : MonoBehaviour
{
    public float xA;
    public float xB;
    public float beatsPerSweep = 4f;
    public Transform track;

    float _z;
    Transform _player;
    bool _warned;

    void Start()
    {
        _z = transform.position.z;
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) _player = go.transform;
    }

    void Update()
    {
        float beats = Time.time / ClubBeat.BeatLength;
        float k = 0.5f - 0.5f * Mathf.Cos(Mathf.PI * beats / beatsPerSweep);
        var p = transform.position;
        p.x = Mathf.Lerp(xA, xB, k);
        transform.position = p;
        if (track != null) track.position = new Vector3((xA + xB) * 0.5f, 0.025f, _z);

        if (!_warned && _player != null && GameManager.GameStarted && _z - _player.position.z < 45f)
        {
            _warned = true;
            ClubRun.Instance?.IntroOnce("sweep", "SWEEPING LASER - WATCH ITS TRACK", new Color(1f, 0.35f, 0.3f));
        }
    }
}

/// <summary>Truss that drops into the lane once the runner is close, after a red floor warning.</summary>
public class FallingRig : MonoBehaviour
{
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    public Transform rig;
    public Renderer warning;
    public Collider hitBox;
    public float triggerDistance = 26f;
    public float warnTime = 0.6f;

    Transform _player;
    MaterialPropertyBlock _block;
    int _state;
    float _t;
    float _vel;

    void Start()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) _player = go.transform;
        _block = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (_player == null || !GameManager.GameStarted) return;
        float ahead = transform.position.z - _player.position.z;
        switch (_state)
        {
            case 0:
                if (ahead < triggerDistance && ahead > 0f)
                {
                    _state = 1;
                    _t = 0f;
                    ClubRun.Instance?.Flash("LIGHT RIG FALLING!", new Color(1f, 0.35f, 0.3f), 1.2f);
                }
                break;
            case 1:
                _t += Time.deltaTime;
                SetWarn(Mathf.Repeat(_t * 10f, 1f) < 0.5f ? 0.45f : 0.12f);
                if (_t >= warnTime) _state = 2;
                break;
            case 2:
                _vel += 30f * Time.deltaTime;
                var lp = rig.localPosition;
                lp.y -= _vel * Time.deltaTime;
                if (lp.y <= 0.9f)
                {
                    lp.y = 0.9f;
                    _state = 3;
                    if (hitBox != null) hitBox.enabled = true;
                    SetWarn(0.15f);
                    AudioManager.Instance?.PlayObstacleHitSfx();
                }
                rig.localPosition = lp;
                break;
        }
    }

    void SetWarn(float a)
    {
        if (warning == null) return;
        warning.GetPropertyBlock(_block);
        _block.SetColor(BaseColor, new Color(1f, 0.15f, 0.1f, a));
        warning.SetPropertyBlock(_block);
    }
}


/// <summary>
/// Drops a disco ball from the ceiling when the runner nears, then rolls it down its lane toward
/// them. States: wait → warn (floor ring pulses) → fall (bounce) → roll → gone.
/// </summary>
public class FallingDiscoBall : MonoBehaviour
{
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    public Transform ball;
    public Renderer ring;
    public Collider hitBox;
    public Light sparkle;
    public float radius = 0.95f;
    public float triggerDistance = 34f;
    public float warnTime = 0.6f;
    public float rollSpeed = 6.5f;

    Transform _player;
    MaterialPropertyBlock _block;
    int _state;
    float _t, _vel;
    bool _bounced;

    void Start()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) _player = go.transform;
        _block = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (_player == null || !GameManager.GameStarted) return;
        float ahead = transform.position.z - _player.position.z;
        if (sparkle != null) sparkle.color = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.6f, 1f), 0.6f, 1f);

        switch (_state)
        {
            case 0:
                if (ahead < triggerDistance && ahead > 0f)
                {
                    _state = 1;
                    _t = 0f;
                    ClubRun.Instance?.IntroOnce("discoball", "DISCO BALL DROP - GET OUT OF ITS LANE!", new Color(1f, 0.35f, 0.85f));
                }
                break;
            case 1:
                _t += Time.deltaTime;
                Ring(Mathf.Repeat(_t * 8f, 1f) < 0.5f ? 0.55f : 0.2f);
                if (_t >= warnTime) { _state = 2; _vel = 0f; }
                break;
            case 2:
                _vel += 26f * Time.deltaTime;
                var p = ball.localPosition;
                p.y -= _vel * Time.deltaTime;
                ball.Rotate(Vector3.right, 180f * Time.deltaTime, Space.Self);
                if (p.y <= radius)
                {
                    p.y = radius;
                    if (!_bounced && _vel > 4f)
                    {
                        _bounced = true;
                        _vel = -_vel * 0.3f;   // one small bounce
                        hitBox.enabled = true;
                        Ring(0f);
                        ObstacleImpactFX.Thud(transform.position);
                    }
                    else
                    {
                        _state = 3;
                    }
                }
                ball.localPosition = p;
                break;
            case 3:
                hitBox.enabled = true;
                transform.position += Vector3.back * rollSpeed * Time.deltaTime;
                ball.Rotate(Vector3.right, -rollSpeed / radius * Mathf.Rad2Deg * Time.deltaTime, Space.World);
                CheckRunover();
                if (ahead < -25f) Destroy(gameObject);
                break;
        }
    }

    bool _hit;

    // A moving ball can slip past the character controller's own contact check at closing
    // speed, so test the overlap directly: touching the rolling ball always counts.
    void CheckRunover()
    {
        if (_hit) return;
        var c = ball.position;
        var p = _player.position;
        float dx = p.x - c.x, dz = p.z - c.z;
        if (dx * dx + dz * dz > (radius + 0.55f) * (radius + 0.55f)) return;
        var cc = _player.GetComponent<CharacterController>();
        float feet = cc != null ? p.y - (cc.height * 0.5f - cc.center.y) : p.y - 1f;
        if (feet > c.y + radius * 0.8f) return;   // cleared it
        _hit = true;
        var pickup = _player.GetComponent<PickupBase>();
        var source = GetComponent<HealthDecreaseObstacle>();
        if (pickup == null || source == null) return;
        var point = Vector3.Lerp(c, p, 0.5f);
        ObstacleImpactFX.Play(gameObject, point, _player);
        pickup.HitByBlast(source, point);
    }

    void Ring(float alpha)
    {
        if (ring == null) return;
        ring.GetPropertyBlock(_block);
        _block.SetColor(BaseColor, new Color(1f, 0.2f, 0.8f, alpha));
        ring.SetPropertyBlock(_block);
    }
}
