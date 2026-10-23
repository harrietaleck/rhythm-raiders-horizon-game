using UnityEngine;

/// <summary>
/// Runtime references to imported effect/prop assets (lives at Resources/FxLibrary,
/// filled by Tools → Setup FX Library). Code that spawns smoke or disco balls loads it from here.
/// </summary>
[CreateAssetMenu(menuName = "Rhythm Raiders/FX Library")]
public class FxLibrary : ScriptableObject
{
    [Header("Fog Particles (smoke)")]
    public GameObject fogPrefab;
    [Tooltip("URP particle material made from the fog pack's smoke texture.")]
    public Material fogMaterial;

    [Header("Rolling Balls Sci-fi Pack (Level 3 disco balls)")]
    public GameObject[] discoBalls;

    [Header("Dance floor characters (Mixamo, Humanoid)")]
    public GameObject[] dancers;
    public GameObject dj;
    public GameObject bodyguard;
    public RuntimeAnimatorController crowdController;
    public RuntimeAnimatorController djController;
    public RuntimeAnimatorController bodyguardController;
    [Tooltip("State names in the crowd controller, one per dance.")]
    public string[] danceStates;

    [Header("Runner backpack (LowPoly Fantasy Bags)")]
    public GameObject backpack;

    [Header("Level 2 museum art")]
    [Tooltip("Greek Statue / HQ Lion Statue prefabs used as breakable path statues.")]
    public GameObject[] statues;
    [Tooltip("Customizable Frames Gallery: Oil Paintings prefabs hung on the hall walls.")]
    public GameObject[] paintings;

    [Header("Obstacle sounds (Assets/Audios)")]
    public AudioClip laserHitSound;
    public AudioClip drumHitSound;

    static FxLibrary _instance;
    static bool _looked;

    public static FxLibrary Get()
    {
        if (!_looked)
        {
            _looked = true;
            _instance = Resources.Load<FxLibrary>("FxLibrary");
        }
        return _instance;
    }
}
