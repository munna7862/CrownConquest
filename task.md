# Sprint 16: Godot Visual Scene Assembly, 2D Graphical RTS Viewport & Desktop Packaging

## Backlog Stories & Ownership Matrix

| Story ID | Title | Owner Persona | Status |
|:---|:---|:---|:---|
| `CNC-1601` | Godot Main Scene & 2D Graphical Viewport (`scenes/main.tscn`) — Root `GameView : Node2D`, layered canvas structure (Terrain, Grid, Entities, VFX, HUD CanvasLayer) | SDE / ARCH | [ ] Pending |
| `CNC-1602` | 2D Visual Unit Rendering & Faction Heraldry — Visual unit tokens, Celtic Blue (`#2563EB`) vs Roman Red (`#DC2626`), directional heading indicators, health bars, veterancy rank badges | Art / SDE | [ ] Pending |
| `CNC-1603` | Settlement Buildings & Resource Node Visuals — Graphical rendering for Town Centers, Barracks, Blacksmiths, Stables, Towers, Stone Walls, and 5-resource nodes | Art / SDE | [ ] Pending |
| `CNC-1604` | Interactive RTS HUD & Mouse Drag Selection Box — Top Resource Bar (Food, Wood, Gold, Stone, Iron, Pop, Era), Minimap radar with blips, selection card with unit stats/XP, green drag selection box | UI / SDE | [ ] Pending |
| `CNC-1605` | Command Card & RPG Hero Ability Buttons — Interactive Command Card (Move, Attack, Stop, Patrol, Formations) and Hero ability buttons (War Cry, Heroic Strike) with live cooldown sweeps | UI / SDE | [ ] Pending |
| `CNC-1606` | 2D RTS Camera Controller & Input Navigation — WASD/arrow key panning, mouse wheel smooth zoom (0.5x–3.0x), middle-mouse drag, edge panning, and right-click order dispatch | SDE | [ ] Pending |
| `CNC-1607` | Combat Visual Impact VFX & Floating Text — Floating damage numbers, combat hit particles, level-up golden aura rings, building construction puffs, arrow trajectories | Art / SDE | [ ] Pending |
| `CNC-1608` | Graphical Presentation & Input Integration Tests — Automated test suite verifying coordinate projections, input command bridge, HUD viewmodel binding, and simulation sync | SDET / QA | [ ] Pending |
| `CNC-1609` | Godot Desktop Export & Windows Release Packaging (v1.1.0) — Export standalone graphical executable (`CrownConquest.exe` + `.pck`), package WiX MSI installer (`CrownConquest_1.1.0_x64_en-US.msi`), checksums, and GitHub Release v1.1.0 | Release / DO | [ ] Pending |

---

## Sprint Checklist

- [ ] **Stage 1: Scrum Master (SM) — Backlog Deconstruction & Planning**
  - [x] Read Sprint 16 specification from [`planning/sprints/SPRINT-16-GODOT-GRAPHICAL-VIEWPORT-AND-SCENE-ASSEMBLY.md`](file:///c:/Workspace/CrownConquest/planning/sprints/SPRINT-16-GODOT-GRAPHICAL-VIEWPORT-AND-SCENE-ASSEMBLY.md).
  - [x] Initialize `task.md` with story matrix and checklist.
  - [ ] Check out feature branch `feature/sprint-16-godot-graphical-viewport`.
  - [ ] Verify baseline tests pass (`dotnet test`: 331/331 green).
  - [ ] Output Stage 1 Handoff Report: `SM -> GD`.

- [ ] **Stage 2: Game Director & Domain Architect (GD/ARCH) — Design Alignment**
  - [ ] Define visual layout and scene graph hierarchy for `scenes/main.tscn`.
  - [ ] Define 2D rendering specifications for unit tokens, faction heraldry, and veterancy badges.
  - [ ] Define building and resource node visual representations and placement grids.
  - [ ] Define HUD layout (TopBar, Minimap radar, bottom unit status card, command card).
  - [ ] Define Hero ability button styling, hotkeys (F1–F4), and cooldown sweep shaders.
  - [ ] Define RTS camera navigation parameters, viewport bounds clamping, and input bindings.
  - [ ] Define combat impact VFX (floating damage text, attack flashes, level-up aura).
  - [ ] Output Stage 2 Handoff Report: `ARCH -> SDET`.

- [ ] **Stage 3: QA & SDET Architect (SDET) — Pre-Implementation Test Catalog**
  - [ ] Author [`docs/testing/test_cases_catalog_S16.md`](file:///c:/Workspace/CrownConquest/docs/testing/test_cases_catalog_S16.md) with comprehensive Tier 1–4 test cases.
  - [ ] Output Stage 3 Handoff Report: `SDET -> SDE`.

- [ ] **Stage 4: Dev Architect & Gameplay SDE (SDE) — Implementation**
  - [ ] Implement `scenes/main.tscn` Godot 2D scene and root node controller.
  - [ ] Implement 2D unit token renderer with health bars and rank badges.
  - [ ] Implement building and resource node renderer.
  - [ ] Implement interactive HUD (Top Resource Bar, Minimap, Selection Panel).
  - [ ] Implement Command Card and Hero ability buttons with active cooldowns.
  - [ ] Implement RTS Camera navigation (WASD, mouse zoom, drag selection box).
  - [ ] Implement combat visual impact effects and floating damage text.
  - [ ] Verify zero build warnings/errors (`dotnet build --warnaserror`).
  - [ ] Output Stage 4 Handoff Report: `SDE -> PERF`.

- [ ] **Stage 5: Performance Officer (PERF) — Zero-Allocation Hot-Loop Audit**
  - [ ] Audit rendering canvas `_Draw()` and viewport update loops for zero per-frame dynamic heap allocations.
  - [ ] Validate 60 FPS frame time (< 16.6ms) and memory footprint (< 500MB).
  - [ ] Output Stage 5 Handoff Report: `PERF -> SDET`.

- [ ] **Stage 6: QA & SDET Architect (SDET) — Test Automation Quality Gate**
  - [ ] Author comprehensive unit, invariant, integration, and scenario tests.
  - [ ] Execute `dotnet test` and confirm 100% green pass rate with zero regressions.
  - [ ] Verify 1,000-tick deterministic replay checksum equality.
  - [ ] Output Stage 6 Handoff Report: `SDET -> GD`.

- [ ] **Stage 7: Game Director & Product Owner (GD/PO) — Acceptance Review**
  - [ ] Validate full graphical gameplay in Godot viewport (selection, movement, combat, abilities, HUD).
  - [ ] Approve sprint for release.
  - [ ] Output Stage 7 Handoff Report: `GD -> DO`.

- [ ] **Stage 8: DevOps & Release Engineer (DO) — Release, Branch & Pull Request**
  - [ ] Export standalone Windows x64 graphical build (`CrownConquest.exe` + `.pck`).
  - [ ] Build WiX MSI installer `CrownConquest_1.1.0_x64_en-US.msi`.
  - [ ] Generate `CrownConquest_1.1.0_x64-setup.exe` and `CrownConquest_1.1.0_win-x64.zip`.
  - [ ] Generate SHA-256 `checksums.txt`.
  - [ ] Create GitHub Release `v1.1.0` with assets attached.
  - [ ] Create Pull Request and link in `task.md`.
  - [ ] Conclude with `<!-- GOAL_COMPLETE -->`.

---

## Sprint Review Comments & Refinement Loop

*(No review comments yet)*
