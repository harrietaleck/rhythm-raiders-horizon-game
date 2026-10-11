using System;
using UnityEngine;

/// <summary>
/// One date-seeded goal per day, completed within a single Level 1 run. Rewards banked notes and builds a streak.
/// </summary>
public static class DailyChallenge
{
    public enum Kind { Distance, Notes, Stunts, BeatJumps, Combo }

    static readonly int[][] Targets =
    {
        new[] { 400, 600, 800 },
        new[] { 30, 50, 80 },
        new[] { 3, 5, 7 },
        new[] { 10, 15, 25 },
        new[] { 3, 4, 5 },
    };

    public static Kind TodayKind { get; private set; }
    public static int Target { get; private set; }
    public static int Reward { get; private set; }

    static string _day;

    static string Today => DateTime.Today.ToString("yyyyMMdd");

    static void Refresh()
    {
        if (_day == Today) return;
        _day = Today;
        var rng = new System.Random(int.Parse(_day));
        TodayKind = (Kind)rng.Next(Targets.Length);
        int tier = rng.Next(3);
        Target = Targets[(int)TodayKind][tier];
        Reward = 100 + tier * 50;
    }

    public static string Description
    {
        get
        {
            Refresh();
            switch (TodayKind)
            {
                case Kind.Distance:  return $"Run {Target} m in one run";
                case Kind.Notes:     return $"Collect {Target} notes in one run";
                case Kind.Stunts:    return $"Pull off {Target} stunts (zip-lines / ramps)";
                case Kind.BeatJumps: return $"Land {Target} on-beat jumps";
                default:             return $"Reach a x{Target} combo";
            }
        }
    }

    public static int RewardNotes { get { Refresh(); return Reward; } }

    public static bool CompletedToday => PlayerPrefs.GetInt("Daily_Done_" + Today, 0) == 1;

    public static int Streak
    {
        get
        {
            string last = PlayerPrefs.GetString("Daily_LastDay", "");
            string yesterday = DateTime.Today.AddDays(-1).ToString("yyyyMMdd");
            return last == Today || last == yesterday ? PlayerPrefs.GetInt("Daily_Streak", 0) : 0;
        }
    }

    public static int Progress(float distance, int notes, int stunts, int beatJumps, int bestMultiplier)
    {
        Refresh();
        switch (TodayKind)
        {
            case Kind.Distance:  return Mathf.FloorToInt(distance);
            case Kind.Notes:     return notes;
            case Kind.Stunts:    return stunts;
            case Kind.BeatJumps: return beatJumps;
            default:             return bestMultiplier;
        }
    }

    /// <summary>Marks today complete and pays out. Returns false if already done.</summary>
    public static bool TryComplete(int progress)
    {
        Refresh();
        if (CompletedToday || progress < Target) return false;

        int streak = Streak + 1;
        PlayerPrefs.SetInt("Daily_Done_" + Today, 1);
        PlayerPrefs.SetString("Daily_LastDay", Today);
        PlayerPrefs.SetInt("Daily_Streak", streak);
        ShopData.AddNotes(Reward + Mathf.Min(streak - 1, 5) * 20);
        return true;
    }
}
