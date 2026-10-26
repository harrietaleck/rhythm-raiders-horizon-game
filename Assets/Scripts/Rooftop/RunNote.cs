using UnityEngine;

/// <summary>Collectible music note. Worth points × combo; pulled in by the magnet.</summary>
public class RunNote : MonoBehaviour
{
    public Transform visual;

    Transform _player;
    bool _taken;

    void Start()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) _player = go.transform;
        if (visual != null) visual.localEulerAngles = new Vector3(0, Random.Range(0f, 360f), 0);
    }

    void Update()
    {
        if (visual != null)
            visual.Rotate(0f, 160f * Time.deltaTime, 0f, Space.World);

        var director = RunDirector.Instance;
        bool magnet = director != null ? director.MagnetActive
                    : MuseumRun.Instance != null ? MuseumRun.Instance.MagnetActive
                    : ClubRun.Instance != null && ClubRun.Instance.MagnetActive;
        if (_player == null || !magnet) return;

        float radius = director != null ? director.magnetRadius : MuseumRun.Instance != null ? MuseumRun.MagnetRadius : ClubRun.MagnetRadius;
        Vector3 target = _player.position + Vector3.up * 0.2f;
        float dist = Vector3.Distance(transform.position, target);
        if (dist < radius)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, (18f + (radius - dist) * 4f) * Time.deltaTime);
            if (dist < 0.9f) Collect();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!PlayerColliderUtility.IsPlayer(other)) return;
        Collect();
    }

    void Collect()
    {
        if (_taken || !GameManager.GameStarted) return;
        _taken = true;
        if (RunDirector.Instance != null) RunDirector.Instance.AddNote();
        else if (MuseumRun.Instance != null) MuseumRun.Instance.AddNote();
        else ClubRun.Instance?.AddNote();
        NoteFx.FlyToBag(visual);   // the fragment flies into the runner's backpack
        Destroy(gameObject);
    }
}
