# Sprint 00: Engineering Foundation — Granular Task Breakdown

**Sprint Goal:** Establish core Godot 4 + C# 12 / .NET 8 architecture, decoupled deterministic domain simulation, command queue, zero-allocation typed domain event bus, structured logging, externalized data schemas, headless QA test harness, and CI/CD automation pipeline.

---

## 1. Story & Sub-Task Tracking Matrix

| Story ID | Story Title | Assigned Agent | Status | Story Points |
|:---|:---|:---|:---:|:---:|
| **CNC-0001** | Build Godot 4 + C# 12 / .NET 8 Project Solution | `game-systems` | DONE | 5 |
| **CNC-0002** | Decoupled Repository Architecture & Domain Base Types | `game-systems` | DONE | 5 |
| **CNC-0003** | Deterministic Fixed-Timestep Simulation Runner & Command Queue | `game-systems` | DONE | 4 |
| **CNC-0004** | Zero-Allocation Typed Domain Event Bus | `game-systems` / `performance` | DONE | 4 |
| **CNC-0005** | Structured Logging & Diagnostic Infrastructure | `game-systems` | DONE | 4 |
| **CNC-0006** | Externalized Data Conventions & JSON Schemas | `economy` / `combat` | DONE | 4 |
| **CNC-0007** | Headless QA Test Framework & Invariant Catalog | `qa` | DONE | 4 |
| **CNC-0008** | GitHub Actions CI/CD & Build Validation Workflow | `release` | DONE | 4 |

---

## 2. Granular Task Decomposition

### CNC-0001: Godot 4 + C# 12 / .NET 8 Project Solution
- [x] Task 1.1: Create root solution `CrownConquest.sln`.
- [x] Task 1.2: Configure Godot project file `project.godot` with C#/.NET compatibility.
- [x] Task 1.3: Set up `.gitignore` and `.editorconfig` for C# 12, Godot 4, and .NET 8 conventions.
- [x] Task 1.4: Establish project assemblies:
  - `src/CrownConquest.Domain` (Pure C# domain simulation, 0 Godot dependencies)
  - `src/CrownConquest.Application` (Game coordinator & command router)
  - `src/CrownConquest.Data` (Externalized JSON models & loaders)
  - `src/CrownConquest.Presentation` (Godot 4 scenes & presentation observers)
  - `tests/CrownConquest.Tests` (xUnit test suite & headless harness)

### CNC-0002: Decoupled Repository Architecture & Domain Base Types
- [x] Task 2.1: Implement strongly typed identifiers (`EntityId`, `FactionId`).
- [x] Task 2.2: Implement 2D simulation math structures (`Vector2D`, `FixedPoint`).
- [x] Task 2.3: Implement robust error handling Result monad (`Result<T, GameError>`).
- [x] Task 2.4: Implement core domain entity representations (`UnitEntity`, `BuildingEntity`, `VeterancyState`).

### CNC-0003: Deterministic Fixed-Timestep Simulation Runner & Command Queue
- [x] Task 3.1: Implement fixed-timestep simulation tick loop (`SimulationEngine`, `TickRate = 20Hz`).
- [x] Task 3.2: Implement deterministic seeded PRNG (`SimulationRandom`).
- [x] Task 3.3: Implement immutable command pattern interface (`ICommand`) and queue (`CommandQueue`).
- [x] Task 3.4: Implement standard unit commands (`MoveCommand`, `AttackCommand`, `SpawnUnitCommand`).

### CNC-0004: Zero-Allocation Typed Domain Event Bus
- [x] Task 4.1: Implement high-performance typed event bus (`DomainEventBus`, `IDomainEvent`).
- [x] Task 4.2: Implement combat and progression domain events (`UnitSpawnedEvent`, `DamageDealtEvent`, `UnitKilledEvent`, `UnitGainedXpEvent`, `UnitLevelUpEvent`, `VeterancyRankChangedEvent`).
- [x] Task 4.3: Validate zero-allocation publishing and deterministic event ordering.

### CNC-0005: Structured Logging & Diagnostic Infrastructure
- [x] Task 5.1: Implement lightweight non-blocking simulation logger (`SimLogger`, `ILogSink`).
- [x] Task 5.2: Add sink support for Console, File, and in-memory test capture.

### CNC-0006: Externalized Data Conventions & JSON Schemas
- [x] Task 6.1: Define C# data records for units, combat stats, and XP progression curves.
- [x] Task 6.2: Create data definitions under `data/definitions/units.json` and `data/definitions/xp_curves.json`.
- [x] Task 6.3: Implement typed JSON loader (`DataLoader`) with schema validation.

### CNC-0007: Headless QA Test Framework & Invariant Catalog
- [x] Task 7.1: Author Pre-Implementation QA Catalog (`docs/testing/test_cases_catalog_S00.md`).
- [x] Task 7.2: Implement headless simulation test harness (`TestSimulationHarness`).
- [x] Task 7.3: Implement Tier 1 Unit tests for math, IDs, results, and events.
- [x] Task 7.4: Implement Tier 2 Deterministic Invariant and Fuzz tests.
- [x] Task 7.5: Implement Tier 3 System Integration tests for command execution and XP progression.

### CNC-0008: GitHub Actions CI/CD & Build Validation Workflow
- [x] Task 8.1: Create `.github/workflows/ci.yml` running `dotnet build` with warnings as errors.
- [x] Task 8.2: Configure automated test runner step with test summary reporting.
- [x] Task 8.3: Validate clean build and 100% test pass locally.
