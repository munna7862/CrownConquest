# Sprint 05: RPG Hero Layer — Task Breakdown

## Backlog Stories & Specialist Ownership

| Story ID | Description | Primary Owner | Status |
|:---|:---|:---|:---|
| **CNC-0501** | Hero Model & Class Archetypes (`HeroClass`, `HeroAttributes`, `HeroState`, `UnitArchetype.Hero`) | `hero` | Complete |
| **CNC-0502** | Hero XP Progression Curve & Squad Shared Kill XP Attribution | `combat` | Complete |
| **CNC-0503** | Hero Levels & Immediate Level-Up State Machine (`HeroLevelUpEvent`) | `ui` / `game-systems` | Complete |
| **CNC-0504** | RPG Core Attributes (STR, AGI, WIL) & Derived Stat Scaling Formulas | `hero` | Complete |
| **CNC-0505** | Hero Abilities Data Schema & Runtime State (`abilities.json`, `heroes.json`, `HeroAbilityDefinition`) | `combat` / `data` | Complete |
| **CNC-0506** | Simulation Cooldown Timers & Deterministic Fixed-Tick Mana Regeneration | `ui` / `game-systems` | Complete |
| **CNC-0507** | Leadership Capacity, Squad Attachment & Battlefield Leadership Auras | `hero` / `combat` | Complete |
| **CNC-0508** | Offensive & Support Ability Execution (`HeroicStrike`, `WarCry`, `EarthMend`) | `combat` | Complete |
| **CNC-0509** | Presentation: Hero UI Presenter, Ability Cards & Selection Synchronization | `ui` | Complete |
| **CNC-0510** | Persistence, State Checksum Integration & Headless Progression E2E Scenario | `hero` / `qa` | Complete |

---

## Detailed Subtasks

### 1. Pre-Implementation QA Catalog
- [x] Create `docs/testing/test_cases_catalog_S05.md` detailing 18 test cases across Tiers 1-4.

### 2. Data Definitions & Models
- [x] Create `data/definitions/abilities.json` (heroic_strike, war_cry, earth_mend, roots_of_fury, shield_bash, pilum_volley).
- [x] Create `data/definitions/heroes.json` (celtic_warlord, celtic_druid, roman_centurion).
- [x] Update `data/definitions/units.json` and `xp_curves.json` with Hero unit definitions and progression curves.
- [x] Create `AbilityDefinitionModel.cs` and `HeroDefinition.cs` in Data layer.
- [x] Update `DataLoader.cs` (`LoadHeroesFromJson`, `LoadAbilitiesFromJson`) and `UnitFactory.cs` (`CreateHeroUnit`).

### 3. Domain Models & Systems
- [x] Create `HeroClass` enum and display helpers.
- [x] Create `HeroAttributes` readonly struct with derived formulas (Health, Damage, Armor, Speed, Cooldown Reduction, Max Mana, Mana Regen, Potency).
- [x] Create `HeroAura`, `HeroAbilityDefinition`, `HeroAbilityState`, and `HeroState` domain entities.
- [x] Update `UnitArchetype` with `Hero` and `UnitEntity` with `HeroState`, `IsHero`, and attribute bonuses.
- [x] Update `CombatFormulas` with `CalculateHeroSpellDamage` and `CalculateCombatDamageWithAura`.

### 4. Commands & Events
- [x] Create `AttachToHeroCommand`, `DetachFromHeroCommand`, `CastHeroAbilityCommand`, `AllocateHeroAttributeCommand`.
- [x] Create `HeroLevelUpEvent`, `HeroAbilityCastEvent`, `HeroAttachedUnitsChangedEvent`, `HeroFallenEvent`, `HeroAttributeAllocatedEvent`.

### 5. Simulation Engine & State
- [x] Add hero command handlers in `SimulationEngine.ExecuteCommand`.
- [x] Implement fixed-tick mana regeneration and cooldown ticking (`UpdateHeroes`).
- [x] Implement leadership aura bonus query (`GetUnitAuraModifiers`).
- [x] Implement shared squad kill XP and level-up handling in `UpdateCombat`.
- [x] Include Hero attributes, mana, cooldowns, and attached squad in `SimulationState.ComputeStateChecksum`.

### 6. Presentation & Scenarios
- [x] Implement `HeroPresenter` providing hero HUD view models and ability card state.
- [x] Implement `HeroProgressionScenario` E2E headless scenario.

### 7. Automated Testing & Verification
- [x] Tier 1: Unit tests for Hero Attributes (`HeroAttributesMathTests.cs`), Abilities (`HeroAbilityMathTests.cs`), and Auras (`HeroAuraMathTests.cs`).
- [x] Tier 2: Simulation Invariant tests for mana conservation, cooldowns, capacity, aura disruption, and 1000-tick replay parity (`HeroInvariantTests.cs`).
- [x] Tier 3: Combat integration tests for squad attachment aura buffs, AoE offensive damage, AoE healing, and kill XP leveling (`HeroCombatIntegrationTests.cs`).
- [x] Tier 4: Headless E2E scenario test (`HeroProgressionScenarioTests.cs`) and Data loading tests (`HeroDataLoaderTests.cs`).
- [x] Verify `dotnet test` passes with 100% green tests (119/119 passed).
