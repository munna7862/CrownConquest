# Phase 07 — Enemy AI

## Objective
Create an AI opponent that manages an economy, builds an army and makes tactical decisions.

## Architecture
Strategic AI
→ Economic AI
→ Military AI
→ Tactical AI
→ Unit AI

## Economic AI
Responsibilities:
- Worker assignment.
- Resource priorities.
- Construction.
- Production.
- Technology.

## Strategic AI
Possible objectives:
- Expand.
- Defend.
- Attack.
- Capture resource.
- Siege.
- Retreat.
- Recover.

## Military AI
Responsibilities:
- Army composition.
- Recruitment.
- Army grouping.
- Army movement.
- Defense.

## Tactical AI
Responsibilities:
- Target selection.
- Flanking.
- Focus fire.
- Formation choice.
- Retreat.
- Terrain use.
- Siege positioning.

## AI Personalities
Initial:
- Aggressive
- Defensive
- Expansionist
- Tactical

Each personality should modify priorities rather than duplicate AI code.

## Unit AI
States:
- Idle
- Moving
- Attacking
- Following
- Defending
- Retreating
- Routing
- Dead

## Simulation Testing
Run automated battles without rendering where possible.

Examples:
- Infantry vs infantry.
- Mixed armies.
- Cavalry raid.
- Siege.
- Hero-led battle.

## Deliverables
A complete AI opponent capable of reaching a basic victory condition.

## Definition of Done
The AI can independently gather resources, construct a base, produce an army, attack, defend and recover.
