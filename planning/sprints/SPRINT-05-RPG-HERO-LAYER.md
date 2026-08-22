# Sprint 05 — RPG hero layer

## Phase
Phase Heroes

## Sprint Goal
RPG hero layer.

## Effort
- Duration: **8–10 working days**
- Planned capacity: **55 story points**
- Primary ownership: **Hero + Combat + UI**
- Planning mode: AI-assisted multi-agent sprint team

Story points are relative engineering effort. They are not a promise of elapsed time.

## Scope
Hero model, XP, levels, attributes, abilities, cooldowns, leadership aura, offensive ability, hero UI, persistence.

## Sprint Backlog
| ID | Story Slice | Owner | SP |
|---|---|---|---:|
| CNC-0501 | Hero model implementation slice | Hero | 6 |
| CNC-0502 | XP implementation slice | Combat | 6 |
| CNC-0503 | levels implementation slice | UI | 6 |
| CNC-0504 | attributes implementation slice | Hero | 6 |
| CNC-0505 | abilities implementation slice | Combat | 6 |
| CNC-0506 | cooldowns implementation slice | UI | 5 |
| CNC-0507 | leadership aura implementation slice | Hero | 5 |
| CNC-0508 | offensive ability implementation slice | Combat | 5 |
| CNC-0509 | hero UI implementation slice | UI | 5 |
| CNC-0510 | persistence. implementation slice | Hero | 5 |

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
A hero changes battle outcomes and retains progression through save/load.

## Risks
- Scope expansion beyond the sprint goal.
- Cross-agent contract drift.
- Temporary visual implementation being mistaken for finished gameplay.
- Untested edge cases becoming permanent behavior.

## Capacity Rule
If work exceeds capacity, move optional stories to the next sprint. Do not remove tests or weaken the core architecture to make the sprint appear complete.

## Next Sprint
Proceed only after the exit gate is green.
