using UnityEngine;

public enum ExhibitKind { Drum, Headset, Note, Violin, Horn, Harp, Gramophone, Treasure }

/// <summary>What smashing a case does: cyan boosts, red hits a sour note, purple is a gamble.</summary>
public enum ExhibitMood { Blessing, Curse, Mystery }

/// <summary>
/// A display case that plays its instrument when the player runs past it in the outer lane.
/// Every case smashes open; cyan cases boost, red cases hit a sour note, purple ones are a gamble. Each kind gives a different boost via MuseumRun.
/// </summary>
public class InteractiveExhibit : MonoBehaviour
{
    public ExhibitKind kind;
    public ExhibitMood mood = ExhibitMood.Blessing;
    public Renderer ring;
    public Renderer glass;
    public ExhibitSpin spin;
    public Light caseLight;
    public Color ringColor = new Color(0.35f, 0.9f, 1f, 1f);

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    MaterialPropertyBlock _block;
    bool _played;
    public bool Played => _played;
    float _playedAt = -10f;

    void Update()
    {
        if (_block == null) _block = new MaterialPropertyBlock();

        if (ring != null)
        {
            if (_played)
            {
                float fade = 1f - Mathf.Clamp01((Time.time - _playedAt) * 1.5f);
                ring.enabled = fade > 0.01f;
                ring.transform.localScale = new Vector3(2.8f + (1f - fade) * 2f, 0.01f, 2.8f + (1f - fade) * 2f);
                SetColor(ring, new Color(ringColor.r, ringColor.g, ringColor.b, 0.9f * fade));
            }
            else
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
                ring.transform.localScale = new Vector3(2.6f + pulse * 0.3f, 0.01f, 2.6f + pulse * 0.3f);
                SetColor(ring, new Color(ringColor.r, ringColor.g, ringColor.b, 0.35f + pulse * 0.45f));
            }
        }

        if (_played && caseLight != null)
            caseLight.intensity = Mathf.Lerp(caseLight.intensity, 0f, Time.deltaTime * 1.2f);
        if (_played && spin != null)
            spin.degreesPerSecond = Mathf.Lerp(spin.degreesPerSecond, 25f, Time.deltaTime);
    }

    void SetColor(Renderer r, Color c)
    {
        r.GetPropertyBlock(_block);
        _block.SetColor(BaseColor, c);
        r.SetPropertyBlock(_block);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_played || !GameManager.GameStarted || !PlayerColliderUtility.IsPlayer(other)) return;
        Play(PlayerColliderUtility.GetPickup(other));
    }

    /// <summary>Plays the exhibit for the player (also used by the debug menu).</summary>
    public void Play(PickupBase pickup)
    {
        var run = MuseumRun.Instance;
        if (_played || run == null) return;

        _played = true;
        _playedAt = Time.time;
        if (spin != null) spin.degreesPerSecond = 720f;
        if (glass != null)
        {
            // Every case smashes open when played.
            GlassShatter.Spawn(glass.bounds, ringColor);
            glass.enabled = false;
        }
        if (caseLight == null)
        {
            caseLight = new GameObject("PlayLight").AddComponent<Light>();
            caseLight.transform.SetParent(transform, false);
            caseLight.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            caseLight.type = LightType.Point;
            caseLight.range = 7f;
            caseLight.shadows = LightShadows.None;
        }
        caseLight.color = ringColor;
        caseLight.intensity = 6f;

        run.PlayExhibit(this, pickup);
    }
}
