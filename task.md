# Sprint 04: Eras & Technology — Task Breakdown

## Backlog Stories & Specialist Ownership

| Story ID | Description | Primary Owner | Status |
|:---|:---|:---|:---|
| **CNC-0401** | Era Model & Progression State Machine (Archaic -> Classical -> Imperial -> Feudal) | `economy` / `game-systems` | Complete |
| **CNC-0402** | Era Advancement Prerequisites, Cost Deduction & Town Center Queuing | `economy` / `game-systems` | Complete |
| **CNC-0403** | Technology Definitions, Prerequisites & Data Schemas (`technologies.json`, `eras.json`) | `economy` / `data` | Complete |
| **CNC-0404** | Deterministic Research Queue & Faction Tech Modifier Manager | `game-systems` / `economy` | Complete |
| **CNC-0405** | Blacksmith Building & Weapon/Armor Upgrade Tech Tree | `economy` / `combat` | Complete |
| **CNC-0406** | Archery Range Building & Ballistic Ranged Training Slice | `combat` / `economy` | Complete |
| **CNC-0407** | Stable Building & Mounted Cavalry Training Slice | `economy` / `combat` | Complete |
| **CNC-0408** | Archer Unit Archetype & Range/Damage Tech Scaling | `combat` | Complete |
| **CNC-0409** | Spearman Unit Archetype & Anti-Cavalry Combat Multipliers | `combat` | Complete |
| **CNC-0410** | Cavalry Unit Archetype, High Mobility & Charge Mechanics | `combat` | Complete |
| **CNC-0411** | Presentation: Era HUD Banner, Research Cards & Tech Dependency Visuals | `ui` / `art-presentation` | Complete |
| **CNC-0412** | Headless Civilization Progression E2E Scenario & Quality Gate Verification | `qa` | Complete |

---

## Detailed Subtasks

### 1. Pre-Implementation QA Catalog
- [x] Create `docs/testing/test_cases_catalog_S04.md` detailing 18+ test cases across Tiers 1-4.

### 2. Domain Models & Systems
- [x] Create `CivilizationEra` enum and `EraState` domain class.
- [x] Create `TechnologyDefinition`, `TechCategory`, `TechModifiers` in Domain/Data.
- [x] Create `FactionTechManager` / `TechTree` tracking researched technologies and cumulative stat multipliers per faction.
- [x] Create `ResearchQueue` and `ResearchQueueItem` for building tech research.
- [x] Update `UnitArchetype` / `UnitClass` (`Infantry`, `Spearman`, `Archer`, `Cavalry`, `Worker`, `Siege`).
- [x] Update `CombatFormulas.CalculateEffectiveDamage` with archetype multipliers (Spearman vs Cavalry) and faction tech modifiers.

### 3. Simulation & Commands
- [x] Implement `StartResearchCommand`, `CancelResearchCommand`, `AdvanceEraCommand`.
- [x] Add Era advancement handling in `SimulationEngine` (cost checks, prerequisite building checks, tick progression, completion events).
- [x] Add Technology research handling in `SimulationEngine` (Blacksmith, Town Center, etc.).
- [x] Add building definitions in `buildings.json` (`blacksmith`, `archery_range`, `stable`).
- [x] Add unit definitions in `units.json` (`celtic_spearman`, `celtic_archer`, `celtic_scout_cavalry`, `celtic_heavy_cavalry`, `roman_spearman`, `roman_equite`).
- [x] Add `eras.json` and `technologies.json`.
- [x] Update `DataLoader.cs` and `UnitFactory.cs`.

### 4. UI & Presentation
- [x] Implement `CivilizationProgressionPresenter` and `CivilizationProgressionScenario`.
- [x] Implement Era HUD banner data, Research command card state, and Tech dependency queries.

### 5. Automated Testing & Verification
- [x] Tier 1: Unit tests for Era prerequisites, Technology modifiers, Damage calculations with bonuses.
- [x] Tier 2: Simulation invariant tests for research queue progression, refund on cancel, deterministic replay.
- [x] Tier 3: Multi-building research & production integration tests.
- [x] Tier 4: Headless E2E scenario test (`CivilizationProgressionScenarioTests`).
- [x] Verify `dotnet test` passes with 100% green tests (101/101 passed).

### 6. Documentation & PR
- [x] Update `walkthrough.md`.
- [x] Commit, push branch, and create Pull Request.
