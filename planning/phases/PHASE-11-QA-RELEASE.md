# Phase 11 — QA & Release

## Objective
Prepare the game for a stable release with automated validation and repeatable builds.

## Test Layers

### Unit Tests
Cover:
- Combat.
- Damage.
- XP.
- Level progression.
- Resources.
- Construction.
- Technologies.
- Hero abilities.
- Morale.
- Save/load.

### Integration Tests
Validate:
- Combat → kill → XP → level-up.
- Economy → production → army.
- Technology → stat modifier.
- Hero → aura → unit.
- Battle → campaign state.
- Save → load → state restoration.

### Simulation Tests
Run large numbers of automated battles.

Record:
- Win rate.
- Average battle duration.
- Unit survival.
- XP progression.
- Hero survival.
- Resource efficiency.

### UI Tests
Validate:
- Selection.
- Commands.
- Building.
- Production.
- Research.
- Save/load.
- Menus.
- Victory/defeat.

## Balance Testing
Create automated balance reports.

Example:

```text
Faction A vs Faction B
1000 simulations

Faction A wins: 51.2%
Faction B wins: 48.8%

Average battle duration: 08:42
```

## Save/Load Testing
Test:
- Mid-battle save.
- Campaign save.
- Hero progression.
- Veteran units.
- Destroyed buildings.
- Technology state.
- World state.

## CI/CD
Pipeline:
1. Checkout.
2. Restore dependencies.
3. Build.
4. Run unit tests.
5. Run integration tests.
6. Run simulation tests.
7. Package.
8. Publish artifacts.

## Release Candidate
Before release:
- No critical defects.
- No save corruption.
- No reproducible progression exploits.
- No major performance regression.
- CI green.
- Smoke tests green.

## Definition of Done
The same build can be reproduced and validated automatically from a clean environment.
