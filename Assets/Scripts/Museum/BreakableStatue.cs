using System.Collections;
using UnityEngine;

/// <summary>
/// A path statue the runner can knock over: on the bump it tips away, smashes on the carpet,
/// breaks into stone chunks and throws up a cloud of dust. Its collider goes so the run continues.
/// </summary>
public class BreakableStatue : MonoBehaviour
{
    public Transform pivot;
    public Color stone = new Color(0.88f, 0.86f, 0.82f);
    public Light spot;
    bool _down;

    // Sent by PickupBase when the runner hits this obstacle.
    void OnPlayerHit(Vector3 point) => Knock();

    public void Knock()
    {
        if (_down || pivot == null) return;
        _down = true;
        foreach (var c in GetComponents<Collider>()) c.enabled = false;
        if (spot != null) spot.enabled = false;
        StartCoroutine(Fall());
    }

    IEnumerator Fall()
    {
        // Wobble, then tip over backwards (away from the runner) with gravity-like acceleration.
        float side = Random.Range(-18f, 18f);
        for (float t = 0f; t < 0.12f; t += Time.deltaTime)
        {
            pivot.localRotation = Quaternion.Euler(-6f * Mathf.Sin(t / 0.12f * Mathf.PI), 0f, 0f);
            yield return null;
        }
        float angle = 0f, speed = 40f;
        while (angle < 88f)
        {
            speed += 520f * Time.deltaTime;
            angle = Mathf.Min(88f, angle + speed * Time.deltaTime);
            pivot.localRotation = Quaternion.Euler(angle, side * angle / 88f, 0f);
            yield return null;
        }
        Smash();
    }

    void Smash()
    {
        var rs = pivot.GetComponentsInChildren<Renderer>();
        Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(pivot.position, Vector3.one);
        foreach (var r in rs) b.Encapsulate(r.bounds);
        foreach (var r in rs) r.enabled = false;
        var floor = new Vector3(b.center.x, transform.position.y, b.center.z);

        // Stone chunks scatter along the fallen statue.
        var mat = MuseumHall.Lit(stone, 0.35f);
        var debris = new GameObject("Statue Rubble").transform;
        debris.position = floor;
        for (int i = 0; i < 16; i++)
        {
            var p = new Vector3(Random.Range(b.min.x, b.max.x), Random.Range(0.2f, 0.9f), Random.Range(b.min.z, b.max.z));
            var size = Vector3.one * Random.Range(0.15f, 0.45f);
            size.y *= Random.Range(0.5f, 1.2f);
            var chunk = RooftopProps.Part(PrimitiveType.Cube, debris, debris.InverseTransformPoint(new Vector3(p.x, transform.position.y + p.y, p.z)), size, mat, false);
            chunk.transform.rotation = Random.rotation;
            var rb = chunk.AddComponent<Rigidbody>();
            chunk.AddComponent<BoxCollider>();
            chunk.layer = 2;   // Ignore Raycast; keeps chunks out of the runner's way checks
            rb.mass = 0.4f;
            rb.AddExplosionForce(Random.Range(180f, 320f), floor + Vector3.down * 0.3f, 4f, 0.6f);
            rb.AddTorque(Random.insideUnitSphere * 6f, ForceMode.Impulse);
        }
        foreach (var col in debris.GetComponentsInChildren<Collider>())
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) foreach (var pc in player.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(col, pc);
        }
        Destroy(debris.gameObject, 4f);

        SmokeCloud.Spawn(floor, new Vector3(Mathf.Max(2.5f, b.size.x), 1.6f, Mathf.Max(2.5f, b.size.z)), 0.5f,
                         new Color(0.82f, 0.79f, 0.72f, 0.85f), new Vector3(0f, 0.2f, -0.4f));
        ObstacleImpactFX.Thud(floor);
        AudioManager.Instance?.PlayExplosionSfx();
    }
}
