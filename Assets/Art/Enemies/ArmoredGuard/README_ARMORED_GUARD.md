# Armored Guard Enemy NPC — Unity 2D Implementation

## Overview
The Armored Guard is a complete, modular enemy NPC created for the 2D side-scrolling dark-fantasy action platformer. He features custom sprite slicing and alignment, a full state-machine AI, multi-tiered melee attacks, directional attack hitboxes, hit flinching, and a clean death sequence.

---

## 1. Folder Structure
- **Sprites**: `Assets/Art/Enemies/ArmoredGuard/Sprites/`
  - 12 subfolders (72 individual frames @ 256x256 px).
  - Configured as `Sprite (2D and UI)`, `Single`, `55 PPU`, `BottomCenter` pivot, `Uncompressed`.
- **Animations**: `Assets/Animations/Enemies/ArmoredGuard/`
  - 12 individual `.anim` clips.
  - `ArmoredGuard.controller` (Animator Controller).
- **Scripts**: `Assets/Scripts/Enemies/ArmoredGuard/`
  - `ArmoredGuardAI.cs` (state machine, ledge/wall avoidance, player tracking).
  - `EnemyAttackHitbox.cs` (directional attack hitbox with single-hit damage gating).
- **Core Integrations**:
  - `Assets/Scripts/EnemyHealth.cs` (enhanced with event hooks & configurable death delay).
  - `Assets/Scripts/PlayerHealth.cs` (player health management & death events).
- **Prefab**: `Assets/Prefabs/Enemies/ArmoredGuard.prefab`

---

## 2. Animation Clips & Animator Setup
All 12 animation sequences from the pack have been built into genuine Unity `.anim` clips and wired into `ArmoredGuard.controller`:

1. **`Idle.anim`** (0.72s, looping) — combat-ready stance breathing cycle.
2. **`Walk.anim`** (0.60s, looping) — steady patrol march.
3. **`Run.anim`** (0.48s, looping) — aggressive pursuit sprint.
4. **`LightAttack1.anim`** (0.60s) — quick forward melee strike (`OpenAttackHitbox` @ 0.20s, `CloseAttackHitbox` @ 0.40s).
5. **`LightAttack2.anim`** (0.60s) — follow-up combo strike (`OpenAttackHitbox` @ 0.20s, `CloseAttackHitbox` @ 0.40s).
6. **`HeavySlam.anim`** (0.96s) — two-handed overhead ground slam (`OpenAttackHitbox` @ 0.48s, `CloseAttackHitbox` @ 0.75s).
7. **`ChargeAttack.anim`** (0.72s) — low-stance dash attack.
8. **`GuardReady.anim`** (0.60s) — defensive guard stance.
9. **`BlockParry.anim`** (0.60s) — parry deflection & counter pose.
10. **`HitReaction.anim`** (0.48s) — flinch & recoil on taking damage.
11. **`Death.anim`** (0.84s) — heavy collapse to the ground (does not loop; stays fallen).
12. **`JumpLand.anim`** (0.60s) — apex jump and ground impact recovery.

### Animator Controller Parameters
- `Speed` (Float) — drives transitions between `Idle` (< 0.1), `Walk` (0.1 - 2.5), and `Run` (> 2.5).
- `IsGrounded` (Bool) — grounding check.
- `IsDead` (Bool) — locks into the Death state.
- `IsGuarding` (Bool) — guard stance.
- `LightAttack1` (Trigger) — initiates Light Attack 1.
- `LightAttack2` (Trigger) — initiates Light Attack 2 combo.
- `HeavySlam` (Trigger) — initiates Heavy Slam.
- `ChargeAttack` (Trigger) — initiates Charge Attack.
- `Hit` (Trigger) — triggers hit flinch (guarded by `IsDead == false`).
- `Death` (Trigger) — triggers death animation from Any State.

---

## 3. Enemy AI State Machine (`ArmoredGuardAI.cs`)
State flow: `Idle → Patrol → Detect Player → Chase → Attack Windup → Attacking → Recovery`

- **Patrol & Ledge Safety**:
  - Enemy walks between configurable distance boundaries (`patrolDistance = 5.0`).
  - Employs a downward raycast in front of the character (`ledgeCheckDistance = 1.2`, `groundCheckOffsetForward = 0.8`). If a ledge or platform edge is detected, the guard stops and turns around, preventing accidental falls.
  - Forward raycast detects walls to prevent running into obstacles.
- **Player Detection**:
  - Detects player within `detectionRadius = 7.0m`.
  - Disengages if the player retreats beyond `disengageRadius = 10.5m`.
- **Pursuit & Facing Direction**:
  - Automatically turns to face the player.
  - Flips sprite visually (`flipX`) and mirrors the `AttackHitbox` position dynamically.
- **Combat Decision**:
  - Enters attack range at `attackRange = 2.1m`.
  - Cycles attacks intelligently (every 3rd attack is a heavier 2-damage `HeavySlam`, with alternating `LightAttack1` and `LightAttack2` strikes).
- **Hit Reaction & Death**:
  - Subscribes to `EnemyHealth.OnDamaged` to interrupt attacks and play `HitReaction`.
  - Subscribes to `EnemyHealth.OnDeath` to disable colliders, switch Rigidbody2D to Kinematic, trigger `Death.anim`, and clean up after 2.5s.

---

## 4. Attack Hitbox (`EnemyAttackHitbox.cs`)
- Child GameObject named `AttackHitbox` with `BoxCollider2D` (isTrigger = true).
- Positioned in front of the enemy ($X = 1.35$, $Y = 2.0$, $Size = 2.2 \times 2.8$).
- Automatically flips along the X axis when the enemy faces left or right.
- Employs a single-hit gate (`HashSet<KnightMovement>`) ensuring the player takes damage exactly once per attack swing.
- Activates only during the active strike window (via animation events with coroutine safety fallbacks).

---

## 5. Player Damage Integration (`PlayerHealth.cs`)
- Added to `Knight`.
- Subscribes to `KnightMovement.OnDamageTaken`.
- Configurable `maxHealth = 10`.
- Respects existing player invulnerability frames (dodge/roll) and parry deflection windows in `KnightMovement.TakeDamage()`.
- Logs health deductions and raises `onHealthChanged` / `onPlayerDied` events.

---

## 6. How to Test in Play Mode
1. Open `Assets/Scenes/SampleScene.unity`.
2. Press **Play**:
   - The player Knight is on the left; the Armored Guard stands ahead at $X = 5.0$.
   - The guard stays in `Idle` / `Patrol` until you walk into detection range ($\le 7\text{m}$).
3. **Approach the Guard**:
   - The guard detects the Knight, faces him, and runs forward in pursuit.
4. **Engage in Combat**:
   - At close range ($\le 2.1\text{m}$), the guard winds up and delivers melee gauntlet strikes.
   - You take damage (or can use <kbd>E</kbd> to Parry or <kbd>Shift</kbd>/<kbd>Ctrl</kbd> to Dodge/Roll through the attack).
5. **Attack the Guard**:
   - Left Click (<kbd>Mouse 0</kbd>) or Right Click (<kbd>Mouse 1</kbd>) to strike the guard.
   - The guard flinches (`HitReaction`) and health decreases from 4.
   - At 0 HP, the guard plays his heavy death collapse animation, stops AI processing, and fades out cleanly.

