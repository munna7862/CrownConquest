# Sprint 06 Test Cases Catalog — Tactical Combat

## Overview
This document specifies the complete test catalog for **Sprint 06: Tactical Combat**, validating Terrain Modifiers, Formations (Line, Shield Wall, Wedge, Square, Loose, Column), Morale & Routing State Machine, Elevation Ranged Combat, and Cavalry Charge & Spear Bracing.

---

## Test Cases Matrix

| Test ID | Tier | Category | Component | Description & Expected Result |
|:---|:---|:---|:---|:---|
| **TC-S06-01** | Tier 1 | Unit | Terrain Math | Verify `TerrainModifiers` movement speed multipliers (Plains 1.0, Forest 0.8, Marsh 0.6, Road 1.25) match data definitions. |
| **TC-S06-02** | Tier 1 | Unit | Elevation Math | Verify High Ground provides +2.0 range and +25% damage bonus; Low Ground attacking uphill suffers -15% damage penalty. |
| **TC-S06-03** | Tier 1 | Unit | Forest Cover | Verify Forest terrain mitigates incoming ranged damage by 35% without reducing melee damage. |
| **TC-S06-04** | Tier 1 | Unit | Formation Offsets | Verify `FormationCalculator` generates correct slot coordinates for Line, Shield Wall, Wedge, Square, Loose, and Column formations. |
| **TC-S06-05** | Tier 1 | Unit | Formation Stat Modifiers | Verify Shield Wall grants +4 Armor and -30% Speed; Wedge grants +30% Charge Damage and +15% Speed; Square grants +2 Armor. |
| **TC-S06-06** | Tier 1 | Unit | Morale Level Evaluation | Verify `MoraleState` correctly transitions through `Confident` ($\ge 80$), `Steady` ($50-79$), `Wavering` ($25-49$), `Breaking` ($1-24$), and `Routed` ($0$). |
| **TC-S06-07** | Tier 1 | Unit | Morale Drain & Recovery | Verify morale drain on casualty (-10), flanking (-15), charge impact (-25), and hero aura passive recovery (+3/s). |
| **TC-S06-08** | Tier 1 | Unit | Charge Impact Formula | Verify unbraced cavalry charge deals 2.0x damage (100% impact bonus) and applies -25 morale shock. |
| **TC-S06-09** | Tier 1 | Unit | Spear Bracing Counter | Verify cavalry charging into Spearman or Shield Wall deals standard/negated charge damage and suffers 50% recoil damage back. |
| **TC-S06-10** | Tier 2 | Simulation | Routing Invariant | Verify unit with 0 morale enters `Routed` state, drops attack/gather orders, flees toward safety, and cannot attack. |
| **TC-S06-11** | Tier 2 | Simulation | Hero Rally Invariant | Verify `RallyUnitCommand` and Hero leadership aura restores morale above threshold ($\ge 25$) to rally routed units back to controllable state. |
| **TC-S06-12** | Tier 2 | Simulation | Terrain Movement Simulation | Verify simulated unit traversal over mixed terrain (Road -> Forest -> Marsh) adjusts movement speeds deterministically without skipping ticks. |
| **TC-S06-13** | Tier 2 | Simulation | Deterministic 1000-Tick Replay | Verify dual seeded simulation runs with formations, terrain, morale, and charge produce bit-for-bit identical 64-bit state checksums. |
| **TC-S06-14** | Tier 3 | Integration | Shield Wall vs Cavalry Integration | Multi-unit skirmish: 5 Cavalry charging into 5 Spearmen in Shield Wall vs 5 Cavalry charging into 5 unformed Swordsmen. Shield wall wins decisively with minimal casualties. |
| **TC-S06-15** | Tier 3 | Integration | High Ground Archer Skirmish | Multi-unit skirmish: Archers on Hill defeat equal archers on Lowland/Marsh due to elevation range and damage multiplier. |
| **TC-S06-16** | Tier 3 | Integration | Flanking & Morale Collapse | Flanking an engaged enemy squad accelerates morale depletion and triggers cascade routing. |
| **TC-S06-17** | Tier 4 | E2E | Tactical Combat Match Scenario | Full headless match scenario demonstrating terrain positioning, formation switching, cavalry charge vs spear wall, and hero rally victory. |
| **TC-S06-18** | Tier 4 | Data | Data Definitions Loading | Verify `DataLoader` properly deserializes `terrain.json` and `formations.json` into domain models without errors. |

---

## Invariant Validation Rules
1. **Routing Rule:** Any unit with Morale = 0 MUST immediately enter `Routed` state, clear attack/gather targets, and flee away from threats.
2. **Bracing Rule:** Cavalry charging a Spearman or Shield Wall MUST NEVER receive the charge damage multiplier and MUST suffer recoil damage.
3. **Determinism Rule:** All formation calculations and morale checks MUST utilize pure deterministic integer/fixed-point math and seeded RNG with zero float drift.
4. **Zero-Allocation Rule:** Per-tick combat, morale, terrain, and formation updates MUST NOT allocate heap memory during active simulation loops.
