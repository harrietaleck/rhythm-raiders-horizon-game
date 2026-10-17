using UnityEngine;

[System.Serializable]
public class LevelDefinition
{
    public int levelNumber = 1;
    public string displayName = "Opera House";
    public string codename = "Pulse Infiltration";
    public string gameplayScene = GameplaySceneNames.L1Opera;
    public string nextIntroScene = "Level2IntroScene";
    public string briefingTitle = "Opera House Briefing";

    public LevelCutsceneType gameplayCutscene = LevelCutsceneType.None;
    public LevelCutsceneType introCutscene = LevelCutsceneType.None;

    [TextArea(2, 4)]
    public string briefingLine1 = "Target artifact confirmed beneath the stage.";
    [TextArea(2, 4)]
    public string briefingLine2 = "The guards respond to movement and sound. Stay sharp.";

    [TextArea(2, 4)]
    public string startHint1 = "Run! Collect two instruments before the guard catches you!";
    [TextArea(2, 4)]
    public string startHint2 = "Jump obstacles and grab power-ups along the way.";

    // ── Movement ──────────────────────────────────────────────────────────────
    public float forwardSpeed    = 5.5f;
    public float guardSpeed      = 3f;
    public float guardBoostSpeed = 22f;
    public float runLookAhead    = 120f;

    // ── Spawning ──────────────────────────────────────────────────────────────
    public int   spawnCount      = 4;
    public int   artifactsToWin  = 2;
    public float artifactScale   = 2.0f;
    public bool  spawnCurtains   = true;

    // ── Difficulty ────────────────────────────────────────────────────────────
    /// <summary>How many obstacle hits before death (shown on HUD as Shield).</summary>
    public int   hitCount        = 10;
    /// <summary>Player starting health for this level.</summary>
    public int   startHealth     = 100;
}

public static class LevelCatalog
{
    // ── L1: Opera House — Easy ────────────────────────────────────────────────
    // Low speed, few obstacles, all curtains, big visible artifacts.
    // Guard is closer in speed ratio; players learn the controls.
    static readonly LevelDefinition Level1 = new LevelDefinition
    {
        levelNumber      = 1,
        displayName      = "Rooftop",
        codename         = "Horizon Run",
        gameplayScene    = "MainGameL1Opera",
        nextIntroScene   = "Level2IntroScene",
        briefingTitle    = "Rooftop — Level 1",
        gameplayCutscene = LevelCutsceneType.RooftopDoor,
        introCutscene    = LevelCutsceneType.None,
        briefingLine1    = "The Conductors stashed a legendary instrument up on the city rooftops.",
        briefingLine2    = "Break out onto the roofs. Collect two artifacts and escape the guard.",
        startHint1       = "Pulse, the artifact is up here somewhere. Keep moving!",
        startHint2       = "Collect two instruments. The guard is already hunting you.",
        forwardSpeed     = 8.0f,     // Comfortable pace — feels like running, not jogging
        guardSpeed       = 6.5f,     // Behind player at baseline; surges create danger
        guardBoostSpeed  = 12f,      // Surge: guard at 12, player at 8 = 4 m/s close rate (fair)
        runLookAhead     = 120f,
        spawnCount       = 6,        // Denser course — more variety, still learnable
        artifactsToWin   = 2,
        artifactScale    = 1.0f,     // Natural size — visible without being overwhelming
        spawnCurtains    = true,
        hitCount         = 10,       // Generous — easy mode
        startHealth      = 100,
    };

    // ── L2: Music Museum — Medium ─────────────────────────────────────────────
    // More speed, more obstacles, museum security door cutscene.
    // Guard pressure increases; rolling drums + alarms.
    static readonly LevelDefinition Level2 = new LevelDefinition
    {
        levelNumber      = 2,
        displayName      = "Music Museum",
        codename         = "Hall of Harmony",
        gameplayScene    = "Level2Museum",
        nextIntroScene   = "Level3IntroScene",
        briefingTitle    = "Music Museum — Level 2",
        gameplayCutscene = LevelCutsceneType.MuseumSecurityDoor,
        introCutscene    = LevelCutsceneType.None,
        briefingLine1    = "Ancient instruments locked behind motion sensors and rolling drum traps.",
        briefingLine2    = "The guards patrol tighter here. Two artifacts — extract before lockdown.",
        startHint1       = "Museum security is heavier. Sneak pickups when you can.",
        startHint2       = "Rolling drums and alarms ahead — jump clean and keep your rhythm.",
        forwardSpeed     = 10.0f,    // Noticeably faster than L1
        guardSpeed       = 8.5f,
        guardBoostSpeed  = 13f,      // Surge: guard at 13, player at 10 = 3 m/s close rate (fair with dense museum rows)
        runLookAhead     = 150f,
        spawnCount       = 8,        // Double the obstacles
        artifactsToWin   = 2,
        artifactScale    = 1.7f,
        spawnCurtains    = false,
        hitCount         = 7,        // Fewer lives
        startHealth      = 100,
    };

    // ── L3: Underground Club — Hard ───────────────────────────────────────────
    // Fastest speed, most obstacles, disco ball cutscene, tightest guard gap.
    // Lasers and sound-wave traps demand fast reactions.
    static readonly LevelDefinition Level3 = new LevelDefinition
    {
        levelNumber      = 3,
        displayName      = "Underground Club",
        codename         = "Neon Beat Vault",
        gameplayScene    = "Level3UndergroundDance",
        nextIntroScene   = "VictoryScene",
        briefingTitle    = "Underground Club — Level 3",
        gameplayCutscene = LevelCutsceneType.ClubDiscoBall,
        introCutscene    = LevelCutsceneType.None,
        briefingLine1    = "The artifact is hidden beneath the main DJ stage.",
        briefingLine2    = "Bass walls pulse on the beat - cross when they go dark. Lasers sweep the floor.",
        startHint1       = "Final run - jump laser fences, dodge speaker stacks, ride the beat.",
        startHint2       = "Collect two artifacts and escape before the club locks down.",
        forwardSpeed     = 11.0f,    // Fastest level, but readable at club density
        guardSpeed       = 9.5f,
        guardBoostSpeed  = 14.5f,    // Surge: guard at 14.5, player at 11 = 3.5 m/s close rate (hard but fair)
        runLookAhead     = 150f,
        spawnCount       = 8,        // Unused - ClubRun builds the course
        artifactsToWin   = 2,
        artifactScale    = 1.5f,
        spawnCurtains    = false,
        hitCount         = 6,        // One fewer shield than Level 2
        startHealth      = 100,
    };

    public static LevelDefinition Get(int level)
    {
        return level switch
        {
            3 => Level3,
            2 => Level2,
            _ => Level1
        };
    }

    public static string GetGameplayScene(int level)
    {
        return Get(level).gameplayScene;
    }
}
