# Crown & Conquest — Phase Plan Index

This directory contains the phase-wise implementation plans for the original RTS project inspired by the design philosophies of Celtic Kings: Rage of War, Nemesis of the Roman Empire, and Age of Empires.

## Phases

| Phase | File | Primary Goal |
|---|---|---|
| 00 | PHASE-00-FOUNDATION.md | Engineering foundation |
| 01 | PHASE-01-RTS-PROTOTYPE.md | First playable RTS battle |
| 02 | PHASE-02-ECONOMY.md | Resources and settlement |
| 03 | PHASE-03-UNIT-PROGRESSION.md | Individual unit XP and automatic leveling |
| 04 | PHASE-04-CIVILIZATION.md | Eras, technology and civilization progression |
| 05 | PHASE-05-HEROES.md | RPG heroes and leadership |
| 06 | PHASE-06-ADVANCED-COMBAT.md | Formations, morale, terrain and siege |
| 07 | PHASE-07-ENEMY-AI.md | Strategic and tactical AI |
| 08 | PHASE-08-CAMPAIGN.md | World map and connected campaign |
| 09 | PHASE-09-LARGE-SCALE-PERFORMANCE.md | Large army performance |
| 10 | PHASE-10-POLISH.md | UX, visuals, audio and tutorial |
| 11 | PHASE-11-QA-RELEASE.md | QA, balancing and release |
| 12 | PHASE-12-GRAPHICAL-PRESENTATION-AND-DISTRIBUTION.md | Full graphical presentation, Godot scene assembly and desktop distribution |
| 13 | PHASE-13-CELTIC-KINGS-ART-AND-INTERACTIVE-RTS.md | Celtic Kings 2D sprite art, animated units, interactive buildings, sound & v1.2.0 release |

## Critical Design Dependency

Phase 03 is deliberately early because individual unit progression is one of the game's signature mechanics.

The progression chain is:

`Combat → Kill → XP → Automatic Level-Up → Veterancy → Veteran Value → Strategic Risk`

All later systems should preserve this chain.

## Recommended Execution Rule

Do not begin a later phase merely because the previous phase is visually complete.

A phase is complete only when:
- Its implementation works.
- Its automated tests pass.
- Its architecture is documented.
- Its data/configuration is externalized where appropriate.
- Existing functionality has no regression.
