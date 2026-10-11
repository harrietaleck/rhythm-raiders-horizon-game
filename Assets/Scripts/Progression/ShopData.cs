using UnityEngine;

/// <summary>
/// Persistent note wallet, power-up upgrades and cosmetic trails (PlayerPrefs).
/// </summary>
public static class ShopData
{
    public enum Upgrade { Magnet, Bubble, SmokeBomb, HeadStart }

    public struct UpgradeInfo
    {
        public string name;
        public string description;
        public int[] costs;
        public float perLevel;
        public string unit;
    }

    public struct TrailInfo
    {
        public string name;
        public int cost;
        public Color color;
    }

    public const int MaxLevel = 3;

    public static readonly UpgradeInfo[] Upgrades =
    {
        new UpgradeInfo { name = "MAGNET",     description = "Magnet lasts longer",          costs = new[] { 60, 120, 200 },  perLevel = 1.5f, unit = "s" },
        new UpgradeInfo { name = "BUBBLE",     description = "Shield bubble lasts longer",   costs = new[] { 80, 150, 240 },  perLevel = 1f,   unit = "s" },
        new UpgradeInfo { name = "SMOKE BOMB", description = "Guard stays stunned longer",   costs = new[] { 80, 150, 240 },  perLevel = 1f,   unit = "s" },
        new UpgradeInfo { name = "HEAD START", description = "Start each run with a bubble", costs = new[] { 120, 220, 350 }, perLevel = 2f,   unit = "s" },
    };

    public static readonly TrailInfo[] Trails =
    {
        new TrailInfo { name = "NO TRAIL",   cost = 0,   color = Color.clear },
        new TrailInfo { name = "CYAN PULSE", cost = 100, color = new Color(0.35f, 0.85f, 1f) },
        new TrailInfo { name = "GOLD RUSH",  cost = 200, color = new Color(1f, 0.8f, 0.25f) },
        new TrailInfo { name = "NEON PINK",  cost = 300, color = new Color(1f, 0.35f, 0.75f) },
    };

    const string WalletKey = "Shop_Notes";
    const string TrailKey = "Shop_Trail";

    public static int Notes
    {
        get => PlayerPrefs.GetInt(WalletKey, 0);
        private set { PlayerPrefs.SetInt(WalletKey, Mathf.Max(0, value)); PlayerPrefs.Save(); }
    }

    public static void AddNotes(int amount)
    {
        if (amount > 0) Notes += amount;
    }

    public static int Level(Upgrade u) => PlayerPrefs.GetInt("Shop_Up_" + u, 0);

    public static float Bonus(Upgrade u) => Level(u) * Upgrades[(int)u].perLevel;

    public static int NextCost(Upgrade u)
    {
        int lvl = Level(u);
        return lvl >= MaxLevel ? -1 : Upgrades[(int)u].costs[lvl];
    }

    public static bool BuyUpgrade(Upgrade u)
    {
        int cost = NextCost(u);
        if (cost < 0 || Notes < cost) return false;
        Notes -= cost;
        PlayerPrefs.SetInt("Shop_Up_" + u, Level(u) + 1);
        PlayerPrefs.Save();
        return true;
    }

    public static bool OwnsTrail(int i) => i == 0 || PlayerPrefs.GetInt("Shop_TrailOwned_" + i, 0) == 1;

    public static int EquippedTrail => Mathf.Clamp(PlayerPrefs.GetInt(TrailKey, 0), 0, Trails.Length - 1);

    /// <summary>Buys the trail if needed, then equips it.</summary>
    public static bool SelectTrail(int i)
    {
        if (i < 0 || i >= Trails.Length) return false;
        if (!OwnsTrail(i))
        {
            if (Notes < Trails[i].cost) return false;
            Notes -= Trails[i].cost;
            PlayerPrefs.SetInt("Shop_TrailOwned_" + i, 1);
        }
        PlayerPrefs.SetInt(TrailKey, i);
        PlayerPrefs.Save();
        return true;
    }

    public static void ApplyTrail(GameObject player)
    {
        int i = EquippedTrail;
        if (player == null || i == 0) return;

        var go = new GameObject("RunTrail");
        go.transform.SetParent(player.transform, false);
        go.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.45f;
        trail.minVertexDistance = 0.1f;
        trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(1f, 0f));
        var c = Trails[i].color;
        trail.startColor = new Color(c.r, c.g, c.b, 0.8f);
        trail.endColor = new Color(c.r, c.g, c.b, 0f);
        trail.sharedMaterial = RooftopProps.Mat(new Color(c.r, c.g, c.b, 0.8f), 2f, true);
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }
}
