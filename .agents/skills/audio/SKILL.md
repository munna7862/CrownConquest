---
name: audio
description: Audio Specialist persona for Crown & Conquest dynamic adaptive soundtrack, unit voice barks, combat impact SFX, positional 2D/3D audio mix, and audio bus management.
---

# Audio Agent Skill — Crown & Conquest

## 1. Mission
The **Audio Specialist** creates an immersive, readable audio environment for Crown & Conquest, including adaptive combat music transitions, responsive unit voice acknowledgments, punchy combat sound effects, and spatial audio mixing.

---

## 2. Audio Architecture & Decoupling

```text
Domain Event Bus (UnitAttacked, UnitLevelUp, BuildingComplete, HeroCast)
       │
       ▼
Audio Event Dispatcher (AudioCoordinator / SoundManager)
       │
       ▼
Audio Channels & Mix Buses:
 ├── [ Master Bus ]
 ├── [ Music Bus ] ──► (Dynamic Adaptive Ambient/Combat Crossfading)
 ├── [ SFX Bus ]   ──► (Combat Hits, Arrows, Siege, Destruction)
 ├── [ Voice Bus ] ──► (Unit Selection & Move Barks, Hero Lines)
 └── [ UI Bus ]    ──► (Button Clicks, Notifications, Alerts)
```

---

## 3. Core Audio Systems

### 1. Dynamic Adaptive Soundtrack
- **Ambient State:** Calm, atmospheric orchestral/folk tracks playing during economic development.
- **Combat State:** High-energy percussion and brass swelling seamlessly when unit engagements cross combat intensity thresholds.
- **Victory / Defeat:** Triumphant fanfares and somber defeat stings.

### 2. Unit Voice Barks & Faction Responses
- Barks triggered on player commands:
  - `Select`: "Ready for battle!", "Sire?", "Legion reporting!"
  - `Move`: "Marching!", "Onward!", "Double time!"
  - `Attack`: "For the King!", "Charge!", "No mercy!"
  - `Under Attack Warning`: "Our settlement is under siege!"

### 3. Combat SFX & Concurrency Limiting
- Limit simultaneous identical sound instances (e.g. max 4 simultaneous sword clangs per frame) to prevent audio bus clipping and ear fatigue during 100-man melee battles.
- Positional audio falloff: Sounds attenuate with distance from camera viewport center.

---

## 4. Testing & Verification Checklist
- [x] Audio pool cleanly reuses `AudioStreamPlayer` nodes without memory leaks.
- [x] Volume levels and bus assignments adhere to master gain constraints.
- [x] Missing audio files fail gracefully with a log warning without crashing the game.
