using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Armed museum guard posted beside an exhibit case. Stands his post facing the runner; when the
/// runner comes into range on his side of the hall he locks on with a red aim laser, then fires a
/// bullet (GuardBullet) at where they are heading. Strafe out of the line to dodge.
/// Each hit costs a little Vibe and one shield: far less than the chasing guard, whose catch
/// ends the run. Sneak keeps you unseen; the horn stuns.
/// </summary>
public class PatrolGuard : MonoBehaviour
{
    static readonly List<PatrolGuard> Active = new List<PatrolGuard>();
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    static float _lastWarning = -100f;

    [Tooltip("World x the guard stands at, beside the case.")]
    public float homeX;
    [Tooltip("How far ahead of the guard (toward the runner) he starts shooting.")]
    public float fireRange = 30f;
    [Tooltip("How far across the hall from the guard the runner must be to be targeted.")]
    public float fireWidth = 6f;
    public float aimTime = 0.4f;
    public float fireCooldown = 1.2f;
    public float bulletSpeed = 45f;
    [Header("Grenadier")]
    [Tooltip("Lobs grenades into the runner's lane instead of shooting.")]
    public bool grenadier;
    public float throwCooldown = 2.6f;
    public int maxThrows = 2;
    public float grenadeFlight = 0.9f;
    public float grenadeFuse = 1.1f;
    public Animator animator;
    public Transform model;
    [Tooltip("Light beam from the guard's overhead lamp (pulses with his alert state).")]
    public Renderer ring;
    public Light lamp;

    Transform _player;
    PickupBase _pickup;
    HealthDecreaseObstacle _damage;
    MaterialPropertyBlock _block;
    Transform _muzzle;
    LineRenderer _laser;
    GameObject _flash;
    Vector3 _lastPlayerPos;
    Vector3 _playerVel;
    bool _warned;
    bool _aiming;
    float _aimEnd;
    float _nextShot;
    float _flashUntil;
    float _stunnedUntil;
    float _recoilUntil;
    int _throws;
    bool _winding;
    float _windEnd;
    GameObject _handGrenade;
    static bool _grenadierIntro;

    public static void ResetIntros() => _grenadierIntro = false;

    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);

    /// <summary>Stuns every posted guard within range ahead of the player.</summary>
    public static void StunNear(Vector3 playerPos, float range, float seconds)
    {
        foreach (var g in Active)
            if (g != null && Mathf.Abs(g.transform.position.z - playerPos.z) < range)
            {
                g._stunnedUntil = Time.time + seconds;
                g._aiming = false;
            }
    }

    void Start()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null)
        {
            _player = go.transform;
            _pickup = go.GetComponent<PickupBase>();
            _lastPlayerPos = _player.position;
        }
        _damage = GetComponent<HealthDecreaseObstacle>();
        _block = new MaterialPropertyBlock();
        transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        _nextShot = Time.time + Random.Range(0f, 0.5f);
        if (grenadier) BuildHandGrenade(); else BuildGun();
    }

    float ModelHeight()
    {
        float height = 1.8f;
        if (model == null) return height;
        var rs = model.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return height;
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return Mathf.Max(1f, b.size.y);
    }

    /// <summary>Grenadier: a grenade held in the throwing hand, shown during the wind-up.</summary>
    void BuildHandGrenade()
    {
        var hand = new GameObject("Hand").transform;
        hand.SetParent(transform, false);
        hand.localPosition = new Vector3(0.35f, ModelHeight() * 0.72f, 0.15f);
        _muzzle = hand;
        Grenade.Build(hand, new Vector3(0f, -0.17f, 0f));
        _handGrenade = hand.GetChild(0).gameObject;
        _handGrenade.SetActive(false);
    }

    /// <summary>A compact sidearm held at chest height, a red aim laser and a muzzle flash.</summary>
    void BuildGun()
    {
        float height = 1.8f;
        if (model != null)
        {
            var rs = model.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                height = Mathf.Max(1f, b.size.y);
            }
        }

        // A rifle from the gun pack in the guard's hands (falls back to the simple sidearm).
        var rifleMuzzle = GuardGuns.Attach(model != null ? model : transform, 0.85f, transform, new Vector3(0.25f, height * 0.62f, 0.35f));
        if (rifleMuzzle != null)
        {
            _muzzle = rifleMuzzle;
            BuildFlashAndLaser(rifleMuzzle.gameObject);
            return;
        }

        var gun = RooftopProps.Root("Sidearm", transform, transform.position).transform;
        gun.localPosition = new Vector3(0.28f, height * 0.68f, 0.5f);
        gun.localRotation = Quaternion.identity;
        var metal = RooftopProps.Mat(new Color(0.08f, 0.08f, 0.1f));
        RooftopProps.Part(PrimitiveType.Cube, gun, new Vector3(0f, 0f, 0.12f), new Vector3(0.12f, 0.15f, 0.55f), metal, false);
        RooftopProps.Part(PrimitiveType.Cube, gun, new Vector3(0f, -0.12f, -0.02f), new Vector3(0.08f, 0.2f, 0.1f), metal, false, new Vector3(-15f, 0f, 0f));
        RooftopProps.Part(PrimitiveType.Cube, gun, new Vector3(0f, 0.07f, 0.22f), new Vector3(0.03f, 0.02f, 0.06f),
                          RooftopProps.Glow(new Color(1f, 0.15f, 0.1f, 0.9f)), false);

        _muzzle = new GameObject("Muzzle").transform;
        _muzzle.SetParent(gun, false);
        _muzzle.localPosition = new Vector3(0f, 0f, 0.42f);
        BuildFlashAndLaser(gun.gameObject);
    }

    void BuildFlashAndLaser(GameObject laserHost)
    {
        _flash = new GameObject("MuzzleLight");
        _flash.transform.SetParent(_muzzle, false);
        var fl = _flash.AddComponent<Light>();
        fl.type = LightType.Point;
        fl.color = new Color(1f, 0.6f, 0.2f);
        fl.range = 7f;
        fl.intensity = 8f;
        _flash.SetActive(false);

        _laser = laserHost.AddComponent<LineRenderer>();
        _laser.positionCount = 2;
        _laser.startWidth = 0.07f;
        _laser.endWidth = 0.05f;
        _laser.material = RooftopProps.Glow(new Color(1f, 0.05f, 0.05f, 0.75f));
        _laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _laser.enabled = false;
    }

    void Update()
    {
        if (_player == null) return;
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        _playerVel = Vector3.Lerp(_playerVel, (_player.position - _lastPlayerPos) / dt, 0.3f);
        _lastPlayerPos = _player.position;

        bool stunned = Time.time < _stunnedUntil;
        bool sneaking = _pickup != null && _pickup.isSneaking;
        var p = transform.position;
        float dx = _player.position.x - p.x;
        float dz = _player.position.z - p.z;
        bool inRange = !stunned && !sneaking && GameManager.GameStarted && _pickup != null
                       && dz < 1.5f && dz > -fireRange && Mathf.Abs(dx) < fireWidth;
        if (grenadier)
        {
            // Throws from further out and across the whole hall, but only a couple of times.
            inRange = !stunned && !sneaking && GameManager.GameStarted && _pickup != null
                      && dz < -14f && dz > -34f && Mathf.Abs(dx) < 17f && _throws < maxThrows;
        }

        // Stand the post; track the runner while they are in range.
        var face = inRange || _aiming || _winding ? new Vector3(dx, 0f, dz) : new Vector3(0f, 0f, -1f);
        if (face.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), dt * 12f);
        if (animator != null) animator.SetFloat("Speed", 0f);

        if (grenadier)
        {
            UpdateGrenadier(inRange, stunned, dt);
            UpdateLamp(inRange, stunned, dz);
            return;
        }

        // Aim, then fire.
        if (_aiming && !inRange) _aiming = false;
        if (!_aiming && inRange && Time.time >= _nextShot)
        {
            _aiming = true;
            _aimEnd = Time.time + aimTime;
        }
        if (_aiming && Time.time >= _aimEnd)
        {
            _aiming = false;
            Fire();
        }

        if (_laser != null)
        {
            _laser.enabled = _aiming;
            if (_aiming)
            {
                _laser.SetPosition(0, _muzzle.position);
                _laser.SetPosition(1, _player.position + Vector3.up * 0.3f);
            }
        }
        if (_flash != null && _flash.activeSelf && Time.time > _flashUntil) _flash.SetActive(false);

        if (model != null)
        {
            float recoil = Time.time < _recoilUntil ? -0.18f : 0f;
            model.localPosition = Vector3.Lerp(model.localPosition, new Vector3(0f, model.localPosition.y, recoil), dt * 14f);
        }

        UpdateLamp(inRange, stunned, dz);
    }

    void UpdateLamp(bool inRange, bool stunned, float dz)
    {
        bool sneaking = _pickup != null && _pickup.isSneaking;
        bool alert = _aiming || _winding;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * (alert ? 20f : inRange ? 12f : 4f));
        if (ring != null)
        {
            var c = stunned ? new Color(0.7f, 0.7f, 0.75f, 0.03f)
                  : inRange ? new Color(1f, 0.85f, 0.35f, 0.06f + pulse * 0.04f)
                  : new Color(1f, 0.85f, 0.4f, 0.035f + pulse * 0.01f);
            if (_block == null) _block = new MaterialPropertyBlock();
            ring.GetPropertyBlock(_block);
            _block.SetColor(BaseColor, c);
            ring.SetPropertyBlock(_block);
        }
        if (lamp != null)
        {
            lamp.intensity = stunned ? 3f : inRange ? 18f + pulse * 10f : 12f + pulse * 2f;
            lamp.color = stunned ? new Color(0.75f, 0.78f, 0.85f) : new Color(1f, 0.82f, 0.45f);
        }

        if (!_warned && !stunned && !sneaking && GameManager.GameStarted && -dz < 40f && -dz > 0f)
        {
            _warned = true;
            if (Time.time - _lastWarning > 6f)
            {
                _lastWarning = Time.time;
                if (grenadier)
                {
                    if (!_grenadierIntro)
                    {
                        _grenadierIntro = true;
                        MuseumRun.Instance?.Flash("GRENADIER! WHEN A RED RING LANDS IN YOUR LANE - SWITCH LANES", new Color(1f, 0.45f, 0.25f), 2.2f);
                    }
                }
                else
                    MuseumRun.Instance?.Flash("ARMED GUARD - WATCH FOR THE RED LASER", new Color(1f, 0.35f, 0.3f), 1.8f);
            }
        }
    }

    void UpdateGrenadier(bool inRange, bool stunned, float dt)
    {
        var movement = _pickup != null ? _pickup.GetComponent<PlayerMovement>() : null;
        bool runnerDown = movement != null && movement.IsStumbling;

        if (_winding && (stunned || !GameManager.GameStarted)) _winding = false;
        if (!_winding && inRange && !runnerDown && Time.time >= _nextShot)
        {
            _winding = true;
            _windEnd = Time.time + 0.45f;
        }
        if (_winding && Time.time >= _windEnd)
        {
            _winding = false;
            ThrowGrenade();
        }
        if (_handGrenade != null) _handGrenade.SetActive(_winding);

        if (model != null)
        {
            // Lean back on the wind-up, snap forward on release.
            float lean = _winding ? -14f : Time.time < _recoilUntil ? 12f : 0f;
            var target = Quaternion.Euler(lean, 0f, 0f);
            model.localRotation = Quaternion.Slerp(model.localRotation, target, dt * 12f);
        }
    }

    void ThrowGrenade()
    {
        if (_player == null) return;
        _throws++;
        _nextShot = Time.time + throwCooldown;
        _recoilUntil = Time.time + 0.2f;

        // Land it where the runner will be when it goes off, in their current lane.
        float speed = Mathf.Max(_playerVel.z, 4f);
        float total = grenadeFlight + grenadeFuse;
        float landZ = _player.position.z + speed * total * 0.92f;
        landZ = Mathf.Min(landZ, transform.position.z - 1.5f);
        float floorY = _player.position.y;
        var cc = _player.GetComponent<CharacterController>();
        if (cc != null) floorY = _player.position.y - (cc.height * 0.5f - cc.center.y);
        var land = new Vector3(Mathf.Clamp(_player.position.x, -7.5f, 7.5f), floorY, landZ);
        var from = _muzzle != null ? _muzzle.position : transform.position + Vector3.up * 1.5f;
        GuardGrenade.Throw(from, land, grenadeFlight, grenadeFuse);
    }

    void Fire()
    {
        _nextShot = Time.time + fireCooldown;
        _recoilUntil = Time.time + 0.12f;
        _flashUntil = Time.time + 0.08f;
        if (_flash != null) _flash.SetActive(true);
        MuzzleFire.Spawn(_muzzle);

        // Lead the runner a little so standing still in the line gets you hit; strafing dodges.
        var from = _muzzle.position;
        var target = _player.position + Vector3.up * 0.2f;
        float t = Vector3.Distance(from, target) / bulletSpeed;
        var lead = new Vector3(0f, 0f, _playerVel.z) * t;
        var dir = (target + lead - from).normalized;
        GuardBullet.Fire(from, dir * bulletSpeed, _damage);
        AudioManager.Instance?.PlayGuardShotSfx();
    }
}
