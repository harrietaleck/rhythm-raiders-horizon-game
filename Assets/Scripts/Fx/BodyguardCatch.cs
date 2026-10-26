using System.Collections;
using UnityEngine;

/// <summary>
/// DJ bodyguards (bouncers and the taser bodyguard): run into one and he grabs the runner -
/// the run is over.
/// </summary>
public class BodyguardCatch : MonoBehaviour
{
    bool _grabbing;

    public void Catch(Transform player)
    {
        if (_grabbing || player == null || !GameManager.GameStarted) return;
        var pickup = player.GetComponent<PickupBase>();
        if (pickup == null || pickup.currentHealth <= 0) return;
        _grabbing = true;
        StartCoroutine(Grab(player, pickup));
    }

    IEnumerator Grab(Transform player, PickupBase pickup)
    {
        var move = player.GetComponent<PlayerMovement>();
        if (move != null) move.enabled = false;
        var taser = GetComponent<TaserGuard>();
        if (taser != null) taser.enabled = false;
        AudioManager.Instance?.PlayObstacleHitSfx();
        Callout("GRABBED BY THE BODYGUARD!");

        // He steps in and grabs her.
        var start = transform.position;
        var toPlayer = player.position - start;
        toPlayer.y = 0f;
        var dir = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : Vector3.forward;
        var grabAt = new Vector3(player.position.x, start.y, player.position.z) - dir * 0.7f;
        var face = Quaternion.LookRotation(dir);
        var startRot = transform.rotation;
        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / 0.35f);
            transform.position = Vector3.Lerp(start, grabAt, k);
            transform.rotation = Quaternion.Slerp(startRot, face, k);
            yield return null;
        }
        yield return new WaitForSecondsRealtime(0.5f);
        pickup.KillPlayer(DeathCause.Guard);
    }

    static void Callout(string text)
    {
        var red = new Color(1f, 0.3f, 0.3f);
        if (RunDirector.Instance != null) RunDirector.Instance.Flash(text, red, 2f);
        else if (MuseumRun.Instance != null) MuseumRun.Instance.Flash(text, red, 2f);
        else if (ClubRun.Instance != null) ClubRun.Instance.Flash(text, red, 2f);
    }
}
