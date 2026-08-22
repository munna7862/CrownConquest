# Phase 03 — Unit Progression

## Objective
Implement the signature battlefield progression system where combat kills make individual units stronger.

## Core Rule
When a combat unit kills another combat unit, the killer receives experience automatically.

## Unit State
Every combat unit tracks:
- Unique ID
- Level
- Experience
- Kill count
- Battles participated
- Battles survived
- Veterancy rank
- Current health
- Maximum health

## XP
Initial formula:
XP = Base Kill XP + Target Level Bonus + Target Tier Bonus

All values must be data-driven.

## Leveling
Example thresholds:
- Level 1: 0 XP
- Level 2: 100 XP
- Level 3: 250 XP
- Level 4: 500 XP
- Level 5: 900 XP

The exact values are balance data, not code constants.

## Level-Up
On threshold:
1. Award XP.
2. Increment kill count.
3. Evaluate progression.
4. Increase applicable stats.
5. Emit UnitLevelUp.
6. Update presentation.

## Veterancy
Ranks:
- Recruit
- Experienced
- Veteran
- Elite
- Legendary

## Stat Progression
Possible modifiers:
- Health
- Attack
- Defense
- Accuracy
- Attack speed
- Movement
- Morale

Unit archetypes should have different progression profiles.

## Death
When a veteran dies:
- Remove active unit.
- Preserve final statistics in history.
- Emit UnitDied.
- Record final kill count and level.

## Tests
Mandatory scenarios:
- Kill grants XP.
- Correct XP for target level.
- Kill count increments exactly once.
- Level-up occurs automatically.
- Multiple thresholds cannot be skipped incorrectly.
- Stats change correctly.
- XP is not awarded for assists unless explicitly supported.
- Friendly fire cannot incorrectly award XP.
- Dead units cannot gain XP.
- Duplicate kill events cannot duplicate rewards.
- Save/load preserves progression.

## Deliverables
A playable battle where individual soldiers visibly progress through combat.

## Definition of Done
A unit that kills enemies can become stronger during the same battle, with deterministic and fully tested progression.
