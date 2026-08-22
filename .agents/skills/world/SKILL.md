---
name: world
description: World, Map & Campaign Specialist persona for Crown & Conquest procedural/authored map generation, terrain movement/combat modifiers, flow-field pathfinding, strategic regions, and persistent campaign world map.
---

# World Agent Skill — Crown & Conquest

## 1. Mission
The **World Specialist** owns maps, terrain layers, movement costs, navigation meshes and flow-fields, resource node placement, strategic territory nodes, and the overarching campaign world map.

---

## 2. Terrain Types & Combat Modifiers

| Terrain Type | Movement Cost | Combat / Line-of-Sight Modifiers | Buildable? |
|:---|:---:|:---|:---:|
| **Grassland / Plains** | $1.0\times$ | Normal combat, clear line of sight | Yes |
| **Roads / Paved** | $0.7\times$ (Fast) | Normal combat, high travel speed | Yes (Roads) |
| **Forest / Woods** | $1.5\times$ (Slow) | +30% Ranged evasion (cover), conceals units | No (Resource) |
| **Hills / High Ground** | $1.3\times$ | +25% Ranged damage & +2 Vision range down-slope | Yes |
| **River / Shallows** | $2.0\times$ (Very Slow) | -20% Armor while crossing water | No |
| **Mountains / Cliffs** | Impassable | Blocks line of sight and projectiles | No |
| **Swamp / Marsh** | $1.8\times$ | -15% Cavalry speed & charge power | No |

---

## 3. Navigation & Pathfinding Architecture

1. **Large Army Flow-Fields:** For 50+ units commanded to move simultaneously, generate a single goal-based flow field / vector field instead of 50 individual $A^*$ searches.
2. **Local Avoidance & Steering:** Combine flow-fields with RVO (Reciprocal Velocity Obstacles) or simple boids separation for smooth local unit avoidance without jitter.
3. **Dynamic Navmesh / Obstacle Updates:** When buildings, walls, or siege gates are constructed or destroyed, update the navigation grid incrementally without re-baking the entire world.

---

## 4. Strategic Map & Campaign World Progression

In Phase 08 & Sprints 10–11, Crown & Conquest features a persistent strategic world map:
- **Provinces & Strongholds:** The continent is divided into interconnected provinces with resource yields (Food, Iron, Gold).
- **Garrisoned Armies & Armies on the March:** Armies and heroes move across the strategic map; when two hostile forces collide in a province, a tactical RTS match is initiated.
- **Supply Lines & Neutral Villages:** Capturing neutral trade posts, monasteries, and mercenary camps yields tactical reinforcements during the battle.

---

## 5. Testing & Quality Gate Checklist
- [x] Path reachability tests across complex maze and choke-point maps.
- [x] High ground elevation combat modifiers apply properly to ranged units.
- [x] Dynamic building obstacle placement updates unit paths in real-time.
- [x] Campaign province state and ownership transitions serialize cleanly.
