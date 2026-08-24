# Pull Request: Sprint 11 — Missions and World Progression

## Summary
Implements the authoritative Campaign Missions and World Progression framework for **Crown & Conquest**, connecting macro grand-strategy campaign objectives directly with the tactical simulation and faction diplomacy layers. Introduces data-driven mission definitions (`Defend`, `Destroy`, `Capture`, `Escort`, `ResourceControl`), comprehensive faction relationship state machines (`AtWar`, `Hostile`, `Neutral`, `Friendly`, `Allied`), diplomatic standing modifiers for economy and trade, unboxed struct domain events, persistent mission and diplomacy save/load state serialization, presentation presenter view models, and a complete headless smoke scenario.

## Key Changes
- **Data Layer:**
  - Added [`data/definitions/missions.json`](file:///c:/Workspace/CrownConquest/data/definitions/missions.json) with historical missions covering Defend, Destroy, Capture, Escort, and Resource Control.
  - Added [`data/definitions/factions.json`](file:///c:/Workspace/CrownConquest/data/definitions/factions.json) with 5 distinct world factions, cultures, initial standing values, color palettes, and trade multipliers.
  - Added [`MissionDefinitionModel.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Data/Models/MissionDefinitionModel.cs) and [`FactionDefinitionModel.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Data/Models/FactionDefinitionModel.cs).
  - Extended [`DataLoader.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Data/Loaders/DataLoader.cs) with `LoadMissionsFromJson`, `LoadMissionsFromFile`, `LoadFactionsFromJson`, and `LoadFactionsFromFile`.
- **Domain Layer:**
  - Added [`MissionType.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/MissionType.cs), [`MissionStatus.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/MissionStatus.cs), and [`DiplomacyStanding.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/DiplomacyStanding.cs).
  - Added [`MissionDefinition.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/MissionDefinition.cs) and [`FactionDefinition.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/FactionDefinition.cs).
  - Added [`MissionRuntimeState.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/MissionRuntimeState.cs) tracking progress, deadlines, completion, and failure state.
  - Added [`FactionDiplomacyManager.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/FactionDiplomacyManager.cs) managing dynamic reputation $[-100, +100]$, standing evaluation, and economy trade multipliers.
  - Added [`MissionEngine.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/MissionEngine.cs) managing mission acceptance, objective evaluations, reward disbursements, and expiration handling.
  - Added [`MissionEvents.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/MissionEvents.cs) defining unboxed domain events for mission and diplomacy lifecycle.
  - Updated [`CampaignEngine.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/CampaignEngine.cs) to integrate `Diplomacy` and `Missions`, ticking mission evaluations and casualty/convoy arrival triggers.
  - Updated [`CampaignSaveData.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Domain/World/CampaignSaveData.cs) with deterministic JSON roundtrip serialization for active missions and faction standing.
- **Presentation Layer:**
  - Added [`CampaignMissionPresenter.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Presentation/CampaignMissionPresenter.cs) providing decoupled view models for active missions, objectives HUD, and diplomacy sheets.
  - Added [`CampaignMissionScenario.cs`](file:///c:/Workspace/CrownConquest/src/CrownConquest.Presentation/CampaignMissionScenario.cs) executing end-to-end mission workflows and campaign progressions.
- **Test Automation Suite:**
  - Authored 18 test cases across Tiers 1–4 in [`docs/testing/test_cases_catalog_S11.md`](file:///c:/Workspace/CrownConquest/docs/testing/test_cases_catalog_S11.md).
  - Implemented unit, invariant, integration, scenario, and 1,000-tick replay tests in `CrownConquest.Tests`. Cumulative test count increased from 212 to 244 tests (100% green).

## Verification Results
- `dotnet build`: 0 Warnings, 0 Errors.
- `dotnet test`: 244 passed, 0 failed, 0 skipped.
- 1,000-Tick Deterministic Replay Parity verified bit-for-bit.
