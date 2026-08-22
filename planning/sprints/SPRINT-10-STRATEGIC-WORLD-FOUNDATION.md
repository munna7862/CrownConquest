# Sprint 10 — Strategic world foundation

## Phase
Phase Campaign

## Sprint Goal
Strategic world foundation.

## Effort
- Duration: **10–12 working days**
- Planned capacity: **65 story points**
- Primary ownership: **World + Game Systems**
- Planning mode: AI-assisted multi-agent sprint team

Story points are relative engineering effort. They are not a promise of elapsed time.

## Scope
World regions, strategic map, armies, movement, territory, resource locations, battle transition, survivor transfer, campaign state, save/load.

## Sprint Backlog
| ID | Story Slice | Owner | SP |
|---|---|---|---:|
| CNC-1001 | World regions implementation slice | World | 6 |
| CNC-1002 | strategic map implementation slice | Game Systems | 6 |
| CNC-1003 | armies implementation slice | World | 6 |
| CNC-1004 | movement implementation slice | Game Systems | 6 |
| CNC-1005 | territory implementation slice | World | 6 |
| CNC-1006 | resource locations implementation slice | Game Systems | 6 |
| CNC-1007 | battle transition implementation slice | World | 6 |
| CNC-1008 | survivor transfer implementation slice | Game Systems | 6 |
| CNC-1009 | campaign state implementation slice | World | 6 |
| CNC-1010 | save/load. implementation slice | Game Systems | 6 |
| CNC-1011 | World regions implementation slice | World | 5 |

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
Army can move on world map, enter battle and return with survivors/progression.

## Risks
- Scope expansion beyond the sprint goal.
- Cross-agent contract drift.
- Temporary visual implementation being mistaken for finished gameplay.
- Untested edge cases becoming permanent behavior.

## Capacity Rule
If work exceeds capacity, move optional stories to the next sprint. Do not remove tests or weaken the core architecture to make the sprint appear complete.

## Next Sprint
Proceed only after the exit gate is green.
