# Sprint 13 — UX, visuals and audio

## Phase
Phase Polish

## Sprint Goal
UX, visuals and audio.

## Effort
- Duration: **10–15 working days**
- Planned capacity: **70 story points**
- Primary ownership: **UI + Art + Audio**
- Planning mode: AI-assisted multi-agent sprint team

Story points are relative engineering effort. They are not a promise of elapsed time.

## Scope
HUD, selection feedback, minimap, veteran presentation, VFX, animations, buildings, combat audio, ambience, music, accessibility, tutorial.

## Sprint Backlog
| ID | Story Slice | Owner | SP |
|---|---|---|---:|
| CNC-1301 | HUD implementation slice | UI | 6 |
| CNC-1302 | selection feedback implementation slice | Art | 6 |
| CNC-1303 | minimap implementation slice | Audio | 6 |
| CNC-1304 | veteran presentation implementation slice | UI | 6 |
| CNC-1305 | VFX implementation slice | Art | 6 |
| CNC-1306 | animations implementation slice | Audio | 6 |
| CNC-1307 | buildings implementation slice | UI | 6 |
| CNC-1308 | combat audio implementation slice | Art | 6 |
| CNC-1309 | ambience implementation slice | Audio | 6 |
| CNC-1310 | music implementation slice | UI | 6 |
| CNC-1311 | accessibility implementation slice | Art | 5 |
| CNC-1312 | tutorial. implementation slice | Audio | 5 |

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
A new player understands the core loop without developer assistance.

## Risks
- Scope expansion beyond the sprint goal.
- Cross-agent contract drift.
- Temporary visual implementation being mistaken for finished gameplay.
- Untested edge cases becoming permanent behavior.

## Capacity Rule
If work exceeds capacity, move optional stories to the next sprint. Do not remove tests or weaken the core architecture to make the sprint appear complete.

## Next Sprint
Proceed only after the exit gate is green.
