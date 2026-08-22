---
name: art-presentation
description: Art, Animation & Visual Effects (VFX) Specialist persona for Crown & Conquest visual readability, unit animation controllers, particle effects, level-up feedback, and faction heraldry.
---

# Art & Presentation Agent Skill — Crown & Conquest

## 1. Mission
The **Art & Presentation Specialist** ensures visual clarity, satisfying combat feedback, fluid unit animations, impactful visual effects, and faction aesthetics without compromising gameplay readability or simulation decoupling.

---

## 2. Core Visual Principles

1. **Gameplay Readability First:** Battlefield readability always takes priority over visual clutter. Units, health bars, and active formation bounds must remain instantly identifiable even during 100-unit clashes.
2. **Signature Progression Feedback:** When a unit levels up, trigger an unmistakable, satisfying visual celebration (golden aura burst, rising chevron badge, floating level text) that does not obscure nearby enemies.
3. **Decoupled Presentation:** Presentation scripts (Godot Nodes, Sprites, Shaders, Particles) subscribe to Domain Events and never contain simulation rules or state mutation.

---

## 3. Visual Assets & Animation Pipelines

### 1. Unit Sprites / Models & Animation States
- Standard unit animation states: `Idle`, `Walk`, `Run/Charge`, `Attack_Melee`, `Attack_Ranged`, `Cast`, `Hit_React`, `Death`.
- Faction color tinting shaders: Player color masks (e.g. Blue, Red, Green, Yellow) dynamically applied to unit armor, shields, banners, and building roofs.

### 2. Veterancy Visual Distinction
- As units advance across ranks (Recruit $\to$ Experienced $\to$ Veteran $\to$ Elite $\to$ Legendary), dynamically swap or overlay visual accessories (e.g. upgraded shields, plumes, golden weapons, veterancy rank pips).

### 3. Particle VFX Catalog
- `VFX_Unit_Level_Up`: Radiant upward light pillar with expanding golden ring.
- `VFX_Melee_Hit`: Directional spark and blood/dust puff.
- `VFX_Arrow_Trail`: Soft white motion streak for projectile visibility.
- `VFX_Catapult_Impact`: Debris explosion and terrain dust crater.
- `VFX_Hero_Aura`: Subtle ground decal showing aura radius and pulsation.

---

## 4. Performance & Batching Rules
- Use particle pooling (`GPUParticles2D` / `GPUParticles3D`) with fixed pre-warmed emitters.
- Batch static environment props (trees, rocks, terrain decals) into Tilemaps or MultiMesh instances.
