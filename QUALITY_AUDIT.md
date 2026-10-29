# QUALITY AUDIT & IMPLEMENTATION PLAN
**Project:** Dark-Fantasy 2D Action Platformer  
**Unity Version:** 6.6 (6000.0.67f1) | **Platform:** PC  
**Audit Date:** October 2026

---

## 1. Executive Summary
The project has a solid foundation: 
- 22 synthesized 16-bit 44.1kHz audio sound effects wired into `AudioManager`.
- Responsive Knight movement with coyote time, jump buffer, slope compensation, variable jump height, dodge, roll, light combo attacks, heavy charging ground impact, and parry deflection.
- Armored Guard enemy AI with patrol, detection, chase, and telegraphed combat.
- A 2-Phase Boss battle (Malakor, the Abyssal Warlord) with poise/hyper-armor, leap slam, double cleave, and a cinematic awakening leap from the colossal background Skull Throne.

However, several critical gaps separate this prototype from a **premium, cohesive, commercial-grade vertical slice**:
1. **Scene Cleanliness & Leftovers**: Uncleaned runtime test objects (`BloodSplatter_FX`, multiple `[BloodSplatterSystem]` duplicates, `Heal_FX` clones) litter the scene root.
2. **Camera & Backdrop Tone**: Camera clear color is set to default Unity sky blue (`#314D79`), causing jarring bright blue flashes if the camera catches an edge.
3. **Atmospheric FX & Lighting**: While running URP 2D, the world lacks ambient drifting ash/ember particles, dynamic red torch/rim lighting on the Skull Throne and braziers, and a subtle infernal haze.
4. **Death, Respawn & Pause Flow**: Dying currently logs `PLAYER DIED!` but leaves the player stuck without an atmospheric Death Screen ("YOU PERISHED") or checkpoint reload key ([R]). A dark-fantasy Pause Menu ([Escape]) is missing.
5. **Background Parallax Depth**: The panoramic background panels lack a subtle multi-speed parallax script to convey immense physical scale as the Knight traverses from left to right.
6. **Level Polish & Visual Readability**: Certain hazard spikes and boundaries benefit from subtle rim highlights and environmental mist.

---

## 2. Prioritized Implementation Plan

### Phase 1: Scene Hygiene & Rendering Baseline (Immediate)
- Purge all orphaned/duplicate FX root objects from `SampleScene.unity`.
- Set Main Camera clear color to deep abyssal void black (`#0A0506`).
- Configure UniversalRP 2D ambient rim and global light settings for high-contrast dark fantasy.

### Phase 2: Atmospheric Environmental Effects
- Implement `AtmosphericEmbersSystem.cs`: lightweight, high-performance particle system of glowing crimson embers and ash flakes drifting across the wind.
- Add dynamic atmospheric 2D lights: flickering red flame light at the Skull Throne and arena braziers.
- Implement smooth parallax scrolling (`ParallaxController.cs`) for distant mountain layers.

### Phase 3: Player Death, Respawn, and Pause Menu Loop
- Implement `GameManager.cs` / `DeathAndPauseUI.cs`:
  - Visceral death screen with crimson vignette, KaTeX-style gothic typography ("YOU PERISHED"), and responsive [R] restart.
  - Dark fantasy Pause Menu with [Escape] key toggle, smooth time freezing (`Time.timeScale = 0`), and clean controls guide.
  - Safe checkpoint respawn logic at active level checkpoint.

### Phase 4: Combat Feel & Hitstop Micro-Tuning
- Fine-tune camera shake damping and directional impulse on heavy impacts.
- Ensure audio channels never clip during rapid combo strikes.

### Phase 5: Verification & Comprehensive Quality Report
- Live play mode simulation via MCP `execute_code`.
- Performance & GC allocation audit.
- Generate `QUALITY_IMPROVEMENTS_REPORT.md`.
