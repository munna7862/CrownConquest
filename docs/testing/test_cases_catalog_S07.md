# Sprint 07 Pre-Implementation Test Cases Catalog: Siege Warfare

**Sprint:** Sprint 07 — Siege Warfare  
**Author:** QA & SDET Architect (SDET)  
**Target Milestone:** Phase Advanced Combat  
**Test Suite Target:** Tier 1 (Unit), Tier 2 (Invariants), Tier 3 (Integration), Tier 4 (Headless E2E)

---

## 1. Test Architecture & Coverage Matrix

| Test ID | Test Category | Target Component | Tier | Description & Verification Goal |
|:---|:---|:---|:---:|:---|
| **TC-S07-01** | Unit / Math | `CombatFormulas` | Tier 1 | Verify Battering Ram structural damage formula ($5.0\times$ multiplier against buildings and walls). |
| **TC-S07-02** | Unit / Math | `CombatFormulas` | Tier 1 | Verify Battering Ram pierce armor mitigation ($80\%$ damage reduction against ranged missile attacks). |
| **TC-S07-03** | Unit / Math | `CombatFormulas` | Tier 1 | Verify Catapult AoE splash damage falloff ($100\%$ at center, $50\%$ at outer radius $R=2.5$). |
| **TC-S07-04** | Unit / Boundary | `CombatFormulas` | Tier 1 | Verify Catapult minimum range ($3.0$) and maximum range ($12.0$) boundary validation. |
| **TC-S07-05** | Unit / Math | `CombatFormulas` | Tier 1 | Verify Ballista armor penetration calculation ($60\%$ target armor ignored). |
| **TC-S07-06** | Unit / Math | `TowerDefenseState` | Tier 1 | Verify Tower damage scaling with garrisoned units ($+20\%$ attack power per garrisoned unit). |
| **TC-S07-07** | Unit / State | `GateEntity` | Tier 1 | Verify `GateState` transitions (`Closed`, `Open`, `Locked`) and state validity rules. |
| **TC-S07-08** | Unit / Data | `TerrainModifiers` | Tier 1 | Verify `TerrainType.Rubble` movement speed ($0.75\times$) and cover mitigation ($0.20\times$). |
| **TC-S07-09** | Simulation / Invariant | `SimulationEngine` | Tier 2 | Wall Destruction Invariant: Destroying a wall creates `TerrainType.Rubble` and publishes `WallBreachedEvent`. |
| **TC-S07-10** | Simulation / Invariant | `SimulationEngine` | Tier 2 | Gate Passability Invariant: Friendly units traverse open gate; enemy units are blocked by closed gate. |
| **TC-S07-11** | Simulation / Invariant | `SimulationEngine` | Tier 2 | Tower Autonomous Defense Invariant: Tower acquires closest enemy in range and fires per cooldown cycle. |
| **TC-S07-12** | Simulation / Invariant | `SimulationEngine` | Tier 2 | Catapult Splash Impact Invariant: Catapult shot damages all enemies and buildings within splash radius. |
| **TC-S07-13** | Integration | `SimulationEngine` | Tier 3 | Siege Workshop Production: Queue and spawn Battering Ram, Catapult, and Ballista with proper costs. |
| **TC-S07-14** | Integration | `SimulationEngine` | Tier 3 | Tower Garrison & Ungarrison: Units enter tower, enhance firepower, and safely egress on command. |
| **TC-S07-15** | Integration / AI | `SiegeAiHooks` | Tier 3 | Siege AI Target Selection: Siege units prioritize gates, towers, and walls over standard infantry. |
| **TC-S07-16** | Integration / AI | `SiegeAiHooks` | Tier 3 | Breach Navigation Hook: Units locate and navigate towards the nearest active breach point. |
| **TC-S07-17** | Headless E2E | `SiegeWarfareScenario` | Tier 4 | Full Fortress Assault Match: Rams breach gates/walls, towers fire on attackers, infantry breaches fortress. |
| **TC-S07-18** | Headless Replay | `SimulationEngine` | Tier 4 | 1,000-Tick Deterministic Replay Parity: Verify bit-for-bit 64-bit checksum match across dual runs. |

---

## 2. Test Execution Invariants & Guardrails

1. **Zero Real-Time Delays:** All simulation steps executed deterministically using fixed tick counts (`SimulateTicks`).
2. **Deterministic Seed Control:** All seeded RNG operations utilize `SimulationRandom` with explicit seeds.
3. **Zero Heap Allocations in Hot Loops:** Hot tick execution must not allocate temporary objects.
4. **Attribution Integrity:** Kill XP and destruction events uniquely attributed.
