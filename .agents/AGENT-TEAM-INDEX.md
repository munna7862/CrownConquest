# Crown & Conquest — Agent Team Index

## Core Multi-Agent Operating Contract
- [`AGENTS.md`](file:///c:/Workspace/CrownConquest/.agents/AGENTS.md) — Universal Agile Operating Contract & Quality Gates

## Specialist Agent Skills

| Category | Skill Path | Role Description |
|:---|:---|:---|
| **Direction & Process** | [`skills/game-director/SKILL.md`](file:///c:/Workspace/CrownConquest/.agents/skills/game-director/SKILL.md) | Vision keeper, scope control, balance criteria, feature approvals |
| | [`skills/sprint-coordinator/SKILL.md`](file:///c:/Workspace/CrownConquest/.agents/skills/sprint-coordinator/SKILL.md) | 16-sprint backlog management, story lifecycle, inter-agent handoffs |
| **Core Simulation** | [`skills/game-systems/SKILL.md`](file:///c:/Workspace/CrownConquest/.agents/skills/game-systems/SKILL.md) | Authoritative simulation, ECS/domain entities, fixed-tick loop, save/load |
| | [`skills/combat/SKILL.md`](file:///c:/Workspace/CrownConquest/.agents/skills/combat/SKILL.md) | Combat math, unit XP/leveling, formations, morale, siege warfare |
| | [`skills/economy/SKILL.md`](file:///c:/Workspace/CrownConquest/.agents/skills/economy/SKILL.md) | 5 resources, gathering state machines, building grids, tech trees, eras |
| | [`skills/hero/SKILL.md`](file:///c:/Workspace/CrownConquest/.agents/skills/hero/SKILL.md) | RPG hero layer, abilities, auras, inventory, equipment, leadership |
| | [`skills/ai/SKILL.md`](file:///c:/Workspace/CrownConquest/.agents/skills/ai/SKILL.md) | Hierarchical AI (Strategic, Economic, Military, Tactical, Unit), personalities |
| | [`skills/world/SKILL.md`](file:///c:/Workspace/CrownConquest/.agents/skills/world/SKILL.md) | Procedural/authored maps, terrain modifiers, navigation, campaign world |
| **Presentation & UI** | [`skills/ui/SKILL.md`](file:///c:/Workspace/CrownConquest/.agents/skills/ui/SKILL.md) | RTS HUD, drag selection, minimap, command cards, hero sheets |
| | [`skills/art-presentation/SKILL.md`](file:///c:/Workspace/CrownConquest/.agents/skills/art-presentation/SKILL.md) | Visual integration, animation controllers, particle VFX, level-up celebration |
| | [`skills/audio/SKILL.md`](file:///c:/Workspace/CrownConquest/.agents/skills/audio/SKILL.md) | Dynamic adaptive music, unit voice barks, combat impact SFX |
| **Quality & Operations** | [`skills/performance/SKILL.md`](file:///c:/Workspace/CrownConquest/.agents/skills/performance/SKILL.md) | 1,000+ unit scale, spatial partitioning, zero hot-loop allocations, 60fps |
| | [`skills/qa/SKILL.md`](file:///c:/Workspace/CrownConquest/.agents/skills/qa/SKILL.md) | Headless simulation test runner, combat invariant fuzzing, test pyramid |
| | [`skills/release/SKILL.md`](file:///c:/Workspace/CrownConquest/.agents/skills/release/SKILL.md) | Godot 4 C# export packaging, GitHub Actions CI/CD, Windows installers |
| **Standards** | [`skills/dev-coding-standards/SKILL.md`](file:///c:/Workspace/CrownConquest/.agents/skills/dev-coding-standards/SKILL.md) | C# 12 / .NET 8 / Godot 4 coding conventions, domain decoupling |
| | [`skills/doc-implementation-standards/SKILL.md`](file:///c:/Workspace/CrownConquest/.agents/skills/doc-implementation-standards/SKILL.md) | Architecture ADRs, combat/economy data schemas, test catalogs |

## Master Plan & Roadmap References
- **Master Plan:** [`planning/master/Master Plan — Original Celtic Kings + Age of Empires Inspired RTS.md`](file:///c:/Workspace/CrownConquest/planning/master/Master%20Plan%20%E2%80%94%20Original%20Celtic%20Kings%20+%20Age%20of%20Empires%20Inspired%20RTS.md)
- **Phase Index:** [`planning/phases/PHASE-INDEX.md`](file:///c:/Workspace/CrownConquest/planning/phases/PHASE-INDEX.md) (Phases 00–11)
- **Sprint Roadmap:** [`planning/sprints/SPRINT-ROADMAP.md`](file:///c:/Workspace/CrownConquest/planning/sprints/SPRINT-ROADMAP.md) (Sprints 00–15)

## Operating Model

The virtual team executes under a collaborative sprint framework.
No agent acts as an isolated code generator.

The standard execution lifecycle is:
$$\text{Plan} \longrightarrow \text{Implement} \longrightarrow \text{Integrate} \longrightarrow \text{Test} \longrightarrow \text{Review} \longrightarrow \text{QA Gate} \longrightarrow \text{Done}$$
