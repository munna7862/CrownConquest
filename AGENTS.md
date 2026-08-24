# Universal Multi-Agent Agile Development Rules & Operating Contract

These rules apply universally to all tasks, workflows, and projects within the **Crown & Conquest** workspace. All AI agents, automated pipelines, and contributors must strictly adhere to this operating contract.

---

## 1. The Core Engineering & Local-First Mandates

- **Local-First Windows Desktop RTS/RPG:** Crown & Conquest is a 100% local Windows desktop game built with **Godot 4 + C# (.NET 8)**. Agents **MUST NOT** introduce backend cloud services, external databases, microservices, web servers, or remote telemetry platforms.
- **Authoritative Deterministic Simulation:** Game logic (combat, XP progression, economy, pathfinding, building, AI decisions) must run entirely decoupled from rendering/presentation. The simulation must be 100% deterministic given the same initial state and input commands.
- **Hardware & Desktop Guardrails:** Enforce strict memory bounds ($< 500\text{ MB}$ total application footprint), non-blocking main/render threads, 60fps rendering frame budgets, and zero dynamic memory allocations in the hot game loop (`Update` / `FixedUpdate`).

---

## 2. Decoupled Architecture & Type Safety Standards

- **Decoupled Layering:** Maintain strict unidirectional dependency flow:
  $$\text{Presentation (Godot Nodes / UI / VFX / Audio)} \longrightarrow \text{Application / Game Coordinator} \longrightarrow \text{Domain Simulation} \longrightarrow \text{Data / Config Providers}$$
  - **Domain Simulation Layer:** Pure C# domain entities, ECS/systems, combat math, individual unit leveling/XP, economy state machines, technology trees, pathfinding, and AI. Completely independent of Godot `Node` hierarchies or graphics rendering.
  - **UI / Presentation Layer:** Godot Control nodes, HUD, minimap, selection overlays, health bars, animation players, particle emitters, and audio players. Never evaluate or mutate game state directly in UI nodes.
  - **Data / Config Layer:** Externalized JSON/Resource data files for unit stats, XP curves, costs, tech requirements, and map definitions. No hard-coded combat or economy numbers in simulation logic.
- **Single Authoritative State:** The Domain Simulation is the single runtime source of truth. UI state is transient; presentation nodes observe and mirror simulation state via typed Domain Events.
- **Strict C# Type Safety:** C# 12 / .NET 8 with nullable reference types enabled (`<Nullable>enable</Nullable>`). Avoid `dynamic` or untyped `object` casting. Enforce strong typing for Entity IDs, Command payloads, and Domain Events.
- **Centralized Error Handling:** Never leak unformatted raw stack traces or null reference panics. All command dispatches, loaders, and domain actions must return standardized `Result<T>` and `Result` structs with `GameError`.

---

## 3. Signature Gameplay Mechanic: Individual Unit Progression

Every combat-capable unit has its own persistent battlefield progression:
$$\text{Combat Engagement} \longrightarrow \text{Kill Event} \longrightarrow \text{Award Kill XP} \longrightarrow \text{Automatic Level-Up} \longrightarrow \text{Veterancy Rank Advancement} \longrightarrow \text{Stat & Visual Upgrades}$$

### Core Progression Invariants:
1. **Immediate Level-Up:** When a unit achieves the required XP threshold, the level-up is evaluated and applied immediately on the current simulation tick.
2. **Attribution Integrity:** Exactly one killer unit receives kill XP per casualty. No XP is awarded for friendly fire, suicide, or from already deceased attackers.
3. **Veterancy Rank Progression:** Level 1–2 (Recruit) $\to$ Level 3–4 (Experienced) $\to$ Level 5–6 (Veteran) $\to$ Level 7–8 (Elite) $\to$ Level 9+ (Legendary).
4. **Data-Driven Curves:** Level thresholds, XP values, and stat multipliers are loaded from data definitions and never hardcoded.

---

## 4. Test Pyramid & Regression Guardrails

```mermaid
graph TD
    subgraph Pyramid ["Crown & Conquest Test Pyramid"]
        E2E["Tier 4: Godot Headless E2E Scenarios / Full Matches (10%)"]
        SystemInt["Tier 3: Multi-System & Economy/Combat Integration Tests (25%)"]
        SimFuzz["Tier 2: Deterministic Simulation & Progression Invariant Fuzzing (30%)"]
        UnitTests["Tier 1: Pure C# Domain & Math Unit Tests (xUnit/NUnit) (35%)"]
    end

    E2E --> SystemInt
    SystemInt --> SimFuzz
    SimFuzz --> UnitTests
```

### 4.1 Test Tier Ownership & Responsibilities
- **Tier 1 (Domain Unit Tests - xUnit):** Combat formulas, damage calculations, individual unit XP gain, level thresholds, stat scaling, resource gathering rates, tech requirements, and building costs. Owned by: **Domain Specialist Architect & SDET / QA Architect**.
- **Tier 2 (Simulation & Invariant Fuzzing - xUnit):** Deterministic headless simulation ticks, randomized combat encounters, kill attribution invariants, save/load state roundtrips, and fixed-tick cooldown countdowns. Owned by: **SDET / QA Architect & Core Systems Architect**.
- **Tier 3 (Multi-System Integration - xUnit):** Production queues, worker state machines, formation movements, morale routing, AI decision loops, hero ability cooldowns, and aura buffs. Owned by: **Dev Architect & SDET / QA Architect**.
- **Tier 4 (Headless E2E & Scenarios):** Full match simulations, win/loss condition triggers, scenario loading, and headless engine validation. Owned by: **SDET / QA Architect & DevOps / Release Engineer**.

### 4.2 Anti-Flakiness Rules
- **Zero Real-Time Sleeps:** Never use `Thread.Sleep()`, `Task.Delay()`, or wall-clock timers for simulation tests. Step the simulation deterministically using fixed tick counts (`SimulateTicks(int count)`).
- **Deterministic Randomness:** All procedural generators, AI decisions, and combat variance must utilize explicit seeded random number generators (`System.Random(seed)`).

---

## 5. Scrum Team Personas & Handoff Sequence

The AI assistant operates under specialized virtual team personas depending on the active sprint stage:

| Role | Persona Name | Primary Responsibility |
|:---|:---|:---|
| **SM** | **Scrum Master / Sprint Coordinator** | Sprint backlog management, task deconstruction in `task.md`, workflow handoffs, DoD enforcement |
| **GD / PO** | **Game Director & Product Owner** | Game balance vision, scope guardrails, historical authenticity, acceptance criteria sign-off |
| **ARCH** | **Domain & Gameplay Specialist Architects** | `game-systems`, `combat`, `economy`, `hero`, `ai`, `world` simulation math, commands, events, ECS state |
| **SDET** | **QA & Test Automation Architect** | Pre-implementation Test Cases Catalog (`test_cases_catalog_SXX.md`), unit/invariant/integration tests, QA Quality Gate Review |
| **SDE** | **Dev Architect & Senior Gameplay SDE** | Presentation layer, UI controls, data loaders, Godot presentation integration, Dev Technical Code Review |
| **PERF** | **Performance & Scalability Officer** | Memory bounds ($< 500\text{ MB}$), zero hot-loop GC allocations, spatial partitioning benchmarks, SIMD scaling |
| **DO** | **DevOps & Release Engineer** | Git feature branching, CI/CD automation, `dotnet build` lint validation, GitHub PR creation via `gh pr create` |

### Multi-Agent Handoff Sequence & Refinement Loop

```mermaid
sequenceDiagram
    autonumber
    actor User as Human Stakeholder / PO
    participant SM as Scrum Master (Sprint Coordinator)
    participant GD as Game Director / PO
    participant ARCH as Domain Specialist Architect
    participant SDET as SDET / QA Architect
    participant SDE as Dev Architect / Senior SDE
    participant PERF as Performance Officer
    participant DO as DevOps & Release Engineer

    User->>SM: Kick off Sprint / Story (e.g. /goal start and execute Sprint XX)
    SM->>SM: Verify prerequisites & deconstruct backlog in task.md
    SM->>GD: Review Gameplay Balance & Acceptance Criteria
    GD->>ARCH: Align Domain Simulation Design & Formulas
    ARCH->>SDET: Hand over Domain Invariants & Specifications
    SDET->>SDET: Draft Pre-Implementation Test Cases Catalog (test_cases_catalog_SXX.md)
    SDET->>SDE: Hand over verified Test Catalog
    SDE->>SDE: Check out feature/sprint-XX branch & implement Data/Domain/Presentation code

    rect rgb(30, 41, 59)
    note over SDE,PERF: Dev Technical Code Acceptance & Performance Audit Loop
    SDE->>SDE: Execute Dev Technical Code Acceptance Review
    SDE->>PERF: Submit changes for Hot-Loop Allocation & Memory Audit
    alt Allocation / Performance Issues Found
        PERF->>SDE: Reject (BLOCKING) & log in task.md -> SDE refactors to zero-allocations
    end
    end

    PERF->>SDET: Trigger Automation Suite

    rect rgb(30, 41, 59)
    note over SDET,GD: QA & Game Director Acceptance Review Loop
    SDET->>SDET: Script test suites & conduct Test Automation Quality Gate Review
    alt Flaky / Failed Tests
        SDET->>SDE: Log BLOCKING defect comments in task.md -> SDE fixes & re-triggers QA
    end
    SDET->>GD: Hand over feature with 100% Green Test Report (Tiers 1-4)
    GD->>GD: Conduct Gameplay & Product Acceptance Criteria Review
    alt Acceptance Feedback Raised
        GD->>SDE: Reject & log review comments -> SDE refactors on branch
    end
    end

    GD->>DO: Approve Sprint -> Authorize Release & Pull Request
    DO->>DO: Commit atomic changes, push branch, create GitHub PR via gh cli, & link in task.md
    DO->>User: Hand off for Human Review & Merge (<!-- GOAL_COMPLETE -->)
```

### 5.3 Sprint Execution Stage-by-Stage Protocol

When the user initiates a sprint (e.g. `/goal start and execute Sprint XX`), the AI assistant must execute the stages in exact sequential order, explicitly announcing the active persona and publishing the 5-point handoff report at each transition:

1. **Stage 1 — Scrum Master (SM): Backlog Deconstruction & Planning**
   - Read the sprint specification in `planning/sprints/SPRINT-XX-*.md`.
   - Initialize/Update `task.md` with the story ownership matrix and checklist.
   - Output `### [ACTIVE PERSONA: SCRUM MASTER (SM)]` followed by `### Persona Handoff Report: SM -> GD`.

2. **Stage 2 — Game Director (GD) & Domain Specialist Architect (ARCH): Design Alignment**
   - Review domain mechanics, combat/progression balance, data models, and formulas.
   - Define exact formula requirements and state invariants.
   - Output `### [ACTIVE PERSONA: GAME DIRECTOR / DOMAIN ARCHITECT (GD/ARCH)]` followed by `### Persona Handoff Report: ARCH -> SDET`.

3. **Stage 3 — SDET / QA Architect (SDET): Pre-Implementation Test Catalog**
   - Author `docs/testing/test_cases_catalog_SXX.md` with complete positive, negative, boundary, and invariant test matrix across Tiers 1–4.
   - Output `### [ACTIVE PERSONA: QA & SDET ARCHITECT (SDET)]` followed by `### Persona Handoff Report: SDET -> SDE`.

4. **Stage 4 — Dev Architect & Gameplay SDE (SDE): Implementation**
   - Create and checkout `feature/sprint-XX-<name>` branch.
   - Implement Data definitions/loaders, Domain entities/systems, Commands/Events, and Presentation view models.
   - Perform Dev Technical Code Acceptance Review.
   - Output `### [ACTIVE PERSONA: DEV ARCHITECT & SENIOR SDE (SDE)]` followed by `### Persona Handoff Report: SDE -> PERF`.

5. **Stage 5 — Performance Officer (PERF): Zero-Allocation Hot-Loop Audit**
   - Audit simulation hot loops (`Tick`, `UpdateUnits`, `UpdateCombat`, `UpdateEconomy`) for zero per-tick dynamic heap allocations.
   - Ensure memory bounds ($< 500\text{ MB}$) and DOD/struct optimizations.
   - Output `### [ACTIVE PERSONA: PERFORMANCE OFFICER (PERF)]` followed by `### Persona Handoff Report: PERF -> SDET`.

6. **Stage 6 — SDET / QA Architect (SDET): Test Automation Quality Gate**
   - Script unit, invariant, integration, and headless scenario tests across Tiers 1–4.
   - Execute `dotnet test` and ensure 100% green pass rate (0 failures, 0 skips).
   - Verify 1000-tick deterministic replay checksum equality.
   - Output `### [ACTIVE PERSONA: QA & SDET ARCHITECT (SDET)]` followed by `### Persona Handoff Report: SDET -> GD`.

7. **Stage 7 — Game Director & Product Owner (GD/PO): Acceptance Review**
   - Conduct acceptance review against sprint acceptance criteria and headless scenarios.
   - Verify game balance, pacing, and visual presentation integration.
   - Output `### [ACTIVE PERSONA: GAME DIRECTOR & PRODUCT OWNER (GD/PO)]` followed by `### Persona Handoff Report: GD -> DO`.

8. **Stage 8 — DevOps & Release Engineer (DO): Release, Branch & Pull Request**
   - Execute `dotnet build` to confirm 0 warnings and 0 errors.
   - Stage and commit changes using conventional commit messages.
   - Push feature branch to origin and create Pull Request via `gh pr create`.
   - Update `task.md` and `walkthrough.md` with all execution data and PR link.
   - Output `### [ACTIVE PERSONA: DEVOPS & RELEASE ENGINEER (DO)]` and conclude with `<!-- GOAL_COMPLETE -->`.

---


## 6. Review Severity & Refinement Protocol

### 6.1 Review Severity Classification
Reviewers must classify all comments into three explicit categories:
- **`BLOCKING`:** Functional bugs, broken invariants, failing tests, non-deterministic RNG, hot-loop memory allocations, architectural layering breaches. **Prevents the next gate.**
- **`NON-BLOCKING`:** Minor code style, naming, or documentation improvements to be resolved before sprint sign-off.
- **`SUGGESTION`:** Optional gameplay polish or optimization ideas logged for future sprint consideration.

### 6.2 Refinement Loop Logging in `task.md`
When defects or quality gaps are identified:
1. The reviewing persona records explicit feedback under `## Sprint Review Comments & Refinement Loop` in `task.md`:
   `[REVIEWER_ROLE] -> [TARGET_ROLE]: [SEVERITY] - Description of issue, failing test/criteria, and required fix.`
2. The target persona checks out the feature branch, implements fixes, and re-executes tests.
3. Handoff occurs only after the reviewing persona posts a formal **APPROVED** sign-off.

---

## 7. Strict Failure-Handling & Anti-Bypass Rules

Agents and developers are strictly prohibited from bypassing failures:
1. **No Test Suppression:** `[Fact(Skip = "...")]`, `[Theory(Skip = "...")]`, or commenting out assertions is strictly forbidden.
2. **No Assertion Weakening:** Never relax exact formulas or checksum assertions to trivial checks (e.g. `Assert.NotNull()`) to mask simulation drift.
3. **No Compiler / Linter Disabling:** `#pragma warning disable` without documented sign-off is prohibited. Zero warnings policy enforced.
4. **No Fabricated Evidence:** Quality gate reports and PR summaries must reflect actual local command execution output.

---

## 8. Git, Branching, and Commit Standards

- **No Direct Commits to Main:** NEVER push code directly to the `main` branch.
- **Branching Strategy:** Autonomously check out an isolated branch for every sprint using `feature/sprint-<XX>-<name>` or `bugfix/<issue-name>`.
- **Atomic Conventional Commits:** Autonomously commit work incrementally using conventional prefixes:
  - `feat:` New gameplay mechanics, domain systems, or UI components.
  - `fix:` Bug fixes or invariant corrections.
  - `test:` Test catalogs, unit/invariant/integration/E2E test additions.
  - `docs:` Test catalogs, PR artifacts, walkthroughs, architectural documentation.
  - `refactor:` Code refactoring with zero functional changes.
  - `perf:` Allocation reductions or algorithmic optimizations.
- **Automated Pull Requests:** Upon Game Director / PO approval, DevOps Engineer pushes branch (`git push -u origin <branch>`) and creates the remote PR using `gh pr create` with a full summary linked in `task.md`.

---

## 9. Standardized Agent Handoff Specification

Every handoff between personas (e.g. from Scrum Master to SDET Architect, or SDET to Dev Architect) must follow this 5-point template in both chat communication and `task.md` logging:

```markdown
### Persona Handoff Report: [CURRENT_ROLE] -> [TARGET_ROLE]

1. **Completed Work:** Granular list of tasks, files, and artifacts produced in this stage.
2. **Remaining Work:** Pending tasks required for sprint completion.
3. **Executed Tests & Results:** Exact commands run, pass/fail counts, and lint/build status.
4. **Known Issues or Deferred Items:** Any non-blocking observations or items deferred to future sprints.
5. **Next Assigned Persona & Verification Required:** Explicit instruction for the incoming persona's gate.
```

---

## 10. Protected Workspace Boundaries & File Areas

To ensure codebase integrity and prevent accidental regressions or unauthorized system modifications during autonomous execution, files are categorized into protected zones:

| Protection Tier | Workspace Paths & Files | Modification Rules |
|:---|:---|:---|
| **Tier 1: Core Solution & Projects** | `CrownConquest.sln`, `src/*/*.csproj`, `tests/*/*.csproj`, `project.godot` | Modified ONLY when dependency additions or project structure changes are explicitly authorized in the active sprint plan. |
| **Tier 2: Core Domain Architecture** | `src/CrownConquest.Domain/`, `docs/architecture/` | Requires explicit sign-off from **Domain Specialist Architect** and **Performance Officer**. Zero Godot dependencies and zero hot-loop allocations. |
| **Tier 3: CI/CD Workflows** | `.github/workflows/ci.yml`, `.github/pull_request_template.md` | Modified ONLY by **DevOps Engineer** with full YAML validation. |
| **Tier 4: Agent Governance & Rules** | `AGENTS.md`, `.agents/AGENTS.md`, `.agents/skills/` | Synced and maintained across canonical locations. |
| **Tier 5: Version Control & System Metadata** | `.git/`, `.gitignore` | Agents must never directly edit files inside `.git/` directory. |

---

## 11. Terminal Command Execution Boundaries

### 11.1 Allowed Terminal Commands
Agents may autonomously execute standard non-destructive development, testing, and lifecycle commands:
- **Testing & Quality:** `dotnet test`, `dotnet build`, `dotnet clean`.
- **Git & Version Control:** `git status`, `git diff`, `git log`, `git branch`, `git checkout -b <branch>`, `git checkout <branch>`, `git add <files>`, `git commit -m "<message>"`, `git push -u origin <branch>`.
- **GitHub CLI:** `gh pr create`, `gh pr view`, `gh pr status`, `gh pr list`, `gh run list`.

### 11.2 Strictly Blocked & Prohibited Commands
Agents are **strictly prohibited** from running destructive, uncontrolled, or hazardous commands:
- **Destructive Git Operations:** `git push --force`, `git push -f`, `git reset --hard`, `git clean -fxd`, `git rebase` on public branches.
- **Destructive OS / Filesystem Operations:** `rm -rf /`, `rmdir /s /q C:\`, deleting files outside the project workspace, executing unreviewed binary executables (`.exe`, `.bat`, `.ps1`) from untrusted origins.
- **Network / Cloud Infrastructure:** Starting unsolicited background servers, establishing external network sockets, downloading third-party binaries, or communicating with external telemetry endpoints.

---

## 12. Review Artifact Expectations & Schemas

Agents must produce standard, structured markdown artifacts in designated workspace directories:
- **`task.md`**: Root tracking document showing active sprint, granular task statuses (`[ ]`, `[/]`, `[x]`), persona handoff log, and review refinement loop comments.
- **`docs/testing/test_cases_catalog_S<YY>.md`**: SDET / QA Architect pre-implementation test catalog with positive, negative, boundary test cases, invariants, and pass/fail criteria.
- **`docs/pull_requests/pr_S<YY>_<name>.md`**: DevOps Engineer pull request submission artifact containing summary, checklist, test results, and DoD verification.
- **`docs/`**: Architecture and system design documents.
- **File Links:** All file references in documentation and reviews MUST use clickable markdown links with absolute file URIs (`file:///c:/Workspace/CrownConquest/...`).

---

## 13. Sprint Definition of Done (DoD)

A sprint story or milestone is complete and ready for release only when:
- [x] **Scope Satisfied:** Implemented strictly per acceptance criteria with no speculative feature drift.
- [x] **100% Green Automation:** All unit, simulation, and integration tests pass cleanly (`dotnet test`).
- [x] **Clean Build & Lint:** `dotnet build` succeeds with 0 errors and 0 warnings.
- [x] **Performance Budget Verified:** Hot simulation loops contain 0 dynamic allocations and run within frame budgets.
- [x] **Save/Load & Replay Compatibility:** Domain entities serialize cleanly; 1000-tick replay matches checksums.
- [x] **Game Director & QA Acceptance:** Formal sign-off in `task.md`.
- [x] **Git Feature Branch & PR Created:** Feature branch pushed to origin and PR submitted via `gh pr create`.
- [x] **Documentation & Walkthrough:** Architecture changes, test catalogs, and `walkthrough.md` updated with real execution data.
