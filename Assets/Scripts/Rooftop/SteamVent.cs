using UnityEngine;

/// <summary>Puffs steam on a rhythm; a puff shoves the player one lane sideways.</summary>
public class SteamVent : MonoBehaviour
{
    public GameObject plume;
    public float phase;
    public float interval = 1.8f;
    public float puffTime = 0.7f;
    public float push = 9f;

    CharacterController _pushing;
    float _pushLeft;
    float _pushDir;

    bool Puffing => Mathf.Repeat(Time.time + phase, interval) < puffTime;

    void Update()
    {
        if (plume != null)
        {
            bool on = Puffing;
            plume.SetActive(on);
            if (on)
            {
                float s = 1.2f + Mathf.PingPong(Time.time * 3f, 0.3f);
                plume.transform.localScale = new Vector3(s, 1.6f, s);
            }
        }

        if (_pushing != null && _pushLeft > 0f)
        {
            float dt = Mathf.Min(Time.deltaTime, _pushLeft);
            _pushLeft -= dt;
            if (_pushing.enabled)
                _pushing.Move(new Vector3(_pushDir * push * dt, 0f, 0f));
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!Puffing || _pushLeft > 0f || !GameManager.GameStarted || !PlayerColliderUtility.IsPlayer(other)) return;
        if (PlayerCarrier.IsCarried) return;

        var cc = other.GetComponentInParent<CharacterController>();
        if (cc == null) return;

        _pushing = cc;
        _pushDir = cc.transform.position.x >= transform.position.x ? 1f : -1f;
        if (Mathf.Abs(cc.transform.position.x + _pushDir * 4f) > 6f) _pushDir = -_pushDir;
        _pushLeft = 0.4f;
        RunDirector.Instance?.Flash("STEAM!", new Color(0.9f, 0.95f, 1f));
    }
}
