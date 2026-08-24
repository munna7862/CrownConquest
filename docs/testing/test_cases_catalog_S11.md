# Sprint 11 Pre-Implementation Test Cases Catalog: Missions and World Progression

**Sprint:** Sprint 11 — Missions and World Progression  
**Author:** QA & SDET Architect (SDET)  
**Target Milestone:** Phase Campaign  
**Test Suite Target:** Tier 1 (Unit), Tier 2 (Invariants), Tier 3 (Integration), Tier 4 (Headless E2E)

---

## 1. Test Architecture & Coverage Matrix

| Test ID | Test Category | Target Component | Tier | Description & Verification Goal |
|:---|:---|:---|:---:|:---|
| **TC-S11-01** | Unit / Data | `DataLoader` / `MissionDefinitionModel` & `FactionDefinitionModel` | Tier 1 | Verify parsing and loading of `missions.json` and `factions.json` data definitions into typed domain records. |
| **TC-S11-02** | Unit / Logic | `FactionDiplomacyManager` | Tier 1 | Verify reputation modification, clamping to $[-100, +100]$, and standing transitions (`AtWar`, `Hostile`, `Neutral`, `Friendly`, `Allied`). |
| **TC-S11-03** | Unit / Logic | `FactionDiplomacyManager` | Tier 1 | Verify trade bonus modifiers and diplomatic relations impact on campaign economy and alliance access. |
| **TC-S11-04** | Unit / Logic | `MissionEngine` | Tier 1 | Verify mission registry, acceptance, state transitions (`Inactive` $\to$ `Active` $\to$ `Completed` / `Failed` / `Expired`). |
| **TC-S11-05** | Unit / Objective | `DefendObjective` | Tier 1 | Verify survival timer evaluation: holding a target stronghold/province until duration expires yields completion; loss of objective entity yields immediate failure. |
| **TC-S11-06** | Unit / Objective | `DestroyObjective` | Tier 1 | Verify kill quota evaluation: eliminating designated enemy armies, commanders, or siege structures completes mission; deadline expiry before quota yields failure. |
| **TC-S11-07** | Unit / Objective | `CaptureObjective` | Tier 1 | Verify territory conquest evaluation: occupying and maintaining control of a target province for $N$ consecutive ticks triggers completion. |
| **TC-S11-08** | Unit / Objective | `EscortObjective` | Tier 1 | Verify convoy progression evaluation: escorting a supply caravan / VIP entity to destination province succeeds upon arrival; entity destruction yields failure. |
| **TC-S11-09** | Unit / Objective | `ResourceControlObjective` | Tier 1 | Verify economy quota evaluation: accumulating target quantities of Food, Iron, or Gold within turn limit completes mission. |
| **TC-S11-10** | Simulation / Invariant | `MissionEngine` | Tier 2 | Reward Attribution Invariant: Exactly one payout of Gold, XP, and Faction Reputation is awarded upon mission completion, with zero double-claim defects. |
| **TC-S11-11** | Simulation / Invariant | `MissionEngine` | Tier 2 | Concurrency Invariant: Multiple active missions with diverse objective types update independently without cross-mission state contamination. |
| **TC-S11-12** | Simulation / Invariant | `FactionDiplomacyManager` | Tier 2 | Diplomatic Repercussion Invariant: Completing a contract for Faction A against Faction B positively scales A's standing while symmetrically penalizing B's standing. |
| **TC-S11-13** | Simulation / Invariant | `MissionEngine` | Tier 2 | Expiration Invariant: Active timed missions strictly expire on tick $T_{\text{start}} + \text{Duration}$ if objectives remain unfulfilled. |
| **TC-S11-14** | Integration | `MissionEngine` & `CampaignEngine` | Tier 3 | Defend/Capture Campaign Integration: Strategic army maneuvers to contested province, triggers defensive battle, holds province, and triggers mission completion event. |
| **TC-S11-15** | Integration | `MissionEngine` & `BattleTransitionEngine` | Tier 3 | Escort Convoy Encounter Integration: Convoy intercepts enemy hostiles, tactical combat executes, convoy survives with tactical veterancy, and arrives at destination. |
| **TC-S11-16** | Integration / Serialization | `CampaignSaveData` & `MissionSaveData` | Tier 3 | Campaign Save/Load Roundtrip Integrity: Save active missions, objective progress, and faction standing at tick $N$, reload, and verify identical subsequent simulation. |
| **TC-S11-17** | Headless E2E | `CampaignMissionScenario` | Tier 4 | Full 5-Mission Campaign Progression: Complete chained Defend, Destroy, Capture, Escort, and Resource Control missions with progressive reputation advancements. |
| **TC-S11-18** | Headless Replay | `MissionEngine` & `CampaignEngine` | Tier 4 | 1,000-Tick Deterministic Replay Parity: Bit-exact 64-bit state checksum equality across dual seeded campaign runs with active mission pipelines. |

---

## 2. Test Execution Invariants & Guardrails

1. **Zero Real-Time Delays:** All mission timers and campaign ticks advance deterministically using fixed tick counts (`SimulateTicks`, `AdvanceTick`).
2. **Deterministic Seed Control:** All objective triggers and RNG events utilize `SimulationRandom` with explicit seeds.
3. **Decoupled Architecture:** Mission simulation, objective evaluators, and diplomacy managers run 100% independent of Godot presentation nodes.
4. **Zero-Allocation Hot-Loop:** Per-tick mission progress checks (`EvaluateMissions`) must execute with 0 dynamic heap allocations.
