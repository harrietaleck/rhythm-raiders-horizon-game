# Rhythm Raiders

**Rhythm Raiders** is a 3D forward-scrolling chase/collection runner built in Unity 6 (URP). You sprint down a scrolling track, collect **2 musical artifacts** to win, dodge obstacles, and stay ahead of a security guard. Three themed levels take you from sunset city rooftops to a museum to an underground club, with a persistent note wallet, shop upgrades and a daily challenge between runs.

> Repository folder: `Gads-Music-game` · Unity product name: **Rhythm Raiders**  
> Branch for active development: `harriet's_work`

---

## Features

- **Three themed levels** — Sunset Rooftops (L1), Museum (L2), Underground Club (L3)
- **Start flow** — Splash (tap to start) → main menu → intro narration → level
- **Level 1 rooftop run** — continuously streamed street at dusk, three districts (Residential Rooftops → Neon Market at 300 m → Construction Zone at 600 m), speed ramp, score/combo multiplier, on-beat jumps (110 BPM), star rating and best distance
- **Rooftop hazards & stunts** — roof gaps, steam vents, wet patches, alarm panels, swinging hazards, pigeon flocks, searchlight helicopter (from 350 m), zip-lines and launch ramps
- **Power-ups** — Magnet, Shield Bubble, Smoke Bomb (stuns the guard)
- **Shop & progression** — notes collected in runs go into a persistent wallet; spend them in the Shop on upgrades (Magnet, Bubble, Smoke Bomb, Head Start — 3 levels each) and cosmetic trails
- **Daily challenge** — one date-seeded goal per day (distance, notes, stunts, beat jumps or combo) that pays bonus notes and builds a streak
- **Guide scene** — how-to-play for every level plus credits
- **Dual health system** — Shield (absorbs hits) + Vibe (direct drain from obstacles)
- **Enemy chase** — Security guard pursues the player with rubber-banding, a surge warning before speed bursts, and can be stunned; contact triggers defeat animation → Death scene
- **Sci-fi HUD** — Icon-led rows for Vibe, Shield, Artifacts, Guard distance, Boost, Score, Level
- **Pause menu** — Escape to pause; Resume / Settings (music + SFX sliders) / Main Menu
- **Pickups** — Health, Jump Boost, Speed Boost, Sneak, Artifacts (goal)
- **Obstacles** — Slow-down zones and health-draining hazards (drain both Shield and Vibe)
- **Level set dressing** — Per-level themed decorations: museum displays (L2), neon club strips (L3)
- **Win / lose** — Victory scene (2 artifacts collected) · Death scene (caught or lethal hit)
- **Audio** — Background music, SFX, countdown ticks, victory / game-over themes
- **Narration** — Typewriter dialogue with scene fades and level intro screens

---

## Tech stack

| Item | Version / detail |
|------|------------------|
| Engine | Unity 6 (6000.3.0f1) |
| Render pipeline | Universal Render Pipeline (URP) 17.x |
| UI | uGUI + TextMesh Pro |
| Input | Legacy Input Manager (`Horizontal`, `Jump`) |
| Characters | Mixamo-style `.dae` models (Ch03 player, Ch15 guard) |
| Icon pack | AIRIDev Sci-fi UI Icons |

---

## Scenes (build order)

| # | Scene file | Purpose |
|---|-----------|---------|
| 0 | `Assets/Scenes/SplashScene.unity` | Horizon splash art — tap to start |
| 1 | `Assets/Scenes/StartScene.unity` | Main menu (Play, Levels, Shop, Guide) & intro narration |
| 2 | `Assets/Scenes/MainGameL1Opera.unity` | Level 1 — Sunset Rooftops (scene keeps legacy Opera name) |
| 3 | `Assets/Scenes/Level2IntroScene.unity` | Level 2 door intro |
| 4 | `Assets/Scenes/Level2Museum.unity` | Level 2 — Museum |
| 5 | `Assets/Scenes/Level3IntroScene.unity` | Level 3 door intro |
| 6 | `Assets/Scenes/Level3UndergroundDance.unity` | Level 3 — Underground Club |
| 7 | `Assets/Scenes/DeathScene.unity` | Game over |
| 8 | `Assets/Scenes/VictoryScene.unity` | Win screen |
| 9 | `Assets/Scenes/GuideScene.unity` | How to play & credits |
| 10 | `Assets/Scenes/ShopScene.unity` | Upgrades, trails, daily challenge |

Run **Tools → Setup Build Scenes** to register all scenes in Unity Build Settings.

---

## Scene flow

```
SplashScene → StartScene ─┬→ MainGameL1Opera → Level2Intro → Level2Museum → Level3Intro → Level3UndergroundDance
                          ├→ ShopScene ──→ back to StartScene
                          └→ GuideScene ─→ back to StartScene
         any level ↘ DeathScene ↗ (Play Again)
                   ↘ VictoryScene ↗ (Play Again / Next Level)
```

---

## Quick start

1. Clone the repo and open the folder in **Unity Hub** (Unity 6 recommended).
2. Open `MainGameL1Opera` or press **Play** from `StartScene`.
3. If materials look pink, run **Tools → Fix Character Pink Materials** (see [setup.md](setup.md)).
4. If HUD is missing, run **Tools → Setup Gameplay HUD (Sci-Fi Icons)**.
5. If pause menu buttons do not respond, run **Tools → Setup Pause Screen**.

Full environment and editor setup: **[setup.md](setup.md)**

---

## Controls

| Action | Input |
|--------|--------|
| Move left / right | `A` / `D` or arrow keys |
| Jump | `Space` |
| Advance dialogue | `Space` / `Enter` or **NEXT** button |
| Pause / unpause | `Escape` |
| UI click | Mouse (cursor auto-unlocked on menus / death / victory) |

---

## Editor tools (Tools menu)

| Tool | What it does |
|------|-------------|
| Fix Character Pink Materials | Remaps Mixamo .dae materials to URP Lit |
| Setup Audio | Wires music & SFX clips across all scenes |
| Setup Narration Scenes | Applies narration panel layout |
| Setup Build Scenes | Registers all 11 scenes in Build Settings |
| Setup Splash Scene / Setup Guide Scene / Setup Shop Scene | Builds those scenes' UI |
| Setup Level 1 Street Stream | Wires the streamed rooftop street + `RunDirector` in Level 1 |
| Debug → Open Level 1/2/3, Open Shop (Play Mode) | Jump straight to a scene while playing |
| Debug → Add 500 Shop Notes | Test the shop without grinding |
| Setup Gameplay HUD (Sci-Fi Icons) | Builds icon-led in-game HUD |
| Setup Pause Screen | Adds pause panel + settings overlay to all gameplay scenes |
| Fix Player Physics | Corrects CharacterController + trigger collider setup |
| Setup Player & Enemy Animators | Wires animator controllers |

---

## Key scripts

| Script | Role |
|--------|------|
| `GameManager` | Countdown, narration timing, game over / victory |
| `PlayerMovement` | CharacterController movement, jump, gravity |
| `PickupBase` | Pickups, dual health (Shield + Vibe), artifacts, `KillPlayer()` |
| `EnemyBase` | Chase AI, catch → game over |
| `PauseMenuHUD` | Pause menu: resume, settings sliders, main menu |
| `HUDfunctions` | In-game HUD: Vibe, Shield, Artifacts, Guard, Score, Level |
| `NarrationManager` | Dialogue typewriter & auto-advance |
| `SceneFader` | Cross-scene black fades |
| `AudioManager` | Music & SFX singleton |
| `LevelBootstrap` | Per-level runtime config (theme, spawner rules, health values) |
| `LevelSetDressing` | Builds per-level visual decorations (L2/L3) |
| `RunDirector` | Level 1 pacing: distance, districts, dusk lighting, speed ramp, combo, beat jumps, power-ups, stars, run HUD |
| `StreetRunStream` | Streams Level 1 street tiles and district set pieces |
| `ShopData` | Note wallet, upgrade levels and trails (PlayerPrefs) |
| `DailyChallenge` | Date-seeded daily goal, reward and streak |
| `ShopSceneUI` / `GuideSceneUI` / `SplashScreenController` | Menu-side scenes |
| `GameplaySceneNames` | Single source of truth for all scene name constants |
| `HealthDecreaseObstacle` | Data container: Shield damage, Vibe damage, instant-kill flag |

---

## Documentation

| File | Purpose |
|------|---------|
| [setup.md](setup.md) | Install, Unity version, editor tools, troubleshooting |
| [refinements-changes.md](refinements-changes.md) | Full changelog of fixes and features |
| [high-concept.md](high-concept.md) | Game vision, pillars, and player fantasy |
| [CLAUDE.md](CLAUDE.md) | Context for AI assistants working in this repo |
| [prompt-used.md](prompt-used.md) | Prompts used during AI-assisted development |
| [feedback-summary.md](feedback-summary.md) | Structured Goethe-Institut playtest feedback record |
| [critical-feedback.md](critical-feedback.md) | Critical engagement with external feedback |
| [final-reflection-report.md](final-reflection-report.md) | Final reflection (600–800 words) |
| [goethe-improvements-plan.md](goethe-improvements-plan.md) | Improvement plan from playtest |
| [attendance-evidence.md](attendance-evidence.md) | Meetup attendance evidence |

---

## License & assets

Third-party packs live under `Assets/` (UI buttons, TMP examples, Mixamo animations, AIRIDev Sci-fi icons, etc.). Check each asset pack's licence before redistribution.

---

## Contributing

Before committing:

- Exclude `Assets/.mcp_auth_token` and local `.screenshots/` unless intentional.
- Confirm Unity Console has **no compile errors**.
- Use **`GameplaySceneNames`** constants — never hardcode scene name strings.
- See [refinements-changes.md](refinements-changes.md) for the latest changelog.
