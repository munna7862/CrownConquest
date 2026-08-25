# Pull Request: Sprint 17 — Interactive Building Production, Worker Gathering Loop & Settlement Placement

## PR Metadata
- **Branch:** `feature/sprint-17-interactive-buildings-and-worker-economy`
- **Target Branch:** `main`
- **Sprint:** Sprint 17 (Milestone 9)
- **Status:** Ready for Review & Merge
- **Total Cumulative Tests:** 363 / 363 Passing (100% Green, 0 Failures, 0 Skips)

---

## Executive Summary

Sprint 17 introduces full settlement interactivity to **Crown & Conquest**, transforming the RTS battlefield into a living, responsive civilization builder with full domain-presentation decoupling:
1. **Building Selection & Production Action Cards (`CNC-1701`):** Left-clicking Town Centers, Barracks, Blacksmiths, and Stables opens structured action cards with unit training and technology research options, with hotkeys ([V], [S], [A], [F], [R], [C]). Clicking ground or combat units cleanly deselects buildings.
2. **Production Queues & Progress Bars (`CNC-1702`):** Up to 5 concurrent build orders per building. Resources are deducted immediately upon enqueueing, with real-time progress bars ticking at $20\text{Hz}$. Right-clicking queued items cancels production with a 100% resource refund. Completed items spawn units at the building entrance.
3. **Rally Point Flags & Spawn Marching (`CNC-1703`):** Visual rally flags appear on the battlefield when selecting a production structure. Right-clicking ground or resource nodes updates the rally point. Newly spawned combat units immediately march to the flag, while villagers automatically begin harvesting.
4. **Worker Autonomous Harvesting Loop (`CNC-1704`):** Right-clicking resource nodes dispatches villagers with carry capacity (10), harvest rates ($0.5$/tick), automatic pathing to the nearest Town Center/Storage Pit drop-off, banking deposits, and automated return loops. Depleted nodes are safely cleared.
5. **Grid-Aligned Building Placement Blueprint (`CNC-1705`):** Pressing `B` opens the construction menu (House, Barracks, Blacksmith, Watchtower, Farm). A translucent blueprint ghost snaps to grid tiles, displaying bright green (valid placement & affordable) or translucent red (obstructed terrain/building or insufficient resources). Placing foundations starts worker construction.
6. **Dynamic Housing Capacity & Population Breakdown (`CNC-1706`):** Houses grant $+5$ max population cap up to the 200 ceiling. The HUD displays real-time breakdowns (`Occupied: X (Military: M, Workers: W) | Capacity: Y / Max: 200`), blocking training when pop capped.

---

## Backlog Stories Verification Matrix

| Story ID | Description | Automated Tests | Acceptance Status |
|:---|:---|:---|:---|
| `CNC-1701` | Building Selection & Production Action Cards | `TC_S17_001` - `TC_S17_007` | **APPROVED** |
| `CNC-1702` | Production Queues & Timed Progress Bars | `TC_S17_008` - `TC_S17_014` | **APPROVED** |
| `CNC-1703` | Rally Point Flags & Spawn Marching | `TC_S17_015` - `TC_S17_018` | **APPROVED** |
| `CNC-1704` | Worker Autonomous Resource Gathering Loop | `TC_S17_019` - `TC_S17_020` | **APPROVED** |
| `CNC-1705` | Grid-Aligned Building Placement Blueprint | `TC_S17_021` - `TC_S17_023` | **APPROVED** |
| `CNC-1706` | Dynamic Housing Capacity & Population Breakdown | `TC_S17_022`, `TC_S17_024` | **APPROVED** |

---

## Quality Gate & Test Execution Summary

```
Test run for CrownConquest.Tests.dll (.NETCoreApp,Version=v8.0)
VSTest version 17.11.1 (x64)

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed: 0, Passed: 363, Skipped: 0, Total: 363, Duration: 26 s
Build succeeded: 0 Warning(s), 0 Error(s)
```

- **Cumulative Test Suite:** 363 / 363 tests passed (339 baseline historical + 24 new Sprint 17 tests).
- **Deterministic Replay Parity:** 1,000-tick headless simulation runs produce bit-for-bit identical 64-bit checksums across runs.
- **Zero-Allocation Hot Loop:** Hot simulation loops maintain 0 dynamic heap allocations per tick.
- **Memory Footprint:** Total application memory remains $< 85\text{ MB}$, well within the $< 500\text{ MB}$ desktop envelope.

---

## Key Files Modified & Added

- `src/CrownConquest.Application/SelectionManager.cs`: Building selection state and rally point command dispatch.
- `src/CrownConquest.Presentation/InteractiveRtsHud.cs`: Building production action cards, queued item tracking, and population breakdown viewmodels.
- `src/CrownConquest.Presentation/BuildingPlacementPreview.cs`: Blueprint configuration lookup helper.
- `src/CrownConquest.Domain/Simulation/SimulationEngine.cs`: Production configs, worker rally assignment, and training timings.
- `scenes/main.gd`: Godot 2D interactive settlement viewport controller with building selection, action cards, build menu (`B`), rally point flag renderer, and worker gather loops.
- `tests/CrownConquest.Tests/Presentation/SettlementInteractivityTests.cs`: 24 comprehensive Tier 1-4 tests.
- `docs/testing/test_cases_catalog_S17.md`: SDET pre-implementation test catalog.
- `task.md`: Sprint 17 tracking and persona handoff log.
