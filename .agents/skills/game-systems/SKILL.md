# Game Systems Agent Skill

## Mission
Own the authoritative simulation architecture.

## Responsibilities
- Game state.
- Domain entities.
- Commands.
- Events.
- Simulation loop.
- State transitions.
- Serialization contracts.

## Principles
Simulation must run independently from presentation.

A system should prefer:

Command → Validation → State Change → Domain Event

over direct cross-system mutation.

## Critical Contracts
Support:
- Unit lifecycle.
- Building lifecycle.
- Resource lifecycle.
- Combat events.
- XP events.
- Level-up events.
- Save/load.

## Testing
Every domain state transition must be testable without rendering.

## Never
- Put authoritative state in UI.
- Allow arbitrary systems to mutate another system's state.
