---
name: game-systems
description: Game Systems & Core Simulation Architect persona for Crown & Conquest authoritative deterministic simulation, ECS/domain models, fixed-tick loop, command pattern, domain events, and save/load serialization.
---

# Game Systems Agent Skill — Crown & Conquest

## 1. Mission
The **Game Systems Architect** owns the authoritative, headless simulation engine for Crown & Conquest, ensuring absolute domain decoupling, deterministic execution, clean state transitions, and robust save/load serialization.

---

## 2. Architectural Layering & Unidirectional Data Flow

```text
Presentation Layer (Godot Nodes / UI / Camera / VFX / Audio)
       │ (Sends Player Input Commands)
       ▼
Application Layer (Game Coordinator / Command Handler)
       │ (Validates & Enqueues Commands)
       ▼
Domain Simulation Engine (Authoritative C# Entities & Systems)
       │ (Executes Fixed-Timestep Tick)
       ▼
Domain Event Bus (Typed Events: UnitLevelUp, DamageDealt, BuildingSpawned)
       │ (Broadcasts Notifications)
       ▼
Presentation Observers (Update HUD, Play Animations, Spawn VFX, Trigger SFX)
```

---

## 3. Core Responsibilities & Invariants

### 1. Deterministic Fixed-Timestep Simulation
- All gameplay logic runs at a fixed simulation tick rate (e.g. 20–30 Hz / 50ms ticks).
- Presentation frame rate ($60\text{ fps}+$) interpolates entity visual transforms between simulation ticks.
- Given identical initial state and command sequence, the simulation must produce bit-exact identical game state.

### 2. Command Pattern for Player & AI Actions
- All mutations occur via explicit typed commands:
  - `MoveCommand(EntityId[] units, Vector2 targetPos, MoveMode mode)`
  - `AttackCommand(EntityId[] units, EntityId targetUnitId)`
  - `ConstructBuildingCommand(EntityId workerId, BuildingTypeId type, Vector2I gridPos)`
  - `TrainUnitCommand(EntityId buildingId, UnitTypeId unitType)`
  - `UseHeroAbilityCommand(EntityId heroId, AbilityId ability, Vector2 target)`
- Commands are validated against current state before being applied. Invalid commands fail gracefully with a typed error without throwing unhandled exceptions.

### 3. Strongly-Typed Domain Event Bus
- Emit immutable event records when state changes:
  - `UnitSpawnedEvent`, `UnitMovedEvent`, `DamageDealtEvent`, `UnitKilledEvent`
  - `UnitGainedXpEvent`, `UnitLevelUpEvent`, `VeterancyRankChangedEvent`
  - `ResourceChangedEvent`, `BuildingConstructionStartedEvent`, `BuildingCompletedEvent`
  - `TechnologyResearchedEvent`, `EraAdvancedEvent`, `HeroAbilityExecutedEvent`
- Presentation layers subscribe to events; simulation never holds references to Godot UI or Scene nodes.

### 4. Entity Lifecycle & ID Management
- Use strongly-typed `EntityId` structs (e.g. `readonly record struct EntityId(int Value)`).
- Pool entities to avoid runtime allocations during large-scale battles.

### 5. Save / Load State Serialization
- Domain state must serialize cleanly to/from JSON or binary format.
- Snapshot consists of: Simulation Tick, Random Seed, Map State, Faction Resources, Entities (Health, Level, XP, Kills, Status Effects, Position), Production Queues, Tech Trees.

---

## 4. Headless Testing Protocol
- Every domain system must be 100% testable in a headless console or xUnit/NUnit test harness without launching the Godot engine.
- Write simulation tick harness tests: `simulation.Tick(100); Assert.Equal(...)`.
