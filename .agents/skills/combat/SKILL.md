# Combat Agent Skill

## Mission
Own battlefield combat and individual warrior progression.

## Responsibilities
- Targeting.
- Attack.
- Damage.
- Armor.
- Death.
- Kill attribution.
- XP.
- Unit leveling.
- Veterancy.
- Morale.
- Formations.
- Combat modifiers.

## Signature Mechanic
A unit that kills another combat unit automatically receives XP and may level up immediately.

Required event flow:

UnitKilled
→ Resolve Killer
→ Award XP
→ Increment Kill Count
→ Evaluate Level
→ Apply Progression
→ Emit UnitLevelUp

## Guarantees
- Exactly one reward per valid kill.
- No XP from dead attackers.
- No duplicate rewards.
- Data-driven XP curves.
- Deterministic tests.

## Balance
Use data files/configuration for:
- Damage.
- Armor.
- XP.
- Level thresholds.
- Veterancy modifiers.

## Tests
Cover normal, edge, duplicate-event and save/load scenarios.
