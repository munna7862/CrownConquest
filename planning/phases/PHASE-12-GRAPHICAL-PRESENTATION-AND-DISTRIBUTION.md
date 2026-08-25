# Phase 12 — Full Graphical Presentation & Desktop Game Distribution

## 1. Objective
Bridge the authoritative, deterministic C# simulation engine into Godot 4's 2D graphical rendering viewport. Assemble the full interactive RTS gameplay scene (`scenes/main.tscn`), render visual unit tokens with faction colors and veterancy badges, wire up the RTS HUD and minimap radar, and package standalone Windows x64 desktop installers (`.msi`, `.exe`, `.zip`) for public distribution.

---

## 2. Core Presentation Systems & Architecture

### 2.1 Graphical Scene Graph (`scenes/main.tscn`)
- **Root Controller:** `GameView : Node2D` orchestrating the 20Hz simulation tick loop.
- **Layer 1 (Terrain & Grid Canvas):** 2D tilemap background with elevation contours, forests, marshlands, and military roads.
- **Layer 2 (Structures & Resource Nodes):** Graphical Town Centers, Barracks, Blacksmiths, Stables, Stone Walls, and harvestable resource nodes.
- **Layer 3 (Unit Entities & Formations):** Animated unit tokens with Celtic Blue (`#2563EB`) and Roman Red (`#DC2626`) faction colors, heading direction indicators, selection rings, and dynamic health bars.
- **Layer 4 (Combat VFX & Floating Numbers):** Floating damage popups, combat hit flashes, level-up golden aura rings, and projectile trajectories.
- **Layer 5 (HUD CanvasLayer):** Top Resource Bar, Minimap Radar, Unit Status Card, and Command Action Card.

### 2.2 Interactive RTS HUD & Viewport Controls
- **Top Resource Bar:** Real-time counters for Food, Wood, Gold, Stone, Iron, Population cap, and Civilization Era indicator.
- **Minimap Radar:** 2D interactive radar showing terrain topology, camera viewport bounds, and live unit/building blips.
- **Unit Selection Card:** Displays unit portrait, health/armor stats, kill count, and veterancy rank badges (Bronze, Silver, Gold, Legendary Crown).
- **Command Action Card:** Interactive buttons for Move, Attack-Move, Stop, Patrol, Formations (Line, Wedge, Shield Wall), and RPG Hero abilities (War Cry, Heroic Strike) with live cooldown sweeps.
- **Mouse Drag Selection:** Green translucent bounding box for fluid multi-unit group selection.

### 2.3 2D RTS Camera Navigation
- **Panning:** Keyboard WASD, arrow keys, screen edge panning, and middle-click drag.
- **Zooming:** Smooth mouse wheel zooming ($0.5\times$ to $3.0\times$) centered on cursor.
- **Command Dispatch:** Right-click context orders (Move to ground, Attack enemy, Gather from resource node).

---

## 3. Distribution & Packaging Pipeline

### 3.1 Godot 4 (.NET Mono) Desktop Export
- Standalone Windows x64 release executable (`CrownConquest.exe` + `.pck`).
- Bundled native libraries and self-contained .NET 8 runtime.

### 3.2 WiX Toolset Windows Installer (`.msi`)
- `CrownConquest_1.1.0_x64_en-US.msi` installing to `Program Files\CrownConquest`.
- Automatic Windows Start Menu shortcuts and desktop icons.

### 3.3 Portable Packages & Checksums
- `CrownConquest_1.1.0_x64-setup.exe` (Self-extracting standalone executable).
- `CrownConquest_1.1.0_win-x64.zip` (Portable zip archive).
- `checksums.txt` (SHA-256 cryptographic verification manifest).
- GitHub Release `v1.1.0` automated publication.

---

## 4. Definition of Done (DoD)
- [x] Launching `CrownConquest.exe` opens a full 1920x1080 graphical RTS window (no console terminal).
- [x] Full RTS control loop: Mouse box selection, right-click move/attack/gather, and hotkey controls.
- [x] Units display real-time health bars, combat animations, and veterancy rank badges.
- [x] Cumulative test suite remains 100% green (`dotnet test` 331+ tests passing).
- [x] Performance certified at 60 FPS ($< 16.6\text{ ms}$ frame budget) and $< 500\text{ MB}$ memory footprint.
- [x] Release packages uploaded to GitHub Release `v1.1.0` with verified SHA-256 checksums.
