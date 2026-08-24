# Pull Request: Sprint 06 - Tactical Combat (Terrain, Formations, Morale & Routing, Elevation & Cavalry Charge)

## Summary of Changes
Sprint 06 implements the complete Tactical Combat simulation layer for **Crown & Conquest**, introducing:
1. **Data-Driven Definitions & Loaders:**
   - `data/definitions/terrain.json`: Plains, Forest, Hills, Marsh, Road, Water.
   - `data/definitions/formations.json`: Line, Shield Wall, Wedge, Square, Loose, Column.
   - `DataLoader.LoadTerrainFromFile` / `DataLoader.LoadFormationsFromFile` with error-handling via `Result<T>`.
2. **Domain Mathematical Formulas & Models:**
   - `TerrainType` enum, `TerrainModifiers` readonly struct, and `TerrainGrid` flat-array 2D spatial map ($O(1)$ grid indexing).
   - `FormationType` enum, `FormationModifiers` readonly struct, and `FormationCalculator` with geometric slot offsets and heading rotation.
   - `MoraleState` and `MoraleLevel` state machine (Confident, Steady, Wavering, Breaking, Routed) with shock application, passive recovery, and hero aura restoration.
   - `ChargeState` with momentum progression, 1.4x speed scaling, +100% impact damage, and spear bracing recoil damage.
   - `CombatFormulas.CalculateTacticalCombatDamage` with elevation bonuses (+2 range, +25% downhill damage, -15% uphill damage), forest ranged cover mitigation (35%), and flanking detection.
3. **Commands & Events:**
   - Commands: `SetFormationCommand`, `SetSquadFormationCommand`, `RallyUnitCommand`, `RallySquadCommand`.
   - Domain Events (`readonly record struct`): `UnitFormationChangedEvent`, `UnitMoraleChangedEvent`, `UnitRoutedEvent`, `UnitRalliedEvent`, `CavalryChargeImpactEvent`.
4. **Authoritative Simulation Loop:**
   - Integrated `UpdateMorale` and terrain speed tracking into `SimulationEngine.cs`.
   - Included tactical combat components into 64-bit state checksum calculation in `SimulationState.cs`.
5. **Presentation Layer:**
   - `TacticalCombatPresenter.cs`: Real-time HUD view models for active formations, morale levels, terrain overlays, and charge gauges.
   - `TacticalCombatScenario.cs`: Headless tactical demonstration match.

---

## Sprint 06 Test Verification (Tiers 1–4)

| Test Identifier | Category | Description | Status |
|:---|:---|:---|:---:|
| `TC-S06-01` | Tier 1 Unit Test | Terrain Modifiers Math & Specification Verification | **PASS** |
| `TC-S06-02` | Tier 1 Unit Test | Elevation Range & Damage Math (+2.0 range, +25% downhill, -15% uphill) | **PASS** |
| `TC-S06-03` | Tier 1 Unit Test | Forest Cover Ranged Mitigation Math (35% cover mitigation) | **PASS** |
| `TC-S06-04` | Tier 1 Unit Test | Formation Offset Slot Calculations (Line, Shield Wall, Wedge, Square, Column) | **PASS** |
| `TC-S06-05` | Tier 1 Unit Test | Formation Combat Modifiers Verification | **PASS** |
| `TC-S06-06` | Tier 1 Unit Test | Morale Threshold Evaluation (Confident, Steady, Wavering, Breaking, Routed) | **PASS** |
| `TC-S06-07` | Tier 1 Unit Test | Morale Shock Application & Rally Recovery Math | **PASS** |
| `TC-S06-08` | Tier 1 Unit Test | Cavalry Charge Momentum & Impact Damage Formula | **PASS** |
| `TC-S06-09` | Tier 1 Unit Test | Spear Bracing Counter & Recoil Damage Formula | **PASS** |
| `TC-S06-10` | Tier 2 Invariant Test | Routing Invariant (Drops orders, flees to safe camp, cannot attack) | **PASS** |
| `TC-S06-11` | Tier 2 Invariant Test | Hero Rally Invariant (Recovers morale $\ge 25$, returns to controllable state) | **PASS** |
| `TC-S06-12` | Tier 2 Invariant Test | Terrain Movement Speed Simulation Traversal Invariant | **PASS** |
| `TC-S06-13` | Tier 2 Invariant Test | **1,000-Tick Deterministic Replay Checksum Bit-for-Bit Equality** | **PASS** |
| `TC-S06-14` | Tier 3 Integration Test | Spearmen in Shield Wall Defeat Charging Cavalry Encounter | **PASS** |
| `TC-S06-15` | Tier 3 Integration Test | High Ground Archer Skirmish Advantage Encounter | **PASS** |
| `TC-S06-16` | Tier 3 Integration Test | Flanking Attack & Squad Morale Collapse Encounter | **PASS** |
| `TC-S06-17` | Tier 4 Scenario Test | Headless Tactical Match Scenario & Presenter View Models | **PASS** |
| `TC-S06-18` | Tier 4 Data Test | DataLoader Deserialization for `terrain.json` and `formations.json` | **PASS** |

**Cumulative Test Results:** 138 / 138 tests passing (100% green pass rate).

---

## Definition of Done (DoD) Checklist
- [x] **Scope Satisfied:** Implemented strictly per acceptance criteria with zero speculative feature drift.
- [x] **100% Green Automation:** Cumulative test suite passed cleanly (`dotnet test` -> 138 passed).
- [x] **Clean Build & Lint:** `dotnet build` succeeds with 0 errors and 0 warnings.
- [x] **Performance Budget Verified:** Simulation hot loops contain zero per-tick dynamic heap allocations.
- [x] **Save/Load & Replay Compatibility:** 1,000-tick replay matches 64-bit state checksum bit-for-bit.
- [x] **Game Director & QA Acceptance:** Formal sign-off in `task.md`.
- [x] **Git Feature Branch & PR Created:** Feature branch pushed to origin and PR created via `gh pr create`.
- [x] **Documentation & Walkthrough:** Architecture changes, test catalogs, and `walkthrough.md` updated with real execution data.
