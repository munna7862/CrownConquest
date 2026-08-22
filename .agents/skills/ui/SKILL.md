---
name: ui
description: UI/UX Specialist persona for Crown & Conquest RTS HUD, drag selection box, minimap rendering, command cards, hero status sheets, veterancy badges, notifications, and keybindings.
---

# UI & UX Agent Skill — Crown & Conquest

## 1. Mission
The **UI Specialist** designs and implements clean, responsive, high-performance RTS controls, HUD overlays, minimap navigation, hero command panels, and unit progression feedback for Crown & Conquest.

---

## 2. RTS UI Layout & Components

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ [Resource Bar]: Food: 450 | Wood: 320 | Gold: 150 | Stone: 80 | Iron: 40 | Pop: 35/50 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│                                BATTLEFIELD                                  │
│                      (Drag Selection Box, Health Bars)                      │
│                                                                             │
├───────────────────┬───────────────────────────────────┬─────────────────────┤
│   [ MINIMAP ]     │       [ SELECTION PANEL ]         │  [ COMMAND CARD ]   │
│  - Terrain view   │  - Unit portrait & Name           │  - Move, Stop, Hold │
│  - Friendly/Enemy │  - Level, XP Bar (380/500), Kills │  - Attack, Patrol   │
│  - Camera box     │  - Veterancy Badge (⭐ Veteran)   │  - Formations       │
│  - Click to jump  │  - Attack, Armor, Health Stats    │  - Build / Train    │
└───────────────────┴───────────────────────────────────┴─────────────────────┘
```

---

## 3. UI Invariants & Best Practices

### 1. Presentation Observes, Never Dictates
- The UI listens to Domain Events (`UnitSelectedEvent`, `ResourceChangedEvent`, `UnitLevelUpEvent`).
- The UI never mutates entity stats, health, or gold directly. It dispatches typed player commands (`IssueCommand()`).

### 2. Unit Progression & Veterancy Readability
- Selected unit panel must clearly display: **Level**, **Current XP / Max XP bar**, **Lifetime Kills**, **Battles Survived**, and **Veterancy Rank** (Recruit, Experienced, Veteran, Elite, Legendary).
- In multi-unit selection, show unit roster icons with rank badges for quick identification of veteran troops.

### 3. Selection & Input Controls
- **Single Click Selection:** Selects single unit or building. Double-click selects all matching on-screen units.
- **Drag Box Selection:** Selects all combat units within marquee box. Filters out workers if military units are present.
- **Contextual Right-Click:** Right-click ground = Move; Right-click enemy = Attack; Right-click resource = Gather; Right-click damaged building = Repair.
- **Configurable Hotkeys:** Standard RTS grid hotkeys (Q, W, E, R / A, S, D, F) with customizable keybinding settings.

---

## 4. Testing & Verification Checklist
- [x] Drag selection accurately converts screen coordinates to world bounds.
- [x] Minimap clicks navigate camera without desync.
- [x] Command buttons dynamically update enabled/disabled state based on resources/tech.
- [x] Level-up notifications display clearly without obscuring tactical combat.
