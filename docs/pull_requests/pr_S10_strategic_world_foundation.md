# Pull Request: Sprint 10 — Strategic World Foundation & Territory Conquest

## Summary
Implements the authoritative Strategic World Foundation for **Crown & Conquest**, connecting macro grand-strategy campaign management directly with the tactical real-time simulation layer. Introduces data-driven province definitions, strategic province topology graph with BFS pathfinding, army formation and terrain-weighted multi-tick movement, turn-based resource yield economy accumulation, tactical battle execution with full individual unit/hero veterancy progression retention, territory ownership control distribution, headless progression scenarios, and robust campaign save/load serialization.

## Key Changes
- **Data Layer:**
  - Added [`data/definitions/provinces.json`](file:///c:/Workspace/CrownConquest/data/definitions/provinces.json) with 9 interconnected historical provinces, terrain types, strategic node roles, resource yields, and defense modifiers.
  - Added [`ProvinceDefinitionModel.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Data/Models/ProvinceDefinitionModel.cs) and extended [`DataLoader.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Data/Loaders/DataLoader.cs) with `LoadProvincesFromJson` and `LoadProvincesFromFile`.
- **Domain Layer:**
  - Added [`ProvinceId.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/ProvinceId.cs), [`StrategicNodeType.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/StrategicNodeType.cs), [`StrategicArmyId.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/StrategicArmyId.cs), [`StrategicStance.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/StrategicStance.cs), [`StrategicUnitSpec.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/StrategicUnitSpec.cs), and [`StrategicHeroSpec.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/StrategicHeroSpec.cs).
  - Added [`StrategicProvince.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/StrategicProvince.cs) and [`StrategicArmy.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/StrategicArmy.cs).
  - Added [`StrategicMap.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/StrategicMap.cs) with BFS shortest-path province route planning.
  - Added [`StrategicMovementCalculator.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/StrategicMovementCalculator.cs) with terrain and stance duration multipliers.
  - Added [`StrategicTerritoryManager.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/StrategicTerritoryManager.cs) tracking dynamic territory ownership and faction control percentages.
  - Added [`BattleTransitionEngine.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/BattleTransitionEngine.cs) seamlessly translating campaign forces into tactical battles and extracting surviving unit/hero XP, levels, and rank advancement.
  - Added [`CampaignEngine.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/CampaignEngine.cs) orchestrating turn and tick progressions, movement arrivals, hostile engagements, territory conquest, and turn resource dividends.
  - Added [`CampaignEvents.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/CampaignEvents.cs) defining unboxed domain events dispatched through `DomainEventBus`.
  - Added [`CampaignSaveData.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/CampaignSaveData.cs) with JSON roundtrip serialization.
- **Presentation Layer:**
  - Added [`CampaignPresenter.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Presentation/CampaignPresenter.cs) providing reactive view models for UI HUD, map overlay, and territory control gauges.
  - Added [`CampaignProgressionScenario.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Presentation/CampaignProgressionScenario.cs) executing headless strategic marches, tactical battle conquest, and territory control shifts.
- **Test Automation Suite:**
  - Authored 18 test cases across Tiers 1–4 in [`docs/testing/test_cases_catalog_S10.md`](file:///c:/Workspace/CrownConquest/docs/testing/test_cases_catalog_S10.md).
  - Implemented unit, invariant, integration, scenario, and 1,000-tick replay tests in `CrownConquest.Tests`. Cumulative test count increased from 194 to 212 tests (100% green).

## Verification Results
- `dotnet build`: 0 Warnings, 0 Errors.
- `dotnet test`: 212 passed, 0 failed, 0 skipped.
- 1,000-Tick Deterministic Replay Parity verified bit-for-bit.
