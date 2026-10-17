using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Level 3: streams club bays ahead of the player, places section gateways, and sets the club
/// lighting (near-black room, coloured ambient that shifts per section, purple fog).
/// Also hosts the ClubBeat clock.
/// </summary>
[RequireComponent(typeof(ClubBeat))]
public class ClubHallStream : MonoBehaviour
{
    [System.Serializable]
    public class Section
    {
        public string name;
        public float startDistance;
        public Color ambient;
    }

    public float lookAhead = 150f;
    public float keepBehind = 30f;
    public float startZ = -12f;

    public Section[] sections =
    {
        new Section { name = "THE BACKSTREET",   startDistance = 0f,   ambient = new Color(0.16f, 0.18f, 0.24f) },
        new Section { name = "MAIN DANCE FLOOR", startDistance = 250f, ambient = new Color(0.2f, 0.12f, 0.22f) },
        new Section { name = "BASS VAULT",       startDistance = 550f, ambient = new Color(0.2f, 0.08f, 0.09f) },
    };

    readonly List<GameObject> _bays = new List<GameObject>();
    Transform _player;
    float _origin;
    float _nextZ;
    int _index;
    int _nextSection;

    void Start()
    {
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null) _player = playerGo.transform;
        _origin = _player != null ? _player.position.z : 0f;
        _nextZ = _origin + startZ;
        ApplyClubLighting();
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
            if (bay.transform.position.z + ClubHall.BayLength < _player.position.z - keepBehind)
            {
                Destroy(bay);
                _bays.RemoveAt(i);
            }
        }
        var s = SectionAt(_player.position.z - _origin);
        RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, s.ambient, Time.deltaTime * 0.6f);
    }

    void Fill()
    {
        float target = (_player != null ? _player.position.z : _origin) + lookAhead;
        var skin = HudSkin.Get();
        while (_nextZ < target)
        {
            float d = _nextZ - _origin;
            int section = SectionIndex(d);
            _bays.Add(ClubHall.Bay(transform, _nextZ, _index++, section));
            if (_nextSection < sections.Length && d >= sections[_nextSection].startDistance)
            {
                var s = sections[_nextSection];
                _bays.Add(ClubHall.SectionArch(transform, _nextZ, s.name, _nextSection, skin != null ? skin.font : null));
                _nextSection++;
            }
            _nextZ += ClubHall.BayLength;
        }
    }

    public int SectionIndex(float distance)
    {
        int index = 0;
        for (int i = 0; i < sections.Length; i++)
            if (distance >= sections[i].startDistance) index = i;
        return index;
    }

    public Section SectionAt(float distance) => sections[SectionIndex(distance)];

    void ApplyClubLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = sections[0].ambient;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.05f, 0.02f, 0.08f);
        RenderSettings.fogStartDistance = 40f;
        RenderSettings.fogEndDistance = 140f;

        var sun = GameObject.Find("Directional Light")?.GetComponent<Light>();
        if (sun != null)
        {
            sun.color = new Color(0.5f, 0.4f, 0.9f);
            sun.intensity = 0.18f;
            sun.transform.rotation = Quaternion.Euler(70f, 10f, 0f);
        }
        if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Exposure"))
        {
            RenderSettings.skybox = new Material(RenderSettings.skybox);
            RenderSettings.skybox.SetFloat("_Exposure", 0.05f);
        }
    }
}
