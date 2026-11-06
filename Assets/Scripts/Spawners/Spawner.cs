using UnityEngine;

[System.Serializable]
public class WeightedSpawnEntry
{
    public GameObject prefab;
    [Tooltip("Higher = more likely to spawn. E.g. weight 3 is 3× more common than weight 1.")]
    [Range(1, 20)]
    public int weight = 1;
}

public class Spawner : MonoBehaviour
{
    public const float MinSpawnZ = 25f;

    public int spawnCount = 8;

    /// <summary>Set by levels that lay out their own course (Level 2 MuseumRun).</summary>
    public static bool Suppressed;

    [Tooltip("If populated, uses weighted random. Leave empty to fall back to spawnObjects.")]
    public WeightedSpawnEntry[] weightedObjects;

    [Tooltip("Legacy equal-weight pool. Used only when weightedObjects is empty.")]
    public GameObject[] spawnObjects;

    [Header("Artifacts")]
    public GameObject artifactTemplate;
    public int artifactsPerSegment = 1;

    [Header("Curtain (side spawn only)")]
    public GameObject curtainPrefab;
    [Range(0f, 1f)]
    public float curtainSpawnChance = 0.7f;

    private const float CurtainSideMinX = 7f;
    private const float CurtainSideMaxX = 10f;
    private const float CurtainSpawnY = 3f;
    private const float SpawnSurfaceLift = 0.12f;
    private const int GroundLayerMask = 1 << 6;

    public void SpawnGameObjects(GameObject ground)
    {
        if (ground == null || Suppressed) return;

        var footprint = GroundChain.GetSpawnFootprint(ground);
        GetSpawnRange(ground, footprint, out float minX, out float maxX, out float minZ, out float maxZ);
        float rayStartY = footprint.max.y + 5f;

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject spawnPickup = PickRandomNonArtifact();
            if (spawnPickup == null) continue;

            float spawnX = Random.Range(minX, maxX);
            float spawnZ = Random.Range(minZ, maxZ);
            Vector3 position = SnapToGroundSurface(spawnX, spawnZ, ground, rayStartY);
            SpawnOnGround(spawnPickup, position, ground);
        }

        SpawnArtifacts(ground, footprint, minX, maxX, minZ, maxZ, rayStartY);
        TrySpawnSideCurtain(ground, footprint);
    }

    static GameObject SpawnOnGround(GameObject template, Vector3 worldPosition, GameObject groundSegment)
    {
        if (template == null || groundSegment == null) return null;

        var spawnedObject = Object.Instantiate(template, worldPosition, template.transform.rotation);
        spawnedObject.transform.localScale = template.transform.localScale;
        PrepareSpawnedClone(spawnedObject);
        GroundSegmentContent.Bind(spawnedObject, groundSegment);
        return spawnedObject;
    }

    public static void PrepareSpawnedClone(GameObject clone)
    {
        if (clone == null) return;

        clone.SetActive(true);
        foreach (var renderer in clone.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = true;
        foreach (var col in clone.GetComponentsInChildren<Collider>(true))
            col.enabled = true;

        GameplayCollisionUtility.ConfigureObject(clone);
        PickupVisuals.Dress(clone);   // power-ups wear their Stylized Fire flame
    }

    static void GetSpawnRange(GameObject ground, Bounds footprint, out float minX, out float maxX, out float minZ, out float maxZ)
    {
        bool isStreamedTile = ground.GetComponent<GroundSegmentLifetime>() != null;

        float minZRaw = footprint.min.z + 4f;
        float maxZRaw = footprint.max.z - 4f;
        if (!isStreamedTile)
            minZRaw = Mathf.Max(minZRaw, MinSpawnZ);

        if (maxZRaw - minZRaw < 6f)
        {
            float centerZ = footprint.center.z;
            float halfZ = Mathf.Max(footprint.extents.z - 4f, 8f);
            minZRaw = centerZ - halfZ;
            maxZRaw = centerZ + halfZ;
            if (!isStreamedTile)
                minZRaw = Mathf.Max(minZRaw, MinSpawnZ);
        }

        minZ = Mathf.Min(minZRaw, maxZRaw);
        maxZ = Mathf.Max(minZRaw, maxZRaw);

        minX = Mathf.Max(footprint.min.x + 2f, -6f);
        maxX = Mathf.Min(footprint.max.x - 2f, 6f);
        if (maxX - minX < 3f)
        {
            float centerX = Mathf.Clamp(footprint.center.x, -4f, 4f);
            minX = centerX - 4f;
            maxX = centerX + 4f;
        }
    }

    static Vector3 SnapToGroundSurface(float x, float z, GameObject ground, float rayStartY)
    {
        Vector3 origin = new Vector3(x, rayStartY, z);
        var hits = Physics.RaycastAll(origin, Vector3.down, 80f, GroundLayerMask, QueryTriggerInteraction.Ignore);
        if (hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                var t = hit.collider.transform;
                if (t == ground.transform || t.IsChildOf(ground.transform))
                    return hit.point + Vector3.up * SpawnSurfaceLift;
            }

            return hits[0].point + Vector3.up * SpawnSurfaceLift;
        }

        return new Vector3(x, SpawnSurfaceLift, z);
    }

    private GameObject PickRandomNonArtifact()
    {
        // ── Weighted pool (preferred) ─────────────────────────────────────────
        if (weightedObjects != null && weightedObjects.Length > 0)
        {
            int totalWeight = 0;
            foreach (var e in weightedObjects)
                if (e.prefab != null && !e.prefab.CompareTag("Artifact"))
                    totalWeight += Mathf.Max(1, e.weight);

            if (totalWeight > 0)
            {
                int roll = Random.Range(0, totalWeight);
                int cumulative = 0;
                foreach (var e in weightedObjects)
                {
                    if (e.prefab == null || e.prefab.CompareTag("Artifact")) continue;
                    cumulative += Mathf.Max(1, e.weight);
                    if (roll < cumulative) return e.prefab;
                }
            }
        }

        // ── Legacy equal-weight fallback ──────────────────────────────────────
        if (spawnObjects == null || spawnObjects.Length == 0) return null;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            var pick = spawnObjects[Random.Range(0, spawnObjects.Length)];
            if (pick == null) continue;
            if (pick.CompareTag("Artifact")) continue;
            if (artifactTemplate != null && pick == artifactTemplate) continue;
            return pick;
        }

        return spawnObjects[Random.Range(0, spawnObjects.Length)];
    }

    private void SpawnArtifacts(GameObject ground, Bounds footprint, float minX, float maxX, float minZ, float maxZ, float rayStartY)
    {
        if (artifactTemplate == null || artifactsPerSegment < 1)
            return;

        for (int i = 0; i < artifactsPerSegment; i++)
        {
            float x = Random.Range(minX, maxX);
            float z = Random.Range(minZ, maxZ);
            Vector3 position = SnapToGroundSurface(x, z, ground, rayStartY);
            position.y += 0.2f;
            SpawnOnGround(artifactTemplate, position, ground);
        }
    }

    private void TrySpawnSideCurtain(GameObject ground, Bounds footprint)
    {
        if (curtainPrefab == null || Random.value > curtainSpawnChance)
            return;

        float side = Random.value < 0.5f ? -1f : 1f;
        float x = side * Random.Range(CurtainSideMinX, CurtainSideMaxX);
        float z = Random.Range(footprint.min.z + 5f, footprint.max.z - 5f);
        if (ground.GetComponent<GroundSegmentLifetime>() == null)
            z = Mathf.Max(z, MinSpawnZ);

        Vector3 position = new Vector3(x, CurtainSpawnY, z);
        var curtain = Object.Instantiate(curtainPrefab, position, curtainPrefab.transform.rotation);
        curtain.transform.localScale = curtainPrefab.transform.localScale;
        PrepareSpawnedClone(curtain);
        GroundSegmentContent.Bind(curtain, ground);
    }

    public GameObject SpawnSideCurtainAt(float worldZ)
    {
        if (curtainPrefab == null)
            return null;

        float side = Random.value < 0.5f ? -1f : 1f;
        float x = side * Random.Range(CurtainSideMinX, CurtainSideMaxX);
        GameObject curtain = Instantiate(curtainPrefab, new Vector3(x, CurtainSpawnY, worldZ), curtainPrefab.transform.rotation);
        curtain.transform.localScale = curtainPrefab.transform.localScale;
        PrepareSpawnedClone(curtain);
        Destroy(curtain, 65f);
        return curtain;
    }
}
