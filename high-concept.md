# High Concept — Rhythm Raiders

## One-sentence pitch

**Rhythm Raiders** is a fast-paced 3D chase game where a musician must sprint through a guarded venue, grab stolen instruments before a security guard catches them, and escape before the music stops.

---

## Fantasy & role

You are a **raider of rhythm**—not a warrior, but a runner with reflexes and timing. The world is a stylized music venue / backlot where instruments are scattered like prizes and hazards represent crowd barriers, equipment, and security response. The emotional beat is **tension + momentum**: always moving forward, always hearing the guard close in.

---

## Core pillars

1. **Forward momentum** — The run never pauses; the player manages lateral position and jumps, not stop-and-go exploration.  
2. **Risk vs reward** — Lane changes for pickups and artifacts trade safety for score/progress.  
3. **Readable threat** — The guard is a visible pursuer; lethal obstacles read clearly as “high danger.”  
4. **Musical identity** — Collecting **instruments (artifacts)** ties progression to the theme; audio and narration reinforce the fantasy.  
5. **Short session loops** — Menu → briefing → countdown → run → win/lose → retry in under a few minutes.

---

## Player goals

| Priority | Goal |
|----------|------|
| Primary | Collect **2 artifacts** (instruments) to win the run |
| Secondary | Survive health hazards and slowdowns |
| Failure | Caught by security **or** hit a lethal obstacle |
| Implicit | Improve route knowledge and pickup timing across retries |

---

## Core loop

```
START → Intro narration → Countdown → RUN
                                    ↓
                    Collect pickups / avoid hazards
                                    ↓
              Guard closes distance ←→ Player speed & sneak
                                    ↓
                    WIN (2 artifacts)  or  LOSE (caught / lethal)
                                    ↓
                    Victory scene  or  Death scene → Play again
```

---

## Key mechanics (current build)

| Mechanic | Description |
|----------|-------------|
| Auto-forward run | Constant forward speed; strafe with horizontal input |
| Jump | Single jump per landing; used for obstacles |
| Security guard | Chases from behind; speeds up under conditions; catch = game over |
| Artifacts | Win condition counter; 2 required |
| Pickups | Health, jump boost, speed boost, temporary sneak |
| Obstacles | Slow-down zones; high-danger instant failure |
| Narration | Story beats at start, death, victory; typewriter UI |
| Audio | Music bed, UI SFX, countdown, win/lose stingers |

---

## Tone & presentation

- **Tone:** Light action / arcade pressure, not horror. Defeat is a caught musician, not graphic violence.  
- **Visuals:** URP 3D with Mixamo characters; sci-fi/UI packs for HUD.  
- **UI:** Bold TMP text, NEXT-driven dialogue, scene fades for polish.  
- **Title:** **Rhythm Raiders** (emphasize action and music theft/recovery fantasy).

---

## Audience

- Casual players who enjoy **runner** and **chase** games  
- Students / jam teams demonstrating Unity gameplay systems  
- Players who want **clear win/lose** feedback and quick retries  

---

## Differentiators (vs generic runner)

- **Pursuer AI** with boost phases tied to player state (sneak, speed boost, near-win)  
- **Narrative framing** (narrator, death/victory monologues)  
- **Music-collecting win condition** instead of distance-only scoring  
- **Dual fail states:** guard catch vs environmental lethal hazard  

---

## Scope boundaries (current)

**In scope**

- Single level (`MainGameL1`) with spawners and prefab obstacles  
- Four-scene flow (start, game, death, victory)  
- Local single-player, keyboard input  

**Out of scope / future ideas**

- Rhythm-timed button presses (name implies rhythm; core is chase/run today)  
- Multiple levels or meta progression  
- Multiplayer  
- Mobile touch UI (desktop-first)  

---

## Success metrics (playtest)

- Player understands win condition within first run  
- Narrator text visible before countdown  
- Defeat and victory screens feel conclusive with working NEXT / Play Again  
- No blocking bugs: black screen, pink characters, float jump, instant unfair catch  

---

## Reference mood board (keywords)

Chase · Concert backstage · Heist energy · Neon UI · Countdown tension · Piano defeat · Victory fanfare · Guard spotlight · Instrument loot  
