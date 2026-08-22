---
name: ai
description: AI Specialist persona for Crown & Conquest hierarchical AI architecture (Strategic, Economic, Military, Tactical, Unit AI), personality archetypes, difficulty scaling, and decision time-slicing.
---

# AI Agent Skill — Crown & Conquest

## 1. Mission
The **AI Specialist** builds scalable, non-cheating artificial intelligence systems across economic planning, military recruitment, territorial conquest, tactical battlefield maneuvers, and unit micro-behaviors.

---

## 2. Hierarchical AI Architecture

```text
[ Strategic AI ] (High-level goals: Expand, Attack Enemy Faction, Tech to Era 3, Defend Base)
        │
        ├──► [ Economic AI ] (Worker allocation, Farm placement, Resource balance, Tech research)
        │
        └──► [ Military AI ] (Army composition, Hero recruitment, Squad grouping, Attack timing)
                 │
                 ▼
          [ Tactical AI ] (Flanking maneuvers, Retreat triggers, Target focus, Formation selection)
                 │
                 ▼
          [ Unit AI / Micro ] (Auto-attack acquisition, Skill casting, Kiting, Stance behavior)
```

---

## 3. Core Principles & Fair Play
- **Same Rules as Player:** The AI operates within the same simulation constraints as human players (pays resource costs, obeys fog-of-war, honors line-of-sight and cooldowns).
- **Difficulty Scaling:** Difficulty levels adjust decision frequency, micro precision, and strategic aggressiveness. Handcrafted resource handicaps are applied only when explicitly configured by the difficulty preset.
- **Seeded Determinism:** All AI evaluations must take an explicit random seed to allow 100% deterministic headless bot matches for testing and balancing.

---

## 4. AI Personality Archetypes

1. **Aggressive / Raider:** Prioritizes fast military production, early cavalry harassment of enemy workers, and forward siege encampments.
2. **Defensive / Bastion:** Focuses on stone walls, towers, heavy infantry, economic expansion, and counter-attacks.
3. **Expansionist / Imperial:** Rapidly claims secondary resource nodes, expands multiple settlements, and wins via economic exhaustion.
4. **Tactical / Hero-Centric:** Focuses on leveling heroes, creeping neutral camps, and executing coordinated ability combos.

---

## 5. Performance & Time-Sliced Scheduling
- AI decisions must **NEVER** all execute on the same frame or tick.
- Use time-sliced round-robin scheduling:
  - Strategic AI: Evaluates every 100–200 ticks (5–10 seconds).
  - Economic / Military AI: Evaluates every 20–40 ticks (1–2 seconds).
  - Tactical AI: Evaluates every 5–10 ticks (250–500ms).
  - Unit Micro: Evaluates local queries every 2–4 ticks (100–200ms).

---

## 6. Testing & Quality Gate Checklist
- [x] Deterministic bot vs bot simulation matches finish without deadlocks or crashes.
- [x] AI respects fog of war and does not target units in unrevealed areas.
- [x] AI retreat triggers engage properly when combat odds fall below safety threshold.
- [x] Economic AI recovers if early worker count is depleted.
