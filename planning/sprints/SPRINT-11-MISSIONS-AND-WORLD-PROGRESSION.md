# Sprint 11 — Missions and world progression

## Phase
Phase Campaign

## Sprint Goal
Missions and world progression.

## Effort
- Duration: **8–10 working days**
- Planned capacity: **55 story points**
- Primary ownership: **World + UI + QA**
- Planning mode: AI-assisted multi-agent sprint team

Story points are relative engineering effort. They are not a promise of elapsed time.

## Scope
Mission framework, defend, destroy, capture, escort, resource control, faction relationships, campaign UI, smoke suite.

## Sprint Backlog
| ID | Story Slice | Owner | SP |
|---|---|---|---:|
| CNC-1101 | Mission framework implementation slice | World | 6 |
| CNC-1102 | defend implementation slice | UI | 6 |
| CNC-1103 | destroy implementation slice | QA | 6 |
| CNC-1104 | capture implementation slice | World | 6 |
| CNC-1105 | escort implementation slice | UI | 6 |
| CNC-1106 | resource control implementation slice | QA | 5 |
| CNC-1107 | faction relationships implementation slice | World | 5 |
| CNC-1108 | campaign UI implementation slice | UI | 5 |
| CNC-1109 | smoke suite. implementation slice | QA | 5 |
| CNC-1110 | Mission framework implementation slice | World | 5 |

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
At least four connected mission types are playable.

## Risks
- Scope expansion beyond the sprint goal.
- Cross-agent contract drift.
- Temporary visual implementation being mistaken for finished gameplay.
- Untested edge cases becoming permanent behavior.

## Capacity Rule
If work exceeds capacity, move optional stories to the next sprint. Do not remove tests or weaken the core architecture to make the sprint appear complete.

## Next Sprint
Proceed only after the exit gate is green.
