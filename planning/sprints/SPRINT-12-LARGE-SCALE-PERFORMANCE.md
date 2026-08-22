# Sprint 12 — Large-scale performance

## Phase
Phase Performance

## Sprint Goal
Large-scale performance.

## Effort
- Duration: **10–12 working days**
- Planned capacity: **60 story points**
- Primary ownership: **Performance + AI + World**
- Planning mode: AI-assisted multi-agent sprint team

Story points are relative engineering effort. They are not a promise of elapsed time.

## Scope
Profiling, 100/250/500-unit benchmarks, spatial queries, AI scheduling, pathfinding optimization, event optimization, memory work.

## Sprint Backlog
| ID | Story Slice | Owner | SP |
|---|---|---|---:|
| CNC-1201 | Profiling implementation slice | Performance | 6 |
| CNC-1202 | 100/250/500-unit benchmarks implementation slice | AI | 6 |
| CNC-1203 | spatial queries implementation slice | World | 6 |
| CNC-1204 | AI scheduling implementation slice | Performance | 6 |
| CNC-1205 | pathfinding optimization implementation slice | AI | 6 |
| CNC-1206 | event optimization implementation slice | World | 6 |
| CNC-1207 | memory work. implementation slice | Performance | 6 |
| CNC-1208 | Profiling implementation slice | AI | 6 |
| CNC-1209 | 100/250/500-unit benchmarks implementation slice | World | 6 |
| CNC-1210 | spatial queries implementation slice | Performance | 6 |

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
Performance bottlenecks are measured and addressed with benchmark evidence.

## Risks
- Scope expansion beyond the sprint goal.
- Cross-agent contract drift.
- Temporary visual implementation being mistaken for finished gameplay.
- Untested edge cases becoming permanent behavior.

## Capacity Rule
If work exceeds capacity, move optional stories to the next sprint. Do not remove tests or weaken the core architecture to make the sprint appear complete.

## Next Sprint
Proceed only after the exit gate is green.
