# Pull Request: Sprint 19 — Audio Soundscape, Unit Voice Barks, Combat VFX & v1.2.0 Release

## 1. Overview & Summary
This PR delivers **Sprint 19** of Crown & Conquest, completing the sensory immersion and battlefield feedback layer of the game, authored historical Gauls vs Romans scenario slice, and standalone desktop release packaging:
- **`CNC-1901`: Unit Voice Barks & Context Speech System** — Authentic Celtic & Roman vocal speech barks for unit selection, marching orders, attack commands, and unique Hero Brennus ability battle cries (*"Feel our wrath!"*, *"Feel my blade!"*), with audio bus ducking and anti-overlap cooldown intervals.
- **`CNC-1902`: Positional Combat Audio & Adaptive Dynamic Soundtrack** — 2D positional panning and Euclidean distance attenuation for weapon clashing, shield blocks, bowstring releases, arrow thuds, catapult impacts, gathering SFX (wood chopping, stone mining, forge anvil), and dynamic acoustic $\to$ martial combat music state transitions.
- **`CNC-1903`: Combat Visual Impact Particles & Projectile Physics** — Ballistic parabolic flight trajectories in 2.5D with ground shadows for arrows and catapult boulders, directional hit sparks & blood splash decal particles on casualties, and golden bursting rune rings on automatic unit & hero Level-Up.
- **`CNC-1904`: Historical Gauls vs Romans Battle Scenario & Match Result Flow** — Authored battlefield featuring river crossing with shallow ford, Celtic hill village (Town Center, Barracks, Watchtower, Hero Brennus) vs Roman garrison (Praetorium, Legion Barracks, Ballista Tower, Centurion, Catapults), clear win/loss conditions, and post-match MVP statistics summary.
- **`CNC-1905`: Standalone Windows Release & WiX Packaging (v1.2.0)** — Standalone native executable, WiX MSI installer configuration (`1.2.0`), 100% cumulative green pass rate (413/413 tests passing), and SHA-256 cryptographic manifest.

---

## 2. Test Execution & Quality Gate Summary
- **Cumulative Test Pass Rate:** 100% Green (413/413 passed, 0 failed, 0 skipped).
- **New Automated Tests:** 25 comprehensive test cases in [`tests/CrownConquest.Tests/Presentation/AudioCombatVfxTests.cs`](file:///c:/Workspace/CrownConquest/tests/CrownConquest.Tests/Presentation/AudioCombatVfxTests.cs).
- **Compiler / Linter Status:** `dotnet build --warnaserror` succeeds with 0 errors and 0 warnings.
- **Deterministic Simulation Parity:** Bit-for-bit state checksum equality verified across dual seeded 1,000-tick runs.
- **Performance Budget:** Zero per-tick dynamic heap allocations in simulation hot loops; mean tick time $< 16.6$ ms; memory working set $< 500$ MB.

---

## 3. Definition of Done (DoD) Verification
- [x] Positional audio and voice barks play correctly on unit actions.
- [x] Combat animations and projectile trajectories execute smoothly.
- [x] Match victory and defeat conditions trigger end-of-match screens.
- [x] Cumulative automated tests pass (100% green).
- [x] Standalone executable and WiX MSI installer `CrownConquest_1.2.0_x64_en-US.msi` configuration verified.
