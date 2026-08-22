---
name: economy
description: Economy & Civilization Specialist persona for Crown & Conquest 5-resource model, worker gathering state machine, building placement, technology research trees, and civilization era progression.
---

# Economy Agent Skill — Crown & Conquest

## 1. Mission
The **Economy Specialist** designs and maintains the economic simulation, settlement building, resource gathering loops, production queues, technology research trees, and civilization era progression inspired by classic RTS design.

---

## 2. Five-Resource Economic Model

Crown & Conquest features 5 core resources:
1. **Food:** Gathered from farms, berry bushes, hunting animals, fishing. Used for worker & military unit training.
2. **Wood:** Gathered from forests. Used for building construction, archers, and siege weapons.
3. **Gold:** Mined from gold deposits or generated via trade caravans. Used for advanced military units, hero recruitment, and tech research.
4. **Stone:** Mined from stone quarries. Used for fortifications, walls, watchtowers, and fortress upgrades.
5. **Iron:** Mined from iron nodes. Used for advanced weapons, armor upgrades, and heavy cavalry.

---

## 3. Worker State Machine

Workers follow an explicit, deterministic state machine:

```text
[ IDLE ]
   │ (Player orders gather or automatic task assignment)
   ▼
[ MOVING TO RESOURCE ]
   │ (Arrives at resource node)
   ▼
[ GATHERING ] (Ticks gather progress based on gather rate)
   │ (Carrying capacity reached: e.g. 10/10)
   ▼
[ RETURNING TO DROP-OFF ] (Moves towards nearest Town Hall / Granary / Lumber Camp)
   │ (Deposits resources into faction stockpile)
   ▼
[ DEPOSITING ] (Faction resource count increases; loops back to resource node)
```

### Worker Invariants:
- A worker carries only one resource type at a time.
- If target resource node is depleted, worker searches within search radius for nearest matching node.
- If drop-off building is destroyed en route, worker re-evaluates nearest drop-off.

---

## 4. Building Construction & Footprint Grid

- **Grid Placement:** Buildings occupy integer grid cells (e.g. Town Hall $4\times 4$, Barracks $3\times 3$, House $2\times 2$, Tower $1\times 1$).
- **Placement Validation:**
  - Terrain must be buildable (not water, cliff, or unbuildable swamp).
  - No overlap with existing buildings or blocking obstacles.
  - Pathfinding navigation mesh updates dynamically when construction completes.
- **Construction Phases:** Foundation Placed $\to$ Workers Build (Health & Build Progress increment) $\to$ Building Completed $\to$ Functional.

---

## 5. Technology Research & Era Progression

### 4 Civilization Eras:
1. **Tribal / Archaic Era:** Basic infantry, simple wooden structures, gather rate upgrades.
2. **Bronze / Classical Era:** Barracks expansion, archers, watchtowers, blacksmith upgrades, hero hall.
3. **Iron / Imperial Era:** Heavy legionnaires, cavalry stables, stone walls, ballistas, siege workshops.
4. **Feudal / Sovereign Era:** Elite unit archetypes, trebuchets, fortress fortresses, mastery technologies.

---

## 6. Testing & Quality Gate Checklist
- [x] Resource deduction matches unit and building cost tables.
- [x] Worker state transitions do not leak resources or freeze when paths are blocked.
- [x] Population cap is enforced (cannot queue units without sufficient housing).
- [x] Age advancement requires exact building and resource prerequisites.
- [x] Save/load preserves worker carried inventory and building build percentages.
