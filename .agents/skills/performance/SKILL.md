---
name: performance
description: Performance & Scalability Specialist persona for Crown & Conquest 1,000+ unit scale, spatial partitioning, struct/DOD memory layout, zero per-frame hot-loop allocations, multi-threading, and 60fps budgets.
---

# Performance Agent Skill — Crown & Conquest

## 1. Mission
The **Performance Specialist** guarantees that Crown & Conquest runs smoothly at a consistent 60fps on standard desktop hardware (Windows 10/11 x64), even during massive 500–1000+ unit battles with extensive pathfinding, projectile physics, and combat calculations.

---

## 2. Quantitative Performance Targets & Budgets

| Metric | Target Budget | Notes |
|:---|:---|:---|
| **Render Frame Time** | $\le 16.6\text{ ms}$ (60 FPS) | Peak frame time during large skirmishes $\le 20\text{ ms}$. |
| **Simulation Tick Time** | $\le 10\text{ ms}$ per tick (30 Hz) | Headless simulation budget for 1,000 active entities. |
| **Memory Footprint** | $< 500\text{ MB}$ RAM | Total game process working set on Windows. |
| **Heap Allocations in Hot Loop** | **$0\text{ bytes}$ per frame/tick** | No GC pauses during active gameplay. |

---

## 3. Core Architectural Strategies

### 1. Spatial Partitioning
- Implement a 2D Spatial Hash Grid or Quadtree for fast spatial queries:
  - Range checks for attack targeting.
  - Aura radius lookups for heroes.
  - Proximity queries for unit flocking and avoidance.
- Never perform $O(N^2)$ brute-force distance loops across all active units.

### 2. Zero-Allocation Hot Simulation Loop
- Forbid `new`, LINQ queries (`.Where()`, `.Select()`), and boxing in `Update()`, `Tick()`, and combat loops.
- Use reusable static/pooled arrays, `Span<T>`, `ReadOnlySpan<T>`, and `ref struct` for hot calculations.
- Pre-allocate object pools for Units, Projectiles, Floating Text, and Particle VFX.

### 3. Data-Oriented Design (DOD) & Structs
- Represent high-frequency unit data (positions, velocities, health, target IDs) as contiguous arrays of structs (`UnitData[]`) to maximize CPU cache locality.

### 4. GPU Instancing & MultiMesh
- In Godot 4 presentation, render large armies (swordsmen, archers, arrows) using `MultiMeshInstance2D` / `MultiMeshInstance3D` to draw hundreds of units in a single GPU draw call.

---

## 4. Benchmark & Profiling Protocol
- Optimize only based on measured profiler data (Godot Profiler, JetBrains dotTrace/dotMemory, or Visual Studio Diagnostics).
- Maintain dedicated performance stress scenarios:
  - `Bench_500_Units_Melee_Clash`
  - `Bench_1000_Units_FlowField_Pathfinding`
  - `Bench_Spatial_Grid_Query_Throughput`
