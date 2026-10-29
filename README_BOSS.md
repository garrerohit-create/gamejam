# The Infernal Executioner — Warden of the Damned
## Boss Encounter Reference & Tuning Guide

---

### Encounter Overview
- **Boss Title**: `The Infernal Executioner — Warden of the Damned`
- **Total Max Health**: `60 HP`
- **Arena Boundaries**:
  - Entrance Boundary Wall: `X = 58.5` (Seals upon boss awakening, dissipates upon defeat)
  - Arena Landing Point: `X = 73.5, Y = -0.08`
  - Giant Skull Throne Perch: `X = 74.5, Y = 5.8, Sorting Order = -4 (Background)`
  - Gateway to Ascended Realm: `X = 84.0, Y = -0.08` (Unlocks on boss defeat)

---

### Cinematic Awakening: The Skull Throne Perch
1. **Dormant Brooding**:
   - The Executioner begins dormant, seated atop the colossal background skull (`Skull_Throne_Giant`) with glowing crimson silhouette breathing (`Boss_Sitting_0.png`).
   - Body colliders, gravity, and AI are dormant to ensure zero combat interference until triggered.
2. **Player Proximity Trigger**:
   - As the player crosses into the arena threshold ($X \ge 59.0$), the Executioner awakens.
   - He rises with his colossal greataxe (`Boss_Sitting_Rise.png`).
   - Leaps through the air in a parabolic arc from the background layer into the foreground gameplay layer (`sortingOrder = 2`).
   - Impacts the ground at $(73.5, -0.08)$ with a ground tremor, screen trauma, and erupting twin shockwaves.
   - Unleashes an intimidating Awakening Roar (`Boss_Roar.anim`), locking the entrance barrier behind the player and engaging combat.

---

### Three Distinct Combat Phases

#### Phase 1: The Executioner (100% to 65% HP | 60 – 39 HP)
*Methodical, heavy, imposing. Teaches the player the Executioner's attack rhythm.*

- **Executioner's Cleave** (`isParryable = true`):
  - Heavy horizontal axe slash preceded by a clear 0.36s windup and blade sheen.
  - Deals `2 Damage`. Blockable and Parryable.
  - Leaves a `0.35s` vulnerable recovery opening.
- **Ruinous Overhead** (`isParryable = false`):
  - Slow vertical slam designed to punish players lingering too close.
  - Windup: `0.50s`. Deals `3 Damage` with ground tremor.
  - Unparryable; player must roll or dodge away.
- **Crimson Advance**:
  - The Executioner stalks forward with heightened presence before transitioning directly into an Executioner's Cleave.
- **Hellfire Shockwave** (Single wave):
  - Rakes axe along the ground, releasing a crescent hellfire projectile across the arena floor.
- **Phase 1 Stats**: Chase Speed: `3.8`, Attack Cooldown: `1.25s`.

---

#### Phase 2: Blood Awakening (65% to 30% HP | 39 – 18 HP)
*Transition triggered at $\le 39$ HP. Boss roars with camera trauma, enters enrage tint (`#FF7373`), and UI updates to `<color=#FF4444>PHASE 2: BLOOD AWAKENING</color>`.*

- **Blood Cleave Combo** (`isParryable = true`):
  - 2-hit rhythm combo: Strike 1 downward cleave, forward dash step (`7.0` velocity), Strike 2 reverse uppercut (+1 extra damage).
- **Delayed Overhead Slam** (`isParryable = false`):
  - Extended 0.70s delayed hold at the apex of the jump, explicitly crafted to bait and catch premature roll/panic dodges.
  - Slams down with explosive force, erupting twin bidirectional shockwaves across the floor.
- **Abyssal Leap Slam**:
  - High-velocity gap-closer leaping across the arena toward the player, crashing down with twin shockwaves.
- **Hellfire Shockwave** (Dual wave):
  - Rapid double crescent projectile sliced across the floor.
- **Shadow Backstep**:
  - Evades player pressure in close melee, immediately chaining into retaliatory strikes.
- **Phase 2 Stats**: Chase Speed: `5.2`, Attack Cooldown: `0.70s`.

---

#### Phase 3: Final Judgment (< 30% HP | 18 – 0 HP)
*Transition triggered at $\le 18$ HP. Apocalyptic roar, maximum camera trauma, deep demonic crimson glow (`#FF2626`), UI shifts to `<color=#FF1111>FINAL JUDGMENT: WARDEN OF THE DAMNED</color>`.*

- **Cataclysmic Execution** (`isParryable = false`):
  - The Executioner's ultimate attack. Leaps high above the arena ceiling, hovers in crimson aura, and plunges directly into the earth.
  - Deals `4 Damage`, shaking the arena (`magnitude = 0.85`), and sending high-speed twin shockwaves.
- **Chained Fury** (3-hit rhythm combo):
  - Strike 1 (Horizontal cleave, parryable) $\rightarrow$ Strike 2 (Dash cleave, parryable) $\rightarrow$ Strike 3 (Overhead smash, unparryable, with forward shockwave).
- **Judgment Shockwave Barrage**:
  - Fires three rapid crescent shockwaves in tight cadence across the floor.
- **Relentless Aggression**:
  - Attack recovery times compressed; boss actively maintains pressure.
- **Phase 3 Stats**: Chase Speed: `6.2`, Attack Cooldown: `0.42s`.

---

### Counterplay & Stagger Architecture
1. **Parry Window Deflection**:
   - When the player presses `[E]` (Parry) during any parryable attack (`Executioner's Cleave`, `Blood Cleave Combo` strikes, `Chained Fury` strikes 1 & 2):
   - The boss attack is canceled immediately.
   - Metallic clash sound (`SoundID.ParryClash`) and spark effect play.
   - Boss enters a **1.0 second vulnerable Stagger window** (`BossState.Hurt`), flashing bright gold/white.
   - Grants the player guaranteed heavy charge or combo damage opportunities.
2. **Unparryable Attacks**:
   - Heavy slams (`Ruinous Overhead`, `Delayed Overhead Slam`, `Cataclysmic Execution`) are unparryable (`isParryable = false`). Attempting to parry will not deflect; player must use Dodge (`Shift`) or Roll (`Ctrl`) to exploit invulnerability frames.
3. **Poise System**:
   - Boss has `10 Poise`. Sustained attacks within a 3.5s window break poise, staggering the boss even outside of parry deflection.

---

### Inspector Tuning Reference (`BossAI.cs`)
| Parameter | Default Value | Description |
|---|---|---|
| `bossMaxHealth` | `60` | Total health pool across all 3 phases |
| `phase2Threshold` | `39` | Health threshold for Phase 2 transition (65%) |
| `phase3Threshold` | `18` | Health threshold for Phase 3 transition (30%) |
| `chaseSpeedPhase1` | `3.8` | Locomotion walk speed in Phase 1 |
| `chaseSpeedPhase2` | `5.2` | Locomotion run speed in Phase 2 |
| `chaseSpeedPhase3` | `6.2` | Locomotion sprint speed in Phase 3 |
| `attackCooldownPhase1`| `1.25s` | Recovery pause between actions in Phase 1 |
| `attackCooldownPhase2`| `0.70s` | Recovery pause between actions in Phase 2 |
| `attackCooldownPhase3`| `0.42s` | Recovery pause between actions in Phase 3 |
| `cleaveDamage` | `2` | Base melee cleave damage |
| `slamDamage` | `3` | Base ground slam damage |
| `cataclysmDamage` | `4` | Phase 3 ultimate slam damage |
| `shockwaveDamage` | `2` | Hellfire projectile damage |

---

### Testing & Verification Instructions
1. **Awakening & Arena Seal**:
   - Walk player past $X = 59.0$. Observe boss awaken from the skull, leap down to $X = 73.5$, and roar.
   - Check that `ArenaEntranceBarrier` at $X = 58.5$ activates solid collision and red runic visual.
2. **Parry Counterplay**:
   - Stand in front of the boss during Phase 1. When he winds up `Executioner's Cleave`, tap `[E]`.
   - Observe parry spark, clash SFX, and 1.0s boss stagger opening.
3. **Phase Transitions**:
   - Attack boss until HP $\le 39$. Observe Phase 2 roar, camera shake, visual tint, and UI banner change.
   - Attack boss until HP $\le 18$. Observe Phase 3 apocalyptic roar and ultimate moveset.
4. **Defeat Flow**:
   - Deplete remaining HP to 0.
   - Boss plays collapse animation and death roar.
   - `ArenaEntranceBarrier` unseals and dissolves.
   - `EndLevelGate` at $X = 84.0$ activates portal passage. Walk player into gate to trigger level victory sequence.
