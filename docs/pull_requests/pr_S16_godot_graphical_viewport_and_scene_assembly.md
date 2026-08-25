# Pull Request: Sprint 16 — Godot 2D Graphical RTS Viewport, Scene Assembly & v1.1.0 Packaging

## 1. Overview & Summary
This Pull Request delivers **Sprint 16** (Phase 12), the final milestone in the Crown & Conquest roadmap. It connects the authoritative C# simulation engine to Godot 4's 2D graphical rendering viewport, introduces the interactive `scenes/main.tscn` and `scenes/main.gd` game scene, renders 2D unit tokens with Celtic/Roman faction heraldry and veterancy badges, enables mouse drag box selection, minimap radar, and packages standalone Windows installers for **v1.1.0**.

---

## 2. Key Deliverables & Systems Implemented

### 2.1 Godot 2D Graphical RTS Viewport (`scenes/main.tscn` & `scenes/main.gd`)
- Real-time 2D graphical rendering canvas drawing terrain grid, forests, stone quarries, gold mines, and settlement buildings.
- Animated unit tokens with Celtic Blue (`#2563EB`) and Roman Red (`#DC2626`) faction heraldry, directional headings, selection rings, health bars, and veterancy rank badge stars.
- Floating combat damage text popups and golden `LEVEL UP!` announcements.
- Green translucent mouse drag marquee box for multi-unit squad selection.

### 2.2 Interactive RTS HUD & Controls
- **Top Resource Bar:** Real-time counters for Food, Wood, Gold, Stone, Iron, Population, and Era indicator.
- **Minimap Radar:** Interactive radar blips showing player/enemy unit positions and structures.
- **Unit Selection Card:** Unit stats (HP, Damage, Armor, Speed, Level, Rank) and Hero active ability buttons (`[F1] War Cry`, `[F2] Heroic Strike`).
- **Camera Navigation:** Keyboard WASD / arrow panning, mouse wheel smooth zoom ($0.5\times$ to $2.5\times$), right-click move/attack/gather commands.

### 2.3 Standalone Desktop Launcher (`src/CrownConquest.Desktop/Program.cs`)
- Added Option `[1] Launch Full 2D Graphical RTS Game Window (Godot 4 Viewport)`.
- Automatically locates and launches the Godot engine viewport for instant visual gameplay.

### 2.4 Distribution Packages & Installers (`v1.1.0`)
- `CrownConquest_1.1.0_x64_en-US.msi` (25.8 MB Windows MSI Installer)
- `CrownConquest_1.1.0_x64-setup.exe` (68.5 MB Standalone Native Executable)
- `CrownConquest_1.1.0_win-x64.zip` (30.1 MB Portable Zip Archive)
- `checksums.txt` (SHA-256 integrity verification manifest)

---

## 3. Verification & Test Results
- **Cumulative Unit, Simulation & Presentation Tests:** **339 / 339 Passed (100% Green)**
- **Deterministic Replay Parity:** Verified 1,000-tick bit-for-bit checksum equality.
- **Godot 4 Headless Verification:** `godot --headless --quit` completed with 0 errors.
- **Zero Warnings / Zero Errors:** Clean build on C# 12 / .NET 8.
