---
name: hero
description: Hero & RPG Specialist persona for Crown & Conquest RPG hero entities, active and passive abilities, aura radii, talent progression trees, equipment inventory, and leadership mechanics.
---

# Hero Agent Skill — Crown & Conquest

## 1. Mission
The **Hero Specialist** owns the RPG hero layer of Crown & Conquest, inspired by *Celtic Kings: Rage of War*. Heroes are commanders, leaders, and tactical focal points on the battlefield, not merely high-stat damage dealers.

---

## 2. Hero Entity Structure & Progression

Every Hero maintains:
- **Identity & Class:** Name, Hero Class (e.g. Warlord, Druid, Centurion, Ranger), Faction, Level, Experience.
- **Attributes:** Strength (Attack & Health), Agility (Attack Speed & Movement), Willpower (Mana / Stamina & Ability Potency).
- **Leadership Capacity:** Determines how many standard combat units can be bound into the Hero's direct army division (e.g. 10–50 units).
- **Army Auras:** Passive combat bonuses bestowed upon attached squad members (e.g. +Morale, +Movement Speed, +Armor).
- **Inventory / Equipment:** 4–6 equipment slots (Weapon, Armor, Relic, Consumables) granting active abilities or stat boosts.
- **Talent Trees:** Branching skill trees unlocked at key level milestones (Levels 1, 3, 5, 7, 10).

---

## 3. Ability Design & Execution Lifecycle

All hero abilities follow a rigid lifecycle:

```text
Player / AI Triggers Ability
       │
       ▼
Validate State (Cooldown ready, Mana/Stamina >= Cost, Hero not stunned, Target in range & line of sight)
       │
       ▼
Deduct Cost & Start Cooldown Timer
       │
       ▼
Begin Cast Time (If cast time > 0, can be interrupted by heavy stun/displacement)
       │
       ▼
Apply Ability Effects (Area-of-effect damage, Healing, Buff/Debuff status effect, Summon)
       │
       ▼
Emit HeroAbilityExecutedEvent
```

### Ability Invariants:
- Ability definitions are externalized in data templates (Cost, Cooldown, Range, Radius, TargetMask, Duration).
- Cooldowns tick in simulation time, not real-world wall-clock time.
- Auras evaluate attachments via spatial partitioning query at fixed intervals (e.g. every 10 ticks) rather than per frame.

---

## 4. Hero Attachment & Army Leadership
- **Squad Binding:** Players can select a group of units and assign them to a Hero (`AttachToHeroCommand`).
- **Formation Anchor:** Attached units move in formation around the Hero and receive the Hero's active leadership auras.
- **Morale Boost:** Attached units gain high resistance to routing while the Hero is alive.
- **Hero Death & Panic:** If a Hero falls in combat, attached squad units suffer a severe morale penalty and risk immediate panic/routing.
- **Hero Recovery:** Fallen heroes drop a tomb/gravestone and can be revived at the Hero Hall after a respawn cooldown and gold cost.

---

## 5. Testing & Verification Checklist
- [x] Hero ability costs, cooldowns, and range limits verified.
- [x] Aura buffs apply correctly to attached units and clear upon leaving radius or hero death.
- [x] Equipment stat modifiers apply and remove cleanly upon equip/unequip.
- [x] Hero progression and talent tree state serialize and deserialize without loss.
