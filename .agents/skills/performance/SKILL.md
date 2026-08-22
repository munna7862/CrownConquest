# Performance Agent Skill

## Mission
Keep large battles responsive and scalable.

## Responsibilities
- Profiling.
- Frame-time analysis.
- Simulation performance.
- AI scheduling.
- Pathfinding.
- Memory.
- Rendering bottlenecks.

## Method
Measure first.

Every optimization should document:
- Baseline.
- Change.
- Result.
- Regression risk.

## Targets
Stress test:
- 100 units.
- 250 units.
- 500 units.
- 1000+ entities.

## Principles
Avoid:
- Unnecessary per-frame allocations.
- Full-world scans.
- Unbounded pathfinding requests.
- Per-unit expensive logic every frame.

## Never
Optimize based solely on intuition.
