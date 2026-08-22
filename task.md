# Sprint 02: Economy Core — Granular Task Breakdown

**Sprint Goal:** Deliver an authoritative, deterministic economy simulation slice featuring 5 core resources (Food, Wood, Gold, Stone, Iron), harvestable resource nodes (Trees, Gold Mines, Berry Bushes, Stone Quarries, Iron Deposits), worker gathering state machine (MoveToResource -> Harvest -> CarryInventory -> ReturnToDropOff -> Deposit), building placement & construction grid, Town Center (drop-off & villager production), Houses (population cap expansion), Barracks (swordsman training queue), population enforcement, and a playable fresh settlement demonstration scenario with headless tests.

---

## 1. Story & Sub-Task Tracking Matrix

| Story ID | Story Title | Assigned Agent | Status | Story Points |
|:---|:---|:---|:---:|:---:|
| **CNC-0201** | Five-Resource Model & Stockpile Slice | `economy` / `game-systems` | DONE | 5 |
| **CNC-0202** | Harvestable Resource Node Entities Slice | `economy` | DONE | 5 |
| **CNC-0203** | Worker Gathering State Machine Slice | `economy` | DONE | 5 |
| **CNC-0204** | Resource Drop-off & Storage Buildings Slice | `economy` / `game-systems` | DONE | 5 |
| **CNC-0205** | Town Center & Villager Training Slice | `economy` / `game-systems` | DONE | 5 |
| **CNC-0206** | Barracks & Swordsman Training Queue Slice | `economy` / `game-systems` | DONE | 5 |
| **CNC-0207** | Building Placement Grid & Construction Progress Slice | `economy` / `ui` | DONE | 5 |
| **CNC-0208** | Population Tracking & Housing Cap Slice | `economy` | DONE | 4 |
| **CNC-0209** | Production Queue Command Cards & UI Slice | `ui` / `art-presentation` | DONE | 4 |
| **CNC-0210** | Resource Stockpile HUD & Placement Preview Slice | `ui` / `art-presentation` | DONE | 4 |
| **CNC-0211** | Economy Conservation Invariant & Fuzzing Suite | `qa` | DONE | 4 |
| **CNC-0212** | Fresh Settlement E2E Scenario & Presenter Demonstration | `art-presentation` / `qa` | DONE | 4 |

---

## 2. Granular Task Decomposition

### CNC-0201: Five-Resource Model & Stockpile Slice (`economy` / `game-systems`)
- [x] Task 1.1: Define `ResourceType` enum (`Food`, `Wood`, `Gold`, `Stone`, `Iron`).
- [x] Task 1.2: Implement `ResourceBank` supporting deposit, deduction, affordance checks, and starting stockpile configurations.
- [x] Task 1.3: Define domain events: `ResourceHarvestedEvent`, `ResourceDepositedEvent`, `ResourceSpentEvent`.
- [x] Task 1.4: Implement `ResourceCost` struct supporting multi-resource requirements and validation.

### CNC-0202: Harvestable Resource Node Entities Slice (`economy`)
- [x] Task 2.1: Implement `ResourceNodeEntity` with `ResourceType`, max capacity, current remaining resources, position, harvest radius, and depletion state.
- [x] Task 2.2: Implement `ResourceNodeDepletedEvent` and automatic removal / depletion marking in spatial grid.
- [x] Task 2.3: Data definitions for resource nodes in `data/definitions/resources.json`.

### CNC-0203: Worker Gathering State Machine Slice (`economy`)
- [x] Task 3.1: Extend `UnitEntity` / create worker capabilities with carried resource type, carried amount, and carry capacity (default 10).
- [x] Task 3.2: Implement worker states: `GatherMoving`, `Harvesting`, `ReturningToStorage`, `Depositing`, `Constructing`.
- [x] Task 3.3: Implement gathering loop: reach node -> tick harvest progress -> fill capacity -> find nearest drop-off -> move to drop-off -> deposit into `ResourceBank` -> return to previous node (or nearest same-type node if depleted).
- [x] Task 3.4: Implement `GatherCommand` handling for single and multi-worker selection orders.

### CNC-0204: Resource Drop-off & Storage Buildings Slice (`economy` / `game-systems`)
- [x] Task 4.1: Implement `BuildingEntity` with building type, faction, position, grid dimensions, health, construction state, and accepted drop-off types.
- [x] Task 4.2: Implement nearest drop-off finder evaluating valid, completed drop-off buildings matching the carried resource type.
- [x] Task 4.3: Implement `StoragePit` and `TownCenter` drop-off compatibility (Town Center accepts Food/Wood/Gold/Stone/Iron; Storage Pit accepts Wood/Stone/Iron/Gold).

### CNC-0205: Town Center & Villager Training Slice (`economy` / `game-systems`)
- [x] Task 5.1: Implement `ProductionQueue` supporting sequential unit training with tick-based training duration.
- [x] Task 5.2: Configure Town Center definition with starting stats, villager production cost (e.g. 50 Food), and spawn offset.
- [x] Task 5.3: Implement `QueueProductionCommand` and training loop in `SimulationEngine` emitting `ProductionStartedEvent` and `ProductionCompletedEvent`.

### CNC-0206: Barracks & Swordsman Training Queue Slice (`economy` / `game-systems`)
- [x] Task 6.1: Configure Barracks definition in `data/definitions/buildings.json` with cost (e.g. 150 Wood), construction time, and swordsman training capabilities.
- [x] Task 6.2: Implement swordsman training queue in Barracks (e.g. 60 Food, 20 Iron or Wood) with population cap validation.
- [x] Task 6.3: Spawn trained military unit at Barracks rally point upon completion.

### CNC-0207: Building Placement Grid & Construction Progress Slice (`economy` / `ui`)
- [x] Task 7.1: Implement `PlacementGrid` / collision validation ensuring buildings do not overlap existing structures or out-of-bounds terrain.
- [x] Task 7.2: Implement `PlaceBuildingCommand` creating unbuilt blueprint `BuildingEntity` with $0\%$ progress.
- [x] Task 7.3: Implement `ConstructBuildingCommand` directing workers to build; worker adds build progress per tick; building finishes at $100\%$ progress emitting `BuildingCompletedEvent`.

### CNC-0208: Population Tracking & Housing Cap Slice (`economy`)
- [x] Task 8.1: Implement `PopulationManager` tracking current unit count and max population capacity per faction.
- [x] Task 8.2: Implement `House` building definition (+5 Pop Cap) and Town Center (+10 Pop Cap).
- [x] Task 8.3: Enforce strict population cap validation on unit training; emit `PopulationCapacityChangedEvent`.

### CNC-0209: Production Queue Command Cards & UI Slice (`ui` / `art-presentation`)
- [x] Task 9.1: Implement `ProductionQueuePresenter` exposing active training progress, queue slots, and unit cancellation.
- [x] Task 9.2: Implement command cards for building selection (Train Villager, Train Swordsman, Rally Point).

### CNC-0210: Resource Stockpile HUD & Placement Preview Slice (`ui` / `art-presentation`)
- [x] Task 10.1: Implement `ResourceBarHudPresenter` rendering Food, Wood, Gold, Stone, Iron balances and Population cap (Current/Max).
- [x] Task 10.2: Implement `BuildingPlacementPreview` with grid snapping and valid/invalid footprint preview.

### CNC-0211: Economy Conservation Invariant & Fuzzing Suite (`qa`)
- [x] Task 11.1: Author Tier 1 unit tests for `ResourceBank`, `ResourceCost`, `PlacementGrid`, and `ProductionQueue`.
- [x] Task 11.2: Author Tier 2 invariant tests: Conservation of resources (Harvested == Deposited + Carried), Zero resource leak on worker redirection/death, Strict pop cap invariant.
- [x] Task 11.3: Author Tier 3 integration tests for full worker gathering cycle, multi-worker building construction, and production queue training.

### CNC-0212: Fresh Settlement E2E Scenario & Presenter Demonstration (`art-presentation` / `qa`)
- [x] Task 12.1: Build `SettlementEconomyScenario` bootstrapping a fresh settlement (Town Center + 3 Villagers + nearby Forest, Berry Bush, Gold Mine).
- [x] Task 12.2: Implement playable/demonstrable flow: Villagers gather 150 Wood & Food -> Place & build Barracks -> Train Swordsman.
- [x] Task 12.3: Author Tier 4 Headless E2E test `SettlementEconomyScenarioTests` validating entire economic progression to military production in under 1,000 ticks.
