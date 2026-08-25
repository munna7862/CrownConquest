using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.Simulation;

#region Serialized DTO Models

public sealed class SerializedUnitDto
{
    public int Id { get; set; }
    public int FactionId { get; set; }
    public string UnitType { get; set; } = string.Empty;
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float Health { get; set; }
    public float MaxHealth { get; set; }
    public float AttackDamage { get; set; }
    public float BaseArmor { get; set; }
    public float AttackRange { get; set; }
    public float MovementSpeed { get; set; }
    public int AttackCooldownTicks { get; set; }
    public int KillXpValue { get; set; }
    public string AttackType { get; set; } = "melee";
    public float AggroRange { get; set; }
    public int Level { get; set; }
    public int CurrentXp { get; set; }
    public int TotalKills { get; set; }
    public int Formation { get; set; }
    public int CurrentTerrain { get; set; }
    public float Morale { get; set; }
    public int MomentumTicks { get; set; }
    public int Archetype { get; set; }
    public int State { get; set; }
    public int AttackTargetId { get; set; }
    public float? MoveTargetX { get; set; }
    public float? MoveTargetY { get; set; }
    public int CooldownRemaining { get; set; }
    public float HeadingX { get; set; }
    public float HeadingY { get; set; }

    public int WorkerCarriedAmount { get; set; }
    public int? WorkerResourceType { get; set; }

    public string? HeroClass { get; set; }
    public string? HeroName { get; set; }
    public int HeroStrength { get; set; }
    public int HeroAgility { get; set; }
    public int HeroWillpower { get; set; }
    public float HeroMana { get; set; }
    public List<int> HeroAbilityCooldowns { get; set; } = new();
}

public sealed class SerializedBuildingDto
{
    public int Id { get; set; }
    public int FactionId { get; set; }
    public string BuildingType { get; set; } = string.Empty;
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float Health { get; set; }
    public float MaxHealth { get; set; }
    public float Armor { get; set; }
    public float BuildProgress { get; set; }
    public bool IsConstructed { get; set; }
    public int GateState { get; set; }
    public int TowerCooldown { get; set; }
    public int TowerGarrison { get; set; }
    public List<string> ProductionQueue { get; set; } = new();
    public List<string> ResearchQueue { get; set; } = new();
}

public sealed class SerializedNodeDto
{
    public int Id { get; set; }
    public int NodeType { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public int RemainingAmount { get; set; }
    public int MaxCapacity { get; set; }
}

public sealed class SerializedBreachDto
{
    public int WallId { get; set; }
    public int DefendingFactionId { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float Radius { get; set; }
    public ulong TickCreated { get; set; }
}

public sealed class SerializedBankDto
{
    public int FactionId { get; set; }
    public int Food { get; set; }
    public int Wood { get; set; }
    public int Gold { get; set; }
    public int Stone { get; set; }
    public int Iron { get; set; }
}

public sealed class SerializedPopDto
{
    public int FactionId { get; set; }
    public int CurrentPop { get; set; }
    public int MaxPop { get; set; }
}

public sealed class SerializedEraDto
{
    public int FactionId { get; set; }
    public int CurrentEra { get; set; }
    public int ProgressTicks { get; set; }
    public int? TargetEra { get; set; }
    public int RequiredTicks { get; set; }
}

public sealed class SerializedTechDto
{
    public int FactionId { get; set; }
    public List<string> UnlockedTechIds { get; set; } = new();
}

public sealed class SerializedSimulationSaveData
{
    public ulong CurrentTick { get; set; }
    public int RandomSeed { get; set; }
    public List<SerializedUnitDto> Units { get; set; } = new();
    public List<SerializedBuildingDto> Buildings { get; set; } = new();
    public List<SerializedNodeDto> Nodes { get; set; } = new();
    public List<SerializedBreachDto> Breaches { get; set; } = new();
    public List<SerializedBankDto> Banks { get; set; } = new();
    public List<SerializedPopDto> Populations { get; set; } = new();
    public List<SerializedEraDto> Eras { get; set; } = new();
    public List<SerializedTechDto> Techs { get; set; } = new();
}

#endregion

/// <summary>
/// Authoritative deterministic JSON serializer and deserializer for simulation states.
/// </summary>
public static class SimulationStateSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string SerializeToJson(SimulationState state, int seed = 42)
    {
        ArgumentNullException.ThrowIfNull(state);

        var data = new SerializedSimulationSaveData
        {
            CurrentTick = state.CurrentTick,
            RandomSeed = seed
        };

        // Units
        for (int i = 0; i < state.ActiveUnits.Count; i++)
        {
            var u = state.ActiveUnits[i];
            var uDto = new SerializedUnitDto
            {
                Id = u.Id.Value,
                FactionId = u.FactionId.Value,
                UnitType = u.UnitType,
                PosX = u.Position.X,
                PosY = u.Position.Y,
                Health = u.CurrentHealth,
                MaxHealth = u.MaxHealth,
                AttackDamage = u.AttackDamage,
                BaseArmor = u.BaseArmor,
                AttackRange = u.AttackRange,
                MovementSpeed = u.MovementSpeed,
                AttackCooldownTicks = u.AttackCooldownTicks,
                KillXpValue = u.KillXpValue,
                AttackType = u.AttackType,
                AggroRange = u.AggroRange,
                Level = u.Veterancy.Level,
                CurrentXp = u.Veterancy.CurrentXp,
                TotalKills = u.Veterancy.KillCount,
                Formation = (int)u.Formation,
                CurrentTerrain = (int)u.CurrentTerrain,
                Morale = u.Morale.CurrentMorale,
                MomentumTicks = u.Charge.MomentumTicks,
                Archetype = (int)u.Archetype,
                State = (int)u.State,
                AttackTargetId = u.AttackTargetId.Value,
                MoveTargetX = u.MoveTarget.HasValue ? u.MoveTarget.Value.X : null,
                MoveTargetY = u.MoveTarget.HasValue ? u.MoveTarget.Value.Y : null,
                CooldownRemaining = u.CooldownRemaining,
                HeadingX = u.HeadingDirection.X,
                HeadingY = u.HeadingDirection.Y
            };

            if (u.WorkerState != null)
            {
                uDto.WorkerCarriedAmount = u.WorkerState.CarriedAmount;
                uDto.WorkerResourceType = u.WorkerState.CarriedResourceType.HasValue ? (int)u.WorkerState.CarriedResourceType.Value : null;
            }

            if (u.HeroState != null)
            {
                uDto.HeroClass = u.HeroState.Class.ToString();
                uDto.HeroName = u.HeroState.HeroName;
                uDto.HeroStrength = u.HeroState.TotalAttributes.Strength;
                uDto.HeroAgility = u.HeroState.TotalAttributes.Agility;
                uDto.HeroWillpower = u.HeroState.TotalAttributes.Willpower;
                uDto.HeroMana = u.HeroState.CurrentMana;
                for (int a = 0; a < u.HeroState.Abilities.Count; a++)
                {
                    uDto.HeroAbilityCooldowns.Add(u.HeroState.Abilities[a].CooldownRemainingTicks);
                }
            }

            data.Units.Add(uDto);
        }

        // Buildings
        for (int i = 0; i < state.ActiveBuildings.Count; i++)
        {
            var b = state.ActiveBuildings[i];
            var bDto = new SerializedBuildingDto
            {
                Id = b.Id.Value,
                FactionId = b.FactionId.Value,
                BuildingType = b.BuildingType,
                PosX = b.Position.X,
                PosY = b.Position.Y,
                Health = b.CurrentHealth,
                MaxHealth = b.MaxHealth,
                Armor = 0f,
                BuildProgress = b.CurrentBuildProgress,
                IsConstructed = b.IsConstructed,
                GateState = b.GateDefense != null ? (int)b.GateDefense.State : 0,
                TowerCooldown = b.TowerDefense != null ? b.TowerDefense.CooldownRemaining : 0,
                TowerGarrison = b.TowerDefense != null ? b.TowerDefense.GarrisonCount : 0
            };

            var prod = b.ProductionQueue.Items;
            for (int p = 0; p < prod.Count; p++)
            {
                bDto.ProductionQueue.Add(prod[p].UnitType);
            }

            var res = b.ResearchQueue.Items;
            for (int r = 0; r < res.Count; r++)
            {
                bDto.ResearchQueue.Add(res[r].TechnologyId);
            }

            data.Buildings.Add(bDto);
        }

        // Nodes
        for (int i = 0; i < state.ActiveResourceNodes.Count; i++)
        {
            var n = state.ActiveResourceNodes[i];
            data.Nodes.Add(new SerializedNodeDto
            {
                Id = n.Id.Value,
                NodeType = (int)n.ResourceType,
                PosX = n.Position.X,
                PosY = n.Position.Y,
                RemainingAmount = n.RemainingAmount,
                MaxCapacity = n.MaxAmount
            });
        }

        // Breaches
        for (int i = 0; i < state.Breaches.Count; i++)
        {
            var br = state.Breaches[i];
            data.Breaches.Add(new SerializedBreachDto
            {
                WallId = br.WallEntityId.Value,
                DefendingFactionId = br.DefendingFactionId.Value,
                PosX = br.Position.X,
                PosY = br.Position.Y,
                Radius = br.BreachRadius,
                TickCreated = br.BreachedAtTick
            });
        }

        // Banks
        foreach (var (fId, bank) in state.ResourceBanks)
        {
            data.Banks.Add(new SerializedBankDto
            {
                FactionId = fId.Value,
                Food = bank.Food,
                Wood = bank.Wood,
                Gold = bank.Gold,
                Stone = bank.Stone,
                Iron = bank.Iron
            });
        }

        // Pop
        foreach (var (fId, pop) in state.PopulationManagers)
        {
            data.Populations.Add(new SerializedPopDto
            {
                FactionId = fId.Value,
                CurrentPop = pop.CurrentPopulation,
                MaxPop = pop.CurrentMaxCapacity
            });
        }

        // Eras
        foreach (var (fId, era) in state.EraStates)
        {
            data.Eras.Add(new SerializedEraDto
            {
                FactionId = fId.Value,
                CurrentEra = (int)era.CurrentEra,
                ProgressTicks = era.ProgressTicks,
                TargetEra = era.TargetEra.HasValue ? (int)era.TargetEra.Value : null,
                RequiredTicks = era.DurationTicks
            });
        }

        // Tech
        foreach (var (fId, tech) in state.TechManagers)
        {
            var tDto = new SerializedTechDto { FactionId = fId.Value };
            foreach (var tId in tech.UnlockedTechIds)
            {
                tDto.UnlockedTechIds.Add(tId);
            }
            data.Techs.Add(tDto);
        }

        return JsonSerializer.Serialize(data, JsonOptions);
    }

    public static Result<SimulationState> DeserializeFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Result<SimulationState>.Failure(new GameError("EMPTY_DATA", "Cannot deserialize empty JSON string."));
        }

        try
        {
            var data = JsonSerializer.Deserialize<SerializedSimulationSaveData>(json, JsonOptions);
            if (data == null)
            {
                return Result<SimulationState>.Failure(new GameError("DESERIALIZE_FAILED", "JSON payload resulted in null object."));
            }

            var state = new SimulationState
            {
                CurrentTick = data.CurrentTick
            };

            // Banks
            for (int i = 0; i < data.Banks.Count; i++)
            {
                var b = data.Banks[i];
                var fId = new FactionId(b.FactionId);
                var bank = state.GetOrCreateResourceBank(fId);
                bank.Deposit(ResourceType.Food, b.Food, 0);
                bank.Deposit(ResourceType.Wood, b.Wood, 0);
                bank.Deposit(ResourceType.Gold, b.Gold, 0);
                bank.Deposit(ResourceType.Stone, b.Stone, 0);
                bank.Deposit(ResourceType.Iron, b.Iron, 0);
            }

            // Pop
            for (int i = 0; i < data.Populations.Count; i++)
            {
                var p = data.Populations[i];
                var fId = new FactionId(p.FactionId);
                var pop = new PopulationManager(fId, baseCapacity: p.MaxPop);
                pop.SetCurrentPopulation(p.CurrentPop, 0);
                state.SetPopulationManager(fId, pop);
            }

            // Eras
            for (int i = 0; i < data.Eras.Count; i++)
            {
                var e = data.Eras[i];
                var fId = new FactionId(e.FactionId);
                var era = new EraState(fId, (CivilizationEra)e.CurrentEra);
                state.SetEraState(fId, era);
            }

            // Techs
            for (int i = 0; i < data.Techs.Count; i++)
            {
                var t = data.Techs[i];
                var fId = new FactionId(t.FactionId);
                var techMgr = state.GetOrCreateTechManager(fId);
                for (int u = 0; u < t.UnlockedTechIds.Count; u++)
                {
                    techMgr.RestoreUnlockedTech(t.UnlockedTechIds[u]);
                }
            }

            // Nodes
            for (int i = 0; i < data.Nodes.Count; i++)
            {
                var n = data.Nodes[i];
                var node = new ResourceNodeEntity(
                    new EntityId(n.Id),
                    (ResourceType)n.NodeType,
                    new Vector2D(n.PosX, n.PosY),
                    n.RemainingAmount,
                    n.MaxCapacity);
                state.AddResourceNode(node);
            }

            // Units
            for (int i = 0; i < data.Units.Count; i++)
            {
                var u = data.Units[i];
                WorkerGatherState? worker = null;
                if (u.WorkerCarriedAmount > 0 || u.WorkerResourceType.HasValue)
                {
                    worker = new WorkerGatherState(10, 0.5f, 1.0f);
                    if (u.WorkerResourceType.HasValue && u.WorkerCarriedAmount > 0)
                    {
                        worker.AddCarried((ResourceType)u.WorkerResourceType.Value, u.WorkerCarriedAmount);
                    }
                }

                HeroState? hero = null;
                if (!string.IsNullOrEmpty(u.HeroClass))
                {
                    var hClass = Enum.TryParse<HeroClass>(u.HeroClass, true, out var hc) ? hc : HeroClass.Warlord;
                    hero = new HeroState(
                        hClass,
                        u.HeroName ?? "Hero",
                        new HeroAttributes(u.HeroStrength, u.HeroAgility, u.HeroWillpower),
                        baseLeadershipCapacity: 20);
                }

                var unit = new UnitEntity(
                    new EntityId(u.Id),
                    new FactionId(u.FactionId),
                    u.UnitType,
                    new Vector2D(u.PosX, u.PosY),
                    maxHealth: u.MaxHealth,
                    attackDamage: u.AttackDamage,
                    attackRange: u.AttackRange,
                    movementSpeed: u.MovementSpeed,
                    attackCooldownTicks: u.AttackCooldownTicks,
                    killXpValue: u.KillXpValue,
                    baseArmor: u.BaseArmor,
                    attackType: u.AttackType,
                    aggroRange: u.AggroRange,
                    archetype: (UnitArchetype)u.Archetype,
                    workerState: worker,
                    heroState: hero,
                    formation: (FormationType)u.Formation,
                    initialLevel: u.Level,
                    initialXp: u.CurrentXp,
                    initialCurrentHealth: u.Health);

                var moveTarget = (u.MoveTargetX.HasValue && u.MoveTargetY.HasValue) ? new Vector2D(u.MoveTargetX.Value, u.MoveTargetY.Value) : (Vector2D?)null;
                var heading = new Vector2D(u.HeadingX, u.HeadingY);
                unit.RestoreTacticalState((UnitState)u.State, new EntityId(u.AttackTargetId), moveTarget, u.CooldownRemaining, heading, u.Morale, u.MomentumTicks);

                state.AddUnit(unit);
            }

            // Buildings
            for (int i = 0; i < data.Buildings.Count; i++)
            {
                var b = data.Buildings[i];
                var building = new BuildingEntity(
                    new EntityId(b.Id),
                    new FactionId(b.FactionId),
                    b.BuildingType,
                    new Vector2D(b.PosX, b.PosY),
                    new Vector2D(3f, 3f),
                    maxHealth: b.MaxHealth,
                    startsConstructed: b.IsConstructed);

                state.AddBuilding(building);
            }

            int maxId = 1;
            for (int i = 0; i < data.Units.Count; i++) if (data.Units[i].Id > maxId) maxId = data.Units[i].Id;
            for (int i = 0; i < data.Buildings.Count; i++) if (data.Buildings[i].Id > maxId) maxId = data.Buildings[i].Id;
            for (int i = 0; i < data.Nodes.Count; i++) if (data.Nodes[i].Id > maxId) maxId = data.Nodes[i].Id;
            state.SetNextEntityId(maxId + 1);

            return Result<SimulationState>.Success(state);
        }
        catch (Exception ex)
        {
            return Result<SimulationState>.Failure(new GameError("DESERIALIZE_EXCEPTION", ex.Message));
        }
    }
}
