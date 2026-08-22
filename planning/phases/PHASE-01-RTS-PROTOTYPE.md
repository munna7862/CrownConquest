# Phase 01 — RTS Prototype

## Objective
Create the first playable battlefield and prove the fundamental RTS interaction loop.

## Core Loop
Select → Move → Target → Attack → Damage → Kill → Death.

## Scope

### Battlefield
- One test map.
- Walkable terrain.
- Basic navigation.
- Camera pan and zoom.

### Units
Implement:
- Unit entity.
- Health.
- Movement.
- Attack range.
- Attack cooldown.
- Target acquisition.
- Death state.

Initial unit:
- Swordsman.

### Player Controls
- Click selection.
- Drag selection.
- Right-click movement.
- Attack command.
- Stop command.

### Combat
- Basic attack.
- Damage calculation.
- Target selection.
- Death handling.
- Combat events.

### Events
Initial event model:
- UnitCreated
- UnitSelected
- UnitMoved
- UnitAttacked
- DamageDealt
- UnitKilled
- UnitDied

## Deliverables
- Playable 10-vs-10 battlefield.
- Selectable units.
- Movement.
- Combat.
- Death.
- Basic HUD.

## Tests
- Unit movement.
- Attack range.
- Damage.
- Death.
- Target switching.
- Multi-unit selection.
- Combat event emission.

## Definition of Done
Two groups of units can be spawned, commanded, and fight until one side is defeated without manual intervention.

## Exit Criteria
The combat simulation is independent enough from presentation that it can be tested without rendering a scene.
