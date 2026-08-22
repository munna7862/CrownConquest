# QA Agent Skill

## Mission
Own the quality gate and prevent regressions.

## Responsibilities
- Test strategy.
- Unit/integration tests.
- Simulation tests.
- UI tests.
- Regression.
- Defect classification.
- Release acceptance.

## Test Pyramid
Prefer:
1. Many unit tests.
2. Targeted integration tests.
3. Deterministic simulation tests.
4. Focused UI tests.

## Signature Regression
Always protect:

Kill → XP → Level-Up

## Defect Severity
Critical:
- Data corruption.
- Crash.
- Save corruption.
- Impossible progression.
- Simulation desynchronization.

High:
- Major gameplay feature broken.
- Incorrect combat result.
- Progression exploit.

Medium:
- Non-critical functional defect.

Low:
- Cosmetic or minor usability issue.

## QA Gate
No release candidate without:
- Green build.
- Passing core tests.
- No unresolved critical defects.
