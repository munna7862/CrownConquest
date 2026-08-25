# Crown & Conquest — Release Notes (v1.1.0 Graphical Edition)

## 1. Executive Summary
**Crown & Conquest v1.1.0** delivers the complete **Godot 4 2D Graphical RTS Viewport** and interactive desktop game experience! Players can now launch the full visual game with animated unit tokens, faction heraldry, dynamic RTS HUD, minimap radar, drag-selection box, combat hit animations, floating damage numbers, and sound effects.

---

## 2. Release Downloads & Packages

| Asset File | Format | Description |
|:---|:---:|:---|
| [`CrownConquest_1.1.0_x64_en-US.msi`](https://github.com/munna7862/CrownConquest/releases/download/v1.1.0/CrownConquest_1.1.0_x64_en-US.msi) | **Windows MSI (25.8 MB)** | Full Windows Installer. Installs to `Program Files\CrownConquest` and registers Start Menu shortcuts. |
| [`CrownConquest_1.1.0_x64-setup.exe`](https://github.com/munna7862/CrownConquest/releases/download/v1.1.0/CrownConquest_1.1.0_x64-setup.exe) | **Standalone EXE (68.5 MB)** | Self-contained executable with zero runtime dependencies. Double-click to launch the GUI game! |
| [`CrownConquest_1.1.0_win-x64.zip`](https://github.com/munna7862/CrownConquest/releases/download/v1.1.0/CrownConquest_1.1.0_win-x64.zip) | **Portable ZIP (30.1 MB)** | Portable archive. Extract and play on any laptop without administrator privileges. |
| [`checksums.txt`](https://github.com/munna7862/CrownConquest/releases/download/v1.1.0/checksums.txt) | **SHA-256 Manifest** | Cryptographic integrity verification hashes. |

---

## 3. How to Launch & Play the Game

### Method 1: Windows Installer (`.msi`) — Recommended
1. Download **`CrownConquest_1.1.0_x64_en-US.msi`**.
2. Run the installer setup wizard.
3. Launch **Crown & Conquest** from your Windows Start Menu.
4. Select **`[1] Launch Full 2D Graphical RTS Game Window`** to open the real-time visual viewport!

### Method 2: Standalone Executable (`.exe` / `.zip`)
1. Download **`CrownConquest_1.1.0_x64-setup.exe`** or unzip **`CrownConquest_1.1.0_win-x64.zip`**.
2. Double-click **`CrownConquest.exe`**.
3. Press **`1`** on the menu to launch the graphical game window.

### Method 3: Direct Godot Engine Launch
```powershell
godot.exe --path "C:\Workspace\CrownConquest"
```

---

## 4. In-Game Controls Reference

| Input | Action |
|:---|:---|
| **Left Click** | Select single unit, building, or resource node |
| **Left Click + Drag** | Box select multiple units with green translucent selection marquee |
| **Right Click (Ground)** | Order selected units to move to location in line formation |
| **Right Click (Enemy)** | Order selected units to attack enemy soldier or fortification |
| **Right Click (Resource)** | Assign workers to gather Food, Wood, Gold, Stone, or Iron |
| **`W` / `A` / `S` / `D` or Arrows** | Pan camera around battlefield |
| **Mouse Wheel** | Smooth zoom in / zoom out ($0.5\times$ to $2.5\times$) |
| **`F1` / `F2`** | Cast active Hero RPG abilities (*War Cry*, *Heroic Strike*) |
| **`1` / `2` / `3`** | Change active squad formation (*Line*, *Wedge*, *Shield Wall*) |

---

## 5. Verification & SHA-256 Checksums

```text
48e061b701daecec07cfda67f9ab955e2db2ae585ca2bf26b51046593ca9ce9d  CrownConquest_1.1.0_x64-setup.exe
fd2d8d3d2e927cfbc0ce7f6328e7e9db28055e33a5188607c0d731f715c97b52  CrownConquest_1.1.0_x64_en-US.msi
1cf4c11ebded64140d67f614bb260d1dc735d50acecb4ef7f87eca0c53084f0c  CrownConquest_1.1.0_win-x64.zip
```
