# Sprint 16 Test Cases Catalog: Godot 2D Graphical RTS Viewport & Presentation Integration

## 1. Overview & Test Strategy
This catalog outlines the automated and headless verification strategy for **Sprint 16** (Phase 12). It defines positive, negative, boundary, and regression tests across Tiers 1–4 to guarantee that the Godot 2D graphical rendering canvas, RTS HUD, camera controller, and input dispatcher seamlessly integrate with the authoritative C# simulation engine with zero regressions.

---

## 2. Test Cases Matrix

| Test ID | Category | Tier | Target Component | Description & Invariant Checked | Expected Outcome |
|:---|:---|:---:|:---|:---|:---|
| **`TC-S16-001`** | Positive | Tier 1 | `RtsCameraController` | Screen-to-world and world-to-screen coordinate inversion parity | `ScreenToWorld(WorldToScreen(P)) == P` within $\epsilon = 0.001$ |
| **`TC-S16-002`** | Boundary | Tier 1 | `RtsCameraController` | Camera position bounds clamping at map borders ($X \in [0, 200], Y \in [0, 200]$) | Position clamped strictly inside bounds |
| **`TC-S16-003`** | Boundary | Tier 1 | `RtsCameraController` | Camera zoom factor clamped between $MinZoom (0.5\times)$ and $MaxZoom (3.0\times)$ | Zoom factor never exceeds defined limits |
| **`TC-S16-004`** | Positive | Tier 1 | `SelectionFeedbackPresenter` | Drag selection box enclosing single and multiple unit entities | Returns all living player-owned units within box bounds |
| **`TC-S16-005`** | Negative | Tier 1 | `SelectionFeedbackPresenter` | Drag selection box containing enemy units when friendly units also present | Enemy units excluded from player command selection |
| **`TC-S16-006`** | Positive | Tier 1 | `VeterancyPresenter` | Veterancy rank badge color and icon mapping for Levels 1 through 10 | Level 1-2: None/Recruit, 3-4: Bronze, 5-6: Silver, 7-8: Gold, 9+: Crown |
| **`TC-S16-007`** | Positive | Tier 1 | `ResourceBarHudPresenter` | Real-time 5-resource and population counter synchronization | Matches exact domain bank quantities for Food, Wood, Gold, Stone, Iron |
| **`TC-S16-008`** | Positive | Tier 1 | `MinimapPresenter` | Minimap radar coordinate projection ($World (0..200) \to Radar (0..160)$) | Units mapped to normalized radar coords with faction color blips |
| **`TC-S16-009`** | Positive | Tier 2 | `CommandCardPresenter` | Command card available actions based on selection type (Worker vs Soldier vs Hero) | Worker has Gather/Build; Soldier has Formations; Hero has Ability buttons |
| **`TC-S16-010`** | Invariant | Tier 2 | `HeroPresenter` | Ability button cooldown progression sweep ($0.0 \to 1.0$) during ticks | Cooldown percentage decrements each tick until 0 (Ready) |
| **`TC-S16-011`** | Positive | Tier 2 | `VfxTriggerPresenter` | Floating damage text generation and lifetime expiration (20 ticks) | Damage numbers fade and remove cleanly after duration |
| **`TC-S16-012`** | Invariant | Tier 2 | `BuildingVisualPresenter` | Construction progress ratio ($0.0 \to 1.0$) and placement grid snap (2.0m) | Unfinished buildings display build bar; constructed show full health |
| **`TC-S16-013`** | Integration | Tier 3 | `GameRootNode` | Game simulation tick loop advancing at fixed 20Hz under presentation bridge | Simulation engine advances 20 ticks per second cleanly |
| **`TC-S16-014`** | Integration | Tier 3 | `PresentationEventBridge` | Combat kill events dispatch level-up visual aura triggers and floating text | `HeroLevelUpEvent` and `UnitPromotedEvent` trigger visual badges |
| **`TC-S16-015`** | Invariant | Tier 3 | `ZeroAllocationGuard` | Zero heap memory allocation per frame during `_Draw()` rendering cycles | 0 GC allocations per frame in steady state |
| **`TC-S16-016`** | Scenario | Tier 4 | `GraphicalE2EScenario` | Full interactive RTS loop: Spawn -> Box Select -> Move -> Attack -> Level Up -> Advance Era | All presentation viewmodels stay synchronized throughout scenario |

---

## 3. Regression Guardrails
- Total cumulative passing tests baseline: **331 tests**.
- Sprint 16 target tests: **345+ tests**.
- 1,000-tick deterministic simulation bit-for-bit checksum parity must be preserved.
