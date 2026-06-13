# Idle God Finger — Game Design Document

**Version:** 0.1 (Pre-Production)
**Engine:** Godot 4.6 · C# (.NET 8) · Desktop
**Genre:** Idle / Roguelite Dungeon Crawler
**Perspective:** 2D Top-Down Grid

---

## Table of Contents

1. [Vision & Pillars](#1-vision--pillars)
2. [Core Gameplay Loop](#2-core-gameplay-loop)
3. [The Hero](#3-the-hero)
4. [God Finger — Player Mechanics](#4-god-finger--player-mechanics)
5. [Combat System](#5-combat-system)
6. [Skill Tree](#6-skill-tree)
7. [Dungeon Generation](#7-dungeon-generation)
8. [Economy](#8-economy)
9. [Progression & Meta-Loop](#9-progression--meta-loop)
10. [Animation State Machine](#10-animation-state-machine)
11. [Technical Architecture](#11-technical-architecture)
12. [Scene & Node Structure](#12-scene--node-structure)
13. [Signal Map](#13-signal-map)
14. [Data Schema](#14-data-schema)
15. [Open Questions](#15-open-questions)

---

## 1. Vision & Pillars

### Elevator Pitch
You are an unseen god. Your chosen hero is brave but ultimately fragile — they will march into dungeons, kill monsters, and gather gold entirely on their own. Your divine power manifests only through your finger: tap to heal, tap to equip loot, tap to invest in their growth. Guide them deeper. Keep them alive.

### Design Pillars

| Pillar | What it Means in Practice |
|---|---|
| **Watchful Tension** | The player should always feel like something needs their attention — a near-death hero, uncollected loot, a skill node ready to unlock. |
| **Meaningful Clicks** | Every tap is a decision. Clicks are limited by cooldown or resource, so the player cannot simply spam. |
| **Satisfying Automation** | The hero's independent movement and combat should feel competent and fun to watch, not frustrating. |
| **Legible Depth** | The skill tree and dungeon systems should be immediately readable, with hidden depth revealed over time. |

---

## 2. Core Gameplay Loop

```
┌─────────────────────────────────────────────────────────┐
│                  DUNGEON RUN LOOP                       │
│                                                         │
│  Hero auto-moves → encounters enemy → auto-fights      │
│       ↓                                                 │
│  Loot / Gold drops → player taps to collect / equip    │
│       ↓                                                 │
│  Hero HP dips → player taps to heal                    │
│       ↓                                                 │
│  Hero reaches exit tile → next floor generates         │
│       ↓                                                 │
│  Floor boss defeated → Dungeon clear                   │
│       ↓                                                 │
│  Return to Shrine (meta hub) → spend gold on skill tree│
│       ↓                                                 │
│  New run begins (deeper dungeon, harder enemies)        │
└─────────────────────────────────────────────────────────┘
```

### Session States

```
SHRINE (hub) → DUNGEON_ACTIVE → BOSS_FIGHT → DUNGEON_COMPLETE
                    ↓
              HERO_DEAD → death_screen → SHRINE
```

---

## 3. The Hero

### Overview
The Hero is fully autonomous. They pathfind, attack, and loot without any player input. The player's only agency over movement is **indirect**: skills that change AI behaviour, or environmental triggers the player unlocks with clicks.

### Hero Stats

| Stat | Description | Base Value |
|---|---|---|
| `max_hp` | Maximum hit points | 100 |
| `hp` | Current hit points | 100 |
| `atk` | Physical attack damage | 8 |
| `def` | Damage reduction (flat) | 2 |
| `spd` | Tiles moved per second | 1.8 |
| `atk_spd` | Attacks per second | 1.0 |
| `crit_chance` | Probability of critical hit | 0.05 |
| `crit_mult` | Critical hit damage multiplier | 1.5 |
| `loot_range` | Tile radius auto-collected | 0 (manual tap by default) |
| `gold_bonus` | Multiplier on gold dropped | 1.0 |

### Hero AI Behaviour

The Hero uses a simple priority-based state machine evaluated every tick:

```
Priority order (highest first):
  1. FLEE      — if HP < flee_threshold% and path to exit exists
  2. FIGHT     — if enemy is in attack_range tiles
  3. APPROACH  — if enemy is visible but out of attack range
  4. LOOT      — if uncollected loot is on current tile
  5. EXPLORE   — move to next unvisited room/tile via pathfinding
  6. IDLE      — no valid targets; wait briefly then re-evaluate
```

`flee_threshold` starts at 0 (hero never flees by default) and can be set via skills.

### Hero AI Movement Rules

- Movement is **tile-by-tile** on a grid (default 16×16px tiles).
- The hero requests a path from the `Pathfinder` singleton (A* on the dungeon graph).
- Each step emits `hero_step_taken(from: Vector2i, to: Vector2i)`.
- Diagonal movement is **disabled** (4-directional only).
- If a tile is occupied by an enemy, the hero stops adjacent and enters FIGHT state.
- The hero re-calculates its path every `path_recalc_interval` seconds (default: 0.4s) or when a new room is revealed.

### Equipment Slots

```
HEAD  |  BODY  |  LEGS
  WEAPON  |  OFFHAND
      ACCESSORY
```

All six slots are visible on the UI. Dropped gear appears as world objects the player must **tap** to equip. The hero never self-equips.

---

## 4. God Finger — Player Mechanics

### Core Interactions

| Interaction | Target | Cost / Cooldown | Effect |
|---|---|---|---|
| **Heal Touch** | Hero | 1 Divine Charge | Restores `heal_amount` HP |
| **Equip** | Gear item on floor | None | Equips item to correct slot, old item drops |
| **Collect Gold** | Gold pile on floor | None | Adds to gold total |
| **Bless Tile** | Any floor tile | 3 Divine Charges | Reveals tile, removes traps |
| **Smite Enemy** | Enemy | 5 Divine Charges | Deals `smite_damage` to target |

### Divine Charge System

Divine Charges are the resource limiting God Finger actions.

- **Max Charges:** 5 (upgradeable via skill tree)
- **Regen Rate:** 1 charge per `regen_interval` seconds (default: 8s)
- **Visual:** A row of glowing orbs in the HUD

The charge cost system prevents spam-healing and forces genuine decision-making: do I smite this elite enemy or save charges for an emergency heal?

### Heal Touch Detail

- `heal_amount` = `base_heal` (20 HP) + `heal_bonus_from_skills`
- Visual: A golden ripple effect emanates from click point toward hero.
- If hero is at full HP, the click returns the charge (no waste).
- Healing is instant (no cast time).

### Gear Interaction Flow

```
Enemy dies → loot_roll() → item spawns as WorldItem node
Player taps WorldItem → EquipSystem.try_equip(item)
  → if slot is empty: equip directly
  → if slot is occupied: drop old item, equip new item
Old item becomes tappable WorldItem on the ground
Player may choose to leave it, equip it back, or let hero walk past it
```

---

## 5. Combat System

### Attack Resolution

```csharp
// res://src/gameplay/character/CombatResolver.cs
// Called by CombatResolver when hero is adjacent to an enemy
public partial class CombatResolver : Node
{
    [Signal] public delegate void DamageDealtEventHandler(Node target, int amount, bool isCrit);
    [Signal] public delegate void EnemyKilledEventHandler(Node enemy, int goldDropped);
    [Signal] public delegate void HeroHealedEventHandler(int amount);

    public int ResolveAttack(Character attacker, Character defender)
    {
        int rawDmg = attacker.Atk;
        bool isCrit = GD.Randf() < attacker.CritChance;

        if (isCrit)
            rawDmg = Mathf.RoundToInt(rawDmg * attacker.CritMult);

        int finalDmg = Mathf.Max(1, rawDmg - defender.Def);
        defender.TakeDamage(finalDmg);

        EmitSignal(SignalName.DamageDealt, defender, finalDmg, isCrit);
        return finalDmg;
    }
}
```

### Enemy Types

| Tier | Name | HP | ATK | DEF | Behaviour | Gold Drop |
|---|---|---|---|---|---|---|
| 1 | Goblin | 20 | 4 | 0 | Stationary until hero adjacent | 2–5 |
| 1 | Skeleton | 30 | 6 | 1 | Patrols corridor | 3–6 |
| 2 | Orc Brute | 60 | 12 | 4 | Charges hero on sight | 8–15 |
| 2 | Dark Mage | 40 | 16 | 0 | Ranged — stays at distance | 10–18 |
| 3 | Stone Golem | 120 | 20 | 8 | Slow, high threat | 20–35 |
| B | Floor Boss | Scaled | Scaled | Scaled | Unique pattern per dungeon | 50–100 |

### Enemy AI States

Enemies use the same priority state machine pattern as the hero for consistency:

```
IDLE → PATROL → AGGRO → ATTACK → STUNNED → DEAD
```

Enemies do **not** move in real-time — they act on a turn-based tick that fires every `enemy_tick_interval` seconds (default: 1.0s). This keeps the grid readable and the hero's movement predictable.

### Damage Numbers
Floating damage numbers appear above the target. Colour-coded:
- White: Normal damage
- Yellow: Critical hit
- Green: Hero healing
- Red: Hero taking damage

---

## 6. Skill Tree

### Overview
The Skill Tree is accessed at the **Shrine** (between runs). Nodes are purchased with **Gold**. The tree is divided into four branches, each with three tiers of nodes (3 nodes per tier = 36 nodes total in v1).

Nodes in a tier unlock when the player has purchased at least one node in the tier below it on that branch.

### Branches

#### ⚔️ Branch A — Warrior's Path (Combat)
Focuses on the hero's attack capabilities.

| Tier | Node | Cost | Effect |
|---|---|---|---|
| 1 | Iron Edge | 50g | +3 ATK |
| 1 | Battle Fury | 60g | +0.1 ATK SPD |
| 1 | First Blood | 40g | +10% damage on first hit in a room |
| 2 | Cleave | 150g | Attacks hit 1 adjacent enemy for 50% damage |
| 2 | Bloodlust | 120g | +5 HP restored on kill |
| 2 | Warlord's Eye | 100g | +0.05 crit chance |
| 3 | Berserker | 400g | Below 30% HP: +50% ATK, -20% DEF |
| 3 | Death Dealer | 350g | Crit multiplier ×2.0 → ×2.5 |
| 3 | Executioner | 500g | Enemies below 20% HP take +100% damage |

#### 🛡️ Branch B — Guardian's Vigil (Defence)
Focuses on hero survivability and god healing power.

| Tier | Node | Cost | Effect |
|---|---|---|---|
| 1 | Thick Hide | 50g | +15 max HP |
| 1 | Iron Will | 50g | +1 DEF |
| 1 | Steadfast | 60g | Heal Touch restores +10 HP |
| 2 | Second Wind | 150g | Once per room, auto-trigger: heal 30 HP when HP < 15% |
| 2 | Divine Ward | 120g | Smite also applies 3-second stun |
| 2 | Stone Skin | 100g | +3 DEF, -0.1 ATK SPD |
| 3 | Bulwark | 350g | +40 max HP, +5 DEF |
| 3 | Sanctuary | 400g | Heal Touch removes 1 debuff |
| 3 | Undying | 600g | Once per run: hero survives lethal hit with 1 HP |

#### 🌟 Branch C — God's Favour (Divine Powers)
Enhances the God Finger's abilities.

| Tier | Node | Cost | Effect |
|---|---|---|---|
| 1 | Deep Reserves | 60g | +1 max Divine Charge |
| 1 | Swift Providence | 50g | -2s charge regen interval |
| 1 | Gold Sight | 40g | Gold piles auto-collected in 1-tile radius |
| 2 | Holy Hand | 150g | Heal Touch charges cost 0 once per 30s |
| 2 | Divine Wrath | 130g | Smite damage +50% |
| 2 | Blessed Ground | 120g | Bless Tile cost reduced to 1 charge |
| 3 | Avatar's Blessing | 400g | Tap hero to grant +25% ATK for 10s (1 charge) |
| 3 | Omniscience | 350g | Dungeon map fully revealed at run start |
| 3 | Hand of God | 500g | +1 max charge; all costs -1 |

#### 🏃 Branch D — Pathfinder (Exploration)
Improves hero movement and dungeon navigation.

| Tier | Node | Cost | Effect |
|---|---|---|---|
| 1 | Fleet Foot | 50g | +0.2 hero SPD |
| 1 | Dungeon Sense | 60g | Hero prioritises rooms with loot first |
| 1 | Treasure Hunter | 40g | +10% gold drop from enemies |
| 2 | Sixth Sense | 150g | Hero avoids trap tiles automatically |
| 2 | Battle-Hardened | 120g | Hero re-engages EXPLORE 0.5s faster after combat |
| 2 | Hoarder | 100g | +20% gold bonus |
| 3 | Blur | 350g | SPD +0.5; hero ignores slow tiles |
| 3 | Cartographer | 300g | Procedural dungeon seed rerolled once (per run) if hero dies |
| 3 | Fortune's Favourite | 450g | Boss gold drop ×2 |

### Skill Tree Data Structure

```csharp
// res://src/resources/item_definitions/SkillTreeData.cs
public static class SkillTreeData
{
    public record SkillNode(
        string Branch,
        int Tier,
        string Name,
        int Cost,
        string Description,
        string[] Prerequisites,
        Dictionary<string, float> StatModifiers,
        string Special = ""
    );

    public static readonly Dictionary<string, SkillNode> Nodes = new()
    {
        ["iron_edge"] = new SkillNode(
            Branch:        "warrior",
            Tier:          1,
            Name:          "Iron Edge",
            Cost:          50,
            Description:   "+3 ATK",
            Prerequisites: Array.Empty<string>(),
            StatModifiers: new() { ["Atk"] = 3f }
        ),
        // ... all 36 nodes
    };
}
```

---

## 7. Dungeon Generation

### Algorithm — BSP Room Placement

The dungeon uses **Binary Space Partitioning (BSP)** to guarantee rooms don't overlap and corridors are always connected.

```
1. Start with the full dungeon rectangle (e.g. 60×60 tiles)
2. Recursively split into sub-regions (min room size: 6×6 tiles)
3. Place a room within each leaf region
4. Connect sibling rooms with L-shaped corridors
5. Guarantee: one entrance tile, one exit tile, one boss room on floor N
```

### Room Types

| Type | Frequency | Contents |
|---|---|---|
| **Combat** | 50% | 1–4 enemies, possible loot |
| **Treasure** | 15% | No enemies, guaranteed chest (tap to open) |
| **Trap** | 10% | Hidden tile effects (spike pits, poison gas) |
| **Shrine** | 5% | Temporary stat buff for the hero (auto-triggered) |
| **Corridor** | 15% | Narrow passage, possible patrol enemy |
| **Boss** | 1/floor | Boss enemy, large room, guaranteed loot |

### Floor Scaling

| Floor | Enemy Tier | Room Count | Boss |
|---|---|---|---|
| 1–3 | Tier 1 | 8–12 | Goblin King |
| 4–6 | Tier 1–2 | 10–15 | Orc Warlord |
| 7–10 | Tier 2 | 12–18 | Lich |
| 11+ | Tier 2–3 | 15–22 | Ancient Golem |

### Tile Types

```csharp
// res://src/levels/DungeonGenerator.cs
public enum TileType
{
    Wall,
    Floor,
    Entrance,
    Exit,
    TrapHidden,
    TrapRevealed,
    ChestClosed,
    ChestOpen,
    Water,   // slow tile
    Void,    // impassable
}
```

### Fog of War
- Unvisited rooms are hidden (full black).
- Rooms adjacent to visited rooms show a dim silhouette (partial reveal).
- The hero's current room and a 1-room radius are fully lit.
- God Finger can "Bless Tile" to reveal hidden areas.

---

## 8. Economy

### Gold

Gold is the primary currency. It drops from enemies and chests.

- **In-run gold** is spent at the **Skill Tree** (only accessible at the Shrine between runs).
- Gold is **persistent** — it carries over on death. The hero's death is a setback, not a wipe.
- Gold can also be spent mid-run at a **Wandering Merchant** (rare room event) for temporary buffs.

### Gear Rarity

| Rarity | Colour | Stat Bonus Range | Drop Chance |
|---|---|---|---|
| Common | Grey | +1–3% | 60% |
| Uncommon | Green | +5–10% | 25% |
| Rare | Blue | +12–20% | 12% |
| Legendary | Gold | Unique effect + stats | 3% |

### Gear Stat Rolls
Each gear piece rolls 1–3 stats from the pool relevant to its slot:
- **Weapon:** ATK, ATK SPD, CRIT CHANCE, CRIT MULT
- **Armour (Head/Body/Legs):** HP, DEF, SPD
- **Offhand:** DEF, HP, gold_bonus, loot_range
- **Accessory:** Any stat, special effect possible

---

## 9. Progression & Meta-Loop

### Run Structure

```
Shrine (hub)
  → Accept Quest (select dungeon depth target)
  → Run begins at Floor 1
  → Clear floors sequentially
  → Hero death OR quest complete → Return to Shrine
  → Spend accumulated gold on Skill Tree
  → Begin next run
```

### Shrine Features

The Shrine is the meta-hub the player returns to between runs:

- **Skill Tree Panel** — Purchase/view nodes.
- **Bestiary** — Enemies the hero has killed, logs their stats.
- **Heirloom Chest** — Rare items persist between runs (unlocked at Floor 10).
- **Dungeon Selector** — Choose which dungeon to attempt (unlockable variants).

### Dungeon Variants (Unlockable)

| Dungeon | Unlock Condition | Theme | Modifier |
|---|---|---|---|
| The Crypts | Default | Stone & undead | Standard |
| The Fungal Warrens | Clear Floor 5 | Organic, mushroom | Enemies apply poison |
| The Obsidian Forge | Clear Floor 10 | Lava & constructs | High DEF enemies |
| The Void Spire | Clear Floor 20 | Surreal, eldritch | Randomised mechanics |

### Death & Persistence

- On hero death, the **current run's gold** is lost.
- Any gold already banked at the Shrine is safe.
- Gear equipped to the hero is lost on death.
- Skill Tree upgrades are permanent.
- Heirloom items persist.

---

## 10. Animation State Machine

The hero `AnimatedSprite2D` uses a **state-driven animation controller**.

### States & Transitions

```
                ┌─────────────┐
           ┌───▶│    IDLE     │◀──────────────┐
           │    └──────┬──────┘               │
           │           │ movement input        │
           │    ┌──────▼──────┐               │
           │    │    WALK     │               │
           │    └──────┬──────┘               │
           │           │ enemy adjacent        │
           │    ┌──────▼──────┐               │
           │    │   ATTACK    │──── attack_finished ─┘
           │    └──────┬──────┘
           │           │ damage taken
           │    ┌──────▼──────┐
           │    │    HURT     │
           │    └──────┬──────┘
           │           │ hurt_finished
           └───────────┘
           
    Any state:
           │ hp == 0
    ┌──────▼──────┐
    │    DEATH    │──── death_finished ───▶ [despawn]
    └─────────────┘
    
    IDLE/WALK + heal received:
    ┌──────────────┐
    │    HEAL      │──── heal_finished ───▶ previous state
    └──────────────┘
```

### C# State Machine Implementation

```csharp
// res://src/gameplay/character/HeroAnimator.cs
using Godot;
using System.Collections.Generic;

public partial class HeroAnimator : Node
{
    public enum State { Idle, Walk, Attack, Hurt, Death, Heal }

    [Signal] public delegate void AnimationFinishedEventHandler(State state);

    private State _currentState = State.Idle;
    private AnimatedSprite2D _sprite = null!;
    private Vector2 _facing = Vector2.Right;

    private static readonly Dictionary<State, string> AnimMap = new()
    {
        [State.Idle]   = "idle",
        [State.Walk]   = "walk",
        [State.Attack] = "attack",
        [State.Hurt]   = "hurt",
        [State.Death]  = "death",
        [State.Heal]   = "heal",
    };

    public override void _Ready()
    {
        _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _sprite.AnimationFinished += OnSpriteAnimationFinished;
    }

    public void SetState(State newState)
    {
        if (_currentState == State.Death) return; // Terminal — no escape
        _currentState = newState;
        PlayAnim(newState);
    }

    public void SetFacing(Vector2 direction) => _facing = direction;

    private void PlayAnim(State state)
    {
        string baseName  = AnimMap[state];
        string full      = baseName + DirectionSuffix();
        _sprite.Play(_sprite.SpriteFrames.HasAnimation(full) ? full : baseName);
    }

    private string DirectionSuffix()
    {
        if (Mathf.Abs(_facing.X) > Mathf.Abs(_facing.Y))
            return _facing.X > 0f ? "_right" : "_left";
        return _facing.Y > 0f ? "_down" : "_up";
    }

    private void OnSpriteAnimationFinished()
    {
        EmitSignal(SignalName.AnimationFinished, (int)_currentState);
        switch (_currentState)
        {
            case State.Attack:
            case State.Hurt:
            case State.Heal:
                SetState(State.Idle);
                break;
            // State.Death is terminal — handled by HeroAI
        }
    }
}
```

### Animation Spritesheet Layout

Each character has a spritesheet with 4-directional variants:

```
Row 0: idle_down    (4 frames)
Row 1: idle_up      (4 frames)
Row 2: idle_right   (4 frames)  # left is h-flipped
Row 3: walk_down    (6 frames)
Row 4: walk_up      (6 frames)
Row 5: walk_right   (6 frames)
Row 6: attack_down  (5 frames)
Row 7: attack_up    (5 frames)
Row 8: attack_right (5 frames)
Row 9: hurt         (3 frames)  # non-directional
Row 10: death       (8 frames)  # non-directional
Row 11: heal        (4 frames)  # non-directional
```

---

## 11. Technical Architecture

### Autoloads

| Singleton | Responsibility |
|---|---|
| `GameManager` | Run state machine, floor transitions, overall game state |
| `AudioManager` | SFX/music playback, audio bus management |
| `SaveSystem` | JSON-based save/load, skill tree persistence, gold persistence |

### Key Systems & Files

```
res://
├── addons/                              ← third-party plugins
├── assets/
│   ├── art/
│   │   ├── characters/                  ← hero & enemy spritesheets (.png)
│   │   ├── ui/                          ← HUD icons, charge orbs, fonts
│   │   └── world/                       ← tilesets, dungeon tile textures
│   └── audio/                           ← SFX (.ogg) and music tracks (.ogg)
├── src/
│   ├── core/
│   │   ├── autoload/
│   │   │   ├── GameManager.cs           ← run state machine (autoload)
│   │   │   ├── AudioManager.cs          ← SFX/music bus control (autoload)
│   │   │   └── SaveSystem.cs            ← JSON persistence (autoload)
│   │   ├── main_game/
│   │   │   ├── MainGame.cs              ← root scene controller, input entry point
│   │   │   └── main_game.tscn           ← root scene (dungeon, HUD, camera)
│   │   └── debug/
│   │       └── DebugOverlay.cs          ← in-editor stat overlay
│   ├── gameplay/
│   │   ├── camera/
│   │   │   └── DungeonCamera.cs         ← hero-following Camera2D
│   │   ├── character/
│   │   │   ├── Hero.cs / hero.tscn      ← CharacterBody2D, stats, HP
│   │   │   ├── HeroAI.cs                ← priority state machine (Flee→Fight→Explore)
│   │   │   ├── HeroAnimator.cs          ← directional animation controller
│   │   │   ├── HeroStats.cs             ← stat container + modifier stack
│   │   │   ├── CombatResolver.cs        ← attack/damage resolution, signals
│   │   │   ├── EquipSystem.cs           ← 6-slot gear management
│   │   │   ├── DivineCharges.cs         ← charge pool, regen timer
│   │   │   └── Pathfinder.cs            ← A* over dungeon tile graph
│   │   ├── enemies/
│   │   │   ├── Enemy.cs / enemy.tscn    ← base enemy CharacterBody2D
│   │   │   ├── EnemyAI.cs               ← tick-based state machine
│   │   │   └── Boss.cs / boss.tscn      ← phase-based boss extension
│   │   └── interactables/
│   │       ├── WorldItem.cs             ← tappable gear drop node
│   │       ├── GoldPile.cs              ← tappable gold drop node
│   │       └── Chest.cs                 ← tappable treasure chest
│   ├── levels/
│   │   ├── DungeonGenerator.cs          ← orchestrates BSP + room placement
│   │   ├── BSPSplitter.cs               ← recursive space partition
│   │   ├── RoomPlacer.cs                ← room/corridor tile writer
│   │   ├── LootTable.cs                 ← weighted loot roller
│   │   ├── dungeon.tscn                 ← dungeon TileMap + entity containers
│   │   └── shrine.tscn                  ← meta hub between runs
│   ├── resources/
│   │   ├── enemy_definitions/
│   │   │   └── EnemyData.cs             ← EnemyStats records + lookup dict
│   │   └── item_definitions/
│   │       ├── SkillTreeData.cs          ← 36 SkillNode records
│   │       ├── LootTables.cs             ← rarity/slot weight tables
│   │       └── ItemData.cs               ← gear record definition
│   ├── shaders/
│   │   └── fog_of_war.gdshader          ← fog reveal shader
│   └── ui/
│       ├── HUD.cs / hud.tscn            ← top-level HUD CanvasLayer
│       ├── DivineChargeBar.cs           ← charge orb row
│       ├── HeroStatPanel.cs             ← HP bar + numeric stats
│       ├── MinimapDisplay.cs            ← minimap renderer
│       ├── DamageNumber.cs              ← pooled floating number labels
│       └── skill_tree/
│           ├── SkillTreePanel.cs        ← full skill tree UI panel
│           └── SkillNodeButton.cs       ← individual purchasable node widget
├── idle_god_finger_GDD.md
└── README.md
```

### GameManager State Machine

```csharp
// res://src/core/autoload/GameManager.cs
using Godot;

public partial class GameManager : Node
{
    public enum GameState
    {
        MainMenu,
        Shrine,
        DungeonActive,
        BossFight,
        DungeonComplete,
        HeroDead,
        Paused,
    }

    [Signal] public delegate void StateChangedEventHandler(GameState newState);
    [Signal] public delegate void FloorChangedEventHandler(int floorNum);
    [Signal] public delegate void GoldChangedEventHandler(int total);

    public GameState State      { get; private set; } = GameState.MainMenu;
    public int CurrentFloor     { get; private set; } = 0;
    public int GoldBanked       { get; private set; } = 0; // Persistent across runs
    public int GoldInRun        { get; private set; } = 0; // Lost on death

    public void TransitionTo(GameState newState)
    {
        State = newState;
        EmitSignal(SignalName.StateChanged, (int)newState);
    }

    public void AddRunGold(int amount)
    {
        GoldInRun += amount;
        EmitSignal(SignalName.GoldChanged, GoldBanked + GoldInRun);
    }

    public void BankRunGold()
    {
        GoldBanked += GoldInRun;
        GoldInRun = 0;
        EmitSignal(SignalName.GoldChanged, GoldBanked);
    }

    public void LoseRunGold() => GoldInRun = 0;
}
```

### Grid Coordinate Convention

- All grid positions use `Vector2i` (integer x, y).
- World positions = `grid_pos * TILE_SIZE` where `TILE_SIZE = 16`.
- The dungeon `TileMap` lives on layer 0 (floor tiles) and layer 1 (walls/objects).

### Input Handling — God Finger

```csharp
// res://src/core/main_game/MainGame.cs (partial — God Finger input)
public override void _UnhandledInput(InputEvent @event)
{
    if (@event is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        return;

    Vector2 worldPos = GetGlobalMousePosition();
    HandleGodClick(worldPos);
}

private void HandleGodClick(Vector2 pos)
{
    // Priority chain: Hero > Enemy > WorldItem > GoldPile > Tile
    if (TryHealHero(pos))    return;
    if (TrySmiteEnemy(pos))  return;
    if (TryEquipItem(pos))   return;
    if (TryCollectGold(pos)) return;
    TryBlessTile(pos);
}
```

---

## 12. Scene & Node Structure

### Dungeon Scene Tree

```
Dungeon (Node2D)
├── TileMap (TileMapLayer ×2)
├── DungeonCamera (Camera2D)
│   └── [follows hero with smoothing]
├── Entities (Node2D)
│   ├── Hero (CharacterBody2D)
│   │   ├── AnimatedSprite2D
│   │   ├── CollisionShape2D
│   │   ├── HeroAI (Node)
│   │   ├── HeroAnimator (Node)
│   │   └── HeroStats (Node)
│   ├── Enemies (Node2D)
│   │   └── [Enemy instances...]
│   └── WorldItems (Node2D)
│       └── [WorldItem / GoldPile instances...]
├── GodFingerLayer (CanvasLayer)   # always on top for click detection
│   └── GodFingerInputCapture (Control)
└── HUD (CanvasLayer)
    ├── DivineChargeBar
    ├── HeroStatPanel
    ├── MinimapDisplay
    └── GoldLabel
```

### Shrine Scene Tree

```
Shrine (Node2D)
├── BackgroundSprite
├── HeroPreview (AnimatedSprite2D)
├── CanvasLayer
│   ├── SkillTreePanel
│   │   ├── SkillNodeButton (×36)
│   │   └── ConnectionLines (Line2D ×n)
│   ├── GoldDisplay
│   ├── BestiaryButton
│   └── StartRunButton
```

---

## 13. Signal Map

These are the primary signals connecting systems. Using signals over direct references keeps systems decoupled.

```csharp
// Hero.cs
[Signal] public delegate void HpChangedEventHandler(int newHp, int maxHp);
[Signal] public delegate void DiedEventHandler();
[Signal] public delegate void StepTakenEventHandler(Vector2I from, Vector2I to);
[Signal] public delegate void EnteredRoomEventHandler(int roomId);

// CombatResolver.cs
[Signal] public delegate void DamageDealtEventHandler(Node target, int amount, bool isCrit);
[Signal] public delegate void EnemyKilledEventHandler(Node enemy, int goldDropped);
[Signal] public delegate void HeroHealedEventHandler(int amount);

// DivineCharges.cs
[Signal] public delegate void ChargesChangedEventHandler(int current, int max);
[Signal] public delegate void ChargeSpentEventHandler(string action);
[Signal] public delegate void ChargeRestoredEventHandler();

// DungeonGenerator.cs
[Signal] public delegate void DungeonGeneratedEventHandler(DungeonData data);
[Signal] public delegate void RoomRevealedEventHandler(int roomId);
[Signal] public delegate void FloorExitReachedEventHandler();

// EquipSystem.cs
[Signal] public delegate void ItemEquippedEventHandler(string slot, ItemData item);
[Signal] public delegate void ItemDroppedEventHandler(ItemData item, Vector2I position);

// GameManager.cs
[Signal] public delegate void StateChangedEventHandler(GameManager.GameState newState);
[Signal] public delegate void FloorChangedEventHandler(int floorNum);
[Signal] public delegate void GoldChangedEventHandler(int total);

// SkillTree.cs
[Signal] public delegate void NodePurchasedEventHandler(string nodeId);
[Signal] public delegate void StatModifierAppliedEventHandler(string stat, float value);
```

---

## 14. Data Schema

### Save File Structure (JSON)

```json
{
  "version": 1,
  "gold_banked": 340,
  "skill_tree": {
    "purchased_nodes": ["iron_edge", "thick_hide", "fleet_foot"],
    "total_spent": 160
  },
  "unlocks": {
    "dungeons": ["crypts", "fungal_warrens"],
    "heirlooms": []
  },
  "bestiary": {
    "goblin": { "kills": 42, "first_seen": 1 },
    "skeleton": { "kills": 27, "first_seen": 1 }
  },
  "statistics": {
    "total_runs": 7,
    "deepest_floor": 6,
    "total_gold_earned": 1240,
    "total_enemies_killed": 183
  }
}
```

### Enemy Data Resource

```csharp
// res://src/resources/enemy_definitions/EnemyData.cs
public static class EnemyData
{
    public record EnemyStats(
        string DisplayName,
        int    Tier,
        int    BaseHp,
        int    BaseAtk,
        int    BaseDef,
        string Behaviour,
        int    GoldMin,
        int    GoldMax,
        string LootTable,
        string Spritesheet
    );

    public static readonly Dictionary<string, EnemyStats> Enemies = new()
    {
        ["goblin"] = new EnemyStats(
            DisplayName: "Goblin",
            Tier:        1,
            BaseHp:      20,
            BaseAtk:     4,
            BaseDef:     0,
            Behaviour:   "stationary",
            GoldMin:     2,
            GoldMax:     5,
            LootTable:   "common_tier1",
            Spritesheet: "res://assets/enemies/goblin.png"
        ),
        // ... all enemy entries
    };
}
```

### Loot Table Resource

```csharp
// res://src/resources/item_definitions/LootTables.cs
public static class LootTables
{
    public record RarityWeights(
        float Common, float Uncommon, float Rare, float Legendary);

    public record SlotWeights(
        float Weapon, float Head, float Body,
        float Legs, float Offhand, float Accessory);

    public record LootTable(
        float DropChance,
        RarityWeights Rarities,
        SlotWeights   Slots);

    public static readonly Dictionary<string, LootTable> Tables = new()
    {
        ["common_tier1"] = new LootTable(
            DropChance: 0.30f,  // 30% chance any item drops at all
            Rarities: new(Common: 0.60f, Uncommon: 0.25f,
                          Rare:   0.12f, Legendary: 0.03f),
            Slots:    new(Weapon: 0.25f, Head:  0.15f, Body:    0.20f,
                          Legs:   0.15f, Offhand: 0.15f, Accessory: 0.10f)
        ),
        // ... additional tables per enemy tier
    };
}
```

---

## 15. Open Questions

These design decisions are unresolved and require playtesting or further discussion:

| # | Question | Options | Priority |
|---|---|---|---|
| 1 | Should the hero ever move diagonally? | No (simpler, more readable) vs Yes (faster traversal) | High |
| 2 | Should gold in-run be automatically banked on floor complete, or only on full dungeon clear? | Auto-bank per floor is more forgiving | High |
| 3 | Is 5 Divine Charges enough starting max? | Could start at 3, make 5 a skill unlock | Medium |
| 4 | Should Smite work on bosses, or be blocked? | Blocking Smite on bosses raises stakes | Medium |
| 5 | How is loot quality balanced with equipment loss on death? | Heirloom system may need expansion | High |
| 6 | Should the hero show a level stat that grows in-run, or only through the skill tree? | In-run levelling adds complexity | Low |
| 7 | Camera behaviour: follow hero tightly, or show a wider dungeon view? | Zoomed-out is better for God Finger legibility | Medium |
| 8 | Should there be a "rescue" mechanic when hero dies mid-run? | Could cost a large Divine resource to revive once | Low |
| 9 | Multiplayer (co-op god fingers)? | Future feature only; two players sharing charges | Low |

---

*Document maintained by development team. Update version and date on every structural revision.*
