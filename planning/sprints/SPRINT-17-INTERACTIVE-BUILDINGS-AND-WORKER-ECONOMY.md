# Sprint 17: Interactive Building Production, Worker Gathering Loop & Settlement Placement

## 1. Executive Summary & Goal
Implement full gameplay interactivity for settlement structures and worker economics in the 2D graphical view. Enable selecting buildings to view and trigger production queues (training villagers, swordsmen, archers, cavalry, and researching tech), setting visual rally flags, full villager resource harvesting loops (chopping trees, mining gold/stone/iron, foraging, and carrying resources to Town Centers), green/red building blueprint placement preview, and population housing capacity mechanics.

---

## 2. Backlog Stories & Acceptance Criteria

### `CNC-1701`: Building Selection & Production Action Cards (8 SP)
- **Goal:** Left-clicking any building (Town Center, Barracks, Blacksmith, Stables) selects it and renders its Production Card in the bottom HUD.
- **Acceptance Criteria:**
  1. Town Center displays: *Train Celtic Villager* (50 Food), *Advance Era* button.
  2. Barracks displays: *Train Celtic Swordsman* (60 Food, 20 Wood), *Train Celtic Archer* (50 Food, 40 Wood).
  3. Blacksmith displays: *Forged Blades Upgrade* (+2 Melee Damage), *Scale Armor Upgrade* (+2 Armor).
  4. Non-selected buildings deselect cleanly when clicking ground or other entities.

### `CNC-1702`: Production Queues & Timed Progress Bars (6 SP)
- **Goal:** Authoritative production queue managing up to 5 concurrent build orders per building.
- **Acceptance Criteria:**
  1. Clicking a train button deducts resources from top stockpile immediately.
  2. Live progress bar advances each tick ($20\text{Hz}$) based on unit train time.
  3. Right-clicking queued icon cancels order and refunds 100% of resources.
  4. On completion, the unit spawns at the building entrance and fires `UnitTrainedEvent`.

### `CNC-1703`: Rally Point Flags & Spawn Marching (4 SP)
- **Goal:** Allow placing visual rally flags on the battlefield for production buildings.
- **Acceptance Criteria:**
  1. Right-clicking anywhere while a production building is selected sets a visual rally flag at that coordinate.
  2. Right-clicking a resource node sets a worker gather rally point.
  3. Newly spawned units immediately march to the designated rally flag.

### `CNC-1704`: Worker Autonomous Resource Gathering Loop (8 SP)
- **Goal:** Full villager resource gathering state machine with visual feedback and resource drop-off.
- **Acceptance Criteria:**
  1. Right-clicking a resource node with villagers selected tasks them to harvest.
  2. Villagers walk to harvest radius, play gathering state, and accumulate resource load (up to carry capacity).
  3. When full, villagers walk to the nearest Town Center / drop-off point, deposit resources into player bank, and return to harvest.
  4. Depleted resource nodes visually clear and free up workers.

### `CNC-1705`: Grid-Aligned Building Placement Blueprint (8 SP)
- **Goal:** Interactive building construction system with green/red placement ghost.
- **Acceptance Criteria:**
  1. Pressing `B` opens the Build Menu (House, Barracks, Blacksmith, Watchtower, Farm).
  2. Mouse cursor shows semi-transparent blueprint ghost snapped to the tile grid.
  3. Displays green if terrain is clear and affordable; displays red if obstructed or unaffordable.
  4. Left-click places construction foundation site; assigned villagers build until 100% complete.

### `CNC-1706`: Dynamic Housing Capacity & Population Breakdown (6 SP)
- **Goal:** Housing population mechanics and accurate HUD population diagnostics.
- **Acceptance Criteria:**
  1. Each completed House adds $+5$ to max population cap.
  2. HUD displays detailed tooltip: `Occupied: X (Military: M, Workers: W) | Capacity: Y / Max: 200`.
  3. Training units is blocked with error message if population cap is reached.

---

## 3. Definition of Done (DoD)
- [ ] All 6 stories implemented and verified against acceptance criteria.
- [ ] Automated unit, invariant, and simulation integration tests passing (100% green).
- [ ] Zero hot-loop heap allocations during queue ticking and worker state transitions.
- [ ] Clean build with 0 compiler warnings.
