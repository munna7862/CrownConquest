using System;
using System.Collections.Generic;
using CrownConquest.Application;
using CrownConquest.Domain.AI;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Profiling;
using CrownConquest.Domain.Simulation;
using CrownConquest.Presentation;
using Xunit;

namespace CrownConquest.Tests.Domain;

public sealed class LargeScalePerformanceTests
{
    [Fact]
    public void TC_S12_001_SimulationProfiler_MeasuresPhasesAndComputesAverages()
    {
        var profiler = new SimulationProfiler();
        profiler.BeginTick();

        using (profiler.Measure(SimulationPhase.Commands))
        {
            // Simulate minimal work
            int sum = 0;
            for (int i = 0; i < 1000; i++) sum += i;
        }

        using (profiler.Measure(SimulationPhase.Combat))
        {
            int sum = 0;
            for (int i = 0; i < 1000; i++) sum += i;
        }

        profiler.EndTick(1, activeUnits: 50, activeBuildings: 10);

        Assert.True(profiler.LastTickDurationMs >= 0.0);
        Assert.True(profiler.GetPhaseDurationMs(SimulationPhase.Commands) >= 0.0);
        Assert.True(profiler.GetPhaseDurationMs(SimulationPhase.Combat) >= 0.0);
        Assert.True(profiler.PeakTickDurationMs >= 0.0);
        Assert.True(profiler.GetAverageTickDurationMs() >= 0.0);
    }

    [Fact]
    public void TC_S12_002_PerformanceMetrics_SnapshotAndAggregation()
    {
        var profiler = new SimulationProfiler();
        profiler.BeginTick();
        profiler.RecordSpatialQuery();
        profiler.RecordSpatialQuery();
        profiler.EndTick(10, activeUnits: 25, activeBuildings: 5);

        var snapshot = profiler.GetSnapshot(10, 25, 5);

        Assert.Equal(10UL, snapshot.CurrentTick);
        Assert.Equal(25, snapshot.ActiveUnitCount);
        Assert.Equal(5, snapshot.ActiveBuildingCount);
        Assert.Equal(2, snapshot.SpatialQueriesPerTick);
        Assert.True(snapshot.AverageTickDurationMs >= 0.0);
    }

    [Fact]
    public void TC_S12_003_SpatialGrid_RadiusAndBoxQueries_Correctness()
    {
        var grid = new SpatialGrid(cellSize: 5.0f);
        var e1 = new EntityId(1);
        var e2 = new EntityId(2);
        var e3 = new EntityId(3);

        var positions = new Dictionary<EntityId, Vector2D>
        {
            [e1] = new Vector2D(10f, 10f),
            [e2] = new Vector2D(12f, 10f),
            [e3] = new Vector2D(30f, 30f)
        };

        grid.Insert(e1, positions[e1]);
        grid.Insert(e2, positions[e2]);
        grid.Insert(e3, positions[e3]);

        var results = new List<EntityId>();

        // Query radius 3 around (10, 10) -> Should include e1 (dist 0) and e2 (dist 2), but not e3
        grid.QueryRadius(new Vector2D(10f, 10f), 3.0f, id => positions.TryGetValue(id, out var pos) ? pos : null, results);
        Assert.Contains(e1, results);
        Assert.Contains(e2, results);
        Assert.DoesNotContain(e3, results);

        // Query box [8, 8] to [15, 15]
        grid.QueryBox(new Rect2D(8f, 8f, 15f, 15f), id => positions.TryGetValue(id, out var pos) ? pos : null, results);
        Assert.Contains(e1, results);
        Assert.Contains(e2, results);
        Assert.DoesNotContain(e3, results);
    }

    [Fact]
    public void TC_S12_004_SpatialGrid_QueryNearestEnemy_EarlyExitAndAccuracy()
    {
        var grid = new SpatialGrid(cellSize: 5.0f);
        var eAlly = new EntityId(1);
        var eEnemyFar = new EntityId(2);
        var eEnemyNear = new EntityId(3);

        var entities = new Dictionary<EntityId, (Vector2D Pos, FactionId Faction, bool IsAlive)>
        {
            [eAlly] = (new Vector2D(10f, 10f), FactionId.Player1, true),
            [eEnemyFar] = (new Vector2D(20f, 10f), new FactionId(2), true),
            [eEnemyNear] = (new Vector2D(13f, 10f), new FactionId(2), true)
        };

        grid.Insert(eAlly, entities[eAlly].Pos);
        grid.Insert(eEnemyFar, entities[eEnemyFar].Pos);
        grid.Insert(eEnemyNear, entities[eEnemyNear].Pos);

        var nearest = grid.QueryNearestEnemy(
            new Vector2D(10f, 10f),
            maxRadius: 15.0f,
            friendlyFaction: FactionId.Player1,
            id => entities.TryGetValue(id, out var info) ? info : null);

        Assert.True(nearest.HasValue);
        Assert.Equal(eEnemyNear, nearest.Value);
    }

    [Fact]
    public void TC_S12_005_SpatialGrid_QueryRay_DirectionalIntersection()
    {
        var grid = new SpatialGrid(cellSize: 5.0f);
        var e1 = new EntityId(1); // Directly on ray path (15, 10)
        var e2 = new EntityId(2); // Far off to the side (15, 30)

        var entities = new Dictionary<EntityId, (Vector2D Pos, float Radius, bool IsAlive)>
        {
            [e1] = (new Vector2D(15f, 10f), 1.0f, true),
            [e2] = (new Vector2D(15f, 30f), 1.0f, true)
        };

        grid.Insert(e1, entities[e1].Pos);
        grid.Insert(e2, entities[e2].Pos);

        var results = new List<EntityId>();
        // Ray starting at (10, 10) heading in direction (1, 0) with distance 20
        grid.QueryRay(
            origin: new Vector2D(10f, 10f),
            direction: new Vector2D(1f, 0f),
            maxDistance: 20.0f,
            rayThickness: 0.5f,
            id => entities.TryGetValue(id, out var info) ? info : null,
            results);

        Assert.Contains(e1, results);
        Assert.DoesNotContain(e2, results);
    }

    [Fact]
    public void TC_S12_006_SpatialGrid_UpdatePositionAndRemove()
    {
        var grid = new SpatialGrid(cellSize: 5.0f);
        var e1 = new EntityId(1);
        var pos1 = new Vector2D(2f, 2f);
        var pos2 = new Vector2D(25f, 25f);

        grid.Insert(e1, pos1);
        Assert.Equal(1, grid.TotalIndexedEntities);

        grid.UpdatePosition(e1, pos1, pos2);
        Assert.Equal(1, grid.TotalIndexedEntities);

        var results = new List<EntityId>();
        grid.QueryRadius(pos2, 2.0f, id => id == e1 ? pos2 : null, results);
        Assert.Contains(e1, results);

        grid.Remove(e1);
        Assert.Equal(0, grid.TotalIndexedEntities);
        grid.QueryRadius(pos2, 2.0f, id => id == e1 ? pos2 : null, results);
        Assert.DoesNotContain(e1, results);
    }

    [Fact]
    public void TC_S12_007_AiUpdateScheduler_TimeSlicedIntervals()
    {
        var scheduler = new AiUpdateScheduler();
        var controller = new AiFactionController(new FactionId(2), new Vector2D(50f, 50f));
        scheduler.RegisterWithExplicitOffset(controller, 0);

        // Perception runs every 5 ticks (tick 0, 5, 10, 15, ...)
        Assert.True(scheduler.ShouldRunPerception(controller.FactionId, 0));
        Assert.False(scheduler.ShouldRunPerception(controller.FactionId, 1));
        Assert.True(scheduler.ShouldRunPerception(controller.FactionId, 5));

        // Tactics runs every 5 ticks (tick 0, 5, 10, 15, ...)
        Assert.True(scheduler.ShouldRunTactics(controller.FactionId, 0));
        Assert.False(scheduler.ShouldRunTactics(controller.FactionId, 1));
        Assert.True(scheduler.ShouldRunTactics(controller.FactionId, 5));

        // Economy runs every 10 ticks
        Assert.True(scheduler.ShouldRunEconomy(controller.FactionId, 0));
        Assert.False(scheduler.ShouldRunEconomy(controller.FactionId, 5));
        Assert.True(scheduler.ShouldRunEconomy(controller.FactionId, 10));

        // Production runs half-phase offset (tick 5, 15, ...)
        Assert.True(scheduler.ShouldRunProduction(controller.FactionId, 5));
        Assert.False(scheduler.ShouldRunProduction(controller.FactionId, 0));
    }

    [Fact]
    public void TC_S12_008_AiUpdateScheduler_InterleavedFactionOffsets()
    {
        var scheduler = new AiUpdateScheduler();
        var c1 = new AiFactionController(new FactionId(2), new Vector2D(20f, 20f));
        var c2 = new AiFactionController(new FactionId(3), new Vector2D(80f, 80f));

        scheduler.Register(c1);
        scheduler.Register(c2);

        int offset1 = scheduler.GetOffset(c1.FactionId);
        int offset2 = scheduler.GetOffset(c2.FactionId);

        Assert.NotEqual(offset1, offset2);
    }

    [Fact]
    public void TC_S12_009_PathfindingCache_HitsAndEviction()
    {
        var cache = new PathfindingCache(maxCapacity: 2, quantizationSize: 2.0f);
        var p1 = new Vector2D(0f, 0f);
        var p2 = new Vector2D(10f, 10f);
        var route1 = new List<Vector2D> { new(0f, 0f), new(5f, 5f), new(10f, 10f) };

        var outRoute = new List<Vector2D>();
        Assert.False(cache.TryGetRoute(p1, p2, 1, outRoute));

        cache.StoreRoute(p1, p2, route1, 1);
        Assert.True(cache.TryGetRoute(p1, p2, 2, outRoute));
        Assert.Equal(3, outRoute.Count);
        Assert.Equal(1UL, cache.TotalHits);

        // Fill cache past capacity to trigger LRU eviction
        cache.StoreRoute(new Vector2D(20f, 20f), new Vector2D(30f, 30f), route1, 3);
        cache.StoreRoute(new Vector2D(40f, 40f), new Vector2D(50f, 50f), route1, 4);

        Assert.True(cache.Count <= 2);
    }

    [Fact]
    public void TC_S12_010_DomainEventRingBuffer_CircularPushAndTraversal()
    {
        var ring = new DomainEventRingBuffer(capacity: 4);
        ring.Push(1, "Event1", 10, 0, 1.0f);
        ring.Push(2, "Event2", 20, 0, 2.0f);
        ring.Push(3, "Event3", 30, 0, 3.0f);
        ring.Push(4, "Event4", 40, 0, 4.0f);

        Assert.Equal(4, ring.Count);
        Assert.Equal(4UL, ring.TotalPushed);

        // Overwrite 2 entries
        ring.Push(5, "Event5", 50, 0, 5.0f);
        ring.Push(6, "Event6", 60, 0, 6.0f);

        Assert.Equal(4, ring.Count);
        Assert.Equal(6UL, ring.TotalPushed);

        var list = new List<TelemetryEventRecord>();
        ring.CopyTo(list);
        Assert.Equal(4, list.Count);
        Assert.Equal(3UL, list[0].Tick); // Oldest remaining event
        Assert.Equal(6UL, list[3].Tick); // Most recent event
    }

    [Fact]
    public void TC_S12_011_ObjectPool_RentReturnAndWarming()
    {
        int resetCount = 0;
        var pool = new ObjectPool<List<int>>(
            factory: () => new List<int>(16),
            resetAction: list => { list.Clear(); resetCount++; },
            initialCapacity: 4,
            maxCapacity: 8);

        Assert.Equal(4, pool.AvailableCount);

        var item1 = pool.Rent();
        Assert.Equal(3, pool.AvailableCount);
        item1.Add(42);

        pool.Return(item1);
        Assert.Equal(4, pool.AvailableCount);
        Assert.Equal(1, resetCount);

        var item2 = pool.Rent();
        Assert.Empty(item2); // Was cleared upon return
    }

    [Fact]
    public void TC_S12_015_PerformanceHudPresenter_ProducesValidViewModel()
    {
        var engine = new SimulationEngine();
        engine.Tick();

        var presenter = new PerformanceHudPresenter();
        var vm = presenter.GetViewModel(engine);

        Assert.Equal(1UL, vm.CurrentTick);
        Assert.True(vm.EstimatedFps > 0);
        Assert.True(vm.IsWithinFrameBudget);
        Assert.False(string.IsNullOrWhiteSpace(vm.SubsystemBreakdownSummary));
    }
}
