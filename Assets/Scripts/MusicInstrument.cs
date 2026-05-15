using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicInstrument : MonoBehaviour
{
    [Tooltip("The sound to play when the player touches this instrument")]
    public AudioClip instrumentSound;

    [Tooltip("Points awarded for collecting this instrument")]
    public int artifactValue = 1;

    private AudioSource audioSource;
    private bool collected = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        if (instrumentSound != null)
            audioSource.clip = instrumentSound;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || !other.CompareTag("Player")) return;
        collected = true;

        PickupBase pickup = other.GetComponent<PickupBase>();
        if (pickup != null)
        {
            pickup.artifactAmount += artifactValue;
            pickup.TryTriggerVictory();
        }

        Animator anim = GetComponent<Animator>();
        if (anim != null)
            anim.SetTrigger("Collected");

        if (instrumentSound != null)
        {
            audioSource.Play();
            Destroy(gameObject, instrumentSound.length);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
