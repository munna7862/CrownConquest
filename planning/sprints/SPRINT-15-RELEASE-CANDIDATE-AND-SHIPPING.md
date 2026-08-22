# Sprint 15 — Release candidate and shipping

## Phase
Phase Release

## Sprint Goal
Release candidate and shipping.

## Effort
- Duration: **7–10 working days**
- Planned capacity: **45 story points**
- Primary ownership: **Release + QA**
- Planning mode: AI-assisted multi-agent sprint team

Story points are relative engineering effort. They are not a promise of elapsed time.

## Scope
Release pipeline, clean-machine validation, packaging, smoke automation, final performance, save/load validation, docs, final regression.

## Sprint Backlog
| ID | Story Slice | Owner | SP |
|---|---|---|---:|
| CNC-1501 | Release pipeline implementation slice | Release | 5 |
| CNC-1502 | clean-machine validation implementation slice | QA | 5 |
| CNC-1503 | packaging implementation slice | Release | 5 |
| CNC-1504 | smoke automation implementation slice | QA | 5 |
| CNC-1505 | final performance implementation slice | Release | 5 |
| CNC-1506 | save/load validation implementation slice | QA | 4 |
| CNC-1507 | docs implementation slice | Release | 4 |
| CNC-1508 | final regression. implementation slice | QA | 4 |
| CNC-1509 | Release pipeline implementation slice | Release | 4 |
| CNC-1510 | clean-machine validation implementation slice | QA | 4 |

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
Reproducible release candidate passes all release gates.

## Risks
- Scope expansion beyond the sprint goal.
- Cross-agent contract drift.
- Temporary visual implementation being mistaken for finished gameplay.
- Untested edge cases becoming permanent behavior.

## Capacity Rule
If work exceeds capacity, move optional stories to the next sprint. Do not remove tests or weaken the core architecture to make the sprint appear complete.

## Next Sprint
Proceed only after the exit gate is green.
