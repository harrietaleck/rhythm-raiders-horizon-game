using UnityEngine;

/// <summary>
/// Persistent note wallet, power-up upgrades, cosmetic trails and jukebox music (PlayerPrefs).
/// Notes collected during a run can be spent in the jukebox before they are banked.
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

    public struct MusicInfo
    {
        public string id;
        public string name;
        public string artist;
        public int cost;
    }

    /// <summary>Jukebox tracks. Index 0 = each level's own theme (free). AudioManager maps ids to clips.</summary>
    public static readonly MusicInfo[] Music =
    {
        new MusicInfo { id = "theme",   name = "LEVEL THEME",     artist = "Each level plays its own track", cost = 0 },
        new MusicInfo { id = "sunrise", name = "ENDLESS SUNRISE", artist = "Infraction - Fashion EDM",       cost = 40 },
        new MusicInfo { id = "opera",   name = "OPERA GROOVE",    artist = "Rhythm Raiders original",        cost = 50 },
        new MusicInfo { id = "museum",  name = "MUSEUM NIGHTS",   artist = "Rhythm Raiders original",        cost = 60 },
        new MusicInfo { id = "vault",   name = "NEON VAULT",      artist = "Rhythm Raiders original",        cost = 80 },
    };

    const string WalletKey = "Shop_Notes";
    const string MusicKey = "Shop_Music";
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

    // ── Run notes (collected this run, not banked yet) ───────────────────────

    static int _runSpent;
    static bool _runBanked = true;

    /// <summary>Call when a run starts (RunStats.Notes reset to 0).</summary>
    public static void BeginRun()
    {
        _runSpent = 0;
        _runBanked = false;
    }

    /// <summary>Banks this run's notes, minus any already spent in the jukebox.</summary>
    public static void BankRunNotes(int runNotes)
    {
        if (_runBanked) return;
        _runBanked = true;
        AddNotes(Mathf.Max(0, runNotes - _runSpent));
        _runSpent = 0;
    }

    /// <summary>Unbanked notes from the current run that can still be spent.</summary>
    public static int RunNotesAvailable => _runBanked ? 0 : Mathf.Max(0, RunStats.Notes - _runSpent);

    /// <summary>Points the player can spend right now: banked wallet + this run's notes.</summary>
    public static int SpendableNotes => Notes + RunNotesAvailable;

    /// <summary>Spends banked notes (music is bought in the Shop, outside gameplay).</summary>
    static bool Spend(int cost)
    {
        if (cost <= 0) return true;
        if (Notes < cost) return false;
        Notes -= cost;
        return true;
    }

    // ── Jukebox music ────────────────────────────────────────────────────────

    public static bool OwnsMusic(int i) => i == 0 || PlayerPrefs.GetInt("Shop_MusicOwned_" + i, 0) == 1;

    public static int SelectedMusic => Mathf.Clamp(PlayerPrefs.GetInt(MusicKey, 0), 0, Music.Length - 1);

    public static string SelectedMusicId => Music[SelectedMusic].id;

    /// <summary>Buys the track with banked notes if needed, then selects it.</summary>
    public static bool SelectMusic(int i)
    {
        if (i < 0 || i >= Music.Length) return false;
        if (!OwnsMusic(i))
        {
            if (!Spend(Music[i].cost)) return false;
            PlayerPrefs.SetInt("Shop_MusicOwned_" + i, 1);
        }
        PlayerPrefs.SetInt(MusicKey, i);
        PlayerPrefs.Save();
        return true;
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
