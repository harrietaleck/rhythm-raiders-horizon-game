using System.Collections;
using UnityEngine;

/// <summary>
/// A drop between buildings. Running into it (feet at roof level) is a fall.
/// Crumbling tiles start solid and collapse shortly after the player steps on them.
/// </summary>
public class RooftopGap : MonoBehaviour
{
    public bool crumble;
    public Transform surface;
    public float crumbleDelay = 0.6f;

    bool _open;
    bool _armed;
    bool _done;

    void Start()
    {
        _open = !crumble;
    }

    void OnTriggerStay(Collider other)
    {
        if (_done || !GameManager.GameStarted || !PlayerColliderUtility.IsPlayer(other)) return;
        if (PlayerCarrier.IsCarried) return;

        var cc = other.GetComponentInParent<CharacterController>();
        float feet = cc != null ? cc.bounds.min.y : other.bounds.min.y;
        float roof = transform.position.y;

        if (!_open)
        {
            if (!_armed && feet < roof + 0.4f)
            {
                _armed = true;
                StartCoroutine(Collapse());
            }
            return;
        }

        if (feet > roof + 0.45f) return;

        _done = true;
        var pickup = other.GetComponentInParent<PickupBase>();
        if (pickup != null)
            StartCoroutine(Fall(pickup));
    }

    IEnumerator Collapse()
    {
        Vector3 home = surface != null ? surface.localPosition : Vector3.zero;
        float t = 0f;
        while (t < crumbleDelay)
        {
            t += Time.deltaTime;
            if (surface != null)
                surface.localPosition = home + new Vector3(Random.Range(-0.05f, 0.05f), 0f, Random.Range(-0.05f, 0.05f));
            yield return null;
        }

        foreach (Transform child in transform)
            child.gameObject.SetActive(false);
        var hole = RooftopProps.Part(PrimitiveType.Cube, transform, new Vector3(0, 0.015f, 0),
                                     new Vector3(RooftopProps.LaneWidth, 0.03f, 4f), RooftopProps.Mat(new Color(0.01f, 0.01f, 0.02f)), false);
        hole.name = "Hole";
        _open = true;
    }

    IEnumerator Fall(PickupBase pickup)
    {
        Debug.Log($"[Run] Fell into {name} at z {transform.position.z:0}");
        pickup.KillPlayer(DeathCause.Fall);
        var movement = pickup.GetComponent<PlayerMovement>();
        var cc = pickup.GetComponent<CharacterController>();
        if (movement != null) movement.enabled = false;
        if (cc != null) cc.enabled = false;

        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            pickup.transform.position += Vector3.down * (9f * t) * Time.deltaTime;
            yield return null;
        }
    }
}
