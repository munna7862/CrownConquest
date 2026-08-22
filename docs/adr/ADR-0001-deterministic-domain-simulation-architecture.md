# ADR-0001: Deterministic Domain Simulation & Decoupled Architecture

## Status
**Accepted** (Sprint 00 — Engineering Foundation)

## Context
Crown & Conquest is a 100% local Windows desktop RTS/RPG inspired by Celtic Kings and Age of Empires, featuring signature individual unit progression, tactical formations, and large-scale battles. To guarantee deterministic replays, save/load reliability, multiplayer parity, and zero presentation-to-logic coupling, game state mutation must be strictly isolated from Godot's rendering and node hierarchy.

## Decision
1. **Domain Decoupling:** The simulation core (`CrownConquest.Domain`) is written in pure C# 12 / .NET 8 without any references to `Godot` or `Godot.Node`.
2. **Fixed-Timestep Simulation:** All game logic runs at a fixed 20 Hz (50ms) tick rate using an authoritative simulation engine (`SimulationEngine`).
3. **Immutable Command Pattern:** All external player and AI mutations are queued as immutable `ICommand` records processed deterministically at tick boundaries.
4. **Zero-Allocation Typed Event Bus:** Presentation nodes, audio controllers, and UI HUD elements observe simulation state transitions via `DomainEventBus` by subscribing to strongly typed readonly struct event records (`IDomainEvent`).
5. **Data-Driven Configuration:** All combat parameters, unit blueprints, and XP curves reside in external JSON definitions (`data/definitions/`).

## Consequences
- **Pros:**
  - 100% headless testing via standard `dotnet test` runners without engine or GPU dependencies.
  - Zero heap allocation in hot simulation loops.
  - Complete protection against presentation code mutating authoritative simulation state.
- **Cons:**
  - Requires maintaining presentation adapter bridges (`PresentationEventBridge`) to map domain events to Godot visual nodes.
