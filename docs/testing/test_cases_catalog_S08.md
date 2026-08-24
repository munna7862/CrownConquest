# Sprint 08 Pre-Implementation Test Cases Catalog: AI Foundation

**Sprint:** Sprint 08 — AI Foundation  
**Author:** QA & SDET Architect (SDET)  
**Target Milestone:** Phase Enemy AI  
**Test Suite Target:** Tier 1 (Unit), Tier 2 (Invariants), Tier 3 (Integration), Tier 4 (Headless E2E)

---

## 1. Test Architecture & Coverage Matrix

| Test ID | Test Category | Target Component | Tier | Description & Verification Goal |
|:---|:---|:---|:---:|:---|
| **TC-S08-01** | Unit / Math | `AiCombatEvaluator` | Tier 1 | Verify unit and squad combat power calculation formulas with health, damage, and level scaling. |
| **TC-S08-02** | Unit / Math | `AiCombatEvaluator` | Tier 1 | Verify combat odds ratio calculation and retreat threshold trigger ($R_{combat} < 0.45$). |
| **TC-S08-03** | Unit / State | `AiPerceptionState` | Tier 1 | Verify entity sight discovery, line-of-sight range boundaries, and fog-of-war memory tracking. |
| **TC-S08-04** | Unit / Math | `AiResourcePriority` | Tier 1 | Verify dynamic resource priority weightings based on worker count, queues, and deficits. |
| **TC-S08-05** | Unit / Logic | `AiTargetingMatrix` | Tier 1 | Verify tactical archetype targeting preferences (Siege $\to$ Forts, Cav $\to$ Ranged, Spear $\to$ Cav). |
| **TC-S08-06** | Unit / State | `AiBuildOrderPlan` | Tier 1 | Verify build order sequence progression and population cap headroom detection. |
| **TC-S08-07** | Simulation / Invariant | `SimulationEngine` | Tier 2 | Fog of War Invariant: AI never issues commands to attack unrevealed enemy units outside sight radius. |
| **TC-S08-08** | Simulation / Invariant | `SimulationEngine` | Tier 2 | Worker Self-Sufficiency Invariant: Idle AI workers automatically search for and harvest nearby resources. |
| **TC-S08-09** | Simulation / Invariant | `SimulationEngine` | Tier 2 | Housing Placement Invariant: AI queues House construction when within 2 pop of maximum capacity. |
| **TC-S08-10** | Simulation / Invariant | `SimulationEngine` | Tier 2 | Base Defense Invariant: AI army disengages from patrol/rally to defend Town Center when base is attacked. |
| **TC-S08-11** | Simulation / Invariant | `SimulationEngine` | Tier 2 | Dynamic Retreat Invariant: Overmatched AI squad disengages and retreats towards friendly base/towers. |
| **TC-S08-12** | Simulation / Invariant | `SimulationEngine` | Tier 2 | Production Queue Invariant: AI keeps barracks and town centers producing without bankrupting resources. |
| **TC-S08-13** | Integration | `SimulationEngine` | Tier 3 | End-to-End Economic AI: AI starts with Town Center + 3 workers, builds farms/houses, and scales to 15+ workers. |
| **TC-S08-14** | Integration | `SimulationEngine` | Tier 3 | Military Assembly & Staging: AI builds barracks, trains 8+ units, and groups them at staging rally point. |
| **TC-S08-15** | Integration | `SimulationEngine` | Tier 3 | Combined Arms Army Composition: AI recruits balanced infantry, archers, cavalry, and siege. |
| **TC-S08-16** | Integration | `SimulationEngine` | Tier 3 | Autonomous Attack Run: AI detects enemy base, marches staged army, breaches defenses, and destroys targets. |
| **TC-S08-17** | Headless E2E | `AiFoundationScenario` | Tier 4 | Full Bot vs Bot Match: 2 AI factions independently build economy, field armies, attack/defend, until decisive victory. |
| **TC-S08-18** | Headless Replay | `SimulationEngine` | Tier 4 | 1,000-Tick Deterministic Replay Parity: Verify bit-for-bit 64-bit state checksum equality in AI matches. |

---

## 2. Test Execution Invariants & Guardrails

1. **Zero Real-Time Delays:** All simulation steps executed deterministically using fixed tick counts (`SimulateTicks`).
2. **Deterministic Seed Control:** All seeded RNG operations utilize `SimulationRandom` with explicit seeds.
3. **Zero Heap Allocations in Hot Loops:** Hot tick execution (`Tick`, `UpdateAi`, `UpdatePerception`, `UpdateWorkers`) must maintain zero dynamic heap allocations.
4. **Fair Play Guardrail:** AI queries strictly query `AiPerceptionState` or visible spatial queries, never bypassing fog-of-war.
