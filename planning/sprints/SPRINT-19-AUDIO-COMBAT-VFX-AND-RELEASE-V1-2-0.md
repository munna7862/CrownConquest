# Sprint 19: Audio Soundscape, Unit Voice Barks, Combat VFX & v1.2.0 Desktop Release

## 1. Executive Summary & Goal
Complete the sensory immersion of Crown & Conquest by implementing a positional 2D audio soundscape (Celtic/Roman voice barks, combat impact SFX, dynamic adaptive musical tracks, building construction sounds), rich visual VFX (blood splatters, weapon sparks, projectile arc physics with shadows, level-up aura explosions), an authored Gauls vs Romans historical battle scenario with victory/defeat banners, and package the final polished **v1.2.0** desktop installers.

---

## 2. Backlog Stories & Acceptance Criteria

### `CNC-1901`: Unit Voice Barks & Context Speech System (8 SP)
- **Goal:** Authentic vocal audio responses when interacting with units.
- **Acceptance Criteria:**
  1. Selection voice lines on unit click (*"Chieftain?", "Ready for battle!", "Orders, commander?"*).
  2. Movement and attack command acknowledgments (*"Moving!", "Charge!", "By the gods!"*).
  3. Hero Brennus unique heroic voice barks on casting *War Cry* and *Heroic Strike*.
  4. Audio bus ducking and cooldown interval to prevent voice chatter overlap.

### `CNC-1902`: Positional Combat Audio & Adaptive Dynamic Soundtrack (8 SP)
- **Goal:** Crisp positional 2D combat SFX and dynamic musical score.
- **Acceptance Criteria:**
  1. Weapon impact sounds: Metal sword clashing, shield block thuds, bowstring releases, and arrow impacts.
  2. Environmental audio: Wood chopping, stone clinking, blacksmith anvil clangs, and ambient wind/birds.
  3. Dynamic soundtrack transitions: Peaceful settlement acoustic guitar/flute $\to$ Intense martial drum combat music.

### `CNC-1903`: Combat Visual Impact Particles & Projectile Physics (8 SP)
- **Goal:** High-impact visual effects for battlefield engagements.
- **Acceptance Criteria:**
  1. Projectile flight physics with arched parabolic trajectories and ground shadows for arrows and catapult boulders.
  2. Hit impact sparks and blood splash decal particles on melee strikes.
  3. Golden bursting rune rings when units and heroes achieve automatic Level-Up.

### `CNC-1904`: Historical Gauls vs Romans Battle Scenario & Match Result Flow (8 SP)
- **Goal:** Hand-crafted scenario slice with victory/defeat triggers and statistics.
- **Acceptance Criteria:**
  1. Authored Gauls vs Romans battlefield featuring river crossing, Roman forward fort, and Celtic hill village.
  2. Clear victory condition (Destroy enemy Town Center) and defeat condition (Loss of Celtic Town Center).
  3. Match end screen displaying combat stats: Total Kills, Units Trained, Resources Harvested, MVP Hero Level.

### `CNC-1905`: Polished Standalone Windows Release & WiX Packaging (v1.2.0) (8 SP)
- **Goal:** Package, verify, and publish Crown & Conquest v1.2.0.
- **Acceptance Criteria:**
  1. Standalone native executable and WiX MSI installer bundling all sprite sheets, tilemaps, audio, and scenarios.
  2. 100% green pass rate across cumulative test suite (Tiers 1–4).
  3. Cryptographic SHA-256 manifest and automated GitHub Release `v1.2.0`.

---

## 3. Definition of Done (DoD)
- [ ] Positional audio and voice barks play correctly on unit actions.
- [ ] Combat animations and projectile trajectories execute smoothly.
- [ ] Match victory and defeat conditions trigger end-of-match screens.
- [ ] Cumulative automated tests pass (100% green).
- [ ] WiX MSI installer `CrownConquest_1.2.0_x64_en-US.msi` builds cleanly and verified on Windows x64.
