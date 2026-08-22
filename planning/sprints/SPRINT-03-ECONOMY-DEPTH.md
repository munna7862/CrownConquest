# Sprint 03 — Economy depth

## Phase
Phase Economy

## Sprint Goal
Economy depth.

## Effort
- Duration: **7–9 working days**
- Planned capacity: **45 story points**
- Primary ownership: **Economy + UI**
- Planning mode: AI-assisted multi-agent sprint team

Story points are relative engineering effort. They are not a promise of elapsed time.

## Scope
Food, wood, stone, iron, gold, farms, camps, quarry, mine, worker reassignment, repair, economy balancing.

## Sprint Backlog
| ID | Story Slice | Owner | SP |
|---|---|---|---:|
| CNC-0301 | Food implementation slice | Economy | 5 |
| CNC-0302 | wood implementation slice | UI | 5 |
| CNC-0303 | stone implementation slice | Economy | 5 |
| CNC-0304 | iron implementation slice | UI | 5 |
| CNC-0305 | gold implementation slice | Economy | 5 |
| CNC-0306 | farms implementation slice | UI | 4 |
| CNC-0307 | camps implementation slice | Economy | 4 |
| CNC-0308 | quarry implementation slice | UI | 4 |
| CNC-0309 | mine implementation slice | Economy | 4 |
| CNC-0310 | worker reassignment implementation slice | UI | 4 |

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
Multiple resource strategies and production choices are viable.

## Risks
- Scope expansion beyond the sprint goal.
- Cross-agent contract drift.
- Temporary visual implementation being mistaken for finished gameplay.
- Untested edge cases becoming permanent behavior.

## Capacity Rule
If work exceeds capacity, move optional stories to the next sprint. Do not remove tests or weaken the core architecture to make the sprint appear complete.

## Next Sprint
Proceed only after the exit gate is green.
