# Sprint 02 — Economy core

## Phase
Phase Economy

## Sprint Goal
Economy core.

## Effort
- Duration: **8–10 working days**
- Planned capacity: **55 story points**
- Primary ownership: **Economy**
- Planning mode: AI-assisted multi-agent sprint team

Story points are relative engineering effort. They are not a promise of elapsed time.

## Scope
Resources, workers, gathering, storage, Town Center, Barracks, construction, population, swordsman production.

## Sprint Backlog
| ID | Story Slice | Owner | SP |
|---|---|---|---:|
| CNC-0201 | Resources implementation slice | Economy | 5 |
| CNC-0202 | workers implementation slice | Economy | 5 |
| CNC-0203 | gathering implementation slice | Economy | 5 |
| CNC-0204 | storage implementation slice | Economy | 5 |
| CNC-0205 | Town Center implementation slice | Economy | 5 |
| CNC-0206 | Barracks implementation slice | Economy | 5 |
| CNC-0207 | construction implementation slice | Economy | 5 |
| CNC-0208 | population implementation slice | Economy | 4 |
| CNC-0209 | swordsman production. implementation slice | Economy | 4 |
| CNC-0210 | Resources implementation slice | Economy | 4 |
| CNC-0211 | workers implementation slice | Economy | 4 |
| CNC-0212 | gathering implementation slice | Economy | 4 |

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
Player can gather, build a Barracks and produce an army from a fresh settlement.

## Risks
- Scope expansion beyond the sprint goal.
- Cross-agent contract drift.
- Temporary visual implementation being mistaken for finished gameplay.
- Untested edge cases becoming permanent behavior.

## Capacity Rule
If work exceeds capacity, move optional stories to the next sprint. Do not remove tests or weaken the core architecture to make the sprint appear complete.

## Next Sprint
Proceed only after the exit gate is green.
