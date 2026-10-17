using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Offline dialogue bank — default when Ollama/Groq unavailable.
/// Lines sourced from Beat Heist / Beat Horizon concept document.
/// </summary>
public static class ScriptedDialogueBank
{
    static readonly Dictionary<DialogueEvent, string[]> GuardLines = new()
    {
        [DialogueEvent.GuardPatrol] = new[]
        {
            "Patrol route clear.",
            "The artifact must remain protected.",
            "No disturbances detected.",
            "Maintain perimeter.",
        },
        [DialogueEvent.GuardSuspicious] = new[]
        {
            "Did you hear that?",
            "Movement detected on my sector.",
            "Someone is in the building.",
            "Check the east wing.",
        },
        [DialogueEvent.GuardChase] = new[]
        {
            "Stop right there!",
            "Drop the artifact!",
            "You cannot escape!",
            "All guards converge immediately!",
        },
        [DialogueEvent.GuardAlerted] = new[]
        {
            "Protect the artifact!",
            "Intruder alert!",
            "Lockdown protocol engaged!",
            "The artifact cannot leave this building!",
        },
    };

    static readonly Dictionary<DialogueEvent, string[]> PulseLines = new()
    {
        [DialogueEvent.LevelStart] = new[]
        {
            "Target artifact confirmed.",
            "Let's move with the beat.",
            "Time to raid some rhythm.",
        },
        [DialogueEvent.ArtifactCollected] = new[]
        {
            "Artifact secured.",
            "One step closer.",
            "Got it — keep moving!",
            "The city needs this sound.",
        },
        [DialogueEvent.MidRun] = new[]
        {
            "Guard nearby.",
            "Stay on beat.",
            "Almost there — don't stop!",
        },
        [DialogueEvent.Victory] = new[]
        {
            "Artifact extracted successfully.",
            "Beat Horizon lives on!",
            "We beat the Conductors tonight.",
        },
        [DialogueEvent.Defeat] = new[]
        {
            "The guards were too fast.",
            "I'll get it next time.",
            "The artifact is gone — for now.",
        },
    };

    static readonly Dictionary<DialogueEvent, string[]> NarratorLines = new()
    {
        [DialogueEvent.LevelStart] = new[]
        {
            "Every artifact has a rhythm.",
            "The guards respond to movement and sound.",
            "Pulse infiltrates another secured venue.",
        },
    };

    // Level-specific opening lines keyed on LevelDefinition.displayName.
    // These are checked first for LevelStart so each venue gets a unique hint.
    static readonly Dictionary<string, string[]> LevelStartByVenue = new()
    {
        ["Rooftop"] = new[]
        {
            "Two artifacts hidden across the rooftops. Keep moving, Pulse!",
            "The city is yours. Grab both instruments before the guard cuts you off!",
            "Rooftop run. One guard on your tail. Learn the rhythm — now go!",
        },
        ["Music Museum"] = new[]
        {
            "Watch the floor — rolling drum traps ahead. Jump early or get crushed!",
            "Motion alarms fire the moment you slow down. Don't stop, Pulse!",
            "Hall of Harmony: two artifacts, tighter patrols, and drum traps on every path.",
        },
        ["Underground Club"] = new[]
        {
            "Laser grids pulse to the bass drop — weave between the beams!",
            "Sound-wave blasts will throw you sideways. Neon everywhere. Stay focused!",
            "Final vault. Lasers, neon traps, maximum guard pressure. Two artifacts. Go!",
        },
    };

    public static string Pick(DialogueContext ctx)
    {
        // Level-specific opening lines take priority over generic narrator lines.
        if (ctx.eventType == DialogueEvent.LevelStart &&
            LevelStartByVenue.TryGetValue(ctx.levelName, out var venueLines) &&
            venueLines.Length > 0)
        {
            int vi = Mathf.Abs(ctx.detail.GetHashCode()) % venueLines.Length;
            return venueLines[vi];
        }

        Dictionary<DialogueEvent, string[]> bank;
        if (ctx.speakerName.Contains("Guard") || ctx.speakerName.Contains("Security"))
            bank = GuardLines;
        else if (ctx.speakerName.Contains("Pulse") || ctx.speakerName.Contains("Player"))
            bank = PulseLines;
        else
            bank = NarratorLines;

        if (!bank.TryGetValue(ctx.eventType, out var lines) || lines.Length == 0)
        {
            if (GuardLines.TryGetValue(ctx.eventType, out lines)) { }
            else if (PulseLines.TryGetValue(ctx.eventType, out lines)) { }
            else return "Keep moving.";
        }

        int idx = Mathf.Abs((ctx.detail + ctx.artifactsCollected).GetHashCode()) % lines.Length;
        return lines[idx];
    }
}
