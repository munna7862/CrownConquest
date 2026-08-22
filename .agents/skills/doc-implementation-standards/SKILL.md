---
name: doc-implementation-standards
description: Documentation standards for Crown & Conquest RTS architecture, game systems, combat/economy data schemas, test catalogs, and sprint walkthroughs.
---

# Documentation Implementation Standards — Crown & Conquest

## 1. Purpose
Every completed feature, phase milestone, and sprint in **Crown & Conquest** must produce clear, accurate, and non-fabricated documentation under the workspace **`docs/`** directory.

---

## 2. Required Documentation by System Area

| System Area | Documentation Location | Required Contents |
|:---|:---|:---|
| **Architecture & Decisions** | `docs/architecture/`, `docs/adr/` | Architectural Decision Records (ADRs) explaining domain boundaries, ECS design, event bus, flow-field algorithms. |
| **Combat & Progression** | `docs/combat/` | Damage formulas, rock-paper-scissors archetype matrices, XP curves, veterancy rank thresholds. |
| **Economy & Technology** | `docs/economy/` | Resource gather rates, building costs, tech tree dependency graphs, era progression charts. |
| **Testing & Quality** | `docs/testing/` | Pre-implementation Test Cases Catalogs (`test_cases_catalog_S<YY>.md`), headless test coverage reports. |
| **User Experience & HUD** | `docs/ux/` | HUD layouts, drag-selection mechanics, minimap behavior, hotkey mapping reference. |
| **Release & Packaging** | `docs/release/` | Godot export settings, Windows packaging checklists, checksums, and version changelogs. |

---

## 3. Mandatory Discipline: No Fabricated Documentation

- **No Placeholder Contracts:** Do not document web APIs, REST endpoints, microservices, or database schemas that do not exist in this local-first C# desktop application.
- **Real Measured Evidence:** Test execution summaries and performance benchmarks must report actual local numbers from test runs, not speculative estimates.
- **Accurate Code Links:** All file references must use clickable markdown links (e.g. [`UnitProgressionSystem.cs`](file:///c:/Workspace/CrownConquest/src/Simulation/Combat/UnitProgressionSystem.cs)).

---

## 4. Sprint Walkthrough (`walkthrough.md`)

At the conclusion of every sprint, author a concise `walkthrough.md` documenting:
- **Sprint Goal & Scope:** What feature or milestone was implemented.
- **Key Technical Changes:** Implemented classes, systems, and data schemas.
- **Verification Evidence:** Executed automated test commands, passing counts, and headless simulation validation results.
- **Playable Demonstration Steps:** How to launch and observe the sprint scenario in Godot.
- **Deferred Items & Next Sprint:** Any non-blocking observations queued for future sprints.
