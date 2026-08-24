# Sprint 10 Pre-Implementation Test Cases Catalog: Strategic World Foundation

**Sprint:** Sprint 10 — Strategic World Foundation  
**Author:** QA & SDET Architect (SDET)  
**Target Milestone:** Phase Campaign  
**Test Suite Target:** Tier 1 (Unit), Tier 2 (Invariants), Tier 3 (Integration), Tier 4 (Headless E2E)

---

## 1. Test Architecture & Coverage Matrix

| Test ID | Test Category | Target Component | Tier | Description & Verification Goal |
|:---|:---|:---|:---:|:---|
| **TC-S10-01** | Unit / Data | `DataLoader` / `ProvinceDefinitionModel` | Tier 1 | Verify parsing and loading of `provinces.json` data definitions into typed records. |
| **TC-S10-02** | Unit / Logic | `StrategicMap` | Tier 1 | Verify province adjacency graph construction, bidirectional connection integrity, and shortest-path graph search. |
| **TC-S10-03** | Unit / Logic | `StrategicArmy` | Tier 1 | Verify strategic army creation, unit composition specs, attached hero spec, and total combat power rating calculations. |
| **TC-S10-04** | Unit / Math | `StrategicMovementCalculator` | Tier 1 | Verify travel tick duration formula across various distances, base army speeds, and terrain biome multipliers (Plains $1.0\times$, Forest $1.5\times$, Hills $1.3\times$, Swamp $1.8\times$, Road $0.7\times$). |
| **TC-S10-05** | Unit / Logic | `StrategicProvince` | Tier 1 | Verify province resource yield generation into faction treasury per campaign turn/tick. |
| **TC-S10-06** | Unit / Logic | `StrategicTerritoryManager` | Tier 1 | Verify territory ownership tracking, control percentage calculation, and province transfer rules. |
| **TC-S10-07** | Simulation / Invariant | `CampaignEngine` | Tier 2 | Movement Travel Invariant: Moving army decrements travel ticks deterministically and arrives at destination province on exact completion tick. |
| **TC-S10-08** | Simulation / Invariant | `CampaignEngine` | Tier 2 | Multi-Waypoint Invariant: Strategic army traverses a multi-hop province path queue sequentially without skipping or stalling. |
| **TC-S10-09** | Simulation / Invariant | `CampaignEngine` | Tier 2 | Hostile Contact Invariant: Moving into an enemy-occupied province triggers an authoritative battle transition event. |
| **TC-S10-10** | Simulation / Invariant | `BattleTransitionEngine` | Tier 2 | Garrison & Fortress Bonus Invariant: Defending units on high-defense provinces receive appropriate armor and health scaling in tactical deployment. |
| **TC-S10-11** | Simulation / Invariant | `BattleTransitionEngine` | Tier 2 | Survivor Retention & XP Invariant: Casualties in tactical battles are permanently removed, while surviving units retain exact current HP, XP gains, level-ups, and hero attributes back in the strategic army. |
| **TC-S10-12** | Simulation / Invariant | `CampaignEngine` | Tier 2 | Total Defeat Invariant: An army suffering 100% casualties is destroyed and removed from the strategic map cleanly. |
| **TC-S10-13** | Integration | `CampaignEngine` & `SimulationEngine` | Tier 3 | Full Strategic-to-Tactical Loop: Army marches, engages enemy army, tactical RTS battle executes, survivors return with veterancy advancements, and province is conquered. |
| **TC-S10-14** | Integration | `CampaignEngine` | Tier 3 | Strategic Economy & Resource Inflow: Controlling multiple resource provinces (Gold, Iron, Food) generates compound treasury growth over multiple turns. |
| **TC-S10-15** | Integration | `CampaignEngine` & `HeroState` | Tier 3 | Hero Strategic Campaign Integration: Hero leads army in battle, uses abilities, gains veterancy/XP, and returns to campaign map with persistent progression. |
| **TC-S10-16** | Integration / Serialization | `CampaignSerializer` | Tier 3 | Campaign Save/Load Roundtrip Integrity: Save full campaign state at tick $N$, reload, and verify identical state and subsequent execution parity. |
| **TC-S10-17** | Headless E2E | `CampaignProgressionScenario` | Tier 4 | Multi-Turn Campaign Match: Autonomous player vs AI campaign playout including movement, resource collection, tactical battle victory, and territory capture. |
| **TC-S10-18** | Headless Replay | `CampaignEngine` | Tier 4 | 1,000-Tick Deterministic Campaign Replay Parity: Bit-exact 64-bit state checksum equality across dual seeded campaign runs. |

---

## 2. Test Execution Invariants & Guardrails

1. **Zero Real-Time Delays:** All campaign and tactical steps executed deterministically using fixed tick counts (`SimulateTicks`, `AdvanceTick`).
2. **Deterministic Seed Control:** All combat simulation and RNG events utilize `SimulationRandom` with explicit seeds.
3. **Decoupled Architecture:** Campaign simulation and strategic domain models run 100% independent of Godot presentation nodes.
4. **Progression Invariant:** Every unit that levels up in a tactical encounter preserves its Level and Veterancy rank when returning to the strategic campaign layer.
