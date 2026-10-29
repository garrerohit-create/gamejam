# Armored Guard NPC — Individual Animation Frames

This archive contains individually exported PNG frames from the three generated 6-column × 4-row atlases.

## Contents
- **12 animation-set folders**
- **72 individual frame PNGs** (6 frames per set)
- **3 original reference atlases** under `Reference_Atlases/`
- `Animation_Manifest.csv` maps each PNG to its source atlas, row, and column.

## Animation folders

### `01_Movement/`
- `Idle` — 6 frames
- `Walk` — 6 frames
- `Run` — 6 frames
- `Jump_Land_Sequence` — 6 frames; the source row combines airborne and landing-style poses, so preview and tune the playback before treating it as a pure jump loop.

### `02_Combat/`
- `Light_Attack_1` — 6 frames
- `Light_Attack_2` — 6 frames
- `Heavy_Slam` — 6 frames
- `Charge_Attack` — 6 frames

### `03_Defense_Damage_Death/`
- `Guard_Ready_Transition` — 6 frames
- `Block_Parry_Counter` — 6 frames
- `Hit_Reaction_Recovery` — 6 frames
- `Death_Fall` — 6 frames

The final four sets are organized from the poses in the third concept atlas. Some generated rows combine multiple related poses/actions; the folder names are practical labels, not a claim that each row is already a polished, game-ready animation.

## PNG and Unity details
- Every frame is **256 × 256 px**, **RGBA PNG**, with the original transparency retained and only a 2-pixel outer edge cleared to remove atlas boundary artifacts.
- Frames within a set are numbered in source-sheet left-to-right order: `_01.png` to `_06.png`.
- Import each folder's PNG files into Unity. They are already individual images; **you do not need to use Sprite Editor slicing**.
- Recommended import settings: `Texture Type = Sprite (2D and UI)`, `Sprite Mode = Single`, and the same Pixels Per Unit (PPU) used for the main character. Use a consistent `Pivot = Bottom Center` if that aligns with your project's character setup.
- Preview the frames in Unity before gameplay. These are AI-generated sprite frames and can have pose, silhouette, or baseline variations that may need cleanup for smooth looping.

## Integrity
- Expected individual frame count: 72
- Expected animation sets: 12
