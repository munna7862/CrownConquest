# Sprint 09 Pre-Implementation Test Cases Catalog: Tactical AI and Personalities

**Sprint:** Sprint 09 — Tactical AI and Personalities  
**Author:** QA & SDET Architect (SDET)  
**Target Milestone:** Phase Enemy AI  
**Test Suite Target:** Tier 1 (Unit), Tier 2 (Invariants), Tier 3 (Integration), Tier 4 (Headless E2E)

---

## 1. Test Architecture & Coverage Matrix

| Test ID | Test Category | Target Component | Tier | Description & Verification Goal |
|:---|:---|:---|:---:|:---|
| **TC-S09-01** | Unit / Math | `AiTacticalScorer` | Tier 1 | Verify Focus Fire scoring formula: prioritization of low-HP targets, high-DPS threats, and armor weaknesses. |
| **TC-S09-02** | Unit / Math | `AiTacticalScorer` | Tier 1 | Verify Flanking maneuver point calculation: side and rear angle vectors based on target orientation/velocity. |
| **TC-S09-03** | Unit / Logic | `AiFormationSelector` | Tier 1 | Verify dynamic formation counter-selection (Square vs Cavalry, Wedge for Cavalry charge, Line for general, Skirmish vs Siege AoE). |
| **TC-S09-04** | Unit / Math | `AiTacticalScorer` | Tier 1 | Verify Elevation and High-Ground tactical evaluation bonus (+25% damage, +2 range advantage preference). |
| **TC-S09-05** | Unit / Logic | `AiSiegeTactics` | Tier 1 | Verify Siege target prioritization (Walls, Gates, Towers before units) and infantry escort positioning. |
| **TC-S09-06** | Unit / Data | `DataLoader` | Tier 1 | Verify loading `ai_personalities.json` into typed `AiPersonalityDefinitionModel` records. |
| **TC-S09-07** | Unit / Logic | `AiPersonalityProfile` | Tier 1 | Verify profile initialization and parameter bounds for Aggressive, Defensive, Expansionist, and Tactical personalities. |
| **TC-S09-08** | Simulation / Invariant | `AiFactionController` | Tier 2 | Focus Fire Invariant: AI squad units concentrate damage on the highest-priority/lowest-health target within range. |
| **TC-S09-09** | Simulation / Invariant | `AiFactionController` | Tier 2 | Flanking Invariant: Fast/Cavalry units execute flanking moves to trigger flank damage multipliers on engaged enemies. |
| **TC-S09-10** | Simulation / Invariant | `AiFactionController` | Tier 2 | Formation Invariant: AI automatically shifts army formation when detecting enemy cavalry or artillery composition. |
| **TC-S09-11** | Simulation / Invariant | `AiFactionController` | Tier 2 | High Ground Invariant: AI ranged units prioritize positioning on elevated terrain when available. |
| **TC-S09-12** | Simulation / Invariant | `AiFactionController` | Tier 2 | Siege Escort Invariant: AI keeps melee escorts within escort radius of deployed Catapults and Rams. |
| **TC-S09-13** | Integration | `AiFactionController` | Tier 3 | Aggressive Raider AI: Launches early attacks with low retreat threshold ($R_{combat} < 0.25$) and heavy cavalry/wedge emphasis. |
| **TC-S09-14** | Integration | `AiFactionController` | Tier 3 | Defensive Bastion AI: Constructs perimeter towers/walls, adopts Square/ShieldWall formations, and holds high ground. |
| **TC-S09-15** | Integration | `AiFactionController` | Tier 3 | Expansionist Imperial AI: Rapidly expands worker count (20+), establishes secondary resource outposts, and builds late-game deathball. |
| **TC-S09-16** | Integration | `AiFactionController` | Tier 3 | Tactical Hero-Centric AI: Protects and levels Hero, combos hero abilities with squad focus fire, and retreats if Hero HP $< 30\%$. |
| **TC-S09-17** | Headless E2E | `TacticalAiScenario` | Tier 4 | Full Multi-Personality Match: Aggressive Raider vs Defensive Bastion autonomous battle to conclusion. |
| **TC-S09-18** | Headless Replay | `SimulationEngine` | Tier 4 | 1,000-Tick Deterministic Replay Parity: Verify bit-for-bit 64-bit state checksum equality across dual seeded runs. |

---

## 2. Test Execution Invariants & Guardrails

1. **Zero Real-Time Delays:** All simulation steps executed deterministically using fixed tick counts (`SimulateTicks`).
2. **Deterministic Seed Control:** All seeded RNG operations utilize `SimulationRandom` with explicit seeds.
3. **Zero Heap Allocations in Hot Loops:** Hot tick execution (`Tick`, `UpdateAi`, `UpdateTactics`, `UpdatePerception`) must maintain zero dynamic heap allocations.
4. **Fair Play Guardrail:** AI queries strictly query `AiPerceptionState` or visible spatial queries, never bypassing fog-of-war.
