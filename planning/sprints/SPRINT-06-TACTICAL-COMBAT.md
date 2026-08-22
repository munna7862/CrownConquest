# Sprint 06 — Tactical combat

## Phase
Phase Advanced Combat

## Sprint Goal
Tactical combat.

## Effort
- Duration: **10–12 working days**
- Planned capacity: **65 story points**
- Primary ownership: **Combat + World + UI**
- Planning mode: AI-assisted multi-agent sprint team

Story points are relative engineering effort. They are not a promise of elapsed time.

## Scope
Terrain modifiers, formations, Line, Shield Wall, Wedge, morale, routing, ranged combat, cavalry charge, tactical UI.

## Sprint Backlog
| ID | Story Slice | Owner | SP |
|---|---|---|---:|
| CNC-0601 | Terrain modifiers implementation slice | Combat | 6 |
| CNC-0602 | formations implementation slice | World | 6 |
| CNC-0603 | Line implementation slice | UI | 6 |
| CNC-0604 | Shield Wall implementation slice | Combat | 6 |
| CNC-0605 | Wedge implementation slice | World | 6 |
| CNC-0606 | morale implementation slice | UI | 5 |
| CNC-0607 | routing implementation slice | Combat | 5 |
| CNC-0608 | ranged combat implementation slice | World | 5 |
| CNC-0609 | cavalry charge implementation slice | UI | 5 |
| CNC-0610 | tactical UI. implementation slice | Combat | 5 |
| CNC-0611 | Terrain modifiers implementation slice | World | 5 |
| CNC-0612 | formations implementation slice | UI | 5 |

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
Terrain, formations, morale and composition materially affect battle outcomes.

## Risks
- Scope expansion beyond the sprint goal.
- Cross-agent contract drift.
- Temporary visual implementation being mistaken for finished gameplay.
- Untested edge cases becoming permanent behavior.

## Capacity Rule
If work exceeds capacity, move optional stories to the next sprint. Do not remove tests or weaken the core architecture to make the sprint appear complete.

## Next Sprint
Proceed only after the exit gate is green.
