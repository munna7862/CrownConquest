using System;
using System.Collections.Generic;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Domain.AI;

/// <summary>
/// Autonomous, authoritative AI controller governing economic planning, worker allocation,
/// build order execution, army staging, tactical combat, and dynamic retreats for a faction.
/// </summary>
public sealed class AiFactionController
{
    private readonly List<EntityId> _cachedIdleWorkerIds = new(32);
    private readonly List<EntityId> _cachedSquadUnitIds = new(64);

    public FactionId FactionId { get; }
    public AiPerceptionState Perception { get; }
    public AiBuildOrderPlan BuildOrder { get; }
    public AiArmySquad ArmySquad { get; }
    public Vector2D BasePosition { get; set; }
    public int TargetWorkerCount { get; set; } = 15;
    public bool IsActive { get; set; } = true;

    public AiFactionController(FactionId factionId, Vector2D basePosition, AiBuildOrderPlan? customPlan = null)
    {
        FactionId = factionId;
        BasePosition = basePosition;
        Perception = new AiPerceptionState(factionId);
        BuildOrder = customPlan ?? AiBuildOrderPlan.CreateStandardPlan();
        ArmySquad = new AiArmySquad(factionId, new Vector2D(basePosition.X + 6.0f, basePosition.Y + 6.0f));
    }

    /// <summary>
    /// Executes time-sliced AI decision logic for the faction.
    /// </summary>
    public void Update(SimulationState state, CommandQueue commandQueue, ulong currentTick)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(commandQueue);

        if (!IsActive) return;

        // 1. Perception update (every 5 ticks)
        if (currentTick % 5 == 0)
        {
            Perception.UpdatePerception(state, currentTick);
        }

        // 2. Economy & Worker AI (every 10 ticks)
        if (currentTick % 10 == 0)
        {
            UpdateEconomyAndWorkers(state, commandQueue, currentTick);
        }

        // 3. Build Orders & Production AI (every 10 ticks, offset by 5)
        if (currentTick % 10 == 5)
        {
            UpdateBuildOrdersAndProduction(state, commandQueue, currentTick);
        }

        // 4. Military & Tactical Combat AI (every 5 ticks)
        if (currentTick % 5 == 0)
        {
            UpdateArmyAndTactics(state, commandQueue, currentTick);
        }
    }

    private void UpdateEconomyAndWorkers(SimulationState state, CommandQueue commandQueue, ulong tick)
    {
        var bank = state.GetOrCreateResourceBank(FactionId);
        var popManager = state.GetOrCreatePopulationManager(FactionId);

        _cachedIdleWorkerIds.Clear();
        int activeWorkerCount = 0;

        var units = state.ActiveUnits;
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (unit.FactionId == FactionId && unit.IsAlive && unit.Archetype == UnitArchetype.Worker)
            {
                activeWorkerCount++;

                if (unit.IsIdleWorker || unit.State == UnitState.Idle)
                {
                    _cachedIdleWorkerIds.Add(unit.Id);
                }
            }
        }

        if (_cachedIdleWorkerIds.Count == 0)
        {
            return;
        }

        var weights = AiResourcePriority.CalculateWeights(
            bank,
            popManager,
            activeWorkerCount,
            TargetWorkerCount,
            isMilitaryProductionActive: ArmySquad.MemberIds.Count < ArmySquad.AttackThreshold,
            isSiegeWanted: true);

        // Assign idle workers to best available resource nodes
        for (int i = 0; i < _cachedIdleWorkerIds.Count; i++)
        {
            var workerId = _cachedIdleWorkerIds[i];
            if (!state.TryGetUnit(workerId, out var worker) || worker == null)
            {
                continue;
            }

            var targetNode = FindBestResourceNode(state, worker.Position, weights.PrimaryResourceDeficit);
            if (targetNode != null)
            {
                commandQueue.Enqueue(new GatherCommand(tick, FactionId, new[] { worker.Id }, targetNode.Id));
            }
        }
    }

    private ResourceNodeEntity? FindBestResourceNode(SimulationState state, Vector2D workerPos, ResourceType preferredType)
    {
        ResourceNodeEntity? bestPreferred = null;
        float bestPreferredDist = float.MaxValue;

        ResourceNodeEntity? bestFallback = null;
        float bestFallbackDist = float.MaxValue;

        var nodes = Perception.KnownResourceNodes;
        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (node.IsDepleted || node.RemainingAmount <= 0)
            {
                continue;
            }

            float dist = workerPos.DistanceTo(node.Position);
            if (node.ResourceType == preferredType)
            {
                if (dist < bestPreferredDist)
                {
                    bestPreferredDist = dist;
                    bestPreferred = node;
                }
            }
            else
            {
                if (dist < bestFallbackDist)
                {
                    bestFallbackDist = dist;
                    bestFallback = node;
                }
            }
        }

        return bestPreferred ?? bestFallback;
    }

    private void UpdateBuildOrdersAndProduction(SimulationState state, CommandQueue commandQueue, ulong tick)
    {
        var bank = state.GetOrCreateResourceBank(FactionId);
        var popManager = state.GetOrCreatePopulationManager(FactionId);
        int availableCap = popManager.CurrentMaxCapacity - popManager.CurrentPopulation;

        // 1. Pop Cap Emergency: If near pop cap, build house
        if (availableCap <= 2 && popManager.CurrentPopulation < popManager.AbsoluteMaxCap)
        {
            if (bank.Wood >= 100 && !HasBuildingUnderConstruction(state, "house"))
            {
                var placementPos = FindPlacementPosition(state, "house", BasePosition, 6.0f, 18.0f);
                if (placementPos.HasValue)
                {
                    commandQueue.Enqueue(new PlaceBuildingCommand(tick, FactionId, "house", placementPos.Value));
                    return;
                }
            }
        }

        // 2. Assign idle workers to any under-construction buildings
        var buildings = state.ActiveBuildings;
        for (int i = 0; i < buildings.Count; i++)
        {
            var bld = buildings[i];
            if (bld.FactionId == FactionId && !bld.IsConstructed && bld.IsAlive)
            {
                var idleWorkers = GetIdleWorkers(state);
                if (idleWorkers.Length > 0)
                {
                    commandQueue.Enqueue(new ConstructBuildingCommand(tick, FactionId, idleWorkers, bld.Id));
                }
            }
        }

        // 3. Keep Town Center training workers if below target
        for (int i = 0; i < buildings.Count; i++)
        {
            var bld = buildings[i];
            if (bld.FactionId == FactionId && bld.BuildingType.Equals("town_center", StringComparison.OrdinalIgnoreCase) && bld.IsConstructed)
            {
                int currentWorkers = CountWorkers(state);
                if (currentWorkers < TargetWorkerCount && bld.ProductionQueue != null && bld.ProductionQueue.IsEmpty)
                {
                    if (bank.Food >= 50 && !popManager.IsPopCapped)
                    {
                        commandQueue.Enqueue(new QueueProductionCommand(tick, FactionId, bld.Id, "worker"));
                    }
                }
            }
        }

        // 4. Progress active build order plan
        if (!BuildOrder.IsPlanFinished && BuildOrder.CurrentStep != null)
        {
            var step = BuildOrder.CurrentStep;
            switch (step.StepType)
            {
                case AiBuildStepType.ConstructBuilding:
                {
                    int existingCount = CountBuildings(state, step.TargetIdentifier);
                    if (existingCount >= step.TargetCount)
                    {
                        BuildOrder.AdvanceStep();
                    }
                    else if (!HasBuildingUnderConstruction(state, step.TargetIdentifier))
                    {
                        var config = SimulationConfigHelper.GetBuildingCost(step.TargetIdentifier);
                        if (bank.CanAfford(config))
                        {
                            var placementPos = FindPlacementPosition(state, step.TargetIdentifier, BasePosition, 8.0f, 22.0f);
                            if (placementPos.HasValue)
                            {
                                commandQueue.Enqueue(new PlaceBuildingCommand(tick, FactionId, step.TargetIdentifier, placementPos.Value));
                            }
                        }
                    }
                    break;
                }

                case AiBuildStepType.TrainUnits:
                {
                    string buildingType = GetProductionBuildingForUnit(step.TargetIdentifier);
                    var prodBld = FindAvailableProductionBuilding(state, buildingType);
                    if (prodBld != null && prodBld.ProductionQueue != null && prodBld.ProductionQueue.Count < 2)
                    {
                        var unitCost = SimulationConfigHelper.GetUnitCost(step.TargetIdentifier);
                        if (bank.CanAfford(unitCost) && !popManager.IsPopCapped)
                        {
                            commandQueue.Enqueue(new QueueProductionCommand(tick, FactionId, prodBld.Id, step.TargetIdentifier));
                            BuildOrder.AdvanceStep();
                        }
                    }
                    break;
                }
            }
        }

        // 5. Continuous Military Production from military buildings if bank allows
        for (int i = 0; i < buildings.Count; i++)
        {
            var bld = buildings[i];
            if (bld.FactionId != FactionId || !bld.IsConstructed || bld.ProductionQueue == null || bld.ProductionQueue.Count > 1)
            {
                continue;
            }

            string? unitToTrain = null;
            if (bld.BuildingType.Equals("barracks", StringComparison.OrdinalIgnoreCase))
            {
                unitToTrain = "spearman";
            }
            else if (bld.BuildingType.Equals("archery_range", StringComparison.OrdinalIgnoreCase))
            {
                unitToTrain = "archer";
            }
            else if (bld.BuildingType.Equals("stable", StringComparison.OrdinalIgnoreCase))
            {
                unitToTrain = "cavalry";
            }
            else if (bld.BuildingType.Equals("siege_workshop", StringComparison.OrdinalIgnoreCase))
            {
                unitToTrain = "catapult";
            }

            if (unitToTrain != null && !popManager.IsPopCapped)
            {
                var cost = SimulationConfigHelper.GetUnitCost(unitToTrain);
                if (bank.CanAfford(cost))
                {
                    commandQueue.Enqueue(new QueueProductionCommand(tick, FactionId, bld.Id, unitToTrain));
                }
            }
        }
    }

    private void UpdateArmyAndTactics(SimulationState state, CommandQueue commandQueue, ulong tick)
    {
        // Enlist all military units
        var units = state.ActiveUnits;
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (unit.FactionId == FactionId && unit.IsAlive && unit.Archetype != UnitArchetype.Worker)
            {
                ArmySquad.AddMember(unit.Id);
            }
        }

        var aliveSquadUnits = ArmySquad.GetAliveUnits(state);
        if (aliveSquadUnits.Count == 0)
        {
            ArmySquad.SetState(AiSquadState.Assembling);
            return;
        }

        _cachedSquadUnitIds.Clear();
        for (int i = 0; i < aliveSquadUnits.Count; i++)
        {
            _cachedSquadUnitIds.Add(aliveSquadUnits[i].Id);
        }
        var squadIds = _cachedSquadUnitIds.ToArray();

        // 1. Check Threat Near Base (Perimeter R=30)
        float baseThreat = Perception.GetThreatLevelNear(BasePosition, 30.0f);
        if (baseThreat > 0f)
        {
            ArmySquad.SetState(AiSquadState.Defending);

            // Find closest perceived threat near base
            PerceivedEntityRecord? closestThreat = null;
            float closestDist = float.MaxValue;
            var enemies = Perception.ActivePerceivedEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy.IsAlive && enemy.Position.DistanceTo(BasePosition) <= 35.0f)
                {
                    float d = enemy.Position.DistanceTo(BasePosition);
                    if (d < closestDist)
                    {
                        closestDist = d;
                        closestThreat = enemy;
                    }
                }
            }

            if (closestThreat.HasValue)
            {
                commandQueue.Enqueue(new AttackCommand(FactionId, tick, squadIds, closestThreat.Value.EntityId));
            }
            else
            {
                commandQueue.Enqueue(new MoveCommand(FactionId, tick, squadIds, BasePosition));
            }
            return;
        }

        // 2. Evaluate State Machine: Attacking vs Assembling vs Retreating
        switch (ArmySquad.State)
        {
            case AiSquadState.Defending:
            case AiSquadState.Assembling:
            {
                if (aliveSquadUnits.Count >= ArmySquad.AttackThreshold)
                {
                    ArmySquad.SetState(AiSquadState.Attacking);
                }
                else
                {
                    // Move to staging rally point
                    commandQueue.Enqueue(new MoveCommand(FactionId, tick, squadIds, ArmySquad.RallyPoint));
                }
                break;
            }

            case AiSquadState.Attacking:
            {
                float friendlyPower = AiCombatEvaluator.CalculateSquadCombatPower(aliveSquadUnits);
                float perceivedEnemyThreat = AiCombatEvaluator.CalculatePerceivedThreat(Perception.ActivePerceivedEnemies);
                float squadHealthPercent = ArmySquad.CalculateTotalHealthPercent(state);

                // Check retreat condition
                if (AiCombatEvaluator.ShouldRetreat(friendlyPower, perceivedEnemyThreat, squadHealthPercent))
                {
                    ArmySquad.SetState(AiSquadState.Retreating);
                    commandQueue.Enqueue(new MoveCommand(FactionId, tick, squadIds, BasePosition));
                    return;
                }

                // Choose best target from perceived enemies
                var bestTarget = SelectBestTacticalTarget(aliveSquadUnits);
                if (bestTarget.HasValue)
                {
                    commandQueue.Enqueue(new AttackCommand(FactionId, tick, squadIds, bestTarget.Value.EntityId));
                }
                else if (Perception.KnownEnemyBases.Count > 0)
                {
                    // March towards enemy base
                    commandQueue.Enqueue(new FormationMoveCommand(FactionId, tick, squadIds, Perception.KnownEnemyBases[0], Spacing: 2.0f));
                }
                else
                {
                    // Scout map center or enemy territory
                    var marchTarget = new Vector2D(BasePosition.X > 50f ? 20f : 80f, BasePosition.Y > 50f ? 20f : 80f);
                    commandQueue.Enqueue(new FormationMoveCommand(FactionId, tick, squadIds, marchTarget, Spacing: 2.0f));
                }
                break;
            }

            case AiSquadState.Retreating:
            {
                float squadHealthPercent = ArmySquad.CalculateTotalHealthPercent(state);
                float distToBase = aliveSquadUnits[0].Position.DistanceTo(BasePosition);

                // Recovered or reached base safely
                if (distToBase < 12.0f && squadHealthPercent >= 0.70f)
                {
                    ArmySquad.SetState(AiSquadState.Assembling);
                }
                else
                {
                    commandQueue.Enqueue(new MoveCommand(FactionId, tick, squadIds, BasePosition));
                }
                break;
            }
        }
    }

    private PerceivedEntityRecord? SelectBestTacticalTarget(IReadOnlyList<UnitEntity> friendlyUnits)
    {
        var enemies = Perception.ActivePerceivedEnemies;
        if (enemies.Count == 0) return null;

        PerceivedEntityRecord? best = null;
        float bestScore = float.MinValue;

        var leadUnit = friendlyUnits[0];

        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (!enemy.IsAlive) continue;

            float dist = leadUnit.Position.DistanceTo(enemy.Position);
            float priorityScore = enemy.IsBuilding
                ? AiTargetingMatrix.GetBuildingTargetPriority(leadUnit.Archetype, enemy.BuildingType)
                : AiTargetingMatrix.GetTargetPriority(leadUnit.Archetype, enemy.UnitArchetype);

            float totalScore = priorityScore * 10f - dist;
            if (totalScore > bestScore)
            {
                bestScore = totalScore;
                best = enemy;
            }
        }

        return best;
    }

    private Vector2D? FindPlacementPosition(SimulationState state, string buildingType, Vector2D center, float minRadius, float maxRadius)
    {
        var config = SimulationConfigHelper.GetBuildingGridSize(buildingType);
        for (float r = minRadius; r <= maxRadius; r += 3.0f)
        {
            for (float angle = 0f; angle < MathF.PI * 2f; angle += 0.5f)
            {
                var candidate = new Vector2D(center.X + MathF.Cos(angle) * r, center.Y + MathF.Sin(angle) * r);
                var snapped = state.PlacementGrid.SnapToGrid(candidate);
                if (state.PlacementGrid.CanPlace(snapped, config, state.ActiveBuildings, state.ActiveResourceNodes, new BattlefieldBounds(0, 0, 100, 100)))
                {
                    return snapped;
                }
            }
        }
        return null;
    }

    private EntityId[] GetIdleWorkers(SimulationState state)
    {
        _cachedIdleWorkerIds.Clear();
        var units = state.ActiveUnits;
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];
            if (unit.FactionId == FactionId && unit.IsAlive && unit.Archetype == UnitArchetype.Worker)
            {
                if (unit.IsIdleWorker || unit.State == UnitState.Idle)
                {
                    _cachedIdleWorkerIds.Add(unit.Id);
                }
            }
        }
        return _cachedIdleWorkerIds.ToArray();
    }

    private int CountWorkers(SimulationState state)
    {
        int count = 0;
        var units = state.ActiveUnits;
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].FactionId == FactionId && units[i].IsAlive && units[i].Archetype == UnitArchetype.Worker)
            {
                count++;
            }
        }
        return count;
    }

    private int CountBuildings(SimulationState state, string type)
    {
        int count = 0;
        var buildings = state.ActiveBuildings;
        for (int i = 0; i < buildings.Count; i++)
        {
            if (buildings[i].FactionId == FactionId && buildings[i].IsAlive && buildings[i].BuildingType.Equals(type, StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }
        return count;
    }

    private bool HasBuildingUnderConstruction(SimulationState state, string type)
    {
        var buildings = state.ActiveBuildings;
        for (int i = 0; i < buildings.Count; i++)
        {
            if (buildings[i].FactionId == FactionId && buildings[i].IsAlive && !buildings[i].IsConstructed &&
                buildings[i].BuildingType.Equals(type, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private BuildingEntity? FindAvailableProductionBuilding(SimulationState state, string type)
    {
        var buildings = state.ActiveBuildings;
        for (int i = 0; i < buildings.Count; i++)
        {
            var bld = buildings[i];
            if (bld.FactionId == FactionId && bld.IsAlive && bld.IsConstructed && bld.BuildingType.Equals(type, StringComparison.OrdinalIgnoreCase))
            {
                return bld;
            }
        }
        return null;
    }

    private static string GetProductionBuildingForUnit(string unitType)
    {
        return unitType.ToLowerInvariant() switch
        {
            "worker" or "villager" => "town_center",
            "spearman" or "swordsman" => "barracks",
            "archer" or "crossbowman" => "archery_range",
            "cavalry" or "knight" => "stable",
            "battering_ram" or "catapult" or "ballista" or "trebuchet" => "siege_workshop",
            _ => "barracks"
        };
    }
}

public static class SimulationConfigHelper
{
    public static ResourceCost GetBuildingCost(string buildingType)
    {
        return buildingType.ToLowerInvariant() switch
        {
            "town_center" => new ResourceCost(Food: 0, Wood: 250, Stone: 100),
            "house" => new ResourceCost(Food: 0, Wood: 80),
            "farm" => new ResourceCost(Food: 0, Wood: 60),
            "barracks" => new ResourceCost(Food: 0, Wood: 150),
            "archery_range" => new ResourceCost(Food: 0, Wood: 150, Gold: 50),
            "stable" => new ResourceCost(Food: 0, Wood: 150, Gold: 100),
            "siege_workshop" => new ResourceCost(Food: 0, Wood: 200, Gold: 100, Stone: 50),
            "watchtower" => new ResourceCost(Food: 0, Wood: 50, Stone: 100),
            _ => new ResourceCost(Food: 0, Wood: 100)
        };
    }

    public static ResourceCost GetUnitCost(string unitType)
    {
        return unitType.ToLowerInvariant() switch
        {
            "worker" or "villager" => new ResourceCost(Food: 50),
            "spearman" => new ResourceCost(Food: 50, Wood: 20),
            "archer" => new ResourceCost(Food: 40, Wood: 40),
            "cavalry" => new ResourceCost(Food: 80, Gold: 40),
            "battering_ram" => new ResourceCost(Wood: 150, Gold: 50),
            "catapult" => new ResourceCost(Wood: 200, Gold: 100),
            "ballista" => new ResourceCost(Wood: 160, Gold: 80),
            _ => new ResourceCost(Food: 50)
        };
    }

    public static Vector2D GetBuildingGridSize(string buildingType)
    {
        return buildingType.ToLowerInvariant() switch
        {
            "town_center" or "fortress" => new Vector2D(4, 4),
            "farm" or "siege_workshop" => new Vector2D(3, 3),
            "barracks" or "archery_range" or "stable" => new Vector2D(3, 3),
            "watchtower" or "guard_tower" => new Vector2D(2, 2),
            _ => new Vector2D(2, 2)
        };
    }
}
