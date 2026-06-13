# Idle God Finger — GitHub Issues

## Milestones

| #   | Name              | Tier | Gate                                                               |
| --- | ----------------- | ---- | ------------------------------------------------------------------ |
| 1   | First Playable    | T1   | Engine skeleton, grid, dungeon, hero movement, AI, enemies, combat |
| 2   | Core Loop         | T2   | God Finger input, charges, loot, equip, HUD, floor loop, death     |
| 3   | Beta              | T3   | Fog of war, save, skill tree, shrine, bosses, traps                |
| 4   | Release Candidate | T4   | Audio, VFX, camera, content, bestiary, menus                       |
| 5   | Post-Launch       | T5   | Variants, heirlooms, co-op, accessibility                          |

---

## 🔴 Milestone 1 — First Playable

---

### T1-01 · Project Setup & C# Configuration

**Labels:** `chore` `p0: critical` `size: s`
**Milestone:** 1 — First Playable
**Blocks:** All other tasks

> ⚠️ Partially complete: `project.godot`, `.editorconfig`, and the solution file already exist in the repo. Only the items below remain.

#### Tasks

- [ ] Register autoloads in `project.godot`: `GameManager`, `AudioManager`, `SaveSystem`
- [ ] Verify all folders from GDD §11 exist: `src/core/autoload/`, `src/gameplay/character/`, `src/gameplay/enemies/`, `src/gameplay/interactables/`, `src/gameplay/camera/`, `src/levels/`, `src/resources/enemy_definitions/`, `src/resources/item_definitions/`, `src/shaders/`, `src/ui/`, `assets/art/characters/`, `assets/art/ui/`, `assets/art/world/`, `assets/audio/`
- [ ] Confirm Rider / VS IDE integration is working

---

### T1-02 · Grid & TileMap Foundation

**Labels:** `feature` `p0: critical` `sys: dungeon-gen` `size: s`
**Milestone:** 1 — First Playable
**Blocks:** T1-03, T1-04, T1-05, T1-06

#### Tasks

- [ ] Define `TILE_SIZE = 16` constant and `Vector2i` coordinate convention
- [ ] Create `TileType` enum in `src/levels/DungeonGenerator.cs`
- [ ] Set up placeholder `TileMapLayer` in `src/levels/dungeon.tscn` (layer 0: floor, layer 1: walls/objects)
- [ ] Write `src/levels/GridUtils.cs`: `WorldToGrid()`, `GridToWorld()`, tile occupancy query helpers

---

### T1-03 · BSP Dungeon Generator

**Labels:** `feature` `p0: critical` `sys: dungeon-gen` `size: l`
**Milestone:** 1 — First Playable
**Blocks:** All in-dungeon gameplay (T1-04 through T2-10)

#### Tasks

- [ ] Implement `src/levels/BSPSplitter.cs`: recursive space partition, min region 6×6, configurable depth
- [ ] Implement `src/levels/RoomPlacer.cs`: room-within-region placement, L-shaped corridor connector
- [ ] Guarantee one `Entrance`, one `Exit`, one `Boss` room per floor
- [ ] Populate rooms with `TileType` assignments and emit `DungeonGenerated` signal
- [ ] Test: generate 50 dungeons, assert all fully connected with no orphaned rooms

---

### T1-04 · Hero Entity & Grid Movement

**Labels:** `feature` `p0: critical` `sys: ai` `sys: animation` `size: m`
**Milestone:** 1 — First Playable
**Blocks:** T1-06, T1-07, T1-08, T2-01, T2-02

#### Tasks

- [ ] Create `src/gameplay/character/Hero.cs` as `CharacterBody2D`, expose stat properties (`Hp`, `Atk`, `Def`, `Spd`, etc.)
- [ ] Implement tile-by-tile tween movement: hero lerps to next grid cell, emits `StepTaken`
- [ ] Enforce 4-directional movement only (no diagonal)
- [ ] Drive `HeroAnimator.SetFacing()` from movement direction each step

---

### T1-05 · AI Pathfinder

**Labels:** `feature` `p0: critical` `sys: pathfinding` `size: m`
**Milestone:** 1 — First Playable
**Blocks:** T1-06

#### Tasks

- [ ] Implement `src/gameplay/character/Pathfinder.cs`: A\* over the dungeon tile graph
- [ ] Walkable tiles: `Floor`, `Entrance`, `Exit`
- [ ] Public API: `List<Vector2i> FindPath(Vector2i from, Vector2i to)`
- [ ] Cache invalidation on room reveal or tile state change

---

### T1-06 · Hero AI State Machine

**Labels:** `feature` `p0: critical` `sys: ai` `size: l`
**Milestone:** 1 — First Playable
**Blocks:** Entire idle experience (T2-\*)

#### Tasks

- [ ] Implement priority states in `src/gameplay/character/HeroAI.cs`: `Flee → Fight → Approach → Loot → Explore → Idle`
- [ ] Evaluate priority every `_PhysicsProcess` tick; transition to highest valid state
- [ ] `Explore` state: request path to nearest unvisited room from `Pathfinder`
- [ ] `Fight` state: stop movement, hand control to `CombatResolver`
- [ ] `Flee` state: active only when `FleeThreshold > 0` (gated behind skill); path to Exit
- [ ] Export `PathRecalcInterval` field (default `0.4s`)

---

### T1-07 · Enemy Entity & Enemy AI

**Labels:** `feature` `p0: critical` `sys: ai` `size: m`
**Milestone:** 1 — First Playable
**Blocks:** T1-08

#### Tasks

- [ ] Create `src/gameplay/enemies/Enemy.cs` as `CharacterBody2D` with stat fields driven by `EnemyData` record
- [ ] Implement `src/gameplay/enemies/EnemyAI.cs` tick loop (`EnemyTickInterval` = 1.0s): `Idle → Patrol → Aggro → Attack → Stunned → Dead`
- [ ] Enemies act only on tick — no real-time movement
- [ ] Wire `src/resources/enemy_definitions/EnemyData.cs` for Goblin and Skeleton (Tier 1) to start

---

### T1-08 · Combat Resolver

**Labels:** `feature` `p0: critical` `sys: combat` `size: m`
**Milestone:** 1 — First Playable

#### Tasks

- [ ] `ResolveAttack(Character attacker, Character defender)`: crit roll, flat DEF reduction, `TakeDamage()`
- [ ] Emit `DamageDealt`, `EnemyKilled`, `HeroHealed` signals
- [ ] On enemy death: call `LootTable.Roll()`, spawn `GoldPile` / `WorldItem` into scene, emit `EnemyKilled`
- [ ] Hook `DamageDealt` signal to spawn `src/ui/DamageNumber.cs` nodes

---

## 🟠 Milestone 2 — Core Loop

---

### T2-01 · Hero Animation State Machine

**Labels:** `feature` `p1: high` `sys: animation` `size: m`
**Milestone:** 2 — Core Loop

#### Tasks

- [ ] Implement `HeroAnimator.cs` per GDD §10: states `Idle`, `Walk`, `Attack`, `Hurt`, `Death`, `Heal`
- [ ] Support 4-directional variants via direction suffix (`_down`, `_up`, `_right`; left is h-flipped right)
- [ ] Hook `AnimationFinished` signal to return non-looping states back to `Idle`
- [ ] Placeholder spritesheet acceptable; structured for final art swap later

---

### T2-02 · God Finger Input System

**Labels:** `feature` `p1: high` `sys: ui` `size: m`
**Milestone:** 2 — Core Loop

#### Tasks

- [ ] `_UnhandledInput` in `MainGame.cs`: left-click → `HandleGodClick(Vector2 worldPos)` priority chain
- [ ] Priority order: Hero → Enemy → WorldItem → GoldPile → Tile
- [ ] Use `PhysicsDirectSpaceState2D` overlap queries to resolve click targets per type
- [ ] Gate all interactions through `DivineCharges.TrySpend(cost)`
- [ ] Add God Finger cursor sprite to `assets/art/ui/`

---

### T2-03 · Divine Charge System

**Labels:** `feature` `p1: high` `sys: ui` `size: s`
**Milestone:** 2 — Core Loop

#### Tasks

- [ ] Implement `src/gameplay/character/DivineCharges.cs`: track `Current` and `Max` (default 5)
- [ ] `TrySpend(int cost) → bool`: deduct if sufficient, return false otherwise
- [ ] Auto-regen: 1 charge per `RegenInterval` seconds via `Timer` node
- [ ] Emit `ChargesChanged`, `ChargeSpent`, `ChargeRestored` signals
- [ ] Wire `src/ui/DivineChargeBar.cs` to react to `ChargesChanged`

---

### T2-04 · Heal Touch

**Labels:** `feature` `p1: high` `sys: combat` `sys: ui` `size: s`
**Milestone:** 2 — Core Loop

#### Tasks

- [ ] In `TryHealHero()`: verify click radius overlaps Hero, call `DivineCharges.TrySpend(1)`
- [ ] Apply `BaseHeal` (20) + skill bonuses to `Hero.Hp`, clamped to `Hero.MaxHp`
- [ ] Refund charge if hero is already at full HP (no resource waste)
- [ ] Trigger `HeroAnimator.SetState(State.Heal)` and emit `HeroHealed`

---

### T2-05 · Loot System

**Labels:** `feature` `p1: high` `sys: loot` `size: m`
**Milestone:** 2 — Core Loop

#### Tasks

- [ ] `LootTable.Roll(string tableId)`: weighted random rarity and slot using `LootTables.Tables` records
- [ ] Procedurally generate `ItemData` record: slot, rarity, 1–3 stat rolls from slot's pool
- [ ] Spawn `WorldItem` node at enemy death position
- [ ] Player tap on `WorldItem` → calls `EquipSystem.TryEquip(ItemData)`

---

### T2-06 · Equipment System

**Labels:** `feature` `p1: high` `sys: loot` `size: m`
**Milestone:** 2 — Core Loop

#### Tasks

- [ ] Maintain 6-slot dictionary in `EquipSystem.cs`: `Head`, `Body`, `Legs`, `Weapon`, `Offhand`, `Accessory`
- [ ] `TryEquip(ItemData)`: empty slot → equip directly; occupied → drop old as `WorldItem`, equip new
- [ ] On equip/unequip: recalculate all hero stat modifiers by iterating equipped items
- [ ] Emit `ItemEquipped`, `ItemDropped` signals

---

### T2-07 · Gold System

**Labels:** `feature` `p1: high` `sys: loot` `size: s`
**Milestone:** 2 — Core Loop

#### Tasks

- [ ] Implement `GoldPile.cs` as tappable world node; on tap call `GameManager.AddRunGold(amount)`
- [ ] Connect HUD gold display to `GameManager.GoldChanged` signal

---

### T2-08 · HUD

**Labels:** `feature` `p1: high` `sys: ui` `size: m`
**Milestone:** 2 — Core Loop

#### Tasks

- [ ] `DivineChargeBar.cs`: row of orb sprites, react to `ChargesChanged`
- [ ] `HeroStatPanel.cs`: HP bar + numeric stats, react to `Hero.HpChanged`
- [ ] `GoldLabel.cs`: react to `GameManager.GoldChanged`
- [ ] `MinimapDisplay.cs`: render room rectangles, light up visited rooms, update on `RoomRevealed`

---

### T2-09 · Floor Transitions

**Labels:** `feature` `p1: high` `sys: dungeon-gen` `size: m`
**Milestone:** 2 — Core Loop

#### Tasks

- [ ] Hero reaching Exit tile → `GameManager.TransitionTo(GameState.DungeonActive)` for next floor
- [ ] `DungeonGenerator` generates new floor seeded from `CurrentFloor + 1`
- [ ] On floor complete, bank gold: `GameManager.BankRunGold()`
- [ ] Apply floor scaling multipliers to `EnemyData` stat values

---

### T2-10 · Hero Death & Run Reset

**Labels:** `feature` `p1: high` `sys: ai` `sys: ui` `size: s`
**Milestone:** 2 — Core Loop

#### Tasks

- [ ] `Hero.TakeDamage()`: if `Hp <= 0`, emit `Died` and set animator to `Death` state
- [ ] `GameManager.TransitionTo(GameState.HeroDead)` → show death screen overlay
- [ ] `GameManager.LoseRunGold()` clears in-run gold; banked gold remains safe
- [ ] Load Shrine scene after death screen dismiss

---

## 🟡 Milestone 3 — Beta

---

### T3-01 · Fog of War System

**Labels:** `feature` `p2: medium` `sys: dungeon-gen` `sys: ui` `size: m`
**Milestone:** 3 — Beta

#### Tasks

- [ ] Track `RoomState` enum (`Hidden`, `Partial`, `Revealed`) per room
- [ ] On `Hero.EnteredRoom`: set current room to `Revealed`, adjacent rooms to `Partial`
- [ ] `TileMap` modulate: Hidden = black, Partial = 40% opacity, Revealed = 100%
- [ ] Bless Tile (3 charges) forces `Hidden → Revealed` transition

---

### T3-02 · Save System

**Labels:** `feature` `p2: medium` `sys: save` `size: m`
**Milestone:** 3 — Beta

#### Tasks

- [ ] JSON save to `user://save.json` using Godot's `FileAccess`
- [ ] Serialise: `GoldBanked`, `PurchasedNodes`, `UnlockedDungeons`, `Bestiary`, `Statistics`
- [ ] `SaveSystem.Save()`: call on floor transition, Shrine entry, and quit
- [ ] `SaveSystem.Load()`: call on game boot

---

### T3-03 · Skill Tree Data & Logic

**Labels:** `feature` `p2: medium` `sys: skills` `size: l`
**Milestone:** 3 — Beta

#### Tasks

- [ ] Define all 36 `SkillNode` records in `SkillTreeData.cs` across 4 branches × 3 tiers (per GDD §6)
- [ ] `SkillTree.cs`: `TryPurchase(string nodeId)` — check gold, check prerequisite nodes, apply modifiers
- [ ] `ApplyModifiers()`: iterate `PurchasedNodes`, push stat deltas to `Hero` and `DivineCharges`
- [ ] Emit `NodePurchased`, `StatModifierApplied` signals

---

### T3-04 · Shrine Scene & Skill Tree UI

**Labels:** `feature` `p2: medium` `sys: skills` `sys: ui` `size: l`
**Milestone:** 3 — Beta

#### Tasks

- [ ] Build `shrine.tscn`: hero preview animation, gold display, dungeon selector, Skill Tree panel, Start Run button
- [ ] `SkillTreePanel.cs`: render nodes as buttons positioned in branch/tier grid
- [ ] `Line2D` connections between nodes; purchased = lit, available = dim, locked = greyed
- [ ] On node click: call `SkillTree.TryPurchase()`, update gold display

---

### T3-05 · Enemy Types — Tier 2 (Orc Brute, Dark Mage)

**Labels:** `feature` `p2: medium` `sys: ai` `size: m`
**Milestone:** 3 — Beta

#### Tasks

- [ ] Add Orc Brute and Dark Mage stat records to `EnemyData.cs`
- [ ] Orc Brute: charge behaviour — moves 2 tiles toward hero on aggro tick
- [ ] Dark Mage: ranged — maintains distance, fires projectile animated via `Tween`
- [ ] Projectile: `Area2D` that travels grid-aligned and calls `ResolveAttack` on overlap

---

### T3-06 · Boss System

**Labels:** `feature` `p2: medium` `sys: ai` `sys: combat` `size: l`
**Milestone:** 3 — Beta

#### Tasks

- [ ] `BossRoom` type: minimum 12×12 tile footprint, unique boss entity
- [ ] `Boss.cs` extends `Enemy.cs`: override tick to run phase-based attack patterns
- [ ] Phase transition at 50% HP: change attack pattern, increase tick speed
- [ ] On boss kill: emit `EnemyKilled` with elevated gold, guaranteed Rare+ loot roll, trigger `DungeonComplete`

---

### T3-07 · Smite & Bless Tile God Actions

**Labels:** `feature` `p2: medium` `sys: combat` `sys: ui` `size: m`
**Milestone:** 3 — Beta

#### Tasks

- [ ] `TrySmiteEnemy(Vector2 pos)`: 5-charge action, deal `SmiteDamage` to clicked enemy
- [ ] `TryBlessTile(Vector2 pos)`: 3-charge action, reveal tile, disarm traps
- [ ] Both actions trigger distinct VFX via `CPUParticles2D` or `GPUParticles2D`

---

### T3-08 · Trap Tiles

**Labels:** `feature` `p2: medium` `sys: dungeon-gen` `sys: ai` `size: s`
**Milestone:** 3 — Beta

#### Tasks

- [ ] `TrapHidden` tiles deal damage when hero steps on them
- [ ] When `Sixth Sense` skill is purchased: `Pathfinder` marks `TrapHidden` as unwalkable
- [ ] Bless Tile converts `TrapHidden → TrapRevealed` (safe to walk, visually distinct)

---

### T3-09 · Room Type Events

**Labels:** `feature` `p2: medium` `sys: dungeon-gen` `sys: ui` `size: m`
**Milestone:** 3 — Beta

#### Tasks

- [ ] Treasure room: spawn `Chest` node; player tap opens it, triggers Rare loot roll
- [ ] Shrine room: auto-buff on hero entry; randomly one of: +10% ATK for floor, +20 HP, or +1 temp Divine Charge max
- [ ] Wandering Merchant room: UI overlay offers 2 items purchasable with in-run gold

---

## 🟢 Milestone 4 — Release Candidate

---

### T4-01 · Audio System

**Labels:** `feature` `p2: medium` `sys: audio` `size: m`
**Milestone:** 4 — Release Candidate

#### Tasks

- [ ] SFX bus: attack, hurt, death, heal, level-up, gold pickup, UI click
- [ ] Music bus: dungeon loop, shrine ambient, boss stinger
- [ ] `AudioManager.PlaySfx(string key)` / `PlayMusic(string key)` with bus routing
- [ ] Expose master/SFX/music volume sliders in settings menu

---

### T4-02 · Damage Numbers VFX

**Labels:** `enhancement` `p2: medium` `sys: ui` `size: s`
**Milestone:** 4 — Release Candidate

#### Tasks

- [ ] Pool `Label` nodes that float upward and fade out via `Tween`
- [ ] Colour coded: White (normal hit), Yellow (crit), Green (heal), Red (hero damaged)
- [ ] Spawn from `CombatResolver.DamageDealt` signal

---

### T4-03 · Camera System

**Labels:** `feature` `p2: medium` `sys: camera` `size: s`
**Milestone:** 4 — Release Candidate

#### Tasks

- [ ] `Camera2D` follows hero with `PositionSmoothingEnabled = true`
- [ ] Zoom level: show 2–3 rooms simultaneously for God Finger legibility
- [ ] Edge panning: pan toward the room ahead when hero approaches dungeon boundary

---

### T4-04 · Enemy Types — Tier 3 & Boss Variants

**Labels:** `feature` `p2: medium` `sys: ai` `size: l`
**Milestone:** 4 — Release Candidate

#### Tasks

- [ ] Stone Golem: slow tick interval (2.0s), very high DEF, cleave attack on adjacent tiles
- [ ] Per-dungeon boss variants: Goblin King, Orc Warlord, Lich, Ancient Golem (per GDD §7)
- [ ] Unique artwork and attack animations for each boss variant

---

### T4-05 · Bestiary

**Labels:** `feature` `p2: medium` `sys: save` `sys: ui` `size: m`
**Milestone:** 4 — Release Candidate

#### Tasks

- [ ] Track kill counts and first-seen floor per enemy type in `SaveSystem`
- [ ] `Bestiary.cs` UI: grid of enemy portraits, tap to see stats and flavour text
- [ ] Locked entries show silhouette until enemy is first encountered

---

### T4-06 · Settings & Pause Menu

**Labels:** `feature` `p2: medium` `sys: ui` `size: m`
**Milestone:** 4 — Release Candidate

#### Tasks

- [ ] Pause: `GetTree().Paused = true`, show settings overlay
- [ ] Settings: master/SFX/music volume sliders, fullscreen toggle, keybinding display
- [ ] Persist settings to `user://settings.json`

---

### T4-07 · Main Menu & Scene Flow

**Labels:** `feature` `p2: medium` `sys: ui` `size: m`
**Milestone:** 4 — Release Candidate

#### Tasks

- [ ] Main menu: New Game, Continue (greyed if no save), Settings, Quit
- [ ] Scene transitions: full-screen fade via `CanvasLayer` + `Tween`
- [ ] Death screen: display run stats (floors cleared, enemies killed, gold earned)

---

## ⚪ Milestone 5 — Post-Launch

---

### T5-01 · Dungeon Variants (Fungal Warrens, Obsidian Forge, Void Spire)

**Labels:** `feature` `p3: low` `sys: dungeon-gen` `size: l`
**Milestone:** 5 — Post-Launch

#### Tasks

- [ ] Fungal Warrens (unlock: Floor 5 clear): organic/mushroom theme, enemies apply poison
- [ ] Obsidian Forge (unlock: Floor 10 clear): lava/constructs theme, high DEF enemies
- [ ] Void Spire (unlock: Floor 20 clear): surreal/eldritch theme, randomised mechanics each run
- [ ] Unique tile sets and enemy rosters per dungeon variant

---

### T5-02 · Heirloom System

**Labels:** `feature` `p3: low` `sys: loot` `sys: save` `size: l`
**Milestone:** 5 — Post-Launch

#### Tasks

- [ ] Tap-hold on a Legendary item mid-run to mark it as Heirloom
- [ ] Heirloom persists across runs, stored in the Shrine chest
- [ ] Stat-scale heirloom to current floor depth on retrieval

---

### T5-03 · In-Run Hero Levelling

**Labels:** `feature` `p3: low` `sys: ai` `sys: combat` `size: l`
**Milestone:** 5 — Post-Launch

#### Tasks

- [ ] Hero gains XP from kills; reaching thresholds grants a randomised bonus stat roll
- [ ] Keep entirely separate from the permanent Skill Tree to avoid v1 complexity creep

---

### T5-04 · Dungeon Seed Reroll (Cartographer Skill)

**Labels:** `feature` `p3: low` `sys: dungeon-gen` `size: m`
**Milestone:** 5 — Post-Launch

#### Tasks

- [ ] Expose run seed in `DungeonGenerator`
- [ ] `Cartographer` skill: one-time reroll of dungeon seed available on hero death
- [ ] UI confirmation dialog: "Reroll this dungeon? (Once per run)"

---

### T5-05 · Co-op God Fingers

**Labels:** `feature` `p3: low` `sys: ui` `size: l`
**Milestone:** 5 — Post-Launch

#### Tasks

- [ ] Second player shares the same Divine Charge pool
- [ ] Both players' clicks contribute to the same hero
- [ ] Local split-input only (two mice, or gamepad + mouse) — no netcode in scope

---

### T5-06 · Accessibility Options

**Labels:** `enhancement` `p3: low` `sys: ui` `size: m`
**Milestone:** 5 — Post-Launch

#### Tasks

- [ ] Colourblind-safe damage number palettes
- [ ] Adjustable UI scale
- [ ] Option to auto-collect all gold (removes tapping friction)
- [ ] Screen reader tags on all UI elements

---

## Quick Reference — Label Summary

| Issue | Type          | Priority       | System(s)                    | Size      |
| ----- | ------------- | -------------- | ---------------------------- | --------- |
| T1-01 | `chore`       | `p0: critical` | —                            | `size: s` |
| T1-02 | `feature`     | `p0: critical` | `sys: dungeon-gen`           | `size: s` |
| T1-03 | `feature`     | `p0: critical` | `sys: dungeon-gen`           | `size: l` |
| T1-04 | `feature`     | `p0: critical` | `sys: ai` `sys: animation`   | `size: m` |
| T1-05 | `feature`     | `p0: critical` | `sys: pathfinding`           | `size: m` |
| T1-06 | `feature`     | `p0: critical` | `sys: ai`                    | `size: l` |
| T1-07 | `feature`     | `p0: critical` | `sys: ai`                    | `size: m` |
| T1-08 | `feature`     | `p0: critical` | `sys: combat`                | `size: m` |
| T2-01 | `feature`     | `p1: high`     | `sys: animation`             | `size: m` |
| T2-02 | `feature`     | `p1: high`     | `sys: ui`                    | `size: m` |
| T2-03 | `feature`     | `p1: high`     | `sys: ui`                    | `size: s` |
| T2-04 | `feature`     | `p1: high`     | `sys: combat` `sys: ui`      | `size: s` |
| T2-05 | `feature`     | `p1: high`     | `sys: loot`                  | `size: m` |
| T2-06 | `feature`     | `p1: high`     | `sys: loot`                  | `size: m` |
| T2-07 | `feature`     | `p1: high`     | `sys: loot`                  | `size: s` |
| T2-08 | `feature`     | `p1: high`     | `sys: ui`                    | `size: m` |
| T2-09 | `feature`     | `p1: high`     | `sys: dungeon-gen`           | `size: m` |
| T2-10 | `feature`     | `p1: high`     | `sys: ai` `sys: ui`          | `size: s` |
| T3-01 | `feature`     | `p2: medium`   | `sys: dungeon-gen` `sys: ui` | `size: m` |
| T3-02 | `feature`     | `p2: medium`   | `sys: save`                  | `size: m` |
| T3-03 | `feature`     | `p2: medium`   | `sys: skills`                | `size: l` |
| T3-04 | `feature`     | `p2: medium`   | `sys: skills` `sys: ui`      | `size: l` |
| T3-05 | `feature`     | `p2: medium`   | `sys: ai`                    | `size: m` |
| T3-06 | `feature`     | `p2: medium`   | `sys: ai` `sys: combat`      | `size: l` |
| T3-07 | `feature`     | `p2: medium`   | `sys: combat` `sys: ui`      | `size: m` |
| T3-08 | `feature`     | `p2: medium`   | `sys: dungeon-gen` `sys: ai` | `size: s` |
| T3-09 | `feature`     | `p2: medium`   | `sys: dungeon-gen` `sys: ui` | `size: m` |
| T4-01 | `feature`     | `p2: medium`   | `sys: audio`                 | `size: m` |
| T4-02 | `enhancement` | `p2: medium`   | `sys: ui`                    | `size: s` |
| T4-03 | `feature`     | `p2: medium`   | `sys: camera`                | `size: s` |
| T4-04 | `feature`     | `p2: medium`   | `sys: ai`                    | `size: l` |
| T4-05 | `feature`     | `p2: medium`   | `sys: save` `sys: ui`        | `size: m` |
| T4-06 | `feature`     | `p2: medium`   | `sys: ui`                    | `size: m` |
| T4-07 | `feature`     | `p2: medium`   | `sys: ui`                    | `size: m` |
| T5-01 | `feature`     | `p3: low`      | `sys: dungeon-gen`           | `size: l` |
| T5-02 | `feature`     | `p3: low`      | `sys: loot` `sys: save`      | `size: l` |
| T5-03 | `feature`     | `p3: low`      | `sys: ai` `sys: combat`      | `size: l` |
| T5-04 | `feature`     | `p3: low`      | `sys: dungeon-gen`           | `size: m` |
| T5-05 | `feature`     | `p3: low`      | `sys: ui`                    | `size: l` |
| T5-06 | `enhancement` | `p3: low`      | `sys: ui`                    | `size: m` |

---

## Creating Issues with GitHub CLI

```bash
# Example — create T1-06 with the gh CLI
gh issue create \
  --title "T1-06 · Hero AI State Machine" \
  --label "feature,p0: critical,sys: ai,size: l" \
  --milestone "1 — First Playable" \
  --body-file t1-06-body.md
```

For bulk creation, extract each issue body into its own `.md` file and loop through them, or use the [GitHub CSV importer](https://github.com/nicklockwood/GitHubIssueImport) for one-shot import.
