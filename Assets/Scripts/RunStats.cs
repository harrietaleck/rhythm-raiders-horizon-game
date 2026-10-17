/// <summary>
/// Persists the last run's stats and death cause for DeathScene / VictoryScene.
/// </summary>
public enum DeathCause
{
    Unknown,
    Guard,
    Obstacle,
    Fall
}

public static class RunStats
{
    public static int        ArtifactsCollected { get; private set; }
    public static int        ArtifactMoney      { get; private set; }
    public static float      Distance           { get; private set; }
    public static float      RunTime            { get; private set; }
    public static DeathCause LastDeathCause     { get; private set; } = DeathCause.Unknown;

    public static int        Notes              { get; set; }
    public static int        Score              { get; set; }
    public static int        Stars              { get; set; }
    public static float      BestDistance       { get; set; }
    public static bool       DailyCompleted     { get; set; }

    private static float _runStartTime = -1f;

    /// <summary>Call when gameplay actually begins (after countdown / cutscene).</summary>
    public static void MarkRunStart()
    {
        _runStartTime = UnityEngine.Time.time;
        CaseBoosts = CaseCurses = CaseTreasures = 0;
        Pockets = 0;
    }

    // ── Museum glass cases (Level 2) ─────────────────────────────────────────
    /// <summary>Level 3: bass walls crossed while dark.</summary>
    public static int Pockets { get; set; }
    public static int CaseBoosts    { get; private set; }
    public static int CaseCurses    { get; private set; }
    public static int CaseTreasures { get; private set; }
    public static bool AnyCases => CaseBoosts + CaseCurses + CaseTreasures > 0;

    public enum CaseResult { Boost, Curse, Treasure }

    public static void RecordCase(CaseResult result)
    {
        switch (result)
        {
            case CaseResult.Boost:    CaseBoosts++;    break;
            case CaseResult.Curse:    CaseCurses++;    break;
            case CaseResult.Treasure: CaseTreasures++; break;
        }
    }

    /// <summary>"Glass cases: 3 boosts | 1 sour note | 1 treasure" (rich text), or "" if none were smashed.</summary>
    public static string FormatCaseLine()
    {
        if (!AnyCases) return "";
        string Plural(int n, string word) => $"{n} {word}{(n == 1 ? "" : "s")}";
        return $"Glass cases:  <color=#5AD9FF>{Plural(CaseBoosts, "boost")}</color>   |   " +
               $"<color=#FF6A5A>{Plural(CaseCurses, "sour note")}</color>   |   " +
               $"<color=#E6EEFF>{Plural(CaseTreasures, "treasure")}</color>";
    }

    /// <summary>Name of the obstacle that last hit the player (shown on Game Over).</summary>
    public static string LastKiller { get; private set; }

    public static void SetDeathCause(DeathCause cause)
    {
        LastDeathCause = cause;
    }

    public static void SetKiller(string name)
    {
        LastKiller = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
    }

    /// <summary>"a Laser Grid", "an AC Unit", or "an obstacle" when unknown.</summary>
    static string KillerPhrase()
    {
        if (string.IsNullOrEmpty(LastKiller)) return "an obstacle";
        char c = char.ToLowerInvariant(LastKiller[0]);
        bool vowel = "aeiou".IndexOf(c) >= 0 || LastKiller.StartsWith("AC ");
        return (vowel ? "an " : "a ") + LastKiller;
    }

    public static void SaveFrom(PickupBase pickup)
    {
        // Capture elapsed run time
        RunTime = _runStartTime >= 0f
            ? UnityEngine.Mathf.Max(0f, UnityEngine.Time.time - _runStartTime)
            : 0f;
        _runStartTime = -1f;

        var player = UnityEngine.GameObject.FindGameObjectWithTag("Player");
        Distance = player != null ? UnityEngine.Mathf.Max(0f, player.transform.position.z) : 0f;

        if (pickup == null)
        {
            ArtifactsCollected = 0;
            ArtifactMoney      = 0;
            return;
        }

        ArtifactsCollected = pickup.artifactAmount;
        ArtifactMoney      = pickup.artifactMoneyTotal;
    }

    public static string GetCauseHeadline()
    {
        return LastDeathCause switch
        {
            DeathCause.Guard    => "You were caught by the guard!",
            DeathCause.Obstacle => $"You were stopped by {KillerPhrase()}!",
            DeathCause.Fall     => "You fell off the track!",
            _                   => "Your run has ended."
        };
    }

    public static string[] GetCauseNarration()
    {
        return LastDeathCause switch
        {
            DeathCause.Guard => new[]
            {
                "You were caught by the security guard!",
                "Don't give up — the instruments are still out there.",
                "Try again and stay ahead of the guard!"
            },
            DeathCause.Obstacle => new[]
            {
                string.IsNullOrEmpty(LastKiller) ? "A dangerous obstacle ended your run!" : $"{char.ToUpperInvariant(KillerPhrase()[0])}{KillerPhrase().Substring(1)} ended your run!",
                "Dodge hazards and grab health pickups when you can.",
                "Try again — watch the path ahead!"
            },
            DeathCause.Fall => new[]
            {
                "You fell off the track!",
                "Keep to the path and don't miss the ground.",
                "Try again — stay on your feet!"
            },
            _ => new[]
            {
                "Your run has ended.",
                "Don't give up — the instruments are still out there.",
                "Try again and make it to the goal!"
            }
        };
    }

    static string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60);
        int s = (int)(seconds % 60);
        return m > 0 ? $"{m}m {s:D2}s" : $"{s}s";
    }

    public static string FormatScoreSummary()
    {
        string time = RunTime > 0f ? $"\nTime: {FormatTime(RunTime)}" : "";
        string run = Score > 0 || Notes > 0
            ? $"\nNotes: {Notes}   Score: {Score:N0}\nDistance: {Distance:0}m   Best: {BestDistance:0}m"
            : "";
        string stars = Stars > 0
            ? $"\n<color=#FFD54A>Rating: {Stars} / 3 stars</color>"
            : "";
        string wallet = Notes > 0 ? $"\nNotes banked for the shop: {ShopData.Notes}" : "";
        string daily = DailyCompleted ? "\n<color=#5AD9FF>Daily challenge complete!</color>" : "";
        string cases = AnyCases ? "\n" + FormatCaseLine() : "";
        return $"Artifacts: {ArtifactsCollected} / {PickupBase.ArtifactsToWin}\n" +
               $"Value: ${ArtifactMoney:N0}{time}{run}{cases}{stars}{wallet}{daily}";
    }

    public static string FormatRunSummaryLine()
    {
        string time = RunTime > 0f ? $"   |   {FormatTime(RunTime)}" : "";
        string cases = AnyCases ? "\n" + FormatCaseLine() : "";
        return $"Distance: {Distance:0}m   |   Fragments: {ArtifactsCollected}   |   ${ArtifactMoney:N0}{time}{cases}";
    }
}
