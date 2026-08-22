# Phase 09 — Large-Scale Performance

## Objective
Ensure the simulation remains stable and responsive as army sizes increase.

## Targets
Milestones:
- 100 active units.
- 250 active units.
- 500 active units.
- 1000+ entity stress test.

These are engineering targets, not guaranteed final gameplay limits.

## Profiling Areas
Measure:
- Frame time.
- Simulation time.
- AI time.
- Pathfinding time.
- Rendering time.
- Memory.
- Garbage collection.
- Event processing.

## Optimization Areas
- Pathfinding.
- Spatial queries.
- AI update frequency.
- Entity updates.
- Animation.
- Rendering.
- Projectile simulation.
- Event dispatch.

## AI Scheduling
Not every AI decision needs to execute every frame.

Introduce:
- Priority-based updates.
- Staggered decision cycles.
- Distance-based simulation frequency.

## Large Battle Tests
Automate:
- 100-vs-100.
- 250-vs-250.
- 500-vs-500.
- Mixed army battles.
- Siege stress tests.

## Regression
Every optimization must preserve:
- Combat correctness.
- XP.
- Level progression.
- Morale.
- AI behavior.
- Save/load integrity.

## Definition of Done
Performance bottlenecks are measured, documented and addressed using profiling evidence rather than assumptions.
