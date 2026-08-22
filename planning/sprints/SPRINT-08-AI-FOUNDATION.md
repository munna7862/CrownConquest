# Sprint 08 — AI foundation

## Phase
Phase Enemy AI

## Sprint Goal
AI foundation.

## Effort
- Duration: **10–12 working days**
- Planned capacity: **65 story points**
- Primary ownership: **AI + Economy + Combat**
- Planning mode: AI-assisted multi-agent sprint team

Story points are relative engineering effort. They are not a promise of elapsed time.

## Scope
Perception, worker AI, resource priorities, build orders, production, army composition, army controller, attack/defend, targeting, retreat.

## Sprint Backlog
| ID | Story Slice | Owner | SP |
|---|---|---|---:|
| CNC-0801 | Perception implementation slice | AI | 6 |
| CNC-0802 | worker AI implementation slice | Economy | 6 |
| CNC-0803 | resource priorities implementation slice | Combat | 6 |
| CNC-0804 | build orders implementation slice | AI | 6 |
| CNC-0805 | production implementation slice | Economy | 6 |
| CNC-0806 | army composition implementation slice | Combat | 5 |
| CNC-0807 | army controller implementation slice | AI | 5 |
| CNC-0808 | attack/defend implementation slice | Economy | 5 |
| CNC-0809 | targeting implementation slice | Combat | 5 |
| CNC-0810 | retreat. implementation slice | AI | 5 |
| CNC-0811 | Perception implementation slice | Economy | 5 |
| CNC-0812 | worker AI implementation slice | Combat | 5 |

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
AI can independently build an economy, raise an army, attack, defend and win.

## Risks
- Scope expansion beyond the sprint goal.
- Cross-agent contract drift.
- Temporary visual implementation being mistaken for finished gameplay.
- Untested edge cases becoming permanent behavior.

## Capacity Rule
If work exceeds capacity, move optional stories to the next sprint. Do not remove tests or weaken the core architecture to make the sprint appear complete.

## Next Sprint
Proceed only after the exit gate is green.
