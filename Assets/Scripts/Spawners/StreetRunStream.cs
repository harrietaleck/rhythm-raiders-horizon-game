using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Repeats the Level 1 street segment ahead of the player and destroys passed copies.
/// Each copy is filled with pickups (advantages) and obstacles (disadvantages);
/// Beat Fragments appear at fixed distances down the street.
/// </summary>
public class StreetRunStream : MonoBehaviour
{
    public GameObject segment;
    public float segmentLength = 60f;
    public float segmentStartZ;
    public float lookAhead = 150f;
    public float keepBehind = 40f;

    [Header("Content")]
    public GameObject[] advantages;
    public GameObject[] hazards;
    public GameObject fragmentTemplate;
    public float[] fragmentDistances = { 300f, 600f };
    public float[] lanes = { -4f, 0f, 4f };
    public float rowSpacing = 14f;
    public float safeStart = 35f;
    [Range(0f, 1f)] public float hazardChanceStart = 0.45f;
    [Range(0f, 1f)] public float hazardChanceMax = 0.75f;
    public float rampDistance = 700f;

    [Header("Gap dressing")]
    [Tooltip("Steel bar panel (PixelDrift Metro Gate_02d) used for the gantry over roof gaps.")]
    public GameObject steelBarPrefab;

    readonly List<GameObject> _segments = new List<GameObject>();
    GameObject _template;
    Transform _player;
    Vector3 _origin;
    float _startZ;
    float _surfaceY;
    int _nextIndex = 1;

    void Start()
    {
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null) _player = playerGo.transform;
        if (segment == null || segmentLength < 1f) return;

        _origin = segment.transform.position;
        _startZ = segmentStartZ;
        _surfaceY = MeasureSurface(segment);
        RooftopProps.SteelBars = steelBarPrefab;

        HideTemplates(advantages);
        HideTemplates(hazards);
        if (fragmentTemplate != null) fragmentTemplate.SetActive(false);

        _template = Instantiate(segment, transform);
        _template.name = "StreetSegTemplate";
        _template.SetActive(false);
        _segments.Add(segment);
        Populate(segment, 0);
        Fill();
    }

    void LateUpdate()
    {
        if (_template == null || _player == null) return;
        Fill();
        UpdateFragments();
        Prune();
    }

    void Fill()
    {
        float target = (_player != null ? _player.position.z : 0f) + lookAhead;
        int guard = 0;
        while (_startZ + _nextIndex * segmentLength < target && guard < 16)
        {
            var copy = Instantiate(_template, transform);
            copy.name = "StreetSeg_" + _nextIndex;
            copy.transform.position = _origin + Vector3.forward * (_nextIndex * segmentLength);
            copy.SetActive(true);
            _segments.Add(copy);
            Populate(copy, _nextIndex);
            _nextIndex++;
            guard++;
        }
    }

    void Populate(GameObject seg, int index)
    {
        float segStart = _startZ + index * segmentLength;
        float segEnd = segStart + segmentLength;
        if (_nextSpecialZ <= 0f) _nextSpecialZ = _startZ + firstSpecial;

        if (fragmentTemplate != null)
        {
            if (_fragmentTargets == null) InitFragments();
            for (int i = 0; i < _fragmentTargets.Count; i++)
            {
                float z = _fragmentTargets[i];
                if (_fragmentObjects[i] != null || z < segStart || z >= segEnd) continue;
                _fragmentObjects[i] = PlaceReturn(fragmentTemplate, seg, PickLane(), z, 0.8f);
            }
        }

        float first = index == 0 ? _startZ + safeStart : segStart + rowSpacing * 0.5f;
        float clearUntil = float.MinValue;
        for (float z = first; z < segEnd - 2f; z += rowSpacing)
        {
            if (z < clearUntil || NearFragment(z)) continue;

            float distance = z - _startZ;
            int district = RunDirector.Instance != null ? RunDirector.Instance.DistrictAt(distance) : 0;

            if (z >= _nextSpecialZ && z + 14f < segEnd)
            {
                clearUntil = z + PlaceSpecial(seg.transform, z, district);
                _nextSpecialZ = z + Mathf.Lerp(150f, 85f, Mathf.Clamp01(distance / 900f)) + Random.Range(0f, 40f);
                continue;
            }

            float t = Mathf.Clamp01(distance / rampDistance);
            float hazardChance = Mathf.Lerp(hazardChanceStart, hazardChanceMax, t);
            float rowZ = z + Random.Range(-2f, 2f);
            float laneX = PickLane();

            if (Random.value < hazardChance)
            {
                PlaceHazard(seg, laneX, rowZ, district);
                RunDirector.Instance?.RegisterHazardRow(rowZ);

                float other = PickLane();
                bool second = t > 0.4f && Random.value < t * 0.5f && !Mathf.Approximately(other, laneX);
                if (second)
                    PlaceHazard(seg, other, rowZ, district);

                // Reward the free lane with notes.
                float free = FreeLane(laneX, second ? other : laneX);
                if (Random.value < 0.5f)
                    NoteLine(seg.transform, free, rowZ - 4f, 4);
            }
            else
            {
                PlaceAdvantage(seg, laneX, rowZ);
                if (Random.value < 0.12f)
                    RooftopProps.Pigeons(seg.transform, new Vector3(FreeLane(laneX, laneX), _surfaceY, rowZ + 5f));
            }
        }
    }

    [Header("Set pieces")]
    public float firstSpecial = 110f;
    float _nextSpecialZ;

    void PlaceHazard(GameObject seg, float x, float z, int district)
    {
        var pos = new Vector3(x, _surfaceY, z);
        var parent = seg.transform;
        float r = Random.value;
        switch (district)
        {
            case 0:
                if (r < 0.3f) Place(PickFrom(hazards), seg, x, z, 0f);
                else if (r < 0.6f) RooftopProps.AcUnit(parent, pos);
                else if (r < 0.8f) RooftopProps.LaundryLine(parent, pos);
                else RooftopProps.WaterTank(parent, pos);
                break;
            case 1:
                if (r < 0.2f) Place(PickFrom(hazards), seg, x, z, 0f);
                else if (r < 0.6f) RooftopProps.NeonSign(parent, pos);
                else if (r < 0.8f) RooftopProps.LaundryLine(parent, pos);
                else RooftopProps.AcUnit(parent, pos);
                break;
            default:
                if (r < 0.2f) Place(PickFrom(hazards), seg, x, z, 0f);
                else if (r < 0.55f) RooftopProps.AcUnit(parent, pos);
                else if (r < 0.85f) RooftopProps.WaterTank(parent, pos);
                else RooftopProps.NeonSign(parent, pos);
                break;
        }
    }

    void PlaceAdvantage(GameObject seg, float x, float z)
    {
        float r = Random.value;
        if (r < 0.5f)
            NoteLine(seg.transform, x, z - 4f, 5);
        else if (r < 0.86f)
            Place(PickFrom(advantages), seg, x, z, 0.6f);
        else
        {
            var kind = (PowerPickup.Kind)Random.Range(0, 3);
            RooftopProps.Power(seg.transform, new Vector3(x, _surfaceY + 1.1f, z), kind);
        }
    }

    /// <summary>Places a set piece and returns how far ahead rows must stay clear.</summary>
    float PlaceSpecial(Transform parent, float z, int district)
    {
        float r = Random.value;
        float zipLane = PickLane();
        switch (district)
        {
            case 0:
                if (r < 0.45f) return GapSet(parent, z, 4f, true, zipLane);
                if (r < 0.7f) return RampSet(parent, z, 4f);
                RooftopProps.WetPatch(parent, new Vector3(0f, _surfaceY, z + 3.5f), 12f);
                NoteLine(parent, PickLane(), z, 3);
                return 10f;
            case 1:
                if (r < 0.3f) return GapSet(parent, z, 4.5f, true, zipLane);
                if (r < 0.55f)
                {
                    for (int i = 0; i < lanes.Length; i++)
                        RooftopProps.SteamVent(parent, new Vector3(lanes[i], _surfaceY, z + i * 3f), i * 0.6f);
                    return 12f;
                }
                if (r < 0.75f)
                {
                    float safe = PickLane();
                    foreach (float lane in lanes)
                        if (!Mathf.Approximately(lane, safe))
                            RooftopProps.AlarmPanel(parent, new Vector3(lane, _surfaceY, z + 2f));
                    NoteLine(parent, safe, z - 2f, 4);
                    return 8f;
                }
                return CrumbleSet(parent, z);
            default:
                if (r < 0.3f)
                {
                    RooftopProps.CraneHook(parent, new Vector3(0f, _surfaceY, z + 4f));
                    return 10f;
                }
                if (r < 0.6f) return GapSet(parent, z, 5f, true, zipLane);
                if (r < 0.8f) return CrumbleSet(parent, z);
                return RampSet(parent, z, 5f);
        }
    }

    float GapSet(Transform parent, float z, float length, bool zip, float zipLane, float avoidLane = float.NaN)
    {
        float gapCenter = z + 6f;
        RooftopProps.Gap(parent, new Vector3(0f, _surfaceY, gapCenter), length, zip, zipLane);

        // Notes trace the jump arc over the gap in a lane without the zip-line (or ramp).
        float arcLane = FreeLane(zipLane, float.IsNaN(avoidLane) ? zipLane : avoidLane);
        for (int i = 0; i < 5; i++)
        {
            float k = i / 4f;
            float nz = gapCenter - length * 0.5f - 1.5f + k * (length + 3f);
            float ny = _surfaceY + 1f + 4f * 1.8f * k * (1f - k);
            RooftopProps.Note(parent, new Vector3(arcLane, ny, nz));
        }
        return 6f + length + 6f;
    }

    float RampSet(Transform parent, float z, float gapLength)
    {
        float rampLane = PickLane();
        RooftopProps.Ramp(parent, new Vector3(rampLane, _surfaceY, z), 17f);
        GapSet(parent, z + 3f, gapLength, true, FreeLane(rampLane, rampLane), rampLane);
        for (int i = 0; i < 4; i++)
            RooftopProps.Note(parent, new Vector3(rampLane, _surfaceY + 3f + Mathf.Sin(i / 3f * Mathf.PI) * 1.5f, z + 4f + i * 3.5f));
        return 3f + 6f + gapLength + 6f;
    }

    float CrumbleSet(Transform parent, float z)
    {
        float safe = PickLane();
        foreach (float lane in lanes)
            if (!Mathf.Approximately(lane, safe))
                RooftopProps.Gap(parent, new Vector3(lane, _surfaceY, z + 3f), 4f, false, 0f, true);
        NoteLine(parent, safe, z, 3);
        return 8f;
    }

    void NoteLine(Transform parent, float x, float z, int count)
    {
        for (int i = 0; i < count; i++)
            RooftopProps.Note(parent, new Vector3(x, _surfaceY + 1f, z + i * 2.2f));
    }

    float FreeLane(float a, float b)
    {
        foreach (float lane in lanes)
            if (!Mathf.Approximately(lane, a) && !Mathf.Approximately(lane, b)) return lane;
        return lanes.Length > 0 ? lanes[0] : 0f;
    }

    bool NearFragment(float z)
    {
        if (fragmentDistances == null) return false;
        foreach (float d in fragmentDistances)
            if (Mathf.Abs(z - (_startZ + d)) < rowSpacing * 0.6f) return true;
        return false;
    }

    void Place(GameObject template, GameObject seg, float x, float z, float lift)
    {
        PlaceReturn(template, seg, x, z, lift);
    }

    GameObject PlaceReturn(GameObject template, GameObject seg, float x, float z, float lift)
    {
        if (template == null) return null;
        var pos = new Vector3(x, _surfaceY + 0.12f + lift, z);
        var clone = Instantiate(template, pos, template.transform.rotation, seg.transform);
        clone.transform.localScale = template.transform.lossyScale;
        Spawner.PrepareSpawnedClone(clone);
        return clone;
    }

    // ── Fragments: missed ones respawn further ahead ─────────────────────────

    List<float> _fragmentTargets;
    List<GameObject> _fragmentObjects;
    List<bool> _fragmentTaken;

    void InitFragments()
    {
        _fragmentTargets = new List<float>();
        _fragmentObjects = new List<GameObject>();
        _fragmentTaken = new List<bool>();
        if (fragmentDistances == null) return;
        foreach (float d in fragmentDistances)
        {
            _fragmentTargets.Add(_startZ + d);
            _fragmentObjects.Add(null);
            _fragmentTaken.Add(false);
        }
    }

    /// <summary>World Z of the next fragment still to collect, or NaN when none remain.</summary>
    public float NextFragmentZ()
    {
        if (_fragmentTargets == null) return float.NaN;
        float best = float.NaN;
        for (int i = 0; i < _fragmentTargets.Count; i++)
        {
            if (_fragmentTaken[i]) continue;
            if (float.IsNaN(best) || _fragmentTargets[i] < best) best = _fragmentTargets[i];
        }
        return best;
    }

    void UpdateFragments()
    {
        if (_fragmentTargets == null || _player == null) return;
        float pz = _player.position.z;
        for (int i = 0; i < _fragmentTargets.Count; i++)
        {
            if (_fragmentTaken[i]) continue;
            var obj = _fragmentObjects[i];
            bool spawned = obj != null || !ReferenceEquals(obj, null);
            if (spawned && (obj == null || !obj.activeInHierarchy || PickupCollectUtility.IsConsumed(obj)))
            {
                _fragmentTaken[i] = true;
                continue;
            }
            if (obj == null || obj.transform.position.z > pz - 6f) continue;

            Destroy(obj);
            float newZ = pz + 80f;
            _fragmentTargets[i] = newZ;
            _fragmentObjects[i] = null;
            var seg = SegmentAt(newZ);
            if (seg != null)
                _fragmentObjects[i] = PlaceReturn(fragmentTemplate, seg, PickLane(), newZ, 0.8f);
            RunDirector.Instance?.Flash("FRAGMENT MISSED — IT'S AHEAD!", new Color(0.35f, 0.85f, 1f));
        }
    }

    GameObject SegmentAt(float worldZ)
    {
        foreach (var seg in _segments)
        {
            if (seg == null) continue;
            float s = _startZ + (seg.transform.position.z - _origin.z);
            if (worldZ >= s && worldZ < s + segmentLength) return seg;
        }
        return null;
    }

    float PickLane()
    {
        if (lanes == null || lanes.Length == 0) return 0f;
        return lanes[Random.Range(0, lanes.Length)];
    }

    static GameObject PickFrom(GameObject[] pool)
    {
        if (pool == null || pool.Length == 0) return null;
        for (int i = 0; i < 6; i++)
        {
            var p = pool[Random.Range(0, pool.Length)];
            if (p != null) return p;
        }
        return null;
    }

    static void HideTemplates(GameObject[] pool)
    {
        if (pool == null) return;
        foreach (var p in pool)
            if (p != null) p.SetActive(false);
    }

    static float MeasureSurface(GameObject seg)
    {
        float top = float.MinValue;
        foreach (var col in seg.GetComponentsInChildren<Collider>())
            if (!col.isTrigger && col.gameObject.layer == 6) top = Mathf.Max(top, col.bounds.max.y);
        return top > float.MinValue ? top : 0f;
    }

    void Prune()
    {
        float limit = _player.position.z - keepBehind;
        for (int i = _segments.Count - 1; i >= 0; i--)
        {
            var seg = _segments[i];
            if (seg == null)
            {
                _segments.RemoveAt(i);
                continue;
            }
            if (_segments.Count <= 2) break;

            float endZ = _startZ + (seg.transform.position.z - _origin.z) + segmentLength;
            if (endZ < limit)
            {
                Destroy(seg);
                _segments.RemoveAt(i);
            }
        }
    }
}
