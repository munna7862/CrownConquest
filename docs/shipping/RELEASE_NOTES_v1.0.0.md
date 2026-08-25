# Crown & Conquest — Release Notes (v1.0.0 Release Candidate)

## 1. Executive Summary
**Crown & Conquest v1.0.0** is a 100% local-first, deterministic hybrid RTS/RPG built with Godot 4 and C# (.NET 8). Command vast armies across five historical civilization eras, construct fortified keeps, recruit heroic commanders with active talents, and command individual soldiers who gain persistent combat veterancy on the battlefield.

---

## 2. Release Downloads & Distribution Packages

| File Asset | Format | Description |
|:---|:---:|:---|
| [`CrownConquest_1.0.0_x64_en-US.msi`](https://github.com/munna7862/CrownConquest/releases/download/v1.0.0/CrownConquest_1.0.0_x64_en-US.msi) | **Windows MSI (25.8 MB)** | Full Windows Installer. Installs to `Program Files\CrownConquest` and registers Start Menu shortcuts. |
| [`CrownConquest_1.0.0_x64-setup.exe`](https://github.com/munna7862/CrownConquest/releases/download/v1.0.0/CrownConquest_1.0.0_x64-setup.exe) | **Standalone EXE (68.4 MB)** | Self-contained executable with zero runtime dependencies. Double-click and play immediately. |
| [`CrownConquest_1.0.0_win-x64.zip`](https://github.com/munna7862/CrownConquest/releases/download/v1.0.0/CrownConquest_1.0.0_win-x64.zip) | **Portable ZIP (30.0 MB)** | Portable archive. Extract to any folder or USB flash drive and play without administrator rights. |
| [`checksums.txt`](https://github.com/munna7862/CrownConquest/releases/download/v1.0.0/checksums.txt) | **SHA-256 Manifest** | Cryptographic integrity verification hashes. |

---

## 3. How to Launch & Play on Your Laptop

### Method 1: Standard Windows Installer (Recommended)
1. Download **`CrownConquest_1.0.0_x64_en-US.msi`**.
2. Double-click the `.msi` file and follow the standard Windows setup wizard.
3. Open the **Windows Start Menu**, search for **Crown & Conquest**, and launch the game.

### Method 2: Standalone Portable (.exe / .zip)
1. Download **`CrownConquest_1.0.0_x64-setup.exe`** or extract **`CrownConquest_1.0.0_win-x64.zip`**.
2. Double-click **`CrownConquest.exe`** to launch the interactive desktop launcher.
3. Choose your desired game mode from the main menu (Modes 1–9).

### Method 3: Development / Godot 4.3 (.NET Edition)
1. Clone the repository and ensure [.NET 8 SDK](https://dotnet.microsoft.com/download) is installed.
2. Open **Godot 4.3 (.NET)**, click **Import**, and select `project.godot`.
3. Press **`F5`** to launch the presentation viewport.

---

## 4. Gameplay Instructions & Core Mechanics

### 4.1 Signature Mechanic: Individual Unit Progression
Every combat unit gains battlefield experience (Kill XP) for defeating enemies:
- **Level 1–2 (Recruit):** Base unit stats.
- **Level 3–4 (Experienced):** $+10\%$ Health, $+5\%$ Damage, $+1$ Armor (Bronze rank badge).
- **Level 5–6 (Veteran):** $+25\%$ Health, $+15\%$ Damage, $+2$ Armor, $+10\%$ Speed (Silver rank badge).
- **Level 7–8 (Elite):** $+45\%$ Health, $+25\%$ Damage, $+3$ Armor, Morale Aura (Gold rank badge).
- **Level 9+ (Legendary):** $+70\%$ Health, $+40\%$ Damage, $+5$ Armor, Fear Immunity (Heroic Crown).

### 4.2 Economy & Resource Gathering
Manage 5 strategic resources:
- **Food:** Harvested from farms and berry bushes; trains villagers and military forces.
- **Wood:** Chopped from forests; constructs buildings, palisades, and archer bows.
- **Gold:** Mined from veins; powers hero abilities, mercenaries, and elite upgrades.
- **Stone:** Quarried from rock outcrops; constructs fortified stone walls, towers, and keeps.
- **Iron:** Extracted from deposits; crafts plate armor, swords, and siege machinery.

### 4.3 Tactical Formations & Morale
- **Line Formation:** Maximizes front-line melee engagement and archer firing arcs.
- **Wedge Formation:** Maximizes cavalry charge momentum and penetration bonus.
- **Square / Shield Wall:** Provides $+3$ armor and cavalry charge resistance.
- **Morale & Routing:** Units take morale damage when flanked or surrounded. Breaking morale causes units to route; hero auras prevent and rally routing troops.

### 4.4 RPG Hero Commanders
Recruit commanders with active spellcasting, squad attachment, and talent trees:
- **War Cry:** AoE burst damage and temporary ally attack speed buff.
- **Heroic Strike:** Massive single-target crushing blow against enemy leaders.
- **Shield Wall Aura:** Passive $+2$ armor and $+15\%$ damage resistance to attached squad units.

---

## 5. Player Controls & Keybindings Reference

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

## 6. Command-Line (CLI) Switches Reference

```powershell
# Launch automated headless smoke test (600 ticks, full lifecycle verification)
CrownConquest.exe --smoke-test --seed 42

# Run 1,000-unit high-density performance benchmark
CrownConquest.exe --benchmark 1000

# Execute clean-machine hardware and runtime diagnostics
CrownConquest.exe --validate-env
```

---

## 7. Package Verification Checksums (SHA-256)

```text
824196d7bec3520f10a37c13bda8d29a0b947f54f51f6e298114ee42559a3e8d  CrownConquest_1.0.0_x64-setup.exe
f9724655b3bc13c803dca513aad967ea3155ee2aea10cf8b8c04dfacfc63fe4b  CrownConquest_1.0.0_x64_en-US.msi
49adb2a18ca069bbb0c6c11dfada8093ed7f2fde59a0c427cb866029bc19b63f  CrownConquest_1.0.0_win-x64.zip
```
