# Sprint 01: Playable Combat Vertical Slice — Granular Task Breakdown

**Sprint Goal:** Deliver an authoritative, fully playable 10v10 combat vertical slice featuring RTS camera controls, unit selection & drag-box marquee, battlefield navigation, data-driven unit models, melee/ranged combat, armor damage mitigation, death handling, deterministic kill attribution, immediate individual unit level-up progression, veterancy rank visual badges, and a comprehensive headless test suite.

---

## 1. Story & Sub-Task Tracking Matrix

| Story ID | Story Title | Assigned Agent | Status | Story Points |
|:---|:---|:---|:---:|:---:|
| **CNC-0101** | Battlefield Implementation Slice | `game-systems` | DONE | 5 |
| **CNC-0102** | RTS Camera Controller Implementation Slice | `combat` / `ui` | DONE | 5 |
| **CNC-0103** | Unit Model & Presentation Slice | `art-presentation` / `ui` | DONE | 5 |
| **CNC-0104** | Data-Driven Spawning Implementation Slice | `game-systems` / `qa` | DONE | 5 |
| **CNC-0105** | Selection & Drag-Box Implementation Slice | `ui` / `game-systems` | DONE | 5 |
| **CNC-0106** | Movement & Formation Spacing Slice | `combat` | DONE | 5 |
| **CNC-0107** | Targeting & Engagement Range Slice | `combat` / `ui` | DONE | 5 |
| **CNC-0108** | Combat Damage & Armor Mitigation Slice | `combat` / `qa` | DONE | 4 |
| **CNC-0109** | Death Handling & Corpse Lifecycle Slice | `game-systems` | DONE | 4 |
| **CNC-0110** | Kill Attribution & Invariant Integrity Slice | `combat` | DONE | 4 |
| **CNC-0111** | XP Gain & Progression Data Slice | `combat` / `ui` | DONE | 4 |
| **CNC-0112** | Automatic Immediate Level-Up & Veterancy Slice | `combat` / `qa` | DONE | 4 |

---

## 2. Granular Task Decomposition

### CNC-0101: Battlefield Implementation Slice (`game-systems`)
- [x] Task 1.1: Implement `BattlefieldBounds` representing playable rectangular combat terrain and coordinate boundaries.
- [x] Task 1.2: Implement spatial partitioning query helper `SpatialGrid` for fast unit lookups in circular radii and rectangular selection boxes.
- [x] Task 1.3: Integrate battlefield bounds checks in `SimulationEngine` to prevent units from escaping playable bounds.

### CNC-0102: RTS Camera Controller Implementation Slice (`combat` / `ui`)
- [x] Task 2.1: Implement `RtsCameraController` with configurable pan speed, zoom levels, edge scrolling, and boundary clamping.
- [x] Task 2.2: Implement screen-to-world and world-to-screen coordinate projection math in presentation coordinate converter.
- [x] Task 2.3: Provide smooth keyboard (WASD / Arrows) and mouse drag camera movement.

### CNC-0103: Unit Model & Presentation Slice (`art-presentation` / `ui`)
- [x] Task 3.1: Implement `UnitPresentationData` and view components representing visual sprites, faction heraldry tints (Celtic Blue vs Roman Red).
- [x] Task 3.2: Implement floating `UnitHealthBar` and `VeterancyBadge` overlays displaying current HP, shield/armor, rank stars, and level indicator.
- [x] Task 3.3: Implement visual celebration feedback on level-up (`LevelUpCelebration`) and floating combat text for damage/kills.

### CNC-0104: Data-Driven Spawning Implementation Slice (`game-systems` / `qa`)
- [x] Task 4.1: Extend `UnitDefinition` with armor, combat type (Melee/Ranged), projectile speed, and XP curve reference.
- [x] Task 4.2: Implement `UnitFactory` to instantiate fully configured `UnitEntity` instances from loaded JSON definitions.
- [x] Task 4.3: Implement `SpawnUnitCommand` factory integration for batch deployment of 10v10 squads.

### CNC-0105: Selection & Drag-Box Implementation Slice (`ui` / `game-systems`)
- [x] Task 5.1: Implement `SelectionState` tracking active selected entities per player faction.
- [x] Task 5.2: Implement single-click point selection and marquee drag-box selection (`Rect2D` bounding box intersection).
- [x] Task 5.3: Implement `SelectUnitsCommand` and dispatch `UnitsSelectedEvent` and `SelectionClearedEvent`.

### CNC-0106: Movement & Formation Spacing Slice (`combat`)
- [x] Task 6.1: Implement multi-unit move order distribution with formation spacing (grid/line offsets) to prevent stacking.
- [x] Task 6.2: Implement arrival detection with stopping radius and smooth velocity dampening.
- [x] Task 6.3: Implement `IssueMoveOrder` application command in `GameCoordinator`.

### CNC-0107: Targeting & Engagement Range Slice (`combat` / `ui`)
- [x] Task 7.1: Implement contextual target acquisition: right-click enemy unit issues `AttackCommand`.
- [x] Task 7.2: Implement weapon range evaluation (Melee $1.5\text{m}$, Ranged $7.0\text{m}-8.0\text{m}$) and pursuit logic.
- [x] Task 7.3: Implement auto-acquire nearest hostile target when idle in combat stance.

### CNC-0108: Combat Damage & Armor Mitigation Slice (`combat` / `qa`)
- [x] Task 8.1: Implement standard damage mitigation formula: $\text{Damage} = \max(1, \text{RawDamage} - \text{Armor})$.
- [x] Task 8.2: Implement attack cooldown countdown with fixed tick precision.
- [x] Task 8.3: Implement ranged projectile delivery / delayed hit resolution.

### CNC-0109: Death Handling & Corpse Lifecycle Slice (`game-systems`)
- [x] Task 9.1: Implement zero-health transition to `UnitState.Dead` with `UnitKilledEvent`.
- [x] Task 9.2: Cancel active movement targets and clear targeting references across all living units targeting the deceased.
- [x] Task 9.3: Cleanup dead units from active spatial partition at tick boundary.

### CNC-0110: Kill Attribution & Invariant Integrity Slice (`combat`)
- [x] Task 10.1: Ensure exactly one valid, living killer entity receives kill credit per casualty.
- [x] Task 10.2: Guard against awarding XP if killer is already dead or belongs to the same faction (friendly fire).
- [x] Task 10.3: Increment lifetime kill counter on killer's `VeterancyState`.

### CNC-0111: XP Gain & Progression Data Slice (`combat` / `ui`)
- [x] Task 11.1: Calculate Kill XP based on target's configured `KillXpValue` and level scaling curve.
- [x] Task 11.2: Publish typed `UnitGainedXpEvent` with old XP, new XP, and current level threshold.
- [x] Task 11.3: Update selection HUD panel with XP progress bar and lifetime kill stats.

### CNC-0112: Automatic Immediate Level-Up & Veterancy Slice (`combat` / `qa`)
- [x] Task 12.1: Evaluate level thresholds immediately on the kill tick (supporting multi-level rollover).
- [x] Task 12.2: Apply permanent stat gains (+MaxHealth, +AttackDamage, +Armor) from data-driven XP curves.
- [x] Task 12.3: Evaluate and advance `VeterancyRank` (Recruit $\to$ Experienced $\to$ Veteran $\to$ Elite $\to$ Legendary) emitting `VeterancyRankChangedEvent`.
- [x] Task 12.4: Build 10v10 combat arena scenario demonstration (`CombatArenaPresenter` & demo runner).
- [x] Task 12.5: Author and pass all 39 automated test cases in `CrownConquest.Tests`.
