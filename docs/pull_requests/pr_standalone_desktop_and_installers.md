# Pull Request: Standalone Windows Desktop Launcher, WiX MSI Installers, and Release Guide

## 1. Overview & Summary
This PR introduces the standalone Windows Desktop runner project (`CrownConquest.Desktop`), WiX Toolset packaging configuration for `.msi` installers, self-contained Windows x64 build artifacts, and detailed release documentation on how to install, launch, and play Crown & Conquest on any Windows laptop.

---

## 2. What Changed

### 2.1 Standalone Windows Desktop Launcher (`src/CrownConquest.Desktop/`)
- **`CrownConquest.Desktop.csproj`**: Standalone executable project with `.NET 8` targeting Windows x64, copying data definitions and documentation assets automatically.
- **`Program.cs`**:
  - Interactive Desktop Launcher with ASCII artwork banner and menu selector (Modes 1–9).
  - CLI argument parsing for `--headless`, `--smoke-test`, `--benchmark`, `--validate-env`, `--scenario`, `--seed`, and `--ticks`.
  - Mode 1: Live Interactive Skirmish Match (Celtic Kingdom vs Roman Empire).
  - Mode 2: Tactical Combat Arena (Spearmen Formations vs Cavalry Charge).
  - Mode 3: Settlement Economy & Worker Gathering (5-Resource Model).
  - Mode 4: Siege Warfare Citadel Assault (Catapults & Wall Breaches).
  - Mode 5: RPG Hero Progression & Ability Showcase (Brennus / Lord Aldric).
  - Mode 6: Civilization Era & Tech Tree Advance (Classical Era).
  - Mode 7: Clean-Machine Hardware & Runtime Diagnostics.
  - Mode 8: High-Density 1,000-Unit Performance Benchmark.
  - Mode 9: In-game Player Controls & User Manual.

### 2.2 Windows Installer Authoring (`installer/Package.wxs`)
- WiX Toolset v5/v7 configuration to generate `CrownConquest_1.0.0_x64_en-US.msi` (25.8 MB).
- Automatically installs binaries, game definitions, and documentation to `Program Files\CrownConquest` and registers Start Menu shortcuts.

### 2.3 Comprehensive Documentation & Release Notes (`docs/shipping/RELEASE_NOTES_v1.0.0.md`)
- Detailed step-by-step installation instructions for `.msi`, standalone `.exe`, and portable `.zip`.
- Complete gameplay guide explaining individual unit progression (Levels 1–9+), 5-resource economy model, combat formations, and RPG hero commanders.
- Complete keyboard & mouse player controls table.
- Published release download links and SHA-256 verification checksums.

---

## 3. How to Launch & Play the Game

### Method A: Windows Installer (`.msi`) — Standard Installation
1. Download [`CrownConquest_1.0.0_x64_en-US.msi`](https://github.com/munna7862/CrownConquest/releases/download/v1.0.0/CrownConquest_1.0.0_x64_en-US.msi) from GitHub Releases.
2. Double-click the `.msi` file and follow the standard Windows setup wizard.
3. Open the **Windows Start Menu**, search for **Crown & Conquest**, and launch the game.

### Method B: Standalone Portable (`.exe` / `.zip`) — Zero Prerequisites
1. Download [`CrownConquest_1.0.0_x64-setup.exe`](https://github.com/munna7862/CrownConquest/releases/download/v1.0.0/CrownConquest_1.0.0_x64-setup.exe) or extract [`CrownConquest_1.0.0_win-x64.zip`](https://github.com/munna7862/CrownConquest/releases/download/v1.0.0/CrownConquest_1.0.0_win-x64.zip).
2. Double-click `CrownConquest.exe`.
3. Select from Modes 1–9 on the interactive launcher menu.

---

## 4. Player Controls Reference

| Input | In-Game Action |
|:---|:---|
| **Left Click** | Select unit, hero, structure, or resource node |
| **Left Click + Drag** | Drag box selection of multiple military units |
| **Right Click (Ground)** | Order selected units to move to location |
| **Right Click (Enemy)** | Order selected units to attack enemy soldier or fortification |
| **Right Click (Resource)** | Assign selected workers to gather food, wood, gold, stone, or iron |
| **Shift + Right Click** | Queue sequential movement or attack waypoints |
| **`1` – `9`** | Select assigned control group squad |
| **`Ctrl + 1` – `9`** | Assign current selection to control group |
| **`F1` – `F4`** | Cast active Hero RPG abilities (War Cry, Heroic Strike, Rally, Cleave) |
| **`Tab`** | Cycle selection through unit types in active squad |
| **`Space`** | Center camera on recent battlefield alert |
| **`Esc`** | Deselect active units / Open settings menu |

---

## 5. Verification & Test Results
- `dotnet build --warnaserror`: **0 Warnings, 0 Errors**
- `dotnet test`: **331 / 331 Tests Passed**
- Standalone `.exe` verified with `--smoke-test` and `--validate-env` (Exit Code 0).
- WiX MSI build tested and validated on Windows 10/11 x64.
