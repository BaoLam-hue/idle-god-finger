# Idle God Finger — Development Task List
*Ordered: most critical (game cannot function without it) → least critical (polish, content, future scope)*

---

## 🔴 Tier 1 — Foundations (Nothing runs without these)

### T1-01 · Project Setup & C# Configuration
- Create Godot 4.6 project with C# (.NET 8) enabled
- Configure `project.godot` autoloads: `GameManager`, `AudioManager`, `SaveSystem`
- Verify existing folder structure matches: `src/core/autoload/`, `src/core/main_game/`, `src/core/debug/`, `src/gameplay/character/`, `src/gameplay/enemies/`, `src/gameplay/interactables/`, `src/gameplay/camera/`, `src/levels/`, `src/resources/enemy_definitions/`, `src/resources/item_definitions/`, `src/shaders/`, `src/ui/`, `assets/art/characters/`, `assets/art/ui/`, `assets/art/world/`, `assets/audio/`
- Add `.editorconfig` and solution file for IDE support (Rider / VS)
- **Blocks:** Everything

### T1-02 · Grid & TileMap Foundation
- Define `TILE_SIZE = 16` constant, `Vector2i` coordinate convention
- Create `TileType` enum in `src/levels/DungeonGenerator.cs`
- Set up a placeholder `TileMapLayer` (layer 0: floor, layer 1: walls/objects) inside `src/levels/dungeon.tscn`
- Write `src/levels/GridUtils.cs`: `WorldToGrid()`, `GridToWorld()`, tile occupancy query helpers
- **Blocks:** Hero movement, dungeon generation, pathfinding

### T1-03 · BSP Dungeon Generator
- Implement `src/levels/BSPSplitter.cs`: recursive space partition, min region 6×6, configurable depth
- Implement `src/levels/RoomPlacer.cs`: room-within-region placement, L-shaped corridor connector
- Guarantee: one `Entrance`, one `Exit`, one `Boss` room per floor
- Populate rooms with `TileType` assignments and emit `DungeonGenerated` signal
- Test: generate 50 dungeons, assert all are fully connected, no orphaned rooms
- **Blocks:** All in-dungeon gameplay

### T1-04 · Hero Entity & Grid Movement
- Create `src/gameplay/character/Hero.cs` as `CharacterBody2D`, expose stat properties (HP, Atk, Def, Spd, etc.)
- Implement tile-by-tile tween movement: hero lerps to next grid cell, emits `StepTaken`
- 4-directional only, no diagonal
- Drive `HeroAnimator.SetFacing()` from movement direction each step
- **Blocks:** HeroAI, combat, all gameplay

### T1-05 · A* Pathfinder
- Implement `src/gameplay/character/Pathfinder.cs`: A* over the dungeon tile graph (walkable = Floor, Entrance, Exit)
- API: `List<Vector2i> FindPath(Vector2i from, Vector2i to)`
- Cache invalidation on room reveal or tile state change
- **Blocks:** HeroAI exploration and flee behaviour

### T1-06 · Hero AI State Machine (`src/gameplay/character/HeroAI.cs`)
- Implement priority states: `Flee → Fight → Approach → Loot → Explore → Idle`
- Evaluate priority order every `_PhysicsProcess` tick; transition to highest valid state
- `Explore`: request path to nearest unvisited room from `Pathfinder`
- `Fight`: stop movement, hand control to `CombatResolver`
- `Flee`: only active when `FleeThreshold > 0` (gated behind skill); path to Exit
- Expose `PathRecalcInterval` (default 0.4 s) as an exported field
- **Blocks:** The entire idle experience

### T1-07 · Enemy Entity & Enemy AI
- Create `src/gameplay/enemies/Enemy.cs` as `CharacterBody2D` with stat fields driven by `EnemyData` record
- Implement `src/gameplay/enemies/EnemyAI.cs` tick loop (`EnemyTickInterval` = 1.0 s): `Idle → Patrol → Aggro → Attack → Stunned → Dead`
- Enemies do **not** move in real-time — act only on tick
- Wire `src/resources/enemy_definitions/EnemyData.cs` dictionary for Goblin and Skeleton (Tier 1) to start
- **Blocks:** Combat system testing, loot drops

### T1-08 · Combat Resolver (`src/gameplay/character/CombatResolver.cs`)
- `ResolveAttack(Character attacker, Character defender)`: crit roll, flat DEF reduction, `TakeDamage()`
- Emit `DamageDealt`, `EnemyKilled`, `HeroHealed` signals
- Handle enemy death: call `LootTable.Roll()`, spawn `GoldPile` / `WorldItem` into scene under `src/gameplay/interactables/`, emit `EnemyKilled`
- Damage number spawner: hook into `DamageDealt` signal, spawn `src/ui/DamageNumber.cs` node

---

## 🟠 Tier 2 — Core Player Loop (Game is playable but incomplete)

### T2-01 · Hero Animation State Machine (`src/gameplay/character/HeroAnimator.cs`)
- Implement as designed in §10 of the GDD
- 4-directional sprites: `idle`, `walk`, `attack`, `hurt`, `death`, `heal` + direction suffix
- Hook `AnimationFinished` signal to return non-looping states to `Idle`
- Placeholder spritesheet acceptable; swap for final art later

### T2-02 · God Finger Input System (`src/core/main_game/MainGame.cs`)
- `_UnhandledInput`: left-click → `HandleGodClick(Vector2 worldPos)` priority chain
- Priority: Hero → Enemy → WorldItem → GoldPile → Tile
- Use `PhysicsDirectSpaceState2D` overlap queries per target type to resolve click targets
- All interactions gated through `DivineCharges.TrySpend(cost)`
- Art cue: cursor sprite lives in `assets/art/ui/`

### T2-03 · Divine Charge System (`src/gameplay/character/DivineCharges.cs`)
- Track `Current` and `Max` (default 5) charges
- `TrySpend(int cost) → bool`: deduct if sufficient, return false otherwise
- Auto-regen: 1 charge per `RegenInterval` seconds via `Timer` node
- Emit `ChargesChanged`, `ChargeSpent`, `ChargeRestored` signals
- HUD integration: `src/ui/DivineChargeBar.cs` reacts to `ChargesChanged`

### T2-04 · Heal Touch
- In `TryHealHero()`: check click radius overlaps Hero, call `DivineCharges.TrySpend(1)`
- Apply `BaseHeal` (20) + skill bonuses to `Hero.Hp`, clamped to `Hero.MaxHp`
- Refund charge if hero is already at full HP
- Trigger `Hero.AnimatedSprite.SetState(State.Heal)` and emit `HeroHealed`

### T2-05 · Loot System — `LootTable.cs` & `WorldItem.cs`
- `LootTable.Roll(string tableId)`: use `LootTables.Tables` record, weighted random rarity and slot
- Procedurally generate `ItemData` record: slot, rarity, 1–3 stat rolls from slot's pool
- Spawn `WorldItem` node at enemy death position
- Player taps `WorldItem` → `EquipSystem.TryEquip(ItemData)`

### T2-06 · Equipment System (`EquipSystem.cs`)
- Maintain 6-slot dictionary: `Head, Body, Legs, Weapon, Offhand, Accessory`
- `TryEquip(ItemData)`: if slot empty → equip; if occupied → drop old as `WorldItem`, equip new
- On equip/unequip: recalculate all hero stat modifiers by iterating equipped items
- Emit `ItemEquipped`, `ItemDropped` signals

### T2-07 · Gold System
- `GoldPile.cs`: tappable world node; on tap call `GameManager.AddRunGold(amount)`
- `GameManager.AddRunGold()` / `BankRunGold()` / `LoseRunGold()` already defined in architecture
- Gold display in HUD reacts to `GoldChanged` signal

### T2-08 · HUD (`hud.tscn`)
- `DivineChargeBar.cs`: row of orb sprites, reacts to `ChargesChanged`
- `HeroStatPanel.cs`: HP bar + numeric, reacts to `Hero.HpChanged`
- `GoldLabel.cs`: reacts to `GameManager.GoldChanged`
- `MinimapDisplay.cs`: renders room rectangles; lights up visited rooms; updates on `RoomRevealed`

### T2-09 · Floor Transitions
- Hero reaching Exit tile → `GameManager.TransitionTo(GameState.DungeonActive)` on next floor
- `DungeonGenerator` generates a new floor seeded from `CurrentFloor + 1`
- On floor complete, bank gold: `GameManager.BankRunGold()`
- Floor number increments, floor scaling applied to `EnemyData` stat multipliers

### T2-10 · Hero Death & Run Reset
- `Hero.TakeDamage()` → if `Hp <= 0`: emit `Died`, set animator to `Death` state
- `GameManager.TransitionTo(GameState.HeroDead)` → show death screen overlay
- `GameManager.LoseRunGold()` clears in-run gold; banked gold untouched
- Load Shrine scene after death screen dismiss

---

## 🟡 Tier 3 — Content & Progression (Game has a loop but no staying power)

### T3-01 · Fog of War System
- Track `RoomState` enum: `Hidden, Partial, Revealed` per room
- On `Hero.EnteredRoom`: set current + adjacent rooms to `Partial`, current to `Revealed`
- `TileMap` modulate: Hidden = black, Partial = 40% opacity, Revealed = 100%
- `DivineCharges`: Bless Tile (3 charges) forces a `Hidden` → `Revealed` transition

### T3-02 · Save System (`SaveSystem.cs`)
- JSON save to `user://save.json` using Godot's `FileAccess`
- Serialise: `GoldBanked`, `PurchasedNodes`, `UnlockedDungeons`, `Bestiary`, `Statistics`
- `SaveSystem.Save()` called: on floor transition, on Shrine entry, on quit
- `SaveSystem.Load()` called: on game boot

### T3-03 · Skill Tree Data & Logic
- Define all 36 `SkillNode` records in `SkillTreeData.cs` across 4 branches × 3 tiers
- `SkillTree.cs`: `TryPurchase(string nodeId)` — check gold, prerequisite nodes, then apply modifiers
- `ApplyModifiers()`: iterate `PurchasedNodes`, push stat deltas to `Hero` and `DivineCharges`
- Emit `NodePurchased`, `StatModifierApplied` signals

### T3-04 · Shrine Scene & Skill Tree UI
- `shrine.tscn`: hero preview animation, gold display, dungeon selector, Skill Tree panel, Start Run button
- `SkillTreePanel.cs`: render nodes as buttons positioned in branch/tier grid
- Line2D connections between nodes; purchased = lit, available = dim, locked = greyed
- On node click: call `SkillTree.TryPurchase()`, update gold display

### T3-05 · Enemy Types — Tier 2 (Orc Brute, Dark Mage)
- Add records to `EnemyData.cs`
- `Orc Brute`: charge behaviour — moves 2 tiles toward hero on aggro
- `Dark Mage`: ranged — stays at distance, fires projectile using `Tween` to animate
- Implement projectile as simple `Area2D` that travels grid-aligned and calls `ResolveAttack` on overlap

### T3-06 · Boss System
- `BossRoom` room type: larger footprint (12×12 minimum), unique boss entity
- `Boss.cs` extends `Enemy.cs`: override tick to run phase-based patterns
- Phase transition at 50% HP: change attack pattern, increase speed
- On boss kill: emit `EnemyKilled` with elevated gold, guaranteed Rare+ loot roll, trigger `DungeonComplete`

### T3-07 · Smite & Bless Tile God Actions
- `TrySmiteEnemy(Vector2 pos)`: 5-charge action, deal `SmiteDamage` (flat, skill-upgradeable) to clicked enemy
- `TryBlessTile(Vector2 pos)`: 3-charge action, reveal tile, disarm traps
- Both actions play a distinct VFX (Godot `CPUParticles2D` or `GPUParticles2D`)

### T3-08 · Trap Tiles
- `TrapHidden` tiles deal damage when hero steps on them
- `HeroAI.Sixth Sense` skill (if purchased): `Pathfinder` marks `TrapHidden` as unwalkable
- Bless Tile converts `TrapHidden` → `TrapRevealed` (safe to walk, visually distinct)

### T3-09 · Room Type Events (Treasure, Shrine)
- `Treasure` room: spawn `Chest` node; player tap opens it, triggers Rare loot roll
- `Shrine` room: auto-buff on hero entry; randomly select one of: +10% ATK for floor, +20 HP, +1 Divine Charge max (temporary)
- `Wandering Merchant` room: UI overlay offers 2 items for in-run gold

---

## 🟢 Tier 4 — Polish & Depth (Great game, but not ship-ready without these)

### T4-01 · Audio (`AudioManager.cs`)
- SFX bus: attack, hurt, death, heal, level-up, gold pickup, UI click
- Music bus: dungeon loop, shrine ambient, boss stinger
- `AudioManager.PlaySfx(string key)` / `PlayMusic(string key)` with bus routing
- Expose volume sliders in settings menu

### T4-02 · Damage Numbers VFX (`DamageNumber.cs`)
- Pooled `Label3D` or `Label` nodes that float upward and fade out via `Tween`
- Colour coded: White (normal), Yellow (crit), Green (heal), Red (hero damaged)
- Spawn from `CombatResolver.DamageDealt` signal

### T4-03 · Camera System
- `Camera2D` follows hero with `PositionSmoothingEnabled = true`
- Zoom level: zoomed out enough to see 2–3 rooms simultaneously (aids God Finger legibility)
- Edge panning: when hero is near dungeon boundary, pan to show the room ahead

### T4-04 · Enemy Types — Tier 3 (Stone Golem) & Boss Variants
- Stone Golem: slow tick interval (2.0 s), very high DEF, cleave attack hits adjacent tiles
- Per-dungeon boss variants: Goblin King, Orc Warlord, Lich, Ancient Golem (§7)
- Unique boss artwork and attack animations

### T4-05 · Bestiary
- Track kill counts and first-seen floor per enemy type in `SaveSystem`
- `Bestiary.cs` UI: grid of enemy portraits, tap to see stats and flavour text
- Unlocks as enemies are encountered; locked entries show silhouette

### T4-06 · Settings & Pause Menu
- Pause: `GetTree().Paused = true`, show settings overlay
- Settings: master/SFX/music volume sliders, fullscreen toggle, keybinding display
- Persist to `user://settings.json`

### T4-07 · Main Menu & Scene Flow
- Main menu: New Game, Continue (greyed if no save), Settings, Quit
- Transitions between scenes use a full-screen fade `CanvasLayer` + `Tween`
- Death screen shows run stats (floors cleared, enemies killed, gold earned)

---

## ⚪ Tier 5 — Future Scope (Post-launch / DLC consideration)

### T5-01 · Dungeon Variants (Fungal Warrens, Obsidian Forge, Void Spire)
- Unique tile sets, enemy rosters, and floor modifiers per dungeon theme
- Unlock gating: clear Floor 5, 10, 20 respectively

### T5-02 · Heirloom System
- One Legendary item per run may be marked as Heirloom (tap-hold on item)
- Heirloom persists across runs in the Shrine chest, stat-scaled to current floor depth

### T5-03 · In-Run Hero Levelling
- Heroes gain XP from kills; reaching thresholds grants a single randomised bonus stat roll
- Kept separate from the permanent Skill Tree to avoid complexity spike in v1

### T5-04 · Dungeon Seed Reroll (Cartographer Skill)
- Expose run seed in `DungeonGenerator`; `Cartographer` node purchases a one-time reroll on death
- UI confirmation: "Reroll this dungeon? (Once per run)"

### T5-05 · Co-op God Fingers
- Second player shares the same Divine Charge pool, clicks contribute to the same hero
- Network: local split-input only (two mice / gamepad + mouse) — no netcode in scope

### T5-06 · Accessibility Options
- Colourblind-safe damage number palettes
- Adjustable UI scale
- Option to auto-collect all gold (removes tapping friction for accessibility)
- Screen reader tags on all UI elements

---

## Summary Cheatsheet

| Tier | Focus | Gate |
|---|---|---|
| 🔴 T1 (8 tasks) | Engine skeleton, grid, dungeon, hero movement, AI, enemies, combat | Must ship before any playtesting |
| 🟠 T2 (10 tasks) | God Finger input, charges, healing, loot, equip, HUD, floor loop, death | First playable milestone |
| 🟡 T3 (9 tasks) | Fog of war, save, skill tree, shrine, Tier 2 enemies, bosses, traps | Beta milestone |
| 🟢 T4 (7 tasks) | Audio, VFX, camera, content, bestiary, menus | Release candidate |
| ⚪ T5 (6 tasks) | Variants, heirlooms, co-op, accessibility | Post-launch |
