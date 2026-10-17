using UnityEngine;

public class HealthDecreaseObstacle : MonoBehaviour
{
    [Tooltip("How much health to take away (ignored when instantKill is true)")]
    public int damage = 15;

    [Tooltip("Always drains Vibe directly, even when Shield remains.")]
    public int vibeDamage = 5;

    [Tooltip("High-danger obstacles end the run immediately.")]
    public bool instantKill = false;

    [Tooltip("Name shown on the Game Over screen. Leave empty to use the object's name.")]
    public string displayName = "";

    /// <summary>Readable name for this obstacle, e.g. "Laser Grid" or "Water Tank".</summary>
    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(displayName)) return displayName.Trim();
            string n = gameObject.name.Replace("(Clone)", "").Replace('_', ' ');
            n = System.Text.RegularExpressions.Regex.Replace(n, @"\s*-\s*(obstacle|pickup|prefab)\b", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            n = System.Text.RegularExpressions.Regex.Replace(n, @"\(\d+\)|\s+\d+$", "");
            n = System.Text.RegularExpressions.Regex.Replace(n, @"\s{2,}", " ").Trim(' ', '-');
            return string.IsNullOrEmpty(n) ? "Obstacle" : n;
        }
    }

    // Collision is handled by PickupBase.OnTriggerEnter (tag "HealthDEC") which calls
    // ApplyObstacleDamage(this). A second OnTriggerEnter here would apply damage twice
    // on the same frame, so this class is now a pure data container.
}
