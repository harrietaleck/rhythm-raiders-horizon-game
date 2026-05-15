# Claude / AI Assistant Context — Rhythm Raiders

This file helps AI coding assistants (Claude, Cursor Agent, etc.) work effectively in this repository.

---

## Project identity

- **Display name:** Rhythm Raiders  
- **Folder / Unity product:** `Gads-Music-game` (legacy naming)  
- **Genre:** 3D forward-scrolling chase / collection runner  
- **Engine:** Unity 6, URP 17.x, C#  

---

## What this game does

Player runs forward automatically, strafes left/right, jumps once per landing, collects **2 artifacts** (instruments) to win, avoids obstacles, and outruns a security guard. Caught by guard or lethal obstacle → defeat animation → Death scene. Win → Victory scene.

---

## Scene flow

```
StartScene → MainGameL1 → DeathScene | VictoryScene
                ↑________________________|
                   (Play Again buttons)
```

- **StartScene:** `StartSceneUI`, intro narration, Play → MainGameL1  
- **MainGameL1:** `GameManager` (narration → countdown → GO), `NarrationManager`, player, enemy, spawners  
- **DeathScene / VictoryScene:** `DeathHUD` / `VictoryHUD`, narration, NEXT, Play Again  

---

## Critical architecture rules

1. **Death must go through `GameManager.TriggerGameOver()`** — sets `IsDead` on player animator, shows GAME OVER text, fades to DeathScene. Do not `LoadScene("DeathScene")` directly from obstacles unless you also trigger defeat anim.

2. **Victory must go through `PickupBase.TryTriggerVictory()` or `GameManager.TriggerVictory()`** — requires `artifactAmount >= PickupBase.ArtifactsToWin` (currently **2**).

3. **`SceneFader.Instance`** — use `SceneFader.LoadScene(name)` for transitions. Fade image is private (`_fadeImage`); do not reference `fadeImage` from editor setup scripts.

4. **Player uses `CharacterController`** + trigger `CapsuleCollider` + kinematic `Rigidbody`. Enemy catch needs **trigger** collider and `OnTriggerEnter`.

5. **Ground check** — `PlayerMovement.EnsureFootGroundCheck()` creates child `GroundCheck` at feet. Never assign `groundCheck` to the scrolling Ground platform.

6. **Narration before gameplay** — `GameManager.StartCountdown()` yields `PlayStartNarrationAndWait()` then counts down. Do not show start narration only after GO.

7. **Canvas scale** — MainGameL1 root Canvas must be scale (1,1,1).

---

## Important types

| Type | Location |
|------|----------|
| `GameManager` | `Assets/Scripts/GameManager.cs` |
| `PickupBase` | `Assets/Scripts/Small Prefabs/PickupBase.cs` |
| `EnemyBase` | `Assets/Scripts/Enemy/EnemyBase.cs` |
| `PlayerMovement` | `Assets/Scripts/Player/PlayerMovement.cs` |
| `PlayerAnimationController` | `Assets/Scripts/PlayerAnimationController.cs` |
| `NarrationManager` | `Assets/Scripts/Narration/NarrationManager.cs` |
| `SceneFader` | `Assets/Scripts/Narration/SceneFader.cs` |
| `AudioManager` | `Assets/Scripts/Audio/AudioManager.cs` |
| `HealthDecreaseObstacle` | `Assets/Scripts/HealthDecreaseObstacle.cs` |

---

## Editor tools (Tools menu)

Prefer existing menu commands over hand-editing scenes:

- `Fix Character Pink Materials`
- `Setup Audio` / `Setup Narration Scenes` / `Setup Build Scenes`
- `Setup Player & Enemy Animators`
- `Fix Player Physics`

---

## Do not commit

- `Assets/.mcp_auth_token`
- `.screenshots/` unless documenting deliberately
- `Library/`, `Temp/`, `Logs/`

---

## Common user requests → where to look

| Request | Files |
|---------|--------|
| Black screen | `SceneFader.cs`, scene Canvas scale, legacy FadeOverlay |
| Narrator not showing | `GameManager.cs`, `NarrationManager.cs`, MainGameL1 Canvas |
| Pink materials | `FixCharacterURPMaterials.cs`, `CharacterMaterialApplier.cs` |
| Jump broken | `PlayerMovement.cs`, `PlayerAnimationController.cs` |
| Enemy speed | `EnemyBase.cs`, `Assets/prefab/Enemy.prefab`, MainGameL1 prefab overrides |
| Buttons / cursor | `UICursor.cs`, `UIButtonRaycastFix.cs`, `DeathHUD`, `VictoryHUD` |
| Win too early/late | `PickupBase.ArtifactsToWin`, `ArtifactGoal.cs`, `MusicInstrument.cs` |

---

## Style preferences for AI edits

- Minimal diffs; match existing naming and patterns.  
- Do not commit unless the user asks.  
- Do not add verbose comments on obvious code.  
- Test compile mentally: editor scripts use `MeshFilter.sharedMesh`, not `MeshRenderer.sharedMesh`.  
- `PlayerMovement.velocity` is a **float** (vertical speed), not `Vector3`.  

---

## Related docs

- [README.md](README.md) — overview  
- [setup.md](setup.md) — install & troubleshooting  
- [refinements-changes.md](refinements-changes.md) — changelog  
- [high-concept.md](high-concept.md) — design intent  
- [prompt-used.md](prompt-used.md) — session prompts  
