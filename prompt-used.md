# Prompts Used — AI-Assisted Development

This document records **user prompts** from the Cursor AI session used to build and refine **Rhythm Raiders** (`Gads-Music-game`). Prompts are listed in chronological order. Assistant work included code changes, scene fixes, and Unity troubleshooting.

---

## Session reference

- **Transcript ID:** `c3c852d2-6874-45d5-ba93-17423cd92855`  
- **Approximate date:** May 2026  
- **Primary tools:** Cursor Agent, Unity Editor, Unity MCP (optional)  

---

## User prompts (chronological)

### 1. Black screen / cannot see gameplay

> There is any error i cant seem to fix. a black screen and i cant view the game play. can you fix it.

**Context:** Screenshot of black game view.  
**Work done:** Scene fade overlay fixes, `SceneFader` persistence, disabled legacy fade blocking view.

---

### 2. NEXT buttons not working

> Now the next buttons seem not to be functional. can you check whats wrong and fix them

**Work done:** Button raycast / UI interaction fixes, narration advance wiring.

---

### 3. Title, narrator audio, game over / victory NEXT

> Instead of gads musi game . write the name of the game which is Rhythm Raiders. again if the narrator sound is done it should stop and not repeat. the next button in the game over is still now working as well as the one in victory

**Work done:** Rebranded to Rhythm Raiders; narrator `loop = false`; Death/Victory HUD button fixes.

---

### 4. Reduce guard speed

> Can you reduce the security gaurd (enemy) speed by maybe 3

**Work done:** Lowered `EnemyBase` chase speeds.

---

### 5. Cursor disappears on win / game over

> The next burron in win screen and game over screen not working properperly the cursor seem to disappear

**Work done:** `UICursor` unlock on menu/end scenes; button interaction fixes.

---

### 6. Pink character textures (×2)

> Now the texture are still pink for the 2 object characters can you fix them

**Work done:** URP Lit materials, `FixCharacterURPMaterials`, `CharacterMaterialApplier`, normal map fixes.

---

### 7. Win only after 10 artifacts

> Win screen must only showif 10 artifacts are collected. fix it

**Work done:** Victory gated on artifact count via `TryTriggerVictory`.

---

### 8. Reduce win to 2 artifacts

> Lets reduce win will show 2 artifacts collected

**Work done:** `PickupBase.ArtifactsToWin = 2`.

---

### 9. Narrator screen not showing

> Also the narrator screen is not showing animore

**Work done:** Narration timing, fade sync, canvas/panel visibility (later: pre-countdown narration + canvas scale).

---

### 10. Single jump only

> Can you fix the jump it should just be one jump

**Work done:** `canJump` flag, `DidJumpThisFrame`, animation only on deliberate jump.

---

### 11. Compiler error

> There is a compilor erroe cant play the game

**Work done:** Fixed `PlayerMovement` (`velocity.y` on float → `velocity < 0f`).

---

### 12. Narrator, game over on catch, high danger (×2)

> Some fixture . the narrator screen before game play is not showing, 2. the game sshould be game over after player is caught by enemy and player must show the defeat animation. 3. game must be over if player heat high danger obstacle

**Work done:** Pre-countdown narration; instant catch → `KillPlayer()` / defeat anim; `HealthDecreaseObstacle.instantKill`; canvas scale fix.

---

### 13. Jump — player stays in air

> There is a proplem with the jump it stays up player must land back

**Work done:** Foot `GroundCheck` (was wired to Ground platform); proper grounded detection and landing.

---

### 14. Enemy still too fast

> The enemy is a little too fast running after player reduce speed by 3

**Work done:** Further speed reduction in `EnemyBase` and scene/prefab overrides.

---

### 15. Pre-commit check

> Check the warning and errors before i commit

**Work done:** Unity log review; compile OK; warnings (TMP ▶ glyph, debug logs); exclude `.mcp_auth_token` advice.

---

### 16. Commit summary

> Create a commit sumary description of what i worked on

**Work done:** Provided commit title and multi-section description (not committed by AI).

---

### 17. Documentation request (this file set)

> Can you create setup.md, readme.md, refinements-changes.md, claude.md, high concept document and prompt -used.md

**Work done:** Created project documentation suite.

---

## How to use this file

- **For coursework:** Cite as “AI-assisted debugging and implementation prompts.”  
- **For team handoff:** Pair with [refinements-changes.md](refinements-changes.md) for what changed per prompt.  
- **For future AI sessions:** Add new prompts at the bottom with date and outcome.  

---

## Template for new entries

```markdown
### N. Short title

> Exact user prompt here.

**Work done:** Bullet list of files/systems touched.
```
