# Sprint 04 — Eras and technology

## Phase
Phase Civilization

## Sprint Goal
Eras and technology.

## Effort
- Duration: **8–10 working days**
- Planned capacity: **55 story points**
- Primary ownership: **Economy + Combat**
- Planning mode: AI-assisted multi-agent sprint team

Story points are relative engineering effort. They are not a promise of elapsed time.

## Scope
Era model, advancement, technology definitions/prerequisites, research, Blacksmith, Archery Range, Stable, Archer, Spearman, Cavalry.

## Sprint Backlog
| ID | Story Slice | Owner | SP |
|---|---|---|---:|
| CNC-0401 | Era model implementation slice | Economy | 5 |
| CNC-0402 | advancement implementation slice | Combat | 5 |
| CNC-0403 | technology definitions/prerequisites implementation slice | Economy | 5 |
| CNC-0404 | research implementation slice | Combat | 5 |
| CNC-0405 | Blacksmith implementation slice | Economy | 5 |
| CNC-0406 | Archery Range implementation slice | Combat | 5 |
| CNC-0407 | Stable implementation slice | Economy | 5 |
| CNC-0408 | Archer implementation slice | Combat | 4 |
| CNC-0409 | Spearman implementation slice | Economy | 4 |
| CNC-0410 | Cavalry. implementation slice | Combat | 4 |
| CNC-0411 | Era model implementation slice | Economy | 4 |
| CNC-0412 | advancement implementation slice | Combat | 4 |

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
At least two eras, multiple unit classes and technologies work end-to-end.

## Risks
- Scope expansion beyond the sprint goal.
- Cross-agent contract drift.
- Temporary visual implementation being mistaken for finished gameplay.
- Untested edge cases becoming permanent behavior.

## Capacity Rule
If work exceeds capacity, move optional stories to the next sprint. Do not remove tests or weaken the core architecture to make the sprint appear complete.

## Next Sprint
Proceed only after the exit gate is green.
