# Test Cases Catalog — Sprint 13: UX, Visuals and Audio

## Overview
This catalog defines the pre-implementation test matrix for Sprint 13, covering HUD, selection feedback, minimap, veterancy presentation, VFX, animations, buildings, combat audio, ambience, music, accessibility, and tutorial systems.

---

## Tier 1: Pure C# Domain & Math Unit Tests

### TC-S13-001: HUD Presenter — Resource Bar View Model Accuracy
- **Precondition:** Faction with known resource bank (Food=500, Wood=300, Gold=200, Stone=100, Iron=50, Pop=10/50).
- **Action:** Generate `ResourceBarViewModel`.
- **Assert:** All fields match simulation state exactly.

### TC-S13-002: HUD Presenter — Command Card View Model Generation
- **Precondition:** Single unit selected with known abilities and production options.
- **Action:** Generate `CommandCardViewModel`.
- **Assert:** Available commands list matches unit type capabilities.

### TC-S13-003: Selection Feedback — Selection Ring Descriptor
- **Precondition:** Unit at position (10, 10) with faction Player1 selected.
- **Action:** Generate `SelectionRingDescriptor`.
- **Assert:** Position, radius, faction color index, and isSelected flag are correct.

### TC-S13-004: Minimap — World-to-Minimap Coordinate Projection
- **Precondition:** World bounds (0,0)-(200,200), minimap viewport (0,0)-(1,1).
- **Action:** Project unit at world position (100, 50) to minimap coordinates.
- **Assert:** Minimap position = (0.5, 0.25).

### TC-S13-005: Veterancy Badge — Rank-to-Badge Mapping
- **Precondition:** Units at each veterancy rank (Recruit, Experienced, Veteran, Elite, Legendary).
- **Action:** Generate `VeterancyBadgeDescriptor` for each.
- **Assert:** Badge icon index and display name match rank.

### TC-S13-006: VFX Trigger — Combat Hit Effect Descriptor
- **Precondition:** `DamageDealtEvent` with known attacker, target, and damage.
- **Action:** Generate `VfxTriggerDescriptor`.
- **Assert:** Effect type = CombatHit, position matches target, intensity scales with damage.

### TC-S13-007: Animation State — UnitState-to-AnimationState Mapping
- **Precondition:** Units in each `UnitState` (Idle, Moving, Attacking, Gathering, Dead, etc.).
- **Action:** Map each to `AnimationState`.
- **Assert:** Correct animation state for each domain state.

### TC-S13-008: Building Visual State — Construction Progress
- **Precondition:** Building at 50% construction progress.
- **Action:** Generate `BuildingVisualState`.
- **Assert:** Progress percentage = 0.5, visual phase = "under_construction".

### TC-S13-009: Audio Trigger — SFX Descriptor from Combat Event
- **Precondition:** `DamageDealtEvent` for melee attack.
- **Action:** Generate `AudioTriggerDescriptor`.
- **Assert:** SFX category = "weapon_impact", sub-category = "melee", volume scales with damage.

### TC-S13-010: Ambience Zone — Terrain-to-Ambience Mapping
- **Precondition:** Camera centered over Forest terrain.
- **Action:** Resolve `AmbienceZoneDescriptor`.
- **Assert:** Zone type = "forest", ambient track ID is valid.

### TC-S13-011: Music State Machine — State Transitions
- **Precondition:** Music in "Peace" state with combat intensity = 0.
- **Action:** Increase combat intensity above threshold.
- **Assert:** State transitions Peace -> Skirmish -> Battle based on thresholds.

### TC-S13-012: Accessibility — Colorblind Palette Remapping
- **Precondition:** Normal palette colors for Player1 (blue) and Player2 (red).
- **Action:** Apply Deuteranopia palette.
- **Assert:** Remapped colors are distinct and colorblind-safe.

### TC-S13-013: Tutorial Step — Objective Completion Tracking
- **Precondition:** Tutorial with 3 steps, step 1 active.
- **Action:** Complete step 1 objective.
- **Assert:** Step 1 marked complete, step 2 becomes active, progress = 1/3.

### TC-S13-014: Unit Status Panel — Multi-Unit Selection Summary
- **Precondition:** 5 units selected with varying health and levels.
- **Action:** Generate `UnitGroupSummaryViewModel`.
- **Assert:** Count = 5, average health percentage, type breakdown correct.

### TC-S13-015: Notification Queue — Event-Driven Notifications
- **Precondition:** Empty notification queue.
- **Action:** Trigger building completed event and unit level-up event.
- **Assert:** Queue contains 2 notifications in chronological order with correct types.

---

## Tier 2: Simulation & Invariant Tests

### TC-S13-016: HUD View Model Determinism — 100-Tick Simulation
- **Precondition:** Standard 2-faction match with economy.
- **Action:** Run 100 ticks, capture HUD view models at each tick.
- **Assert:** View models are deterministic across dual runs with same seed.

### TC-S13-017: Selection Feedback Integrity — Select/Deselect Cycle
- **Precondition:** 10 spawned units.
- **Action:** Select 5, deselect 2, verify descriptors.
- **Assert:** Exactly 3 units have selection rings active.

### TC-S13-018: Minimap Unit Tracking — Units Moving Across Map
- **Precondition:** Units at known positions moving to targets.
- **Action:** Advance 50 ticks, check minimap blips.
- **Assert:** Blip positions track unit positions within floating-point epsilon.

### TC-S13-019: Music State Machine — Combat Intensity Cycle
- **Precondition:** 10v10 battle simulation.
- **Action:** Run combat to completion.
- **Assert:** Music transitions Peace -> Battle -> (Victory or Defeat).

### TC-S13-020: Tutorial System — Full Tutorial Completion
- **Precondition:** 5-step tutorial scenario.
- **Action:** Complete all objectives sequentially.
- **Assert:** Tutorial completion flag set, all steps marked done.

---

## Tier 3: Multi-System Integration Tests

### TC-S13-021: Full HUD Integration — Resource + Selection + Minimap
- **Precondition:** Active match with economy and military.
- **Action:** Spawn workers, gather resources, select units, check all HUD panels.
- **Assert:** Resource bar, selection panel, and minimap all reflect simulation truth.

### TC-S13-022: VFX + Audio Integration — Combat Event Pipeline
- **Precondition:** 5v5 combat encounter.
- **Action:** Run combat, collect all VFX and audio trigger descriptors.
- **Assert:** Every `DamageDealtEvent` produces matching VFX and audio descriptors.

### TC-S13-023: Veterancy + Animation Integration — Level-Up Visual Feedback
- **Precondition:** Unit near level-up threshold.
- **Action:** Grant kill XP to trigger level-up.
- **Assert:** `VeterancyBadgeDescriptor` updates, animation state reflects level-up trigger.

### TC-S13-024: Building Visual + Audio Integration — Construction Lifecycle
- **Precondition:** Place building, assign worker.
- **Action:** Run ticks through construction completion.
- **Assert:** Visual progress updates, completion audio trigger fires.

---

## Tier 4: Headless E2E Scenarios

### TC-S13-025: UX Scenario — Full Match with All Presentation Systems
- **Precondition:** 2-faction match with economy, combat, heroes, buildings.
- **Action:** Simulate 500 ticks headless.
- **Assert:** All presentation systems produce valid descriptors, zero null references, all events consumed.

### TC-S13-026: 1000-Tick Deterministic Replay Parity
- **Precondition:** Seeded match with all Sprint 13 systems active.
- **Action:** Dual 1000-tick replay.
- **Assert:** Bit-exact state checksums, presentation descriptors match.

---

## Summary

| Tier | Test Count | Coverage Area |
|:---|---:|:---|
| Tier 1 (Unit) | 15 | Domain math, view models, state mappings |
| Tier 2 (Invariant) | 5 | Determinism, selection integrity, music state |
| Tier 3 (Integration) | 4 | Cross-system HUD/VFX/Audio/Animation |
| Tier 4 (E2E) | 2 | Full match headless scenarios |
| **Total** | **26** | |
