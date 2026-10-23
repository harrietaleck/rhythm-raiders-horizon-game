using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>One completed (won) level run.</summary>
[Serializable]
public class LevelRecord
{
    public string playerName;
    public int level;
    public string levelName;
    public int score;
    public int notes;
    public int stars;
    public float distance;
    public float timeSeconds;
    public int artifacts;
    public int artifactMoney;
    public int shieldsLeft;
    public int vibeLeft;
    public int caseBoosts;
    public int caseSourNotes;
    public int caseTreasures;
    public int pockets;
    public string date;   // ISO 8601
}

/// <summary>A full campaign (all levels won), for the Hall of Fame.</summary>
[Serializable]
public class CampaignRecord
{
    public string playerName;
    public int totalScore;
    public int totalStars;
    public int totalNotes;
    public float totalTimeSeconds;
    public string date;
    public List<LevelRecord> levels = new List<LevelRecord>();
}

/// <summary>
/// Saved player progress: the player's name, every level win (recent history), the best run per
/// level, the latest win per level (the current campaign) and the Hall of Fame of finished campaigns.
/// Stored as JSON in Application.persistentDataPath so it survives restarts.
/// </summary>
public static class PlayerRecords
{
    public const string DefaultName = "PLAYER";
    const int MaxHistory = 60;
    const int MaxHallOfFame = 10;

    [Serializable]
    class SaveData
    {
        public string playerName = "";
        public List<LevelRecord> history = new List<LevelRecord>();
        public List<LevelRecord> best = new List<LevelRecord>();
        public List<LevelRecord> latest = new List<LevelRecord>();
        public List<CampaignRecord> hallOfFame = new List<CampaignRecord>();
    }

    static SaveData _data;
    static string FilePath => Path.Combine(Application.persistentDataPath, "rhythm_raiders_records.json");

    static SaveData Data
    {
        get
        {
            if (_data != null) return _data;
            try
            {
                if (File.Exists(FilePath))
                    _data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            }
            catch (Exception e) { Debug.LogWarning("[PlayerRecords] Could not read records: " + e.Message); }
            if (_data == null) _data = new SaveData();
            return _data;
        }
    }

    static void Save()
    {
        try { File.WriteAllText(FilePath, JsonUtility.ToJson(Data, true)); }
        catch (Exception e) { Debug.LogWarning("[PlayerRecords] Could not save records: " + e.Message); }
    }

    // ── Player name ───────────────────────────────────────────────────────────

    public static bool HasPlayerName => !string.IsNullOrWhiteSpace(Data.playerName);
    public static string PlayerName => HasPlayerName ? Data.playerName : DefaultName;

    /// <summary>Sets (or changes) the name; this player's earlier records move to the new name.</summary>
    public static void SetPlayerName(string name)
    {
        name = (name ?? "").Trim();
        if (name.Length > 14) name = name.Substring(0, 14);
        if (name.Length == 0) return;
        string old = PlayerName;
        bool hadName = HasPlayerName;
        Data.playerName = name.ToUpperInvariant();
        foreach (var r in AllLevelRecords()) if (r.playerName == old || string.IsNullOrEmpty(r.playerName)) r.playerName = Data.playerName;
        foreach (var c in Data.hallOfFame) if (c.playerName == old || string.IsNullOrEmpty(c.playerName)) c.playerName = Data.playerName;
        Save();
    }

    static IEnumerable<LevelRecord> AllLevelRecords()
    {
        foreach (var r in Data.history) yield return r;
        foreach (var r in Data.best) yield return r;
        foreach (var r in Data.latest) yield return r;
    }

    // ── Level wins ────────────────────────────────────────────────────────────

    public static LevelRecord LastRecorded { get; private set; }
    public static bool LastWasNewBest { get; private set; }
    public static bool LastCompletedCampaign { get; private set; }

    /// <summary>Builds a record from this run's RunStats and saves it.</summary>
    public static LevelRecord RecordWin(int level, PickupBase pickup)
    {
        var def = LevelCatalog.Get(level);
        int score = RunStats.Score > 0 ? RunStats.Score : RunStats.Notes * 10 + RunStats.ArtifactMoney / 10;
        var rec = new LevelRecord
        {
            playerName = PlayerName,
            level = level,
            levelName = def.codename,
            score = score,
            notes = RunStats.Notes,
            stars = Mathf.Max(1, RunStats.Stars),
            distance = RunStats.Distance,
            timeSeconds = RunStats.RunTime,
            artifacts = RunStats.ArtifactsCollected,
            artifactMoney = RunStats.ArtifactMoney,
            shieldsLeft = pickup != null ? pickup.hitCounter : 0,
            vibeLeft = pickup != null ? pickup.currentHealth : 0,
            caseBoosts = RunStats.CaseBoosts,
            caseSourNotes = RunStats.CaseCurses,
            caseTreasures = RunStats.CaseTreasures,
            pockets = RunStats.Pockets,
            date = DateTime.Now.ToString("s"),
        };

        var d = Data;
        d.history.Insert(0, rec);
        if (d.history.Count > MaxHistory) d.history.RemoveRange(MaxHistory, d.history.Count - MaxHistory);

        var prevBest = Best(level);
        LastWasNewBest = prevBest == null || rec.score > prevBest.score;
        if (LastWasNewBest)
        {
            d.best.RemoveAll(r => r.level == level);
            d.best.Add(rec);
        }
        d.latest.RemoveAll(r => r.level == level);
        d.latest.Add(rec);

        // Winning the final level with every level cleared completes a campaign.
        LastCompletedCampaign = false;
        if (level >= LevelProgress.MaxLevel)
        {
            var campaign = new CampaignRecord { playerName = PlayerName, date = rec.date };
            bool complete = true;
            for (int l = 1; l <= LevelProgress.MaxLevel; l++)
            {
                var r = Latest(l);
                if (r == null) { complete = false; break; }
                campaign.levels.Add(r);
                campaign.totalScore += r.score;
                campaign.totalStars += r.stars;
                campaign.totalNotes += r.notes;
                campaign.totalTimeSeconds += r.timeSeconds;
            }
            if (complete)
            {
                d.hallOfFame.Add(campaign);
                d.hallOfFame.Sort((a, b) => b.totalScore.CompareTo(a.totalScore));
                if (d.hallOfFame.Count > MaxHallOfFame) d.hallOfFame.RemoveRange(MaxHallOfFame, d.hallOfFame.Count - MaxHallOfFame);
                LastCompletedCampaign = true;
            }
        }

        LastRecorded = rec;
        Save();
        return rec;
    }

    public static LevelRecord Best(int level) => Data.best.Find(r => r.level == level);
    public static LevelRecord Latest(int level) => Data.latest.Find(r => r.level == level);
    public static IReadOnlyList<CampaignRecord> HallOfFame => Data.hallOfFame;
    public static IReadOnlyList<LevelRecord> History => Data.history;

    /// <summary>Rank (1-based) of a campaign in the Hall of Fame, or 0 if it didn't place.</summary>
    public static int RankOf(CampaignRecord c) => c == null ? 0 : Data.hallOfFame.IndexOf(c) + 1;
    public static CampaignRecord LastCampaign => LastCompletedCampaign ? Data.hallOfFame.Find(c => c.date == LastRecorded?.date) : null;

    /// <summary>Deletes every saved record and the player name (debug / reset progress).</summary>
    public static void ClearAll()
    {
        _data = new SaveData();
        Save();
    }

    public static string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60f);
        int s = (int)(seconds % 60f);
        return $"{m}:{s:D2}";
    }
}
