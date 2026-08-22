# Master Plan
## Working Title: **Crown & Conquest**
### Original Real-Time Strategy + RPG Game

**Version:** 1.0  
**Project Type:** Desktop Real-Time Strategy / RPG  
**Primary Inspirations:** Celtic Kings: Rage of War, Nemesis of the Roman Empire, Age of Empires  
**Design Principle:** Inspired by classic RTS mechanics, but with an original world, factions, characters, art, terminology, lore, and gameplay systems.

---

# 1. Vision

Build a desktop real-time strategy game combining:

- The tactical battlefield and hero progression of Celtic Kings
- The settlement building and economy of Age of Empires
- RPG-style character progression
- Individual unit progression
- Tactical formations
- Terrain-aware combat
- Siege warfare
- Strategic AI
- Campaign/world progression

The player should feel that they are simultaneously:

1. Building a civilization
2. Managing an economy
3. Commanding an army
4. Developing individual warriors
5. Leading heroes
6. Controlling territory
7. Making strategic decisions

The game should begin as a small, technically manageable RTS and evolve incrementally into a large-scale strategy game.

---

# 2. Core Gameplay Pillars

The entire game should be built around seven pillars.

## Pillar 1: Build

Gather resources and construct settlements.

## Pillar 2: Develop

Research technologies and advance civilization capabilities.

## Pillar 3: Recruit

Create workers, soldiers, cavalry, ranged units, siege weapons, and heroes.

## Pillar 4: Command

Control individual units, groups, formations, and armies.

## Pillar 5: Progress

Units and heroes become stronger through experience and battlefield achievements.

## Pillar 6: Conquer

Capture territory, resources, strategic locations, and enemy settlements.

## Pillar 7: Survive

Protect experienced units and heroes because losing veterans has meaningful consequences.

---

# 3. The Most Important Unique Mechanic

## Individual Unit Experience

Every combat-capable unit has its own progression.

A unit does not remain identical throughout the entire game.

Example:

```text
Recruit Swordsman
      ↓
First Kill
      ↓
Level 2
      ↓
More Combat
      ↓
Level 3
      ↓
Veteran
      ↓
Elite
      ↓
Legendary Warrior
```

## Core Rule

When Unit A kills Unit B:

```text
Unit A
  │
  ├── receives Kill XP
  ├── receives kill count +1
  ├── checks level threshold
  └── levels up automatically if threshold reached
```

The level-up happens immediately.

This is inspired by the progression philosophy of Celtic Kings but will be implemented as an original system.

---

# 4. Unit Progression System

Every combat unit maintains:

```text
Unit
├── Unique ID
├── Unit Type
├── Faction
├── Level
├── Experience
├── Kill Count
├── Damage Dealt
├── Battles Participated
├── Battles Survived
├── Current Health
├── Maximum Health
├── Morale
├── Veterancy Rank
└── Status Effects
```

Example:

```text
Marcus
Roman Legionnaire

Level: 4
XP: 380 / 500
Kills: 17
Battles: 8
Survived: 7
Veterancy: Veteran
```

---

# 5. XP Model

The initial model should be simple.

A kill provides XP based on the target.

```text
Kill XP =
Base XP
+ Target Level Bonus
+ Target Unit Tier Bonus
```

Example:

```text
Kill Recruit
→ +50 XP

Kill Veteran
→ +100 XP

Kill Elite
→ +175 XP

Kill Hero
→ special XP reward
```

The exact numbers should be data-driven and balanceable.

Never hard-code progression values into combat logic.

---

# 6. Level-Up System

Each unit type has configurable progression thresholds.

Example:

```text
Level 1 → 0 XP
Level 2 → 100 XP
Level 3 → 250 XP
Level 4 → 500 XP
Level 5 → 900 XP
```

A level-up can improve:

- Maximum health
- Attack
- Defense
- Accuracy
- Attack speed
- Movement
- Morale
- Ability effectiveness
- Resistance

However, not every unit should gain every stat.

Stat progression should depend on unit archetype.

---

# 7. Veterancy Ranks

Levels and ranks are separate concepts.

Example:

```text
Level 1-2
Recruit

Level 3-4
Experienced

Level 5-6
Veteran

Level 7-8
Elite

Level 9+
Legendary
```

The rank can unlock visual and gameplay effects.

For example:

```text
Veteran
├── improved morale
├── small combat bonus
└── veteran visual indicator

Elite
├── improved combat statistics
├── resistance bonus
└── special ability

Legendary
├── unique ability
├── major morale bonus
└── unique visual identity
```

---

# 8. The Veteran Problem

A powerful consequence of this system is that experienced units become valuable.

Therefore:

```text
New Army
↓
Low experience
↓
Cheap to replace
```

versus:

```text
Veteran Army
↓
Highly experienced
↓
Extremely valuable
↓
Player becomes emotionally invested
```

This creates an important strategic choice:

> Do I risk my veteran army to win this battle?

That decision should be one of the game's strongest strategic emotions.

---

# 9. Unit Death

Death must matter.

When a unit dies:

```text
Unit
↓
Death Event
↓
Remove from Active Army
↓
Record Final Statistics
```

Optional future feature:

## Memorial System

Important veterans can appear in:

- Hall of Heroes
- Campaign history
- Battle records
- Memorial screens

Example:

```text
LEGENDARY WARRIOR

Marcus
Level 8

Kills: 41
Battles: 19
Victories: 15

Fell at the Battle of Red Valley.
```

---

# 10. Heroes

Heroes are different from normal units.

Heroes have:

- Levels
- XP
- Abilities
- Equipment
- Attributes
- Skill trees
- Reputation
- Personal history
- Special quests
- Wounds
- Potential permanent death

Heroes can also command groups of units.

Example:

```text
Hero
 │
 ├── Leadership
 ├── Combat
 ├── Strategy
 └── Special Ability
```

---

# 11. Hero + Unit Interaction

Heroes should influence nearby troops.

Example:

```text
Hero Aura

Nearby units:

+10% morale
+5% attack
+5% movement
```

Different heroes create different battlefield identities.

A defensive commander may provide:

```text
+20% defense
+15% morale
```

A cavalry commander:

```text
+15% cavalry movement
+10% cavalry attack
```

---

# 12. Civilization Layer

The game incorporates the economic depth of classic RTS games.

Resources:

- Food
- Wood
- Stone
- Iron
- Gold

Potential strategic resource:

- Prestige

The economy should be completely data-driven.

---

# 13. Workers

Workers are responsible for:

- Gathering
- Construction
- Repair
- Resource transport

Worker AI should support:

```text
Idle
↓
Receive Job
↓
Move
↓
Perform Job
↓
Return Resource
↓
Repeat
```

---

# 14. Buildings

Building categories:

## Economy

- Farm
- Lumber Camp
- Quarry
- Mine
- Storage

## Military

- Barracks
- Archery Range
- Stable
- Siege Workshop

## Technology

- Academy
- Blacksmith
- Research Center

## Defense

- Watchtower
- Walls
- Gate
- Fortress
- Castle

## Civilization

- Town Center
- Market
- Temple
- Monument

---

# 15. Civilization Progression

The game should have eras.

Example:

```text
Age I
Tribal Age

      ↓

Age II
Kingdom Age

      ↓

Age III
Imperial Age

      ↓

Age IV
Dominion Age
```

Each era unlocks:

- Buildings
- Technologies
- Units
- Heroes
- Siege capabilities
- Civilization bonuses

The names are placeholders and should eventually become part of the game's original lore.

---

# 16. Technology System

Technologies modify gameplay.

Examples:

```text
Iron Forging
→ +10% infantry attack

Reinforced Shields
→ +10% infantry defense

Horse Breeding
→ +10% cavalry movement

Siege Engineering
→ improved siege damage
```

Technologies should be represented as data objects rather than hard-coded rules.

---

# 17. Military Units

Initial unit families:

## Infantry

- Swordsman
- Spearman
- Heavy Infantry

## Ranged

- Archer
- Crossbowman
- Skirmisher

## Cavalry

- Scout
- Light Cavalry
- Heavy Cavalry

## Siege

- Ram
- Catapult
- Ballista

## Support

- Healer
- Engineer
- Supply Unit

The first MVP should contain only a few units.

---

# 18. Combat System

Combat should consider:

```text
Attack
Defense
Armor
Weapon Type
Range
Accuracy
Attack Speed
Terrain
Formation
Morale
Unit Level
Veterancy
Hero Effects
Technology
```

Combat should be deterministic enough for testing while still allowing controlled randomness.

---

# 19. Terrain

Terrain is a strategic mechanic.

Initial terrain:

- Grassland
- Forest
- Mountain
- River
- Road
- Hill
- Swamp

Terrain affects:

- Movement
- Vision
- Ranged attacks
- Cavalry
- Formation effectiveness
- Ambushes
- Defense

Example:

```text
Forest
→ cavalry movement penalty
→ improved concealment
→ ambush opportunity
```

---

# 20. Formations

Players should be able to select groups and assign formations.

Initial formations:

- Line
- Column
- Defensive
- Wedge
- Shield Wall

Formation behavior should influence unit positioning and combat modifiers.

---

# 21. Morale

Units should not behave like emotionless chess pieces.

Morale can be affected by:

- Nearby deaths
- Hero presence
- Unit experience
- Formation
- Being surrounded
- Losing a battle
- Winning a battle
- Terrain
- Enemy strength

Possible states:

```text
Inspired
Confident
Stable
Shaken
Panicked
Routing
```

Routing units can retreat instead of fighting to the death.

---

# 22. Siege Warfare

Sieges should eventually become a major gameplay system.

Features:

- Walls
- Gates
- Towers
- Siege weapons
- Rams
- Catapults
- Defensive repairs
- Gate destruction
- Wall breaches

The goal is to make attacking a fortress a tactical problem rather than simply increasing attack damage.

---

# 23. World Map

Later versions should introduce a strategic world map.

The player moves armies between regions.

Strategic locations:

- Villages
- Mines
- Forests
- Forts
- Cities
- Ruins
- Trade routes
- Mountain passes

Entering combat can transition into the RTS battlefield.

---

# 24. Factions

The first release should have a small number of original factions.

Potential initial factions:

## The Imperial Legion

Strength:

- Discipline
- Formations
- Engineering
- Siege

## The Celtic Clans

Strength:

- Heroes
- Raiding
- Forest warfare
- Berserker units

## The Northern Tribes

Strength:

- Heavy infantry
- Endurance
- Defensive warfare

## The Eastern Kingdoms

Strength:

- Cavalry
- Trade
- Mobility

These are temporary design names and should eventually be replaced by original lore-specific identities.

---

# 25. Faction Identity

Each faction should have:

- Unique units
- Unique buildings
- Unique technologies
- Unique heroes
- Unique bonuses
- Unique visual identity
- Unique military philosophy

The objective is asymmetric gameplay without making factions impossible to balance.

---

# 26. AI Architecture

Enemy AI should have multiple layers.

```text
Strategic AI
      ↓
Economic AI
      ↓
Military AI
      ↓
Tactical AI
      ↓
Unit AI
```

## Strategic AI

Decides:

- Expand
- Defend
- Attack
- Capture resource
- Siege
- Retreat

## Economic AI

Manages:

- Workers
- Resources
- Buildings
- Production
- Research

## Military AI

Manages:

- Army composition
- Recruitment
- Army movement

## Tactical AI

Manages:

- Flanking
- Focus fire
- Retreat
- Formation
- Target selection

## Unit AI

Handles:

- Movement
- Attacking
- Following
- Searching for targets
- Receiving commands

---

# 27. AI Personalities

Different commanders should behave differently.

Example:

## Aggressive

- Attacks early
- Takes risks
- Builds military quickly

## Defensive

- Builds walls
- Protects economy
- Counterattacks

## Expansionist

- Captures resources
- Builds multiple settlements

## Tactical

- Flanks
- Uses terrain
- Avoids unfavorable engagements

This creates replayability.

---

# 28. Selection and Command

Core RTS controls:

- Left-click selection
- Drag selection
- Right-click movement
- Attack command
- Patrol
- Hold position
- Stop
- Formation commands
- Ability commands

Keyboard shortcuts should eventually be configurable.

---

# 29. Camera

Initial camera:

- Pan
- Zoom
- Optional rotation
- Edge scrolling
- Keyboard movement

Later:

- Camera bookmarks
- Follow selected unit
- Follow hero
- Strategic overview

---

# 30. Game Architecture

Recommended architecture:

```text
Presentation Layer
        ↓
Game Application Layer
        ↓
Game Domain Layer
        ↓
Simulation Layer
        ↓
Data Layer
```

The game simulation should not depend directly on UI code.

This is critical for testing.

---

# 31. Simulation Architecture

The simulation should own:

- Units
- Buildings
- Resources
- Combat
- Movement
- AI
- Experience
- Level progression
- Events
- Victory conditions

UI should observe the simulation.

It should never become the source of truth.

---

# 32. Event-Driven Combat

Important events:

```text
UnitCreated
UnitMoved
UnitAttacked
DamageDealt
UnitKilled
ExperienceAwarded
UnitLevelUp
HeroAbilityUsed
BuildingDestroyed
BuildingCompleted
ResourceCollected
TechnologyResearched
```

Example:

```text
UnitKilled
     ↓
Combat System
     ↓
Experience System
     ↓
Award XP
     ↓
Check Level
     ↓
LevelUp Event
     ↓
UI Notification
     ↓
Visual Effect
```

This architecture makes the unit-leveling mechanic easy to extend and test.

---

# 33. Data-Driven Design

Unit definitions should live in data.

Example conceptual structure:

```text
UnitDefinition
├── id
├── name
├── faction
├── health
├── attack
├── defense
├── armor
├── speed
├── range
├── xpPerKill
├── levelCurve
├── abilities
└── progression
```

The game engine should consume this data.

This allows balancing without rewriting core systems.

---

# 34. Save System

The game should eventually support:

- Save
- Load
- Auto-save
- Campaign save
- Battle state
- Unit XP
- Unit kills
- Hero progression
- Civilization technology
- Resources
- World state

Every important state must be serializable.

---

# 35. Testing Philosophy

Testing is a first-class system.

The QA architecture should contain:

## Unit Tests

Test:

- Damage
- XP
- Level progression
- Resource calculations
- Movement
- Building costs
- Technology effects

## Integration Tests

Test:

```text
Combat
→ Kill
→ XP
→ Level Up
→ Stat Change
→ UI Event
```

## Simulation Tests

Run automated battles.

Example:

```text
100 vs 100 infantry
100 vs 100 mixed army
Siege battle
Cavalry attack
Hero battle
```

## UI Tests

Test:

- Selection
- Commands
- Building
- Resource display
- Production
- Save/load
- Menus

---

# 36. Automated Balance Testing

One of the most powerful future systems:

Run thousands of simulated battles.

Example:

```text
1000 simulations

Roman Infantry
vs
Celtic Infantry

Results:

Roman win rate: 51.2%
Celtic win rate: 48.8%
```

The balance agent can then identify suspicious units.

This should eventually become a dedicated development tool.

---

# 37. Performance Target

The game should be designed for large battles.

Initial target:

**100 active units**

Then:

**250 units**

Then:

**500 units**

Long-term stretch target:

**1000+ active entities**

Performance must be measured rather than assumed.

Important optimization areas:

- Pathfinding
- AI updates
- Rendering
- Collision
- Animation
- Projectile simulation
- Memory allocation
- Event processing

---

# 38. Recommended Technology Direction

For this particular project, the preferred direction is:

## Game Engine

**Godot 4**

Reasons:

- Open source
- Desktop friendly
- Excellent 2D capabilities
- Lightweight development workflow
- Strong scene system
- Good tooling
- Suitable for RTS prototypes
- No dependence on proprietary ecosystem

## Language

**C#**

Use C# for:

- Game simulation
- AI
- Combat
- Economy
- Data systems
- Save/load
- Automated testing

GDScript can be used selectively for engine/editor-specific scripting if necessary, but the core simulation should remain strongly structured.

---

# 39. Repository Architecture

Proposed structure:

```text
crown-and-conquest/
│
├── game/
│   ├── core/
│   ├── simulation/
│   ├── combat/
│   ├── economy/
│   ├── units/
│   ├── heroes/
│   ├── buildings/
│   ├── technology/
│   ├── ai/
│   ├── terrain/
│   ├── formations/
│   ├── siege/
│   ├── factions/
│   ├── campaign/
│   ├── save/
│   └── presentation/
│
├── data/
│   ├── units/
│   ├── buildings/
│   ├── technologies/
│   ├── heroes/
│   ├── factions/
│   └── balance/
│
├── tests/
│   ├── unit/
│   ├── integration/
│   ├── simulation/
│   └── ui/
│
├── tools/
│   ├── battle-simulator/
│   ├── balance-analyzer/
│   └── map-tools/
│
├── docs/
│
├── AGENTS.md
├── README.md
└── project configuration
```

---

# 40. AI Agent Team

The development team should mirror the successful multi-agent approach used for the Chess project.

## Game Director Agent

Owns:

- Overall architecture
- Game vision
- Feature consistency
- Technical decisions

## Game Systems Agent

Owns:

- Simulation
- Game state
- Events
- Core architecture

## Combat Agent

Owns:

- Combat
- Damage
- XP
- Unit progression
- Morale
- Formations

## Economy Agent

Owns:

- Resources
- Workers
- Buildings
- Production
- Technology

## AI Agent

Owns:

- Strategic AI
- Tactical AI
- Unit behavior
- AI personalities

## World Agent

Owns:

- Maps
- Terrain
- World systems
- Campaign

## UI Agent

Owns:

- RTS interface
- HUD
- Menus
- Selection panels
- Unit information

## Art/Presentation Agent

Owns:

- Visual effects
- Animations
- Presentation systems

## Audio Agent

Owns:

- Music
- SFX
- Battle audio
- UI audio

## QA Agent

Owns:

- Test strategy
- Regression
- Simulation tests
- Failure analysis

## Performance Agent

Owns:

- Profiling
- Large battles
- AI performance
- Memory
- Rendering

## Release Agent

Owns:

- Build
- Packaging
- CI/CD
- Versioning
- Release validation

---

# 41. Development Phases

## Phase 0 — Foundation

Create:

- Repository
- Game engine
- Architecture
- Agent rules
- Skills
- Coding standards
- Test framework
- CI pipeline

No gameplay yet.

---

## Phase 1 — RTS Prototype

Implement:

- Map
- Camera
- Unit creation
- Unit selection
- Movement
- Basic combat
- Basic health
- Death

Goal:

> Two groups of units can fight.

---

## Phase 2 — Economy

Implement:

- Resources
- Workers
- Gathering
- Buildings
- Construction
- Population
- Production

Goal:

> Player can build a small settlement and produce an army.

---

## Phase 3 — Unit Progression

Implement:

- XP
- Kill tracking
- Automatic level-up
- Veterancy
- Stat progression
- Veteran UI
- Unit history

Goal:

> A surviving warrior becomes increasingly valuable.

---

## Phase 4 — Civilization

Implement:

- Technologies
- Civilization progression
- Multiple buildings
- Multiple unit classes
- Faction bonuses

---

## Phase 5 — Heroes

Implement:

- Hero creation
- Hero XP
- Abilities
- Equipment
- Hero progression
- Hero/unit interaction

---

## Phase 6 — Advanced Combat

Implement:

- Formations
- Morale
- Terrain modifiers
- Cavalry
- Ranged combat
- Siege

---

## Phase 7 — Enemy AI

Implement:

- Worker AI
- Economy AI
- Military AI
- Tactical AI
- Commander personalities

---

## Phase 8 — Campaign

Implement:

- World map
- Strategic movement
- Missions
- Story
- Territory
- Faction relationships

---

## Phase 9 — Large-Scale Optimization

Target:

- 100 units
- 250 units
- 500 units
- 1000-unit stress test

Optimize only based on profiling data.

---

## Phase 10 — Polish

Implement:

- Art
- Animations
- VFX
- Audio
- UI polish
- Tutorials
- Accessibility
- Settings

---

## Phase 11 — QA & Release

Complete:

- Regression suite
- Automated simulation
- Performance testing
- Save/load testing
- Campaign testing
- CI/CD
- Desktop packaging
- Release candidate

---

# 42. MVP Definition

The first playable MVP should be intentionally small.

### One map

### Two factions

### One Town Center

### Five resources/building types

### Four unit types

- Worker
- Infantry
- Archer
- Cavalry

### One hero

### Basic technologies

### Basic AI opponent

### Basic XP system

### Unit kill leveling

### Basic victory condition

The player should be able to:

```text
Gather
 ↓
Build
 ↓
Recruit
 ↓
Explore
 ↓
Fight
 ↓
Gain XP
 ↓
Level up units
 ↓
Destroy enemy base
 ↓
Win
```

That is enough for the first real game.

---

# 43. Definition of Done

A feature is not complete merely because it works visually.

Every major feature requires:

```text
Implementation
+
Unit Tests
+
Integration Tests
+
Error Handling
+
Logging
+
Documentation
+
Performance Consideration
+
AI Interaction
+
Save/Load Consideration
```

Where applicable.

---

# 44. Non-Negotiable Architecture Rules

1. UI must never own game state.

2. Combat must not directly manipulate UI.

3. XP progression must be independent from rendering.

4. Unit definitions must be data-driven.

5. Balance values must not be scattered through code.

6. AI must interact through game systems rather than bypassing rules.

7. Save/load must serialize simulation state, not UI state.

8. Every major system must be independently testable.

9. New features must not silently bypass existing game rules.

10. Performance decisions must be backed by profiling.

---

# 45. Long-Term Vision

The final game should eventually support:

```text
                 WORLD
                   │
          ┌────────┴────────┐
          │                 │
      KINGDOM             WAR
          │                 │
      Economy            Armies
          │                 │
    Technologies         Heroes
          │                 │
          └────────┬────────┘
                   │
                BATTLE
                   │
          ┌────────┼────────┐
          │        │        │
        Units    Terrain   Morale
          │        │        │
          └────────┼────────┘
                   │
              EXPERIENCE
                   │
              VETERANS
                   │
             LEGENDARY
                UNITS
```

The ultimate identity of the game should be:

> **Build a civilization. Raise an army. Forge warriors through battle. Lead heroes. Shape the world.**

The player should eventually remember not just the battles they won, but **the individual soldiers who became legends during those battles.**

---

# 46. First Technical Milestone

Before implementing economy, heroes, campaign, or advanced AI:

Build a tiny battlefield containing:

```text
Player
 ├── 10 Swordsmen

Enemy
 ├── 10 Swordsmen
```

Then prove the complete progression loop:

```text
Select Units
     ↓
Move
     ↓
Attack
     ↓
Damage
     ↓
Kill
     ↓
Award XP
     ↓
Increase Kill Count
     ↓
Check XP Threshold
     ↓
Automatic Level Up
     ↓
Increase Stats
     ↓
Display Level
     ↓
Continue Fighting
```

If this loop is architecturally clean, the rest of the game has a strong foundation.

---

# 47. Design North Star

Whenever a future feature is proposed, ask:

### Does it improve one of these?

- Strategy
- Tactical decision-making
- Civilization development
- Unit progression
- Hero progression
- World interaction
- Replayability

If not, it should not automatically enter the game.

The goal is not to create the largest possible RTS.

The goal is to create an RTS where **every system interacts meaningfully with the others.**

---

# 48. Project Mantra

```text
Build the Kingdom.
Train the Army.
Forge the Veteran.
Raise the Hero.
Conquer the World.
```

**End of Master Plan v1.0**