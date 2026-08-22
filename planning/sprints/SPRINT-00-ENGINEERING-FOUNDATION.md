# Sprint 00 — Engineering foundation

## Phase
Phase Foundation

## Sprint Goal
Engineering foundation.

## Effort
- Duration: **5–7 working days**
- Planned capacity: **34 story points**
- Primary ownership: **Game Systems**
- Planning mode: AI-assisted multi-agent sprint team

Story points are relative engineering effort. They are not a promise of elapsed time.

## Scope
Build Godot/C# project, repository architecture, test framework, CI, logging, data conventions, agent contracts.

## Sprint Backlog
| ID | Story Slice | Owner | SP |
|---|---|---|---:|
| CNC-0001 | Build Godot/C# project implementation slice | Game Systems | 5 |
| CNC-0002 | repository architecture implementation slice | Game Systems | 5 |
| CNC-0003 | test framework implementation slice | Game Systems | 4 |
| CNC-0004 | CI implementation slice | Game Systems | 4 |
| CNC-0005 | logging implementation slice | Game Systems | 4 |
| CNC-0006 | data conventions implementation slice | Game Systems | 4 |
| CNC-0007 | agent contracts. implementation slice | Game Systems | 4 |
| CNC-0008 | Build Godot/C# project implementation slice | Game Systems | 4 |

## Dependencies
- Previous sprint must have passed its exit gate.
- Shared contracts must be agreed before cross-domain implementation.
- QA work begins during implementation, not after it.

## Integration Strategy
1. Establish the domain contract.
2. Implement the smallest vertical slice.
3. Integrate with existing simulation.
4. Add automated tests.
5. Connect UI/presentation.
6. Run regression.
7. Demonstrate the complete slice.

## Acceptance Criteria
- The scoped systems work through the real game simulation.
- No authoritative state is owned by presentation code.
- Balance/configuration values are data-driven where applicable.
- Existing regression tests remain green.
- Cross-domain contracts are documented.

## QA Gate
- Build passes.
- Relevant unit tests pass.
- Relevant integration tests pass.
- No new critical regression.
- Manual smoke test completed for the sprint's main scenario.

## Definition of Done
- Implementation complete.
- Acceptance criteria verified.
- Automated tests added or updated.
- Existing tests pass.
- Integration complete.
- Save/load impact considered.
- Performance impact considered.
- Documentation/contracts updated.
- QA accepted.

## Sprint Exit
Clean checkout builds, game launches, tests run locally and in CI, architecture review passes.

## Risks
- Scope expansion beyond the sprint goal.
- Cross-agent contract drift.
- Temporary visual implementation being mistaken for finished gameplay.
- Untested edge cases becoming permanent behavior.

## Capacity Rule
If work exceeds capacity, move optional stories to the next sprint. Do not remove tests or weaken the core architecture to make the sprint appear complete.

## Next Sprint
Proceed only after the exit gate is green.
