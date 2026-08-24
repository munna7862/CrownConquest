# Pull Request: Sprint 07 — Siege Warfare & Fortifications

## Description
This pull request delivers **Sprint 07 — Siege Warfare** for Crown & Conquest. It introduces fully simulated fortifications, siege weaponry, autonomous tower defenses, and tactical wall breach mechanics.

---

## Key Features & Changes
1. **Fortifications & Gate State Machine:**
   - Added `wooden_wall`, `stone_wall`, `wooden_gate`, `stone_gate`, `guard_tower`, `ballista_tower`, and `siege_workshop` in `data/definitions/buildings.json`.
   - Implemented `GateState` (Closed, Open, Locked) and `GateDefenseState` supporting automated friendly passage and enemy blocking.
2. **Siege Engine Fleet & Combat Math:**
   - Added Celtic and Roman `battering_ram`, `catapult`, and `ballista` in `data/definitions/units.json`.
   - **Battering Rams**: $5.0\times$ structural multiplier vs buildings/gates and $80\%$ damage reduction from ranged pierce attacks.
   - **Catapults / Onagers**: Minimum range ($3.0$ tiles), maximum range ($12.0$ tiles), and $2.5$-radius AoE splash damage with linear distance falloff ($100\%$ at epicenter to $50\%$ at outer boundary).
   - **Ballistas**: High direct piercing damage ignoring $60\%$ of target armor.
3. **Autonomous Tower Defense & Garrisoning:**
   - Implemented `TowerDefenseState` allowing towers to scan for enemies in range and fire autonomous volleys every tick cooldown.
   - Garrisoning units inside towers provides $+20\%$ damage scaling per garrisoned archer/infantry.
4. **Wall Breach & Terrain Transformation:**
   - When a wall is destroyed ($HP \le 0$), the underlying tile transforms dynamically to `TerrainType.Rubble` ($0.75\times$ speed, $20\%$ cover), opening assault breach corridors.
   - Published `WallBreachedEvent` and tracked active breaches in `SimulationState.Breaches`.
5. **Presentation & Headless E2E Scenarios:**
   - Implemented `SiegeWarfarePresenter` to capture siege VFX/audio state.
   - Implemented `SiegeWarfareScenario` validating full fortress assault battles.
6. **Automated Test Suite (18 Test Cases across Tiers 1–4):**
   - Cumulative test suite increased from 138 to 156 tests (100% green pass, 0 skips).
   - Bit-for-bit 1,000-tick replay parity test verified identical checksums.

---

## Test Verification Summary
- **Total Tests Executed:** 156 passed, 0 failed, 0 skipped.
- **Build Status:** 0 warnings, 0 errors (`dotnet build`).
- **Memory & Allocation Guard:** 0 dynamic heap allocations per simulation tick; memory footprint $< 150\text{ MB}$.
