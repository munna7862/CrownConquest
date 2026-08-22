---
name: combat
description: Combat & Progression Specialist persona for Crown & Conquest signature unit leveling, Kill-to-XP formulas, damage calculations, tactical formations, morale and routing, and siege warfare.
---

# Combat Agent Skill — Crown & Conquest

## 1. Mission
The **Combat Specialist** owns all battlefield interactions, weapon damage calculations, armor mitigation, tactical formations, unit morale, siege mechanics, and the signature **Individual Unit Progression** system.

---

## 2. Signature Gameplay Mechanic: Individual Unit Progression

In Crown & Conquest, every combat-capable unit maintains its own persistent combat record and progresses independently throughout the match.

```text
Unit Kills Target
       │
       ▼
Resolve Killer Entity (Must be alive and valid)
       │
       ▼
Calculate Kill XP:
  Kill XP = BaseTargetXP + (TargetLevel * LevelBonus) + TargetTierBonus
       │
       ▼
Award XP to Killer & Increment Total Kills (+1)
       │
       ▼
Evaluate Level Threshold:
  While CurrentXP >= Threshold(CurrentLevel + 1) -> LevelUp()
       │
       ▼
Apply Archetype Stat Progression (Health, Damage, Armor, Speed)
       │
       ▼
Evaluate Veterancy Rank (Recruit -> Experienced -> Veteran -> Elite -> Legendary)
       │
       ▼
Emit UnitLevelUpEvent / VeterancyRankChangedEvent
```

### Invariant Rules:
- **Immediate Execution:** Level-up happens on the exact tick the kill occurs.
- **Single Reward:** Exactly one killer receives credit. Assist XP is optional and data-configured, but kill XP is uniquely attributed.
- **No Dead Attacker XP:** If the attacker is dead when a projectile lands, no XP is awarded to prevent dangling state corruption.
- **Data-Driven Progression Tables:** Level thresholds and stat gains must never be hardcoded into C# classes; they must be loaded from external JSON/Resource configuration.

---

## 3. Combat Calculations & Damage Types

### 1. Damage Mitigation Formula
$$\text{Effective Damage} = \max\left(1, (\text{Raw Attack Damage} \times \text{Damage Modifier}) - \text{Target Armor}\right)$$

### 2. Unit Archetypes & Rock-Paper-Scissors Balance
- **Infantry (Swordsmen / Spearmen):** Strong against Cavalry, vulnerable to Archers & Siege.
- **Cavalry (Scouts / Knights):** High mobility, flanking bonus, strong against Archers & Siege, vulnerable to Spearmen.
- **Ranged (Archers / Crossbowmen):** Long range, elevation bonuses, vulnerable to Cavalry charges.
- **Siege (Rams / Catapults / Ballistas):** Heavy structural damage vs buildings and fortifications, slow reload, high friendly-fire risk.

---

## 4. Tactical Formations & Morale

### 1. Formations
- **Line Formation:** Maximizes melee front, vulnerable to flanking.
- **Box / Square Formation:** All-around defensive bonus (+Armor, -Speed), protects ranged units inside.
- **Wedge Formation:** +Charge damage, breaks through enemy lines.
- **Column / Travel:** High movement speed on roads, -Defense when ambushed.
- **Loose / Skirmish:** -50% damage from area-of-effect and siege projectiles, weak to cavalry.

### 2. Morale & Routing
- Morale is depleted by: heavy casualties in squad, death of nearby friendly hero, flanking attacks, encirclement.
- When Morale drops to 0, the unit enters **Routed** state (loses control, flees towards nearest friendly settlement, -Armor).
- Morale is restored by: nearby Hero aura, Rally abilities, winning local skirmishes.

---

## 5. Testing & Verification Checklist
- [x] Kill XP attribution verified in single-kill, multi-kill, and AoE scenarios.
- [x] Automatic level-up stat increases match data tables.
- [x] Veterancy rank boundaries trigger appropriate events.
- [x] Formation offset calculations handle obstacles without unit overlap.
- [x] Morale breakdown and recovery tested under deterministic conditions.
