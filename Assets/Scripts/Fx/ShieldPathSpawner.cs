using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scatters plasma shield orbs along the run in every level: a new one every
/// 18–38 m ahead of the runner, in a random lane, only on solid floor and never
/// inside an obstacle. Shield is run momentum, so these keep her pace up.
/// </summary>
public class ShieldPathSpawner : MonoBehaviour
{
    public float minGap = 40f;
    public float maxGap = 70f;
    public float spawnAhead = 45f;
    public float height = 1.1f;
    static readonly float[] Lanes = { -3f, 0f, 3f };

    [Header("Vibe pickups (+5 health)")]
    public float vibeMinGap = 30f;
    public float vibeMaxGap = 55f;

    Transform _player;
    float _nextZ, _nextVibeZ;
    readonly List<GameObject> _orbs = new List<GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Attach();
    }

    static void OnSceneLoaded(Scene s, LoadSceneMode m) => Attach();

    static void Attach()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || player.GetComponent<PlayerMovement>() == null) return;   // menus, cutscenes
        if (FindAnyObjectByType<ShieldPathSpawner>() != null) return;
        new GameObject("ShieldPathSpawner").AddComponent<ShieldPathSpawner>()._player = player.transform;
    }

    void Update()
    {
        if (_player == null || !GameManager.GameStarted) return;
        float pz = _player.position.z;
        if (_nextZ < pz + 10f) _nextZ = pz + Random.Range(minGap, maxGap);

        while (_nextZ < pz + spawnAhead)
        {
            TrySpawn(_nextZ, false);
            _nextZ += Random.Range(minGap, maxGap);
        }

        if (_nextVibeZ < pz + 10f) _nextVibeZ = pz + Random.Range(vibeMinGap, vibeMaxGap);
        while (_nextVibeZ < pz + spawnAhead)
        {
            TrySpawn(_nextVibeZ, true);
            _nextVibeZ += Random.Range(vibeMinGap, vibeMaxGap);
        }

        for (int i = _orbs.Count - 1; i >= 0; i--)
        {
            var o = _orbs[i];
            if (o == null) { _orbs.RemoveAt(i); continue; }
            if (o.transform.position.z < pz - 20f) { Destroy(o); _orbs.RemoveAt(i); }
        }
    }

    void TrySpawn(float z, bool vibe)
    {
        if (!PickupLimiter.HasRoom(vibe ? "Vibe" : "Shield")) return;
        int start = Random.Range(0, Lanes.Length);
        int ground = GroundPlacementUtility.DefaultGroundMask.value;
        for (int k = 0; k < Lanes.Length; k++)
        {
            float x = Lanes[(start + k) % Lanes.Length];
            var top = new Vector3(x, _player.position.y + 8f, z);
            if (!Physics.Raycast(top, Vector3.down, out var hit, 20f, ground, QueryTriggerInteraction.Ignore))
                continue;   // a roof gap or off the course
            var pos = hit.point + Vector3.up * height;
            if (Blocked(pos) || PickupLimiter.Crowded(pos)) continue;
            GameObject orb;
            if (vibe) orb = PickupVisuals.SpawnVibe(pos);
            else
            {
                orb = RooftopProps.Power(null, pos, PowerPickup.Kind.Bubble);
                orb.name = "Shield Pickup";
            }
            _orbs.Add(orb);
            PickupLimiter.Register(orb, vibe ? "Vibe" : "Shield");
            return;
        }
    }

    static bool Blocked(Vector3 pos)
    {
        foreach (var c in Physics.OverlapSphere(pos, 1.3f, ~0, QueryTriggerInteraction.Collide))
        {
            if (c == null || c.gameObject.layer == GroundPlacementUtility.GroundLayer) continue;
            if (c.GetComponentInParent<PlayerMovement>() != null) continue;
            if (c.isTrigger && c.GetComponentInParent<PowerPickup>() == null && c.GetComponentInParent<VibePickup>() == null
                && !GameplayCollisionUtility.IsPickupTag(c.tag) && !c.CompareTag("HealthDEC"))
                continue;   // big zone triggers (gaps, beats, doors) don't block a spot
            return true;   // an obstacle, prop or another pickup is already here
        }
        return false;
    }
}
