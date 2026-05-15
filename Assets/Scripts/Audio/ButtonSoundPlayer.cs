using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to any GameObject with a Button component.
/// Plays the button sound from AudioManager whenever the button is clicked.
/// The Setup Audio editor tool adds this to every button automatically.
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonSoundPlayer : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() =>
            AudioManager.Instance?.PlayButton());
    }
}
