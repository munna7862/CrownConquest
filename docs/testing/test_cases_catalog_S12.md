# Pre-Implementation Test Cases Catalog: Sprint 12 — Large-Scale Performance

## Overview

This catalog specifies the complete test matrix for **Sprint 12: Large-Scale Performance**. It validates spatial grid acceleration, profiling telemetry instrumentation, AI decision time-slicing, pathfinding route caching, event ring buffers, memory pooling, and 100/250/500-unit scale benchmarks.

---

## Test Tier Matrix

| Test ID | Tier | Target Component | Scenario / Invariant Description | Success Criteria |
|:---|:---|:---|:---|:---|
| `TC-S12-001` | Tier 1 (Unit) | `SimulationProfiler` | Profiler phase timing recording & metrics computation | Subsystem durations recorded accurately via ref struct scopes; non-negative timings; metrics snapshot computes average, peak, and tick totals correctly |
| `TC-S12-002` | Tier 1 (Unit) | `PerformanceMetrics` | Telemetry snapshot metrics aggregation & reset | Correctly aggregates average tick time, peak tick time, entity counts, spatial query counts, and GC generation samples |
| `TC-S12-003` | Tier 1 (Unit) | `SpatialGrid` | Spatial radius and box queries correctness | Entities inside radius/box are found; entities outside are excluded; boundary points correctly handled |
| `TC-S12-004` | Tier 1 (Unit) | `SpatialGrid` | `QueryNearestEnemy` early exit and Chebyshev ring traversal | Accurately returns nearest enemy within max radius; ignores friendly units; returns null if none in range |
| `TC-S12-005` | Tier 1 (Unit) | `SpatialGrid` | `QueryRay` directional line and collision detection | Returns entities intersecting line segment from origin to target distance in sorted order |
| `TC-S12-006` | Tier 1 (Unit) | `SpatialGrid` | Entity movement update & removal across cell boundaries | Entity moving from cell $(X_1, Y_1)$ to $(X_2, Y_2)$ is immediately queryable at new cell and removed from old cell |
| `TC-S12-007` | Tier 1 (Unit) | `AiUpdateScheduler` | Time-sliced staggered task scheduling | AI tasks registered for intervals $N=3, 10$ execute strictly on ticks matching `(tick + offset) % interval == 0` |
| `TC-S12-008` | Tier 1 (Unit) | `AiUpdateScheduler` | Faction load balancing across tick offsets | Multiple AI factions are assigned interleaved tick offsets to distribute CPU load evenly across consecutive frames |
| `TC-S12-009` | Tier 1 (Unit) | `PathfindingCache` | Route lookup, hit/miss caching, and invalidation | Common start/destination queries hit cache; obstructed or modified destinations trigger cache eviction/refresh |
| `TC-S12-010` | Tier 1 (Unit) | `DomainEventRingBuffer` | Zero-allocation circular event ring buffer | Circular buffer maintains fixed capacity, overwrites oldest entries without heap allocations, and supports typed traversal |
| `TC-S12-011` | Tier 1 (Unit) | `EntityPool` / `ProjectilePool` | Object pooling rent, return, reset & capacity growth | Pooled instances are recycled with state reset; zero heap allocation when renting from warmed pool |
| `TC-S12-012` | Tier 2 (Invariant) | `SpatialGrid` | Spatial query equivalence with linear brute force | `SpatialGrid.QueryRadius` returns bit-for-bit identical entity sets as brute-force $O(N)$ sweep across 500 randomized unit positions |
| `TC-S12-013` | Tier 2 (Invariant) | `AiUpdateScheduler` | AI decision determinism across staggered ticks | Dual seeded runs produce bit-for-bit identical AI commands and state evolutions despite staggered time-slicing |
| `TC-S12-014` | Tier 2 (Invariant) | Simulation Hot Loop | Zero dynamic heap allocations in hot loop | 100 consecutive ticks of active 100-unit simulation generate 0 bytes of dynamic GC heap allocation |
| `TC-S12-015` | Tier 3 (Integration) | `PerformanceHudPresenter` | Presentation HUD telemetry view models | Telemetry presenter transforms domain `PerformanceMetrics` into UI-formatted view models (FPS, Tick Time, Entity Counts, AI Slicing) |
| `TC-S12-016` | Tier 4 (Benchmark) | `Bench_100_Units_Clash` | 100-unit melee & ranged clash benchmark | 100 units clashing over 100 ticks maintain average tick duration $\le 1.5\text{ ms}$ |
| `TC-S12-017` | Tier 4 (Benchmark) | `Bench_250_Units_Combined_Arms` | 250-unit combined arms benchmark | 250 units (infantry, cavalry, archers, siege) over 100 ticks maintain average tick duration $\le 4.0\text{ ms}$ |
| `TC-S12-018` | Tier 4 (Benchmark) | `Bench_500_Units_Mass_Battle` | 500-unit full scale battle benchmark | 500 units with 2 active AI factions over 100 ticks maintain average tick duration $\le 10.0\text{ ms}$ |
| `TC-S12-019` | Tier 4 (Headless E2E) | `DeterministicReplay` | 1,000-Tick deterministic replay parity at scale | Dual seeded 1,000-tick runs with 200+ units produce identical 64-bit state checksums bit-for-bit |
| `TC-S12-020` | Tier 4 (Headless E2E) | `SaveLoadParity` | State serialization roundtrip with spatial & profiling state | Serializing and restoring state at tick 300 reproduces exact subsequent simulation states and spatial indexing |

---

## Acceptance Sign-Off Criteria

1. All 20 test cases implemented in `tests/CrownConquest.Tests/`.
2. Cumulative test suite (`dotnet test`) passes 100% green with 0 warnings, 0 errors, and 0 skips.
3. 100, 250, and 500-unit benchmark scenarios execute well within their respective timing budgets ($\le 1.5\text{ ms}$, $\le 4.0\text{ ms}$, $\le 10.0\text{ ms}$).
4. Zero GC allocations verified in hot simulation loops.
