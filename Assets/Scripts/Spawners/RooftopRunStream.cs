using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns rooftop segments ahead of the player and removes them behind.
/// Gaps stay inside the existing jump. Does not change PlayerMovement.
/// </summary>
public class RooftopRunStream : MonoBehaviour
{
    public GameObject[] buildingPrefabs;
    public float lookAhead = 70f;
    public float keepBehind = 40f;
    public float startGap = 6.5f;
    public float maxGap = 10f;

    readonly List<GameObject> _segments = new List<GameObject>();
    Transform _player;
    float _nextZ;
    float _roofY = 7f;
    int _index;

    void Start()
    {
        GameManager.EndlessRun = true;
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null) _player = playerGo.transform;
        foreach (Transform child in transform)
            child.gameObject.SetActive(false);

        if (buildingPrefabs == null || buildingPrefabs.Length == 0) return;
        _roofY = MeasureTop(buildingPrefabs[0]);
        _nextZ = 0f;
        Fill();
        PlaceActors();
    }

    void LateUpdate()
    {
        if (_player == null || buildingPrefabs == null || buildingPrefabs.Length == 0) return;
        Fill();
        Prune();
    }

    void Fill()
    {
        float target = _player != null ? _player.position.z + lookAhead : lookAhead;
        int guard = 0;
        while (_nextZ < target && guard < 24)
        {
            SpawnOne();
            guard++;
        }
    }

    void SpawnOne()
    {
        var prefab = buildingPrefabs[_index % buildingPrefabs.Length];
        _index++;
        var building = Instantiate(prefab, transform);
        building.name = "RoofSeg_" + _index;
        building.transform.rotation = Quaternion.identity;
        building.transform.position = new Vector3(0f, 0f, _nextZ);
        SnapTop(building, _roofY);
        AlignMinZ(building, _nextZ);

        var bounds = RendererBounds(building);
        AddRoof(building, bounds);

        float travelled = Mathf.Max(0f, bounds.max.z);
        float gap = Mathf.Lerp(startGap, maxGap, Mathf.Clamp01(travelled / 400f));
        _nextZ = bounds.max.z + gap;
        _segments.Add(building);
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
            if (RendererBounds(seg).max.z < limit)
            {
                Destroy(seg);
                _segments.RemoveAt(i);
            }
        }
    }

    void PlaceActors()
    {
        if (_segments.Count == 0 || _player == null) return;
        var bounds = RendererBounds(_segments[0]);
        float y = bounds.max.y + 1.2f;
        _player.position = new Vector3(0f, y, bounds.min.z + 4f);
        var enemy = GameObject.Find("Enemy");
        if (enemy != null)
            enemy.transform.position = new Vector3(0f, y, bounds.min.z + 1f);
    }

    static void AddRoof(GameObject building, Bounds bounds)
    {
        const float thickness = 3f;
        var roof = new GameObject("RoofWalk");
        roof.layer = 6;
        roof.transform.SetParent(building.transform, true);
        roof.transform.rotation = Quaternion.identity;
        roof.transform.localScale = Vector3.one;
        roof.transform.position = new Vector3(bounds.center.x, bounds.max.y - thickness * 0.5f, bounds.center.z);
        var box = roof.AddComponent<BoxCollider>();
        box.size = new Vector3(bounds.size.x, thickness, bounds.size.z);
        box.isTrigger = false;
    }

    static float MeasureTop(GameObject prefab)
    {
        var temp = Instantiate(prefab);
        float top = RendererBounds(temp).max.y;
        Destroy(temp);
        return top < 2f ? 7f : top;
    }

    static void SnapTop(GameObject go, float worldY)
    {
        var bounds = RendererBounds(go);
        go.transform.position += new Vector3(0f, worldY - bounds.max.y, 0f);
    }

    static void AlignMinZ(GameObject go, float worldZ)
    {
        var bounds = RendererBounds(go);
        go.transform.position += new Vector3(0f, 0f, worldZ - bounds.min.z);
    }

    static Bounds RendererBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }
}
