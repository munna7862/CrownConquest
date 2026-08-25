# Crown & Conquest — Release Notes (v1.0.0 Release Candidate)

## 1. Executive Release Summary
Crown & Conquest v1.0.0 marks the culmination of 16 rigorous development sprints and 12 engineering phases. Crown & Conquest delivers a 100% local-first, deterministic hybrid RTS/RPG experience built with Godot 4 and C# (.NET 8).

---

## 2. Core Feature Highlights
- **Authoritative Deterministic Simulation:** Fixed-tick game simulation engine completely decoupled from rendering, guaranteeing bit-for-bit replay parity.
- **Signature Unit Progression:** Persistent combat experience, individual unit level-ups (Levels 1–9+), veterancy rank badges, and stat scaling for every soldier.
- **5-Resource Economy:** Food, Wood, Gold, Stone, and Iron resource management with gathering state machines, storage drops, and resource exhaustion.
- **Civilization Eras & Tech Trees:** 5 distinct civilization eras (Dark Age through Industrial Dawn) with comprehensive tech trees and building progressions.
- **Hero & RPG Layer:** Heroic commanders with talent trees, active abilities, auras, equipment inventories, and leadership caps.
- **Tactical Combat & Formations:** Line, Wedge, Square, and Column formations with dynamic flanking bonuses, morale routing, and terrain modifiers.
- **Siege Warfare:** Fortified stone walls, gatehouses, defensive towers with garrison mechanics, battering rams, catapults, and wall breach pathing.
- **Strategic AI System:** Multi-tiered AI architecture (Easy, Normal, Hard, Brutal) featuring adaptive personalities, tactical flanking, and economic priorities.
- **Large-Scale Performance:** Spatial partitioning grid supporting 1,000+ simultaneous combat entities at 60 FPS within a strict $<500\text{ MB}$ memory footprint.
- **Rich Audiovisuals & Accessibility:** Adaptive multi-layered soundtrack, positional combat audio, high-contrast modes, colorblind filters, and dynamic RTS HUD.

---

## 3. Shipping & Quality Certification
- **Deterministic Replay Checksum:** 1,000-tick parity verified ($Checksum_A == Checksum_B$).
- **Test Automation:** 330+ cumulative automated tests across Tiers 1–4 with 100% green pass rate.
- **Performance Budget:** Mean tick duration $< 16.6\text{ ms}$, peak tick $< 33.3\text{ ms}$, zero GC allocations in hot simulation loops.
- **Package Integrity:** All release binaries cryptographically verified with SHA-256 digests.
