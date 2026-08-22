# Sprint 01 — Playable combat vertical slice

## Phase
Phase RTS Prototype + Progression

## Sprint Goal
Playable combat vertical slice.

## Effort
- Duration: **7–10 working days**
- Planned capacity: **55 story points**
- Primary ownership: **Game Systems + Combat + UI + QA**
- Planning mode: AI-assisted multi-agent sprint team

Story points are relative engineering effort. They are not a promise of elapsed time.

## Scope
Battlefield, camera, unit model, spawning, selection, movement, targeting, damage, death, kill attribution, XP, automatic level-up, progression UI.

## Sprint Backlog
| ID | Story Slice | Owner | SP |
|---|---|---|---:|
| CNC-0101 | Battlefield implementation slice | Game Systems | 5 |
| CNC-0102 | camera implementation slice | Combat | 5 |
| CNC-0103 | unit model implementation slice | UI | 5 |
| CNC-0104 | spawning implementation slice | QA | 5 |
| CNC-0105 | selection implementation slice | Game Systems | 5 |
| CNC-0106 | movement implementation slice | Combat | 5 |
| CNC-0107 | targeting implementation slice | UI | 5 |
| CNC-0108 | damage implementation slice | QA | 4 |
| CNC-0109 | death implementation slice | Game Systems | 4 |
| CNC-0110 | kill attribution implementation slice | Combat | 4 |
| CNC-0111 | XP implementation slice | UI | 4 |
| CNC-0112 | automatic level-up implementation slice | QA | 4 |

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
10v10 battle works; a killer gains XP and automatically levels; core progression tests are green.

## Risks
- Scope expansion beyond the sprint goal.
- Cross-agent contract drift.
- Temporary visual implementation being mistaken for finished gameplay.
- Untested edge cases becoming permanent behavior.

## Capacity Rule
If work exceeds capacity, move optional stories to the next sprint. Do not remove tests or weaken the core architecture to make the sprint appear complete.

## Next Sprint
Proceed only after the exit gate is green.
