# Phase 00 — Foundation

## Objective
Establish the engineering foundation for Crown & Conquest before gameplay implementation begins.

## Goals
- Initialize the Godot 4 + C# desktop project.
- Establish repository conventions and architecture boundaries.
- Configure automated testing and CI.
- Create the multi-agent development structure.
- Establish data-driven configuration and logging.
- Create the minimum playable project shell.

## Scope

### Project Setup
- Godot 4 project.
- C# solution/project structure.
- Desktop target.
- Debug/release configurations.
- Git repository and branch conventions.

### Architecture
Create clear boundaries for:
- Core
- Simulation
- Combat
- Economy
- Units
- Heroes
- Buildings
- Technology
- AI
- Terrain
- Formations
- Siege
- Campaign
- Save
- Presentation

### Testing
Set up:
- Unit-test project.
- Integration-test project.
- Test execution from command line.
- CI test execution.
- Basic test reporting.

### Development Agents
Create:
- Game Director
- Game Systems
- Combat
- Economy
- AI
- World
- UI
- Art/Presentation
- Audio
- QA
- Performance
- Release

## Deliverables
- Working Godot project.
- C# solution.
- Repository structure.
- AGENTS.md.
- Initial SKILL.md files.
- CI workflow.
- Test framework.
- Architecture documentation.
- Coding standards.

## Definition of Done
- Project launches successfully.
- A test can run locally.
- The same test can run in CI.
- Core assemblies/namespaces have defined ownership.
- No gameplay logic is placed in UI-only code.
- A new developer/agent can understand where a new feature belongs.

## Exit Criteria
Foundation is considered complete only when the project can build, launch, test, and run through CI without gameplay-specific dependencies.
