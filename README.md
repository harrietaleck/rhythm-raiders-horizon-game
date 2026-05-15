# Rhythm Raiders

**Rhythm Raiders** is a 3D endless-runner style Unity game where you sprint down a scrolling path, collect musical instruments (artifacts), dodge obstacles, and stay ahead of a security guard. Collect **2 artifacts** to win; get caught or hit a lethal hazard and it's game over.

> Repository folder name: `Gads-Music-game` · Unity product name in Project Settings may still show the legacy name.

---

## Features

- **Start flow:** Title screen → intro narration → main level
- **Main level:** 5–4–3–2–1 countdown → narrator hints → run/jump gameplay
- **Enemy chase:** Security guard pursues the player; contact triggers defeat animation and death scene
- **Pickups:** Health, jump boost, speed boost, sneak, artifacts
- **Obstacles:** Slow-down zones and high-danger instant-kill hazards
- **Win / lose:** Victory scene (2 artifacts) · Death scene (caught or lethal hit)
- **Audio:** Background music, SFX, countdown ticks, victory / game-over themes
- **Narration:** Typewriter dialogue with optional voice clips and scene fades

---

## Tech stack

| Item | Version / detail |
|------|------------------|
| Engine | Unity 6 (6000.x) |
| Render pipeline | Universal Render Pipeline (URP) 17.x |
| UI | uGUI + TextMesh Pro |
| Input | Legacy Input Manager (`Horizontal`, `Jump`) |
| Characters | Mixamo-style `.dae` models (Ch03 player, Ch15 guard) |

---

## Scenes (build order)

1. `Assets/Scenes/StartScene.unity` — Main menu & intro narration  
2. `Assets/Scenes/MainGameL1.unity` — Core gameplay  
3. `Assets/Scenes/DeathScene.unity` — Game over  
4. `Assets/Scenes/VictoryScene.unity` — Win screen  

---

## Quick start

1. Clone the repo and open the folder in **Unity Hub** (Unity 6 recommended).
2. Open `MainGameL1` or press **Play** from `StartScene`.
3. If materials look pink, run **Tools → Fix Character Pink Materials** (see [setup.md](setup.md)).

Full environment and editor setup: **[setup.md](setup.md)**

---

## Documentation

| File | Purpose |
|------|---------|
| [setup.md](setup.md) | Install, Unity version, editor tools, troubleshooting |
| [refinements-changes.md](refinements-changes.md) | Changelog of fixes and polish from development |
| [high-concept.md](high-concept.md) | Game vision, pillars, and player fantasy |
| [claude.md](claude.md) | Context for AI assistants working in this repo |
| [prompt-used.md](prompt-used.md) | User prompts used during AI-assisted development |

---

## Controls

| Action | Input |
|--------|--------|
| Move left / right | `A` / `D` or arrow keys |
| Jump | `Space` |
| Advance dialogue | `Space` / `Enter` or **NEXT** button |
| UI click | Mouse (cursor unlocked on menu / death / victory) |

---

## Key scripts

| Script | Role |
|--------|------|
| `GameManager` | Countdown, narration timing, game over / victory |
| `PlayerMovement` | CharacterController movement, jump, gravity |
| `PickupBase` | Pickups, health, artifacts, `KillPlayer()` |
| `EnemyBase` | Chase AI, catch → game over |
| `NarrationManager` | Dialogue typewriter & auto-advance |
| `SceneFader` | Cross-scene black fades |
| `AudioManager` | Music & SFX singleton |

---

## License & assets

Third-party packs live under `Assets/` (UI buttons, TMP examples, Mixamo animations, etc.). Check each asset pack’s license before redistribution.

---

## Contributing

Before committing:

- Exclude `Assets/.mcp_auth_token` and local `.screenshots/` unless intentional.
- Confirm Unity Console has **no compile errors**.
- See [refinements-changes.md](refinements-changes.md) for what changed recently.
