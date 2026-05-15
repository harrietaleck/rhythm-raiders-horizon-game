# Setup Guide — Rhythm Raiders

This guide covers opening the project, required software, editor menu tools, and common fixes.

---

## Requirements

| Requirement | Notes |
|-------------|--------|
| **Unity Hub** | [unity.com/download](https://unity.com/download) |
| **Unity Editor** | **6000.3.x** or compatible Unity 6 (project uses URP 17.3) |
| **Disk space** | ~5 GB+ with Library folder generated |
| **OS** | Windows tested; macOS/Linux should work with same Unity version |

Optional:

- **Visual Studio** or **Rider** (C# IDE integration packages included)
- **Git** for version control

---

## First-time project open

1. **Clone or download** the repository.
2. Open **Unity Hub** → **Add** → select the folder `Gads-Music-game`.
3. Use Unity **6000.3.0f1** (or nearest Unity 6) when prompted.
4. Wait for the initial import (Library folder is created; first open can take several minutes).
5. Open scene: `Assets/Scenes/StartScene.unity`.
6. Press **Play** to run from the main menu.

### Build settings

Scenes are registered via **Tools → Setup Build Scenes** or manually in **File → Build Settings**:

- StartScene  
- MainGameL1  
- DeathScene  
- VictoryScene  

---

## Recommended editor setup steps

Run these once after cloning (with `MainGameL1` or target scene open where noted):

| Menu item | When to use |
|-----------|-------------|
| **Tools → Setup Build Scenes** | Build list empty or scenes missing |
| **Tools → Setup Audio** | No music/SFX; add `AudioManager` to scenes |
| **Tools → Setup Narration Scenes** | Rebuild narration UI on menu/end scenes |
| **Tools → Setup Scene References** | Wire GameManager / HUD references in MainGameL1 |
| **Tools → Setup Player & Enemy Animators** | Animator controllers missing or broken |
| **Tools → Fix Character Pink Materials** | Player/guard render magenta/pink |
| **Tools → Fix URP Pink Materials** | Other meshes pink (wrong shader) |
| **Tools → Restore TextMeshPro Shaders** | TMP text pink or invisible |
| **Tools → Fix Player Physics** | Player falls through floor / bad colliders |
| **Tools → Fix Animation Loop Settings** | Animations loop incorrectly |

---

## Audio assets

Clips live in `Assets/Audios/`:

| File | Use |
|------|-----|
| `background_sound.mp3` | Looping gameplay music |
| `button_sound.mp3` | UI clicks |
| `count_down_sound.mp3` | Pre-run countdown |
| `game_victory_sound.mp3` | Victory scene |
| `piano_GameOver_Sound.mp3` | Death scene |

**Tools → Setup Audio** assigns clips to `AudioManager` on scenes.

---

## MCP / Cursor (optional)

The project may include `io.realvirtual.mcp` in `Packages/manifest.json` for Unity MCP integration in Cursor.

- Do **not** commit `Assets/.mcp_auth_token` (local auth).
- `Assets/StreamingAssets/realvirtual-MCP/` is generated tooling data — commit only if your team uses MCP.

---

## Running a build

1. **File → Build Settings**
2. Platform: **PC, Mac & Linux Standalone** (or your target)
3. Ensure all four scenes are enabled and ordered: Start → MainGame → Death → Victory
4. **Build** or **Build And Run**

---

## Troubleshooting

### Black screen on play

- Check **SceneFader** exists; fade should clear after ~0.5s.
- Ensure MainGameL1 **Canvas** scale is `(1,1,1)` not zero.
- Disable duplicate legacy `FadeOverlay` objects if present.

### Narrator panel not visible

- Narration runs **before** countdown on MainGameL1.
- Confirm `NarrationManager` on Canvas has `NarrationPanel` assigned.
- Canvas scale must be non-zero.

### Pink characters (URP)

1. **Tools → Fix Character Pink Materials**
2. Play scene — `CharacterMaterialApplier` on models applies at runtime
3. Materials saved under `Assets/Materials/Characters/`

### NEXT buttons not clickable / no cursor

- Death and Victory scenes use `UICursor.UnlockForMenus()`.
- Buttons use `UIButtonRaycastFix` — TMP child text should not block rays.

### Jump floats or double-jumps

- `PlayerMovement` creates a **GroundCheck** child at the feet.
- Do not assign `groundCheck` to the Ground platform transform.

### Enemy too fast / instant catch

- Speeds in `EnemyBase`: default chase ~9, boosted ~34, after boost ~4 (tune in script/prefab).
- Enemy collider should be a **trigger** for `CharacterController` player.

### Compile errors

- Open **Console** window; fix red errors before Play.
- Common fixes already applied: `PlayerMovement` velocity type, `FixCharacterURPMaterials` mesh access.

### TMP warning: missing ▶ glyph

- Harmless: NEXT buttons use Unicode ▶; font shows □.
- Change button text to `NEXT >` in scenes if desired.

---

## Project structure (essentials)

```
Assets/
  Scenes/           # StartScene, MainGameL1, DeathScene, VictoryScene
  Scripts/          # Gameplay, narration, audio, UI, editor tools
  Audios/           # Music and SFX
  Animations/       # Player & guard .dae / controllers
  Materials/        # URP character materials
  prefab/           # Enemy prefab
Packages/
  manifest.json     # URP, Input System, MCP package
ProjectSettings/
```

---

## Contact / support

For course or team projects, document your Unity version in commits and keep `refinements-changes.md` updated when merging gameplay changes.
