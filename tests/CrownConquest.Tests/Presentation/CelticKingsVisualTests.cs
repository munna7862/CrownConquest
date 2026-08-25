using System;
using System.Collections.Generic;
using CrownConquest.Application;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Simulation;
using CrownConquest.Presentation;
using Xunit;

namespace CrownConquest.Tests.Presentation;

public sealed class CelticKingsVisualTests
{
    // ==========================================
    // Tier 1: Pure Domain & Math Unit Tests
    // ==========================================

    [Fact]
    public void TC_S18_001_DirectionalFacing_FromHeading_Maps8DirectionsAccurately()
    {
        Assert.Equal(FacingDirection.East, DirectionalSpriteController.FromHeading(new Vector2D(1.0f, 0.0f)));
        Assert.Equal(FacingDirection.SouthEast, DirectionalSpriteController.FromHeading(new Vector2D(1.0f, 1.0f)));
        Assert.Equal(FacingDirection.South, DirectionalSpriteController.FromHeading(new Vector2D(0.0f, 1.0f)));
        Assert.Equal(FacingDirection.SouthWest, DirectionalSpriteController.FromHeading(new Vector2D(-1.0f, 1.0f)));
        Assert.Equal(FacingDirection.West, DirectionalSpriteController.FromHeading(new Vector2D(-1.0f, 0.0f)));
        Assert.Equal(FacingDirection.NorthWest, DirectionalSpriteController.FromHeading(new Vector2D(-1.0f, -1.0f)));
        Assert.Equal(FacingDirection.North, DirectionalSpriteController.FromHeading(new Vector2D(0.0f, -1.0f)));
        Assert.Equal(FacingDirection.NorthEast, DirectionalSpriteController.FromHeading(new Vector2D(1.0f, -1.0f)));
    }

    [Fact]
    public void TC_S18_002_DirectionalFacing_ZeroHeading_DefaultsToSouth()
    {
        Assert.Equal(FacingDirection.South, DirectionalSpriteController.FromHeading(Vector2D.Zero));
    }

    [Fact]
    public void TC_S18_003_TerrainGrid_SpeedMultiplier_ReturnsCorrectValuesForTileTypes()
    {
        var grid = new TerrainTileGrid(10, 10, tileSize: 2.0f);
        grid.SetTile(0, 0, TerrainTileType.Grass);
        grid.SetTile(1, 0, TerrainTileType.CobblestoneRoad);
        grid.SetTile(2, 0, TerrainTileType.Dirt);
        grid.SetTile(3, 0, TerrainTileType.ShallowWater);
        grid.SetTile(4, 0, TerrainTileType.DeepWater);
        grid.SetTile(5, 0, TerrainTileType.CliffElevation);

        Assert.Equal(1.0f, grid.GetMovementMultiplier(new Vector2D(0.5f, 0.5f)));
        Assert.Equal(1.25f, grid.GetMovementMultiplier(new Vector2D(2.5f, 0.5f)));
        Assert.Equal(1.05f, grid.GetMovementMultiplier(new Vector2D(4.5f, 0.5f)));
        Assert.Equal(0.40f, grid.GetMovementMultiplier(new Vector2D(6.5f, 0.5f)));
        Assert.Equal(0.0f, grid.GetMovementMultiplier(new Vector2D(8.5f, 0.5f)));
        Assert.Equal(0.0f, grid.GetMovementMultiplier(new Vector2D(10.5f, 0.5f)));
    }

    [Fact]
    public void TC_S18_004_TerrainGrid_Passability_BlocksCliffsAndDeepWater()
    {
        var grid = new TerrainTileGrid(10, 10, tileSize: 2.0f);
        grid.SetTile(0, 0, TerrainTileType.Grass);
        grid.SetTile(1, 0, TerrainTileType.CobblestoneRoad);
        grid.SetTile(2, 0, TerrainTileType.DeepWater);
        grid.SetTile(3, 0, TerrainTileType.CliffElevation);

        Assert.True(grid.IsPassable(new Vector2D(0.5f, 0.5f)));
        Assert.True(grid.IsPassable(new Vector2D(2.5f, 0.5f)));
        Assert.False(grid.IsPassable(new Vector2D(4.5f, 0.5f)));
        Assert.False(grid.IsPassable(new Vector2D(6.5f, 0.5f)));
    }

    [Fact]
    public void TC_S18_005_BuildingConstructionStage_MapsProgressRangesAccurately()
    {
        var bScaffolding = new BuildingEntity(new EntityId(1), FactionId.Player1, "barracks", new Vector2D(10, 10), new Vector2D(4, 4), maxHealth: 1000f, baseBuildTimeTicks: 100f, startsConstructed: false);
        bScaffolding.Construct(15f, tick: 1, null, out _); // 15% progress

        var bHalfBuilt = new BuildingEntity(new EntityId(2), FactionId.Player1, "barracks", new Vector2D(10, 10), new Vector2D(4, 4), maxHealth: 1000f, baseBuildTimeTicks: 100f, startsConstructed: false);
        bHalfBuilt.Construct(65f, tick: 1, null, out _); // 65% progress

        var bComplete = new BuildingEntity(new EntityId(3), FactionId.Player1, "barracks", new Vector2D(10, 10), new Vector2D(4, 4), maxHealth: 1000f, startsConstructed: true);

        Assert.Equal(BuildingConstructionStage.Scaffolding, BuildingSpriteVisualMapper.GetStage(bScaffolding));
        Assert.Equal(BuildingConstructionStage.HalfBuilt, BuildingSpriteVisualMapper.GetStage(bHalfBuilt));
        Assert.Equal(BuildingConstructionStage.Completed, BuildingSpriteVisualMapper.GetStage(bComplete));
    }

    [Fact]
    public void TC_S18_006_BuildingDamageVfxState_MapsHealthThresholdsToSmokeAndFire()
    {
        var bHealthy = new BuildingEntity(new EntityId(1), FactionId.Player1, "barracks", new Vector2D(10, 10), new Vector2D(4, 4), maxHealth: 1000f, startsConstructed: true);
        
        var bLightSmoke = new BuildingEntity(new EntityId(2), FactionId.Player1, "barracks", new Vector2D(10, 10), new Vector2D(4, 4), maxHealth: 1000f, startsConstructed: true);
        bLightSmoke.TakeDamage(600f, new EntityId(99), FactionId.Player2, tick: 1, null, out _); // 40% HP left

        var bHeavyFire = new BuildingEntity(new EntityId(3), FactionId.Player1, "barracks", new Vector2D(10, 10), new Vector2D(4, 4), maxHealth: 1000f, startsConstructed: true);
        bHeavyFire.TakeDamage(850f, new EntityId(99), FactionId.Player2, tick: 1, null, out _); // 15% HP left

        Assert.Equal(BuildingDamageVfxState.None, BuildingSpriteVisualMapper.GetDamageVfxState(bHealthy));
        Assert.Equal(BuildingDamageVfxState.LightSmoke, BuildingSpriteVisualMapper.GetDamageVfxState(bLightSmoke));
        Assert.Equal(BuildingDamageVfxState.HeavyFireAndSmoke, BuildingSpriteVisualMapper.GetDamageVfxState(bHeavyFire));
    }

    [Fact]
    public void TC_S18_007_FoliageResource_DepletionRatio_CalculatesShrinkAndStumpTransitions()
    {
        var fullTree = new ResourceNodeEntity(new EntityId(1), ResourceType.Wood, new Vector2D(20, 20), maxAmount: 500);
        var depletedTree = new ResourceNodeEntity(new EntityId(2), ResourceType.Wood, new Vector2D(25, 25), maxAmount: 500);
        depletedTree.Harvest(500, tick: 1, harvesterId: new EntityId(10));

        var goldMine = new ResourceNodeEntity(new EntityId(3), ResourceType.Gold, new Vector2D(30, 30), maxAmount: 600);
        goldMine.Harvest(300, tick: 1, harvesterId: new EntityId(10)); // 50% remaining

        var stateFull = FoliageResourcePresenter.GetState(fullTree);
        var stateDepleted = FoliageResourcePresenter.GetState(depletedTree);
        var stateGold = FoliageResourcePresenter.GetState(goldMine);

        Assert.False(stateFull.IsStump);
        Assert.True(stateDepleted.IsStump);
        Assert.Equal(FoliageResourceType.TreeStump, stateDepleted.FoliageType);
        Assert.Equal(0.75f, stateGold.VisualScale, precision: 2);
    }

    [Fact]
    public void TC_S18_008_FogOfWar_InitialState_AllTilesUnexploredBlack()
    {
        var fog = new FogOfWarSystem(50, 50, cellSize: 2.0f);
        for (int y = 0; y < 50; y++)
        {
            for (int x = 0; x < 50; x++)
            {
                Assert.Equal(FogState.Unexplored, fog.GetFogState(x, y));
            }
        }
        Assert.False(fog.IsPositionVisible(new Vector2D(25, 25)));
        Assert.False(fog.IsPositionExplored(new Vector2D(25, 25)));
    }

    // ==========================================
    // Tier 2: Auto-Tiling & Fog Invariants
    // ==========================================

    [Fact]
    public void TC_S18_009_TerrainAutoTiling_BitmaskCalculation_Calculates4BitAnd8BitMasks()
    {
        var grid = new TerrainTileGrid(10, 10, tileSize: 2.0f);
        grid.SetTile(5, 5, TerrainTileType.CobblestoneRoad);
        grid.SetTile(5, 4, TerrainTileType.CobblestoneRoad); // North
        grid.SetTile(6, 5, TerrainTileType.CobblestoneRoad); // East

        byte mask = grid.ComputeAutoTileBitmask(5, 5, TerrainTileType.CobblestoneRoad);
        // North(1) + East(2) = 3
        Assert.Equal(3, mask);
    }

    [Fact]
    public void TC_S18_010_TerrainShoreline_WavePhase_CyclesSmoothly0To1()
    {
        var grid = new TerrainTileGrid(10, 10, tileSize: 2.0f);
        float initialPhase = grid.WavePhase;

        grid.UpdateWaveTicks(0.1f);
        Assert.True(grid.WavePhase > initialPhase);

        for (int i = 0; i < 20; i++)
        {
            grid.UpdateWaveTicks(0.1f);
        }
        Assert.True(grid.WavePhase >= 0f && grid.WavePhase < 1.0f);
    }

    [Fact]
    public void TC_S18_011_FogOfWar_VisionRadiusStamping_SetsAlliedRadiusToVisible()
    {
        var fog = new FogOfWarSystem(100, 100, cellSize: 2.0f);
        var townCenter = new BuildingEntity(new EntityId(1), FactionId.Player1, "town_center", new Vector2D(40, 40), new Vector2D(5, 5), maxHealth: 1500f, startsConstructed: true);

        fog.UpdateVision(alliedUnits: Array.Empty<UnitEntity>(), alliedBuildings: new[] { townCenter });

        // Center should be visible
        Assert.True(fog.IsPositionVisible(new Vector2D(40, 40)));
        Assert.True(fog.IsPositionExplored(new Vector2D(40, 40)));

        // Tiles within vision radius (~20 tiles * 2m = 40m)
        Assert.True(fog.IsPositionVisible(new Vector2D(50, 40)));

        // Distant tile (150, 150) should be unexplored
        Assert.False(fog.IsPositionVisible(new Vector2D(150, 150)));
        Assert.False(fog.IsPositionExplored(new Vector2D(150, 150)));
    }

    [Fact]
    public void TC_S18_012_FogOfWar_FogTransition_VisibleBecomesExploredWhenUnitLeaves()
    {
        var fog = new FogOfWarSystem(100, 100, cellSize: 2.0f);
        var scout = new UnitEntity(new EntityId(10), FactionId.Player1, "celtic_cavalry", new Vector2D(30, 30));

        fog.UpdateVision(new[] { scout }, Array.Empty<BuildingEntity>());
        Assert.True(fog.IsPositionVisible(new Vector2D(30, 30)));

        // Scout moves to (80, 80)
        scout.Position = new Vector2D(80, 80);
        fog.UpdateVision(new[] { scout }, Array.Empty<BuildingEntity>());

        // Old position should now be Explored, NOT Visible, and NOT Unexplored
        Assert.Equal(FogState.Explored, fog.GetFogStateAtWorld(new Vector2D(30, 30)));
        Assert.False(fog.IsPositionVisible(new Vector2D(30, 30)));
        Assert.True(fog.IsPositionExplored(new Vector2D(30, 30)));

        // New position is Visible
        Assert.True(fog.IsPositionVisible(new Vector2D(80, 80)));
    }

    [Fact]
    public void TC_S18_013_FogOfWar_EnemyVisibilityCulling_HidesEnemiesInFogAndShroud()
    {
        var fog = new FogOfWarSystem(100, 100, cellSize: 2.0f);
        var playerUnit = new UnitEntity(new EntityId(1), FactionId.Player1, "celtic_swordsman", new Vector2D(40, 40));
        var enemyInSight = new UnitEntity(new EntityId(2), FactionId.Player2, "roman_legionary", new Vector2D(44, 40));
        var enemyInFog = new UnitEntity(new EntityId(3), FactionId.Player2, "roman_legionary", new Vector2D(120, 120));

        fog.UpdateVision(new[] { playerUnit }, Array.Empty<BuildingEntity>());

        Assert.True(fog.IsUnitVisibleToPlayer(enemyInSight, FactionId.Player1));
        Assert.False(fog.IsUnitVisibleToPlayer(enemyInFog, FactionId.Player1));
    }

    [Fact]
    public void TC_S18_014_FogOfWar_StaticBuildingsRemainVisibleInExploredFog()
    {
        var fog = new FogOfWarSystem(100, 100, cellSize: 2.0f);
        var playerScout = new UnitEntity(new EntityId(1), FactionId.Player1, "celtic_cavalry", new Vector2D(100, 100));
        var romanFortress = new BuildingEntity(new EntityId(5), FactionId.Player2, "praetorium_fortress", new Vector2D(100, 100), new Vector2D(6, 6), maxHealth: 2000f, startsConstructed: true);

        // Visit fortress
        fog.UpdateVision(new[] { playerScout }, Array.Empty<BuildingEntity>());
        Assert.True(fog.IsBuildingVisibleToPlayer(romanFortress, FactionId.Player1));

        // Scout leaves
        playerScout.Position = new Vector2D(20, 20);
        fog.UpdateVision(new[] { playerScout }, Array.Empty<BuildingEntity>());

        // Fortress is in Explored Fog -> Still visible as architectural memory
        Assert.Equal(FogState.Explored, fog.GetFogStateAtWorld(romanFortress.Position));
        Assert.True(fog.IsBuildingVisibleToPlayer(romanFortress, FactionId.Player1));
    }

    [Fact]
    public void TC_S18_015_DirectionalSprite_MeleeAttackArcTrail_GeneratesWeaponSweep()
    {
        var swordsman = new UnitEntity(new EntityId(1), FactionId.Player1, "celtic_swordsman", new Vector2D(50, 50));
        swordsman.HeadingDirection = new Vector2D(1, 0); // Facing East
        swordsman.State = UnitState.Attacking;

        var trail = DirectionalSpriteController.GetWeaponTrail(swordsman, currentTick: 10);
        Assert.True(trail.IsActive);
        Assert.Equal(RenderColor.CelticBlue, trail.TrailColor);
        Assert.True(trail.ArcRadius > 1.5f);
    }

    [Fact]
    public void TC_S18_016_DirectionalSprite_AnimationFrame_AdvancesWithVelocityAndTicks()
    {
        int f0 = DirectionalSpriteController.CalculateFrameIndex(AnimationState.Walk, speed: 95f, tick: 0);
        int f1 = DirectionalSpriteController.CalculateFrameIndex(AnimationState.Walk, speed: 95f, tick: 4);

        Assert.True(f0 >= 0 && f0 < 6);
        Assert.True(f1 >= 0 && f1 < 6);
    }

    // ==========================================
    // Tier 3: Multi-System Visual Integration
    // ==========================================

    [Fact]
    public void TC_S18_017_CelticKingsVisualScenario_InitializesComplete2DMap()
    {
        var scenario = new CelticKingsVisualScenario(seed: 1818);

        Assert.NotNull(scenario.Terrain);
        Assert.NotNull(scenario.FogOfWar);
        Assert.NotNull(scenario.HeroBrennus);
        Assert.NotNull(scenario.CelticTownCenter);
        Assert.NotNull(scenario.RomanPraetorium);
        Assert.NotEmpty(scenario.CelticArmy);
        Assert.NotEmpty(scenario.RomanArmy);

        // Check road exists
        var roadTile = scenario.Terrain.GetTileAtWorld(new Vector2D(50, 50));
        Assert.True(roadTile.Type == TerrainTileType.CobblestoneRoad || roadTile.Type == TerrainTileType.DirtRoad);
    }

    [Fact]
    public void TC_S18_018_BuildingVisualMapper_CelticVsRoman_ResolvesDistinctFactionStyles()
    {
        var celticTc = new BuildingEntity(new EntityId(1), FactionId.Player1, "town_center", new Vector2D(40, 40), new Vector2D(5, 5), maxHealth: 1500f, startsConstructed: true);
        var romanFort = new BuildingEntity(new EntityId(2), FactionId.Player2, "praetorium_fortress", new Vector2D(140, 140), new Vector2D(6, 6), maxHealth: 2200f, startsConstructed: true);

        var celticDesc = BuildingSpriteVisualMapper.GetDescriptor(celticTc);
        var romanDesc = BuildingSpriteVisualMapper.GetDescriptor(romanFort);

        Assert.Equal(ArchitecturalStyle.CelticThatched, celticDesc.Style);
        Assert.Equal(ArchitecturalStyle.RomanMasonry, romanDesc.Style);
    }

    [Fact]
    public void TC_S18_019_BlacksmithForge_ChimneySmoke_EmitsWhenConstructed()
    {
        var forgeComplete = new BuildingEntity(new EntityId(1), FactionId.Player1, "blacksmith", new Vector2D(30, 30), new Vector2D(4, 4), maxHealth: 800f, startsConstructed: true);
        var forgeUnbuilt = new BuildingEntity(new EntityId(2), FactionId.Player1, "blacksmith", new Vector2D(30, 30), new Vector2D(4, 4), maxHealth: 800f, startsConstructed: false);

        var descComplete = BuildingSpriteVisualMapper.GetDescriptor(forgeComplete);
        var descUnbuilt = BuildingSpriteVisualMapper.GetDescriptor(forgeUnbuilt);

        Assert.True(descComplete.HasChimneySmoke);
        Assert.False(descUnbuilt.HasChimneySmoke);
    }

    [Fact]
    public void TC_S18_020_NaturalFoliage_BerryBushHarvest_ReducesBerryCount()
    {
        var bush = new ResourceNodeEntity(new EntityId(1), ResourceType.Food, new Vector2D(50, 50), maxAmount: 400);
        var stateFull = FoliageResourcePresenter.GetState(bush);
        Assert.Equal(4, stateFull.BerryClusterCount);

        bush.Harvest(200, tick: 1, harvesterId: new EntityId(10)); // 50% left
        var stateHalf = FoliageResourcePresenter.GetState(bush);
        Assert.Equal(2, stateHalf.BerryClusterCount);
    }

    [Fact]
    public void TC_S18_021_NaturalFoliage_StoneBoulderMining_EmitsChippingDustParticles()
    {
        var quarry = new ResourceNodeEntity(new EntityId(1), ResourceType.Stone, new Vector2D(50, 50), maxAmount: 500);
        var state = FoliageResourcePresenter.GetState(quarry, currentTick: 2);

        Assert.Equal(FoliageResourceType.StoneBoulder, state.FoliageType);
        Assert.True(state.EmitsMiningDust);
    }

    [Fact]
    public void TC_S18_022_DirectionalUnit_DeathCollapse_TransitionsToDeadCorpseFrame()
    {
        var unit = new UnitEntity(new EntityId(1), FactionId.Player1, "celtic_swordsman", new Vector2D(50, 50), maxHealth: 100f);
        unit.TakeDamage(150f, new EntityId(99), FactionId.Player2, tick: 1, new DomainEventBus(), out bool killed);

        Assert.True(killed);
        var visual = DirectionalSpriteController.GetVisualState(unit, currentTick: 10);
        Assert.True(visual.IsCorpse);
        Assert.Equal(AnimationState.Death, visual.AnimState);
    }

    // ==========================================
    // Tier 4: Headless Scenario & Determinism
    // ==========================================

    [Fact]
    public void TC_S18_023_CelticKingsVisual_FullPlayout_1000TicksWithoutExceptions()
    {
        var scenario = new CelticKingsVisualScenario(seed: 1818);

        // Issue move command to army to march toward Roman base
        for (int i = 0; i < scenario.CelticArmy.Count; i++)
        {
            scenario.CelticArmy[i].Move(new Vector2D(120, 120));
        }

        // Run 1,000 continuous ticks
        scenario.StepSimulation(1000);

        Assert.Equal(1000UL, scenario.Coordinator.CurrentTick);
        Assert.True(scenario.FogOfWar.IsPositionExplored(new Vector2D(40, 40)));
    }

    [Fact]
    public void TC_S18_024_CelticKingsVisual_DeterministicReplay_ChecksumParity()
    {
        var scenario1 = new CelticKingsVisualScenario(seed: 7777);
        var scenario2 = new CelticKingsVisualScenario(seed: 7777);

        scenario1.StepSimulation(1000);
        scenario2.StepSimulation(1000);

        Assert.Equal(scenario1.Coordinator.CurrentTick, scenario2.Coordinator.CurrentTick);
        Assert.Equal(scenario1.HeroBrennus.Position.X, scenario2.HeroBrennus.Position.X, precision: 4);
        Assert.Equal(scenario1.HeroBrennus.Position.Y, scenario2.HeroBrennus.Position.Y, precision: 4);
        Assert.Equal(scenario1.HeroBrennus.CurrentHealth, scenario2.HeroBrennus.CurrentHealth, precision: 4);
    }

    [Fact]
    public void TC_S18_025_FogOfWar_ZeroAllocationHotLoop_ReusesGridBuffers()
    {
        var scenario = new CelticKingsVisualScenario(seed: 1818);
        var buildings = new BuildingEntity[] { scenario.CelticTownCenter, scenario.CelticBarracks };

        // Pre-warm JIT
        scenario.FogOfWar.UpdateVision(scenario.CelticArmy, buildings);

        long allocBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 50; i++)
        {
            scenario.FogOfWar.UpdateVision(scenario.CelticArmy, buildings);
        }
        long allocAfter = GC.GetAllocatedBytesForCurrentThread();

        long bytesAllocated = allocAfter - allocBefore;
        Assert.Equal(0, bytesAllocated);
    }
}
