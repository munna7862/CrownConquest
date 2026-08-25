# Sprint 18: Celtic Kings 2D Sprite Art, Directional Unit Animation & Terrain

## 1. Executive Summary & Goal
Replace all geometric vector primitives (circles, rectangles, outline strokes) with authentic, high-detail 2D sprite artwork inspired by *Celtic Kings: Rage of War*. Implement a multi-layered tilemap terrain engine, illustrated building structures with construction stages, 8-directional/4-directional animated unit spritesheets (Idle, Walk, Attack, Death), rich natural resource sprites, and a dynamic Fog of War line-of-sight visual system.

---

## 2. Backlog Stories & Acceptance Criteria

### `CNC-1801`: Multi-Layered 2D Terrain Tileset & Auto-Tiling (10 SP)
- **Goal:** Rich 2D terrain graphics replacing flat polygon backgrounds.
- **Acceptance Criteria:**
  1. Textured grass terrain tiles with color variation, wildflower patches, and dirt blending.
  2. Cobblestone and dirt military roads with speed multipliers.
  3. Water bodies (rivers, lakes) with animated shoreline wave foam.
  4. Stone cliff elevation contours with impassable tile boundaries.

### `CNC-1802`: Illustrated Building Sprites & Construction Stages (10 SP)
- **Goal:** Hand-crafted building artwork with construction scaffolding animations.
- **Acceptance Criteria:**
  1. Celtic Thatched Town Center, Timber Barracks, Blacksmith Forge with animated chimney smoke, Wooden Watchtowers, and Stone Walls.
  2. Roman Stone Fortresses, Legionary Barracks, Siege Workshops, and Ballista Towers.
  3. Three visual stages: Foundation Scaffolding $\to$ Half-Built $\to$ Complete Structure.
  4. Smoke and fire particles emitted when structure health drops below 50%.

### `CNC-1803`: Animated Unit Spritesheets & Directional Controllers (12 SP)
- **Goal:** Multi-frame animated 2D unit spritesheets for all playable units and heroes.
- **Acceptance Criteria:**
  1. Celtic Units: Swordsman (Sword & Shield), Archer (Bow & Arrow), Chariot/Cavalry, Villager, Hero Brennus (Claymore).
  2. Roman Units: Legionary (Gladius & Scutum), Centurion, Equites (Cavalry), Catapult.
  3. Animation states: `Idle`, `Walk`, `Attack`, `Hurt`, and `Death` with directional facing.
  4. Visual weapon trails on heavy strikes and arrow release frames.

### `CNC-1804`: Natural Resource & Foliage Sprites (8 SP)
- **Goal:** Beautiful 2D natural resources with interactive depletion visuals.
- **Acceptance Criteria:**
  1. Forest clusters: Oak trees and Pine pines with rustling foliage; stump remains after harvesting.
  2. Gold ore veins with sparkling highlights; shrinks as mined.
  3. Stone rock boulders and Iron ore outcroppings with mining chip particles.
  4. Berry bushes that shed fruit when harvested.

### `CNC-1805`: Dynamic Line-of-Sight & Fog of War System (8 SP)
- **Goal:** Authoritative Fog of War shading based on unit sight radii.
- **Acceptance Criteria:**
  1. **Black Shroud:** Unexplored areas are pitch black.
  2. **Fog of War:** Visited areas outside current line-of-sight show terrain and static buildings, but hide enemy units.
  3. **Visible Line of Sight:** Areas within unit/building vision radius ($12\text{--}24\text{ tiles}$) are fully illuminated in real-time.

---

## 3. Definition of Done (DoD)
- [ ] Vector primitives completely eliminated from gameplay viewport.
- [ ] Unit spritesheets animate smoothly at 60 FPS without frame jitter.
- [ ] Buildings display construction stages and damage fire particles.
- [ ] Fog of War efficiently computes sight masks with zero GC allocations in hot loop.
- [ ] 100% green test suite on presentation render tests.
