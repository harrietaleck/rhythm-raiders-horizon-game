using UnityEngine;

/// <summary>Slippery roof — strafing is sluggish while standing in it.</summary>
public class WetPatch : MonoBehaviour
{
    public float sideMultiplier = 0.35f;

    PlayerMovement _player;

    void OnTriggerEnter(Collider other)
    {
        if (!PlayerColliderUtility.IsPlayer(other)) return;
        var pm = PlayerColliderUtility.GetMovement(other);
        if (pm == null || _player == pm) return;
        _player = pm;
        pm.sidewaySpeed = pm.baseSidewaySpeed * sideMultiplier;
        RunDirector.Instance?.Flash("SLIPPERY!", new Color(0.5f, 0.75f, 1f));
    }

    void OnTriggerExit(Collider other)
    {
        if (_player == null || PlayerColliderUtility.GetMovement(other) != _player) return;
        Restore();
    }

    void OnDisable() => Restore();

    void Restore()
    {
        if (_player == null) return;
        if (_player.sidewaySpeed < _player.baseSidewaySpeed)
            _player.sidewaySpeed = _player.baseSidewaySpeed;
        _player = null;
    }
}
