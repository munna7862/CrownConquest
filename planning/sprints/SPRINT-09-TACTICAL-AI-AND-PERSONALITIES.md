# Sprint 09 — Tactical AI and personalities

## Phase
Phase Enemy AI

## Sprint Goal
Tactical AI and personalities.

## Effort
- Duration: **8–10 working days**
- Planned capacity: **55 story points**
- Primary ownership: **AI + Combat + QA**
- Planning mode: AI-assisted multi-agent sprint team

Story points are relative engineering effort. They are not a promise of elapsed time.

## Scope
Focus fire, flanking, formation choice, terrain tactics, siege decisions, aggressive, defensive, expansionist and tactical personalities.

## Sprint Backlog
| ID | Story Slice | Owner | SP |
|---|---|---|---:|
| CNC-0901 | Focus fire implementation slice | AI | 5 |
| CNC-0902 | flanking implementation slice | Combat | 5 |
| CNC-0903 | formation choice implementation slice | QA | 5 |
| CNC-0904 | terrain tactics implementation slice | AI | 5 |
| CNC-0905 | siege decisions implementation slice | Combat | 5 |
| CNC-0906 | aggressive implementation slice | QA | 5 |
| CNC-0907 | defensive implementation slice | AI | 5 |
| CNC-0908 | expansionist and tactical personalities. implementation slice | Combat | 5 |
| CNC-0909 | Focus fire implementation slice | QA | 5 |
| CNC-0910 | flanking implementation slice | AI | 5 |
| CNC-0911 | formation choice implementation slice | Combat | 5 |

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
AI personalities behave differently while following the same game rules.

## Risks
- Scope expansion beyond the sprint goal.
- Cross-agent contract drift.
- Temporary visual implementation being mistaken for finished gameplay.
- Untested edge cases becoming permanent behavior.

## Capacity Rule
If work exceeds capacity, move optional stories to the next sprint. Do not remove tests or weaken the core architecture to make the sprint appear complete.

## Next Sprint
Proceed only after the exit gate is green.
