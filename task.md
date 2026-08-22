# Sprint 03: Economy Depth — Granular Task Breakdown

**Sprint Goal:** Deliver an authoritative, deterministic economy depth simulation slice featuring renewable Farms (harvesting & re-seeding cycle), specialized drop-off camps (Lumber Camp, Mining Camp, Granary/Mill, Stone Quarry), building repair mechanics (worker repair order, HP restoration, proportional resource cost), worker auto-reassignment/task switching, and economic throughput balancing across all 5 resources.

---

## 1. Story & Sub-Task Tracking Matrix

| Story ID | Story Title | Assigned Agent | Status | Story Points |
|:---|:---|:---|:---:|:---:|
| **CNC-0301** | Food Implementation Slice (Farms & Granary) | `economy` / `game-systems` | DONE | 5 |
| **CNC-0302** | Wood Implementation Slice (Lumber Camp) | `ui` / `economy` | DONE | 5 |
| **CNC-0303** | Stone Implementation Slice (Stone Quarry Camp) | `economy` | DONE | 5 |
| **CNC-0304** | Iron Implementation Slice (Mining Camp Iron Extraction) | `ui` / `economy` | DONE | 5 |
| **CNC-0305** | Gold Implementation Slice (Mining Camp Gold Extraction) | `economy` | DONE | 5 |
| **CNC-0306** | Renewable Farm Entity & Reseeding Slice | `economy` / `game-systems` | DONE | 4 |
| **CNC-0307** | Specialized Drop-off Camps Slice | `economy` / `game-systems` | DONE | 4 |
| **CNC-0308** | Stone Quarry Camp & Fortification Repair Costs Slice | `ui` / `economy` | DONE | 4 |
| **CNC-0309** | Mining Camp Placement & Dual-Resource Drop-off Slice | `economy` | DONE | 4 |
| **CNC-0310** | Worker Reassignment, Repair & Idle Query Slice | `ui` / `game-systems` | DONE | 4 |
| **CNC-0311** | Economy Depth Invariant & Fuzzing Suite | `qa` | DONE | 4 |
| **CNC-0312** | Multi-Cluster Economy Depth E2E Scenario & Presenter | `art-presentation` / `qa` | DONE | 4 |

---

## 2. Granular Task Decomposition

### CNC-0301: Food Implementation Slice (`economy` / `game-systems`)
- [x] Task 1.1: Implement Food gathering from renewable Farms and Berry Bushes.
- [x] Task 1.2: Implement Granary / Mill building accepting only Food drop-offs.
- [x] Task 1.3: Ensure workers deposit Food into nearest Granary or Town Center.

### CNC-0302: Wood Implementation Slice (`ui` / `economy`)
- [x] Task 2.1: Implement Lumber Camp building accepting only Wood drop-offs.
- [x] Task 2.2: Ensure workers gathering Wood route to nearest Lumber Camp or Town Center.
- [x] Task 2.3: Add Lumber Camp HUD placement preview and command card.

### CNC-0303: Stone Implementation Slice (`economy`)
- [x] Task 3.1: Implement Stone Quarry Camp building accepting only Stone drop-offs.
- [x] Task 3.2: Configure stone extraction rate and stone storage accounting.

### CNC-0304: Iron Implementation Slice (`ui` / `economy`)
- [x] Task 4.1: Support Iron node harvesting and drop-off routing to Mining Camp or Town Center.
- [x] Task 4.2: Update resource stockpile HUD to highlight Iron reserves for heavy military production.

### CNC-0305: Gold Implementation Slice (`economy`)
- [x] Task 5.1: Support Gold node harvesting and drop-off routing to Mining Camp or Town Center.
- [x] Task 5.2: Configure Gold gathering rates and trade economy balance.

### CNC-0306: Renewable Farm Entity & Reseeding Slice (`economy` / `game-systems`)
- [x] Task 6.1: Add Farm state to `BuildingEntity` (Food capacity: 250, reseed cost: 60 Wood).
- [x] Task 6.2: Implement worker farm harvesting loop and auto-reseeding when depleted if Wood is available in `ResourceBank`.
- [x] Task 6.3: Implement `ReseedFarmCommand` and domain events: `FarmReseededEvent`, `FarmDepletedEvent`.

### CNC-0307: Specialized Drop-off Camps Slice (`economy` / `game-systems`)
- [x] Task 7.1: Add `LumberCamp`, `MiningCamp`, `StoneQuarryCamp`, `Granary`/`Mill` to `GetBuildingConfig` and `data/definitions/buildings.json`.
- [x] Task 7.2: Implement resource filtering per camp in `AcceptsDropOff(ResourceType)`.
- [x] Task 7.3: Implement nearest drop-off resolver optimizing worker pathing to specialized camps.

### CNC-0308: Stone Quarry Camp & Fortification Repair Costs Slice (`ui` / `economy`)
- [x] Task 8.1: Define Stone repair costs for stone buildings/fortifications.
- [x] Task 8.2: Implement proportional Stone deduction during repair.

### CNC-0309: Mining Camp Placement & Dual-Resource Drop-off Slice (`economy`)
- [x] Task 9.1: Configure Mining Camp to accept both Gold and Iron.
- [x] Task 9.2: Test dual-resource workers utilizing the same Mining Camp.

### CNC-0310: Worker Reassignment, Repair & Idle Query Slice (`ui` / `game-systems`)
- [x] Task 10.1: Implement `RepairBuildingCommand` and worker repair state machine (`MovingToRepair`, `Repairing`).
- [x] Task 10.2: Implement proportional resource deduction (Wood/Stone) per tick of repair.
- [x] Task 10.3: Implement `SelectIdleWorkersCommand` / `GetIdleWorkers(FactionId)` querying idle villagers.
- [x] Task 10.4: Implement instant task switching retaining carried inventory.

### CNC-0311: Economy Depth Invariant & Fuzzing Suite (`qa`)
- [x] Task 11.1: Author Tier 1 unit tests: `FarmMathTests.cs`, `BuildingRepairMathTests.cs`, `SpecializedCampTests.cs`.
- [x] Task 11.2: Author Tier 2 invariant tests: `EconomyDepthInvariantTests.cs` (conservation during repair and reseeding, zero leak).
- [x] Task 11.3: Author Tier 3 integration tests: `EconomyDepthIntegrationTests.cs` (multi-camp routing, farm reseed, repair cycle).

### CNC-0312: Multi-Cluster Economy Depth E2E Scenario & Presenter (`art-presentation` / `qa`)
- [x] Task 12.1: Build `EconomyDepthScenario` with 4 gathering clusters (Forest, Gold/Iron Mine, Farmstead, Stone Quarry) and damaged building repair.
- [x] Task 12.2: Implement `EconomyDepthPresenter` tracking worker distribution and resource throughput.
- [x] Task 12.3: Author Tier 4 Headless E2E test `EconomyDepthScenarioTests` validating complete multi-resource economy in under 1,000 ticks.
