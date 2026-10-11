using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Level 2: streams night-museum hall bays ahead of the player, places wing archways,
/// and sets the night lighting (moonlight, low ambient, dark sky).
/// </summary>
public class MuseumHallStream : MonoBehaviour
{
    [System.Serializable]
    public class Wing
    {
        public string name;
        public float startDistance;
        public Color accent = Color.white;
        public Color ambient = new Color(0.3f, 0.27f, 0.28f);
    }

    public Texture ceilingTexture;
    public GameObject[] instrumentModels;
    public float lookAhead = 140f;
    public float keepBehind = 30f;
    public float startZ = -12f;

    [Header("Night lighting")]
    public Color moonColor = new Color(0.55f, 0.65f, 1f);
    public float moonIntensity = 0.35f;
    public float skyExposure = 0.25f;

    public Wing[] wings =
    {
        new Wing { name = "GRAND FOYER",        startDistance = 0f,   accent = new Color(1f, 0.8f, 0.4f),  ambient = new Color(0.34f, 0.29f, 0.25f) },
        new Wing { name = "INSTRUMENT GALLERY", startDistance = 250f, accent = new Color(0.45f, 0.75f, 1f), ambient = new Color(0.26f, 0.28f, 0.34f) },
        new Wing { name = "VAULT WING",         startDistance = 550f, accent = new Color(1f, 0.2f, 0.15f),  ambient = new Color(0.3f, 0.18f, 0.18f) },
    };

    readonly List<GameObject> _bays = new List<GameObject>();
    Transform _player;
    float _origin;
    float _nextZ;
    int _index;
    int _nextWing;

    void Start()
    {
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null) _player = playerGo.transform;
        _origin = _player != null ? _player.position.z : 0f;
        _nextZ = _origin + startZ;
        ApplyNight();
        Fill();
    }

    void LateUpdate()
    {
        if (_player == null) return;
        Fill();

        for (int i = _bays.Count - 1; i >= 0; i--)
        {
            var bay = _bays[i];
            if (bay == null) { _bays.RemoveAt(i); continue; }
            if (bay.transform.position.z + MuseumHall.BayLength < _player.position.z - keepBehind)
            {
                Destroy(bay);
                _bays.RemoveAt(i);
            }
        }

        float distance = _player.position.z - _origin;
        var wing = WingAt(distance);
        RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, wing.ambient, Time.deltaTime * 0.6f);
    }

    void Fill()
    {
        float target = (_player != null ? _player.position.z : _origin) + lookAhead;
        var skin = HudSkin.Get();
        while (_nextZ < target)
        {
            _bays.Add(MuseumHall.Bay(transform, _nextZ, _index++, ceilingTexture, instrumentModels));
            if (_nextWing < wings.Length && _nextZ - _origin >= wings[_nextWing].startDistance)
            {
                var w = wings[_nextWing++];
                _bays.Add(MuseumHall.WingArch(transform, _nextZ, w.name, w.accent, skin != null ? skin.font : null));
            }
            _nextZ += MuseumHall.BayLength;
        }
    }

    public Wing WingAt(float distance)
    {
        var wing = wings[0];
        foreach (var w in wings)
            if (distance >= w.startDistance) wing = w;
        return wing;
    }

    void ApplyNight()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = wings.Length > 0 ? wings[0].ambient : new Color(0.3f, 0.27f, 0.28f);
        RenderSettings.fogColor = new Color(0.06f, 0.05f, 0.08f);

        var sun = GameObject.Find("Directional Light")?.GetComponent<Light>();
        if (sun != null)
        {
            sun.color = moonColor;
            sun.intensity = moonIntensity;
            sun.transform.rotation = Quaternion.Euler(62f, 20f, 0f);
        }

        if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Exposure"))
        {
            RenderSettings.skybox = new Material(RenderSettings.skybox);
            RenderSettings.skybox.SetFloat("_Exposure", skyExposure);
        }
    }
}
