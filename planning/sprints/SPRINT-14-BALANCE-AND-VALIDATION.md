# Sprint 14 — Balance and validation

## Phase
Phase QA

## Sprint Goal
Balance and validation.

## Effort
- Duration: **10–12 working days**
- Planned capacity: **65 story points**
- Primary ownership: **QA + Combat + AI + Performance**
- Planning mode: AI-assisted multi-agent sprint team

Story points are relative engineering effort. They are not a promise of elapsed time.

## Scope
Unit/integration audit, deterministic battle simulator, 1000-battle balance runs, faction reports, progression balance, AI difficulty, save/load, soak testing.

## Sprint Backlog
| ID | Story Slice | Owner | SP |
|---|---|---|---:|
| CNC-1401 | Unit/integration audit implementation slice | QA | 7 |
| CNC-1402 | deterministic battle simulator implementation slice | Combat | 7 |
| CNC-1403 | 1000-battle balance runs implementation slice | AI | 7 |
| CNC-1404 | faction reports implementation slice | Performance | 7 |
| CNC-1405 | progression balance implementation slice | QA | 7 |
| CNC-1406 | AI difficulty implementation slice | Combat | 6 |
| CNC-1407 | save/load implementation slice | AI | 6 |
| CNC-1408 | soak testing. implementation slice | Performance | 6 |
| CNC-1409 | Unit/integration audit implementation slice | QA | 6 |
| CNC-1410 | deterministic battle simulator implementation slice | Combat | 6 |

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
No critical defects; balance and persistence meet agreed thresholds.

## Risks
- Scope expansion beyond the sprint goal.
- Cross-agent contract drift.
- Temporary visual implementation being mistaken for finished gameplay.
- Untested edge cases becoming permanent behavior.

## Capacity Rule
If work exceeds capacity, move optional stories to the next sprint. Do not remove tests or weaken the core architecture to make the sprint appear complete.

## Next Sprint
Proceed only after the exit gate is green.
