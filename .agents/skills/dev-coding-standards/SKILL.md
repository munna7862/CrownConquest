---
name: dev-coding-standards
description: Production coding standards for Crown & Conquest in C# 12 / .NET 8 / Godot 4, domain decoupling, memory management, and zero per-frame allocations.
---

# Production Dev Coding Standards — Crown & Conquest

## 1. Scope & Technology Stack
These standards apply to all production C# code written for **Crown & Conquest** across the **Domain Simulation**, **Application Services**, **Godot Presentation Nodes**, and **Data Providers**.

- **Language:** C# 12 / .NET 8
- **Engine:** Godot 4 (.NET Build)
- **Nullability:** Strict Nullable Reference Types enabled (`<Nullable>enable</Nullable>`)
- **Target OS:** Windows 10/11 x64

---

## 2. Decoupled Layering & Architecture

Maintain strict unidirectional dependencies:

$$\text{Presentation (Godot Nodes)} \longrightarrow \text{Application Services} \longrightarrow \text{Domain Simulation} \longrightarrow \text{Data / Config}$$

### Golden Architecture Rules:
1. **Zero Godot References in Domain:** Domain entities, combat systems, economy logic, and AI classes must **NEVER** import `Godot` or inherit from `Godot.Node`.
2. **Domain Authority:** All game state transitions (health changes, XP gain, level-up, resource deductions) occur strictly inside the Domain Simulation.
3. **Presentation Observes Via Events:** Godot visual nodes listen to typed domain events (`UnitLevelUpEvent`, `DamageDealtEvent`) to trigger animations, sounds, and UI updates.
4. **Command Pattern for Mutations:** Player interactions (clicks, keypresses) dispatch immutable command objects (`MoveCommand`, `AttackCommand`) through the application layer to the simulation.

---

## 3. High-Performance C# & Memory Discipline

During active simulation ticks and rendering frames:

- **Zero Allocations in Hot Loops:**
  - Avoid `new` inside `Update()`, `_Process()`, `_PhysicsProcess()`, and simulation tick loops.
  - Avoid LINQ (`.Where()`, `.Select()`, `.ToList()`) in hot paths; use standard `for` loops or `Span<T>`.
  - Avoid boxing: Use generic collections (`List<T>`, `Dictionary<TKey, TValue>`) instead of non-generic collections or `object` parameters.
- **Value Types & Data Locality:**
  - Use `readonly struct` or `record struct` for lightweight mathematical and identifier types (e.g. `EntityId`, `GridPosition`, `DamageInfo`).
- **Object Pooling:**
  - Pre-allocate pools for high-churn instances (projectiles, floating combat text, damage events, audio players).

---

## 4. Error Handling & Defensive Programming
- **No Silent Failures:** Never catch exceptions with empty blocks. Use structured logging (`Logger.LogError(...)`).
- **Result Pattern:** For operations that may fail due to valid gameplay rules (e.g. invalid building placement, insufficient gold), return a typed `Result<T, GameError>` rather than throwing exceptions.
- **Fail Fast on Invariants:** Use `Debug.Assert(...)` for simulation invariant verification during development.

---

## 5. Code Style & Naming Conventions
- **PascalCase:** Classes, Structs, Interfaces (`IUnit`), Methods, Properties, Public Events.
- **camelCase:** Local variables, method arguments, private fields (`_fieldName`).
- **Data Externalization:** Unit statistics, resource costs, level-up XP tables, and tech research times must be stored in external JSON or Resource files, never hardcoded in logic classes.
