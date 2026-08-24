using System;
using Xunit;
using CrownConquest.Application;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;
using CrownConquest.Presentation;

namespace CrownConquest.Tests.Domain;

/// <summary>
/// Sprint 13 — UX, Visuals and Audio tests covering HUD, selection feedback, minimap,
/// veterancy presentation, VFX, animations, buildings, audio, ambience, music,
/// accessibility, and tutorial systems.
/// </summary>
public class UxVisualsAudioTests
{
    // ─────────────────────────────────────────────────
    // Tier 1: Pure C# Domain & Math Unit Tests
    // ─────────────────────────────────────────────────

    [Fact]
    public void TC_S13_001_HudPresenter_ResourceBarViewModel_Accuracy()
    {
        // Arrange
        var eventBus = new DomainEventBus();
        var coordinator = new GameCoordinator(null, eventBus);
        var hudPresenter = new MainHudPresenter(coordinator, eventBus, FactionId.Player1);

        // Act
        var vm = hudPresenter.GetResourceBarViewModel();

        // Assert — should reflect initial state (0 resources, 0 pop)
        Assert.Equal(0, vm.Food);
        Assert.Equal(0, vm.Wood);
        Assert.Equal(0, vm.Gold);
        Assert.Equal(0, vm.Stone);
        Assert.Equal(0, vm.Iron);
        Assert.Equal(0, vm.CurrentPopulation);

        hudPresenter.Unregister();
    }

    [Fact]
    public void TC_S13_002_HudPresenter_CommandCardViewModel_Generation()
    {
        // Arrange
        var unit = CreateTestUnit("celtic_swordsman");
        var eventBus = new DomainEventBus();
        var coordinator = new GameCoordinator(null, eventBus);
        var hudPresenter = new MainHudPresenter(coordinator, eventBus, FactionId.Player1);

        // Act
        var vm = hudPresenter.GetCommandCardForUnit(unit);

        // Assert — non-worker unit should have attack, patrol, no gather
        Assert.True(vm.CanAttackMove);
        Assert.True(vm.CanPatrol);
        Assert.True(vm.CanStop);
        Assert.False(vm.CanGather);
        Assert.False(vm.CanConstruct);
        Assert.False(vm.CanRepair);
        Assert.True(vm.AvailableCommands.Length > 0);

        hudPresenter.Unregister();
    }

    [Fact]
    public void TC_S13_003_SelectionFeedback_SelectionRingDescriptor()
    {
        // Arrange
        var unit = CreateTestUnit("celtic_swordsman", new Vector2D(10f, 10f));
        var presenter = new SelectionFeedbackPresenter();
        var selectedIds = new[] { unit.Id };

        // Act
        presenter.UpdateDescriptors(
            new[] { unit }, 1,
            selectedIds, 1,
            EntityId.None);

        // Assert
        Assert.Equal(1, presenter.ActiveRingCount);
        var ring = presenter.GetSelectionRing(0);
        Assert.Equal(unit.Id, ring.UnitId);
        Assert.True(ring.IsSelected);
        Assert.False(ring.IsHovered);
        Assert.Equal(0, ring.FactionColorIndex); // Player1 = blue = 0
        Assert.True(ring.Radius > 0f);
    }

    [Fact]
    public void TC_S13_004_Minimap_WorldToMinimapCoordinateProjection()
    {
        // Arrange
        var minimap = new MinimapPresenter(200f, 200f);

        // Act
        var (x, y) = minimap.ProjectToMinimap(new Vector2D(100f, 50f));

        // Assert — (100/200, 50/200) = (0.5, 0.25)
        Assert.Equal(0.5f, x, 4);
        Assert.Equal(0.25f, y, 4);
    }

    [Fact]
    public void TC_S13_005_VeterancyBadge_RankToBadgeMapping()
    {
        // Arrange & Act & Assert
        Assert.Equal(0, VeterancyPresenter.GetBadgeIconIndex(VeterancyRank.Recruit));
        Assert.Equal(1, VeterancyPresenter.GetBadgeIconIndex(VeterancyRank.Experienced));
        Assert.Equal(2, VeterancyPresenter.GetBadgeIconIndex(VeterancyRank.Veteran));
        Assert.Equal(3, VeterancyPresenter.GetBadgeIconIndex(VeterancyRank.Elite));
        Assert.Equal(4, VeterancyPresenter.GetBadgeIconIndex(VeterancyRank.Legendary));

        Assert.Equal(0, VeterancyPresenter.GetChevronCount(VeterancyRank.Recruit));
        Assert.Equal(1, VeterancyPresenter.GetChevronCount(VeterancyRank.Experienced));
        Assert.Equal(2, VeterancyPresenter.GetChevronCount(VeterancyRank.Veteran));
        Assert.Equal(3, VeterancyPresenter.GetChevronCount(VeterancyRank.Elite));
        Assert.Equal(5, VeterancyPresenter.GetChevronCount(VeterancyRank.Legendary));
    }

    [Fact]
    public void TC_S13_006_VfxTrigger_CombatHitEffectDescriptor()
    {
        // Arrange
        var damageEvt = new DamageDealtEvent(
            SimulationTick: 10,
            AttackerId: new EntityId(1),
            TargetId: new EntityId(2),
            DamageAmount: 25f,
            RemainingHealth: 75f,
            IsCritical: false);

        // Act
        var vfx = VfxTriggerPresenter.CreateCombatHitDescriptor(damageEvt, new Vector2D(10f, 10f));

        // Assert
        Assert.Equal(VfxEffectType.CombatHit, vfx.EffectType);
        Assert.True(vfx.Intensity > 0f);
        Assert.True(vfx.Scale > 1.0f);
        Assert.Equal(10UL, vfx.TriggerTick);
    }

    [Fact]
    public void TC_S13_007_AnimationState_UnitStateToAnimationMapping()
    {
        // Assert all mappings
        Assert.Equal(AnimationState.Idle, AnimationStateMapper.MapUnitState(UnitState.Idle));
        Assert.Equal(AnimationState.Walk, AnimationStateMapper.MapUnitState(UnitState.Moving));
        Assert.Equal(AnimationState.Attack, AnimationStateMapper.MapUnitState(UnitState.Attacking));
        Assert.Equal(AnimationState.Gather, AnimationStateMapper.MapUnitState(UnitState.Gathering));
        Assert.Equal(AnimationState.Walk, AnimationStateMapper.MapUnitState(UnitState.Returning));
        Assert.Equal(AnimationState.Construct, AnimationStateMapper.MapUnitState(UnitState.Constructing));
        Assert.Equal(AnimationState.Repair, AnimationStateMapper.MapUnitState(UnitState.Repairing));
        Assert.Equal(AnimationState.Routed, AnimationStateMapper.MapUnitState(UnitState.Routed));
        Assert.Equal(AnimationState.Death, AnimationStateMapper.MapUnitState(UnitState.Dead));
    }

    [Fact]
    public void TC_S13_008_BuildingVisualState_ConstructionProgress()
    {
        // Arrange — building at 50% construction
        var building = new BuildingEntity(
            new EntityId(100), FactionId.Player1, "barracks",
            new Vector2D(20f, 20f), new Vector2D(3f, 3f),
            maxHealth: 500f, baseBuildTimeTicks: 100f);

        // Simulate 50% progress
        for (int i = 0; i < 50; i++)
        {
            building.Construct(1.0f, 0, null, out _);
        }

        // Act
        var vs = BuildingVisualPresenter.GetVisualState(building);

        // Assert
        Assert.Equal(0.5f, vs.ConstructionProgress, 1);
        Assert.Equal(BuildingVisualPhase.UnderConstruction, vs.VisualPhase);
        Assert.True(vs.ShowConstructionAnimation);
    }

    [Fact]
    public void TC_S13_009_AudioTrigger_SfxDescriptorFromCombatEvent()
    {
        // Arrange & Act
        var sfx = CombatAudioPresenter.CreateWeaponImpactDescriptor(
            damageAmount: 30f,
            attackType: "melee",
            position: new Vector2D(15f, 15f),
            tick: 42);

        // Assert
        Assert.Equal(SfxCategory.WeaponImpact, sfx.Category);
        Assert.Equal(WeaponSubCategory.Melee, sfx.WeaponType);
        Assert.True(sfx.Volume > 0f && sfx.Volume <= 1.0f);
        Assert.Equal(42UL, sfx.TriggerTick);

        // Ranged attack
        var rangedSfx = CombatAudioPresenter.CreateWeaponImpactDescriptor(20f, "ranged", new Vector2D(0f, 0f), 50);
        Assert.Equal(WeaponSubCategory.Ranged, rangedSfx.WeaponType);
    }

    [Fact]
    public void TC_S13_010_AmbienceZone_TerrainToAmbienceMapping()
    {
        // Assert terrain-to-zone mappings
        Assert.Equal(AmbienceZoneType.Plains, AmbiencePresenter.MapTerrainToZone(TerrainType.Plains));
        Assert.Equal(AmbienceZoneType.Forest, AmbiencePresenter.MapTerrainToZone(TerrainType.Forest));
        Assert.Equal(AmbienceZoneType.Mountain, AmbiencePresenter.MapTerrainToZone(TerrainType.Hills));
        Assert.Equal(AmbienceZoneType.Water, AmbiencePresenter.MapTerrainToZone(TerrainType.Water));
        Assert.Equal(AmbienceZoneType.Water, AmbiencePresenter.MapTerrainToZone(TerrainType.Marsh));

        // Verify track IDs are non-empty
        Assert.False(string.IsNullOrEmpty(AmbiencePresenter.GetTrackId(AmbienceZoneType.Forest)));
        Assert.False(string.IsNullOrEmpty(AmbiencePresenter.GetTrackId(AmbienceZoneType.Plains)));
    }

    [Fact]
    public void TC_S13_011_MusicStateMachine_StateTransitions()
    {
        // Arrange
        var music = new AdaptiveMusicPresenter(
            skirmishThreshold: 0.2f,
            battleThreshold: 0.6f,
            crossfadeDuration: 0.1f,
            peaceDelayTicks: 5);

        // Initial state = Peace
        Assert.Equal(MusicState.Peace, music.CurrentState);

        // Act — increase to skirmish
        music.Update(0.3f);
        Assert.Equal(MusicState.Skirmish, music.CurrentState);

        // Act — increase to battle
        music.Update(0.8f);
        Assert.Equal(MusicState.Battle, music.CurrentState);

        // Act — terminal state
        var victoryDesc = music.SetTerminalState(true);
        Assert.Equal(MusicState.Victory, music.CurrentState);
        Assert.Equal("mus_victory_fanfare", victoryDesc.TrackId);
    }

    [Fact]
    public void TC_S13_012_Accessibility_ColorblindPaletteRemapping()
    {
        // Arrange
        var settings = new AccessibilitySettings { ColorblindMode = ColorblindMode.Deuteranopia };
        var presenter = new AccessibilityPresenter(settings);

        // Act & Assert — colors must be distinct
        Assert.True(presenter.AreFactionColorsDistinct(0, 1));

        var p1Color = presenter.GetFactionColor(0);
        var p2Color = presenter.GetFactionColor(1);

        // Colors should not be identical
        Assert.False(p1Color.R == p2Color.R && p1Color.G == p2Color.G && p1Color.B == p2Color.B);

        // Test all colorblind modes produce distinct colors
        foreach (ColorblindMode mode in Enum.GetValues<ColorblindMode>())
        {
            settings.ColorblindMode = mode;
            Assert.True(presenter.AreFactionColorsDistinct(0, 1),
                $"Colors not distinct for mode: {mode}");
        }
    }

    [Fact]
    public void TC_S13_013_TutorialStep_ObjectiveCompletionTracking()
    {
        // Arrange
        var tutorial = new TutorialPresenter(
            titles: new[] { "Step 1", "Step 2", "Step 3" },
            objectives: new[] { "Do A", "Do B", "Do C" },
            hints: new[] { "Hint A", "Hint B", "Hint C" });

        tutorial.Start();

        // Assert initial state
        Assert.True(tutorial.IsActive);
        Assert.Equal(0, tutorial.CurrentStepIndex);
        Assert.Equal(3, tutorial.TotalSteps);
        Assert.Equal(0, tutorial.CompletedSteps);

        // Act — complete step 1
        bool result = tutorial.CompleteCurrentStep();
        Assert.True(result);
        Assert.Equal(1, tutorial.CurrentStepIndex);
        Assert.Equal(1, tutorial.CompletedSteps);

        // Verify step states
        var step0 = tutorial.GetStep(0);
        Assert.Equal(TutorialStepState.Completed, step0.State);
        var step1 = tutorial.GetStep(1);
        Assert.Equal(TutorialStepState.Active, step1.State);

        // Complete remaining steps
        tutorial.CompleteCurrentStep();
        tutorial.CompleteCurrentStep();
        Assert.True(tutorial.IsComplete);
        Assert.False(tutorial.IsActive);

        // Overlay should reflect completion
        var overlay = tutorial.GetOverlayViewModel();
        Assert.True(overlay.IsComplete);
        Assert.Equal(1.0f, overlay.ProgressPercentage, 3);
    }

    [Fact]
    public void TC_S13_014_UnitStatusPanel_MultiUnitSelectionSummary()
    {
        // Arrange
        var units = new[]
        {
            CreateTestUnit("celtic_swordsman"),
            CreateTestUnit("celtic_swordsman"),
            CreateTestUnit("celtic_swordsman"),
            CreateTestUnit("roman_archer", attackType: "ranged"),
            CreateTestUnit("roman_archer", attackType: "ranged")
        };

        var eventBus = new DomainEventBus();
        var coordinator = new GameCoordinator(null, eventBus);
        var hudPresenter = new MainHudPresenter(coordinator, eventBus, FactionId.Player1);

        // Act
        var summary = hudPresenter.GetGroupSummary(units);

        // Assert
        Assert.Equal(5, summary.TotalCount);
        Assert.True(summary.AverageHealthPercentage > 0f);
        Assert.Equal(3, summary.MeleeCount);
        Assert.Equal("celtic_swordsman", summary.PrimaryUnitType);

        hudPresenter.Unregister();
    }

    [Fact]
    public void TC_S13_015_NotificationQueue_EventDrivenNotifications()
    {
        // Arrange
        var eventBus = new DomainEventBus();
        var coordinator = new GameCoordinator(null, eventBus);
        var hudPresenter = new MainHudPresenter(coordinator, eventBus, FactionId.Player1, maxNotifications: 16);

        // Act — publish events
        eventBus.Publish(new UnitLevelUpEvent(1, new EntityId(1), 1, 2, 10f, 2f));
        eventBus.Publish(new BuildingCompletedEvent(2, new EntityId(100), FactionId.Player1, "barracks", new Vector2D(10f, 10f)));

        // Assert
        Assert.Equal(2, hudPresenter.NotificationCount);
        var n0 = hudPresenter.GetNotification(0);
        Assert.Equal(NotificationType.UnitLevelUp, n0.Type);
        var n1 = hudPresenter.GetNotification(1);
        Assert.Equal(NotificationType.BuildingCompleted, n1.Type);

        // Clear
        hudPresenter.ClearNotifications();
        Assert.Equal(0, hudPresenter.NotificationCount);

        hudPresenter.Unregister();
    }

    // ─────────────────────────────────────────────────
    // Tier 2: Simulation & Invariant Tests
    // ─────────────────────────────────────────────────

    [Fact]
    public void TC_S13_016_HudViewModelDeterminism_100TickSimulation()
    {
        // Arrange
        var config = new SimulationConfig();
        var eventBus1 = new DomainEventBus();
        var coordinator1 = new GameCoordinator(config, eventBus1);
        var hud1 = new MainHudPresenter(coordinator1, eventBus1, FactionId.Player1);

        var eventBus2 = new DomainEventBus();
        var coordinator2 = new GameCoordinator(config, eventBus2);
        var hud2 = new MainHudPresenter(coordinator2, eventBus2, FactionId.Player1);

        // Both spawn same units
        SpawnUnits(coordinator1, FactionId.Player1, 3);
        SpawnUnits(coordinator2, FactionId.Player1, 3);
        SpawnUnits(coordinator1, FactionId.Player2, 3);
        SpawnUnits(coordinator2, FactionId.Player2, 3);

        // Act — run 100 ticks each
        coordinator1.SimulateTicks(100);
        coordinator2.SimulateTicks(100);

        // Assert — HUD view models should be identical
        var vm1 = hud1.GetResourceBarViewModel();
        var vm2 = hud2.GetResourceBarViewModel();
        Assert.Equal(vm1.Food, vm2.Food);
        Assert.Equal(vm1.Wood, vm2.Wood);
        Assert.Equal(vm1.Gold, vm2.Gold);

        hud1.Unregister();
        hud2.Unregister();
    }

    [Fact]
    public void TC_S13_017_SelectionFeedbackIntegrity_SelectDeselectCycle()
    {
        // Arrange
        var units = new UnitEntity[10];
        for (int i = 0; i < 10; i++)
        {
            units[i] = CreateTestUnit("celtic_swordsman", new Vector2D(i * 3f, 10f), new EntityId(i + 1));
        }

        var presenter = new SelectionFeedbackPresenter();

        // Select first 5
        var selectedIds = new[] { units[0].Id, units[1].Id, units[2].Id, units[3].Id, units[4].Id };

        // Act
        presenter.UpdateDescriptors(units, 10, selectedIds, 5, EntityId.None);

        // Assert — exactly 5 selection rings
        Assert.Equal(5, presenter.ActiveRingCount);

        // Deselect 2 (select only 3)
        selectedIds = new[] { units[0].Id, units[1].Id, units[2].Id };
        presenter.UpdateDescriptors(units, 10, selectedIds, 3, EntityId.None);
        Assert.Equal(3, presenter.ActiveRingCount);
    }

    [Fact]
    public void TC_S13_018_MinimapUnitTracking_UnitsMovingAcrossMap()
    {
        // Arrange
        var minimap = new MinimapPresenter(200f, 200f);
        var unit = CreateTestUnit("celtic_swordsman", new Vector2D(50f, 100f));

        // Act
        minimap.UpdateBlips(
            new[] { unit }, 1,
            Array.Empty<BuildingEntity>(), 0,
            Array.Empty<EntityId>(), 0);

        // Assert
        Assert.Equal(1, minimap.ActiveUnitBlipCount);
        var blip = minimap.GetUnitBlip(0);
        Assert.Equal(0.25f, blip.MinimapX, 4); // 50/200
        Assert.Equal(0.5f, blip.MinimapY, 4);  // 100/200
    }

    [Fact]
    public void TC_S13_019_MusicStateMachine_CombatIntensityCycle()
    {
        // Arrange
        var music = new AdaptiveMusicPresenter(
            skirmishThreshold: 0.2f,
            battleThreshold: 0.6f,
            crossfadeDuration: 0.01f,
            peaceDelayTicks: 3);

        // Peace -> Skirmish -> Battle -> Peace
        music.Update(0.0f);
        Assert.Equal(MusicState.Peace, music.CurrentState);

        music.Update(0.4f);
        Assert.Equal(MusicState.Skirmish, music.CurrentState);

        music.Update(0.8f);
        Assert.Equal(MusicState.Battle, music.CurrentState);

        // Return to peace after delay
        for (int i = 0; i < 5; i++) music.Update(0.0f);
        Assert.Equal(MusicState.Peace, music.CurrentState);
    }

    [Fact]
    public void TC_S13_020_TutorialSystem_FullTutorialCompletion()
    {
        // Arrange
        var tutorial = new TutorialPresenter(
            titles: new[] { "A", "B", "C", "D", "E" },
            objectives: new[] { "Do A", "Do B", "Do C", "Do D", "Do E" },
            hints: new[] { "H1", "H2", "H3", "H4", "H5" });

        tutorial.Start();

        // Act — complete all
        for (int i = 0; i < 5; i++)
        {
            Assert.True(tutorial.IsActive);
            var overlay = tutorial.GetOverlayViewModel();
            Assert.True(overlay.IsActive);
            tutorial.CompleteCurrentStep();
        }

        // Assert
        Assert.True(tutorial.IsComplete);
        Assert.False(tutorial.IsActive);
        Assert.Equal(5, tutorial.CompletedSteps);
    }

    // ─────────────────────────────────────────────────
    // Tier 3: Multi-System Integration Tests
    // ─────────────────────────────────────────────────

    [Fact]
    public void TC_S13_021_FullHudIntegration_ResourceSelectionMinimap()
    {
        // Arrange
        var eventBus = new DomainEventBus();
        var coordinator = new GameCoordinator(null, eventBus);
        var hud = new MainHudPresenter(coordinator, eventBus, FactionId.Player1);
        var selection = new SelectionFeedbackPresenter();
        var minimap = new MinimapPresenter(200f, 200f);

        // Spawn units
        SpawnUnits(coordinator, FactionId.Player1, 5);
        coordinator.SimulateTicks(1);

        // Act — all systems should be functional
        var resources = hud.GetResourceBarViewModel();
        Assert.True(true); // Resources generated without exception

        // Minimap blips
        var units = GetAliveUnits(coordinator);
        minimap.UpdateBlips(units, units.Length, Array.Empty<BuildingEntity>(), 0, Array.Empty<EntityId>(), 0);
        Assert.Equal(5, minimap.ActiveUnitBlipCount);

        // Selection feedback
        selection.UpdateDescriptors(units, units.Length, new[] { units[0].Id }, 1, EntityId.None);
        Assert.True(selection.ActiveRingCount >= 1);

        hud.Unregister();
    }

    [Fact]
    public void TC_S13_022_VfxAudioIntegration_CombatEventPipeline()
    {
        // Arrange
        var eventBus = new DomainEventBus();
        var coordinator = new GameCoordinator(null, eventBus);
        var vfx = new VfxTriggerPresenter(eventBus);
        var audio = new CombatAudioPresenter(eventBus);

        // Spawn opposing units
        SpawnUnits(coordinator, FactionId.Player1, 5);
        SpawnUnits(coordinator, FactionId.Player2, 5);
        coordinator.SimulateTicks(1);

        // Act — run combat
        coordinator.SimulateTicks(200);

        // Assert — both VFX and audio should have received triggers
        Assert.True(vfx.PendingEffectCount > 0, "VFX should have generated effects from combat");
        Assert.True(audio.PendingTriggerCount > 0, "Audio should have generated triggers from combat");

        vfx.Unregister();
        audio.Unregister();
    }

    [Fact]
    public void TC_S13_023_VeterancyAnimationIntegration_LevelUpVisualFeedback()
    {
        // Arrange
        var eventBus = new DomainEventBus();
        var veterancy = new VeterancyPresenter(eventBus);

        // Act — simulate level-up event
        eventBus.Publish(new UnitLevelUpEvent(10, new EntityId(1), 1, 2, 15f, 2.5f));

        // Assert — level-up VFX should be pending
        Assert.Equal(1, veterancy.PendingLevelUpEffectCount);
        Assert.Equal(new EntityId(1), veterancy.GetPendingLevelUpUnit(0));

        // Consume
        Assert.True(veterancy.ConsumeLevelUpEffect(new EntityId(1)));
        Assert.Equal(0, veterancy.PendingLevelUpEffectCount);

        // Animation state should map correctly for level-up context
        var desc = AnimationStateMapper.GetDescriptor(UnitState.Idle, AnimationState.Attack);
        Assert.True(desc.HasTransitioned);

        veterancy.Unregister();
    }

    [Fact]
    public void TC_S13_024_BuildingVisualAudioIntegration_ConstructionLifecycle()
    {
        // Arrange
        var eventBus = new DomainEventBus();
        var audio = new CombatAudioPresenter(eventBus);
        var buildingPresenter = new BuildingVisualPresenter();

        var building = new BuildingEntity(
            new EntityId(200), FactionId.Player1, "barracks",
            new Vector2D(20f, 20f), new Vector2D(3f, 3f),
            maxHealth: 500f, baseBuildTimeTicks: 100f);

        // Under construction
        var vs = BuildingVisualPresenter.GetVisualState(building);
        Assert.Equal(BuildingVisualPhase.UnderConstruction, vs.VisualPhase);
        Assert.True(vs.ShowConstructionAnimation);

        // Complete construction
        for (int i = 0; i < 100; i++) building.Construct(1.0f, 0, null, out _);

        // Building completed audio trigger
        eventBus.Publish(new BuildingCompletedEvent(100, new EntityId(200), FactionId.Player1, "barracks", new Vector2D(20f, 20f)));

        vs = BuildingVisualPresenter.GetVisualState(building);
        Assert.Equal(BuildingVisualPhase.Completed, vs.VisualPhase);
        Assert.False(vs.ShowConstructionAnimation);

        // Audio trigger should fire
        Assert.True(audio.PendingTriggerCount > 0);

        audio.Unregister();
    }

    // ─────────────────────────────────────────────────
    // Tier 4: Headless E2E Scenarios
    // ─────────────────────────────────────────────────

    [Fact]
    public void TC_S13_025_UxScenario_FullMatchWithAllPresentationSystems()
    {
        // Arrange
        var scenario = new UxVisualsAudioScenario();

        // Act
        var report = scenario.RunFullScenario(500);

        // Assert
        Assert.True(report.HudResourceBarValid, "HUD resource bar should be valid");
        Assert.True(report.VfxTriggersGenerated, "VFX triggers should be generated");
        Assert.True(report.AudioTriggersGenerated, "Audio triggers should be generated");
        Assert.True(report.ColorblindPaletteDistinct, "Colorblind palette should be distinct");
        Assert.True(report.TutorialSystemValid, "Tutorial system should be valid");
        Assert.True(report.AnimationMappingValid, "Animation mapping should be valid");
        Assert.True(report.AllSystemsValid, "All presentation systems should be valid");

        scenario.Cleanup();
    }

    [Fact]
    public void TC_S13_026_DeterministicReplay_1000Ticks_BitExactParity()
    {
        // Arrange
        var config = new SimulationConfig { InitialRandomSeed = 42 };

        // Run 1: Full 1000-tick simulation
        var eventBus1 = new DomainEventBus();
        var coordinator1 = new GameCoordinator(config, eventBus1);
        SpawnUnits(coordinator1, FactionId.Player1, 5);
        SpawnUnits(coordinator1, FactionId.Player2, 5);
        coordinator1.SimulateTicks(1000);
        ulong checksum1 = coordinator1.Simulation.State.ComputeStateChecksum();

        // Run 2: Identical simulation
        var eventBus2 = new DomainEventBus();
        var coordinator2 = new GameCoordinator(config, eventBus2);
        SpawnUnits(coordinator2, FactionId.Player1, 5);
        SpawnUnits(coordinator2, FactionId.Player2, 5);
        coordinator2.SimulateTicks(1000);
        ulong checksum2 = coordinator2.Simulation.State.ComputeStateChecksum();

        // Assert — bit-exact parity
        Assert.Equal(checksum1, checksum2);
        Assert.Equal(1000UL, coordinator1.CurrentTick);
        Assert.Equal(1000UL, coordinator2.CurrentTick);
    }

    // ─────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────

    private static int _nextEntityId = 5000;

    private static UnitEntity CreateTestUnit(
        string unitType,
        Vector2D? position = null,
        EntityId? entityId = null,
        string attackType = "melee")
    {
        var id = entityId ?? new EntityId(System.Threading.Interlocked.Increment(ref _nextEntityId));
        return new UnitEntity(
            id, FactionId.Player1, unitType,
            position ?? new Vector2D(10f, 10f),
            maxHealth: 120f,
            attackDamage: 18f,
            attackRange: attackType == "ranged" ? 8.0f : 2.0f,
            movementSpeed: 4.0f,
            attackCooldownTicks: 10,
            killXpValue: 50,
            attackType: attackType);
    }

    private static void SpawnUnits(GameCoordinator coordinator, FactionId factionId, int count)
    {
        float baseX = factionId == FactionId.Player1 ? 10f : 11.5f;
        for (int i = 0; i < count; i++)
        {
            coordinator.DispatchCommand(new SpawnUnitCommand(
                factionId,
                SubmittedTick: 0,
                UnitType: factionId == FactionId.Player1 ? "celtic_swordsman" : "roman_legionary",
                Position: new Vector2D(baseX + (i * 1.0f), 10f),
                MaxHealth: 120f,
                AttackDamage: 18f,
                AttackRange: 2.0f,
                MovementSpeed: 4.0f,
                AttackCooldownTicks: 10,
                KillXpValue: 50));
        }
    }

    private static UnitEntity[] GetAliveUnits(GameCoordinator coordinator)
    {
        var state = coordinator.Simulation.State;
        var units = state.ActiveUnits;
        int count = 0;
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].IsAlive) count++;
        }
        var result = new UnitEntity[count];
        int idx = 0;
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].IsAlive) result[idx++] = units[i];
        }
        return result;
    }
}
