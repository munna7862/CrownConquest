using System;
using System.Collections.Generic;
using System.Text.Json;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.World;

public sealed class SerializedProvinceData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public float PosX { get; set; }
    public float PosY { get; set; }
    public List<string> ConnectedProvinceIds { get; set; } = new();
    public string Terrain { get; set; } = "Plains";
    public string NodeType { get; set; } = "Settlement";
    public string OwnerFaction { get; set; } = "Neutral";
    public float GarrisonDefenseBonus { get; set; } = 1.0f;
    public int GoldYield { get; set; }
    public int FoodYield { get; set; }
    public int WoodYield { get; set; }
    public int StoneYield { get; set; }
    public int IronYield { get; set; }
    public List<StrategicUnitSpec> GarrisonUnits { get; set; } = new();
}

public sealed class SerializedArmyData
{
    public int Id { get; set; }
    public string FactionId { get; set; } = "Player";
    public string Name { get; set; } = string.Empty;
    public string CurrentProvinceId { get; set; } = string.Empty;
    public string? DestinationProvinceId { get; set; }
    public int MovementTicksRemaining { get; set; }
    public int TotalMovementTicksForEdge { get; set; }
    public List<string> Waypoints { get; set; } = new();
    public List<StrategicUnitSpec> Units { get; set; } = new();
    public StrategicHeroSpec? AttachedHero { get; set; }
    public string Stance { get; set; } = "Aggressive";
    public float BaseMovementSpeed { get; set; } = 50f;
}

public sealed class SerializedTreasuryData
{
    public string FactionId { get; set; } = "Player";
    public int Gold { get; set; }
    public int Food { get; set; }
    public int Wood { get; set; }
    public int Stone { get; set; }
    public int Iron { get; set; }
}

public sealed class SerializedMissionData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "Defend";
    public string IssuingFactionId { get; set; } = string.Empty;
    public string? TargetFactionId { get; set; }
    public string TargetProvinceId { get; set; } = string.Empty;
    public string? DestinationProvinceId { get; set; }
    public int DurationTicks { get; set; }
    public int TargetQuantity { get; set; }
    public int RequiredFood { get; set; }
    public int RequiredIron { get; set; }
    public int RequiredGold { get; set; }
    public int GoldReward { get; set; }
    public int XpReward { get; set; }
    public int ReputationReward { get; set; }
    public bool IsPrimaryCampaign { get; set; }

    public string Status { get; set; } = "Inactive";
    public int StartTick { get; set; }
    public int ElapsedTicks { get; set; }
    public int CurrentProgress { get; set; }
    public int CompletedTick { get; set; }
    public int FailedTick { get; set; }
    public string? FailureReason { get; set; }
    public int? AssignedArmyId { get; set; }
}

public sealed class SerializedFactionData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Culture { get; set; } = string.Empty;
    public string HomeProvinceId { get; set; } = string.Empty;
    public int Reputation { get; set; }
    public string ColorHex { get; set; } = string.Empty;
    public double TradeModifier { get; set; } = 1.0;
    public string Description { get; set; } = string.Empty;
}

public sealed class CampaignSaveData
{
    public int SimulationTick { get; set; }
    public int CampaignTurn { get; set; }
    public int TicksPerTurn { get; set; } = 100;
    public List<SerializedProvinceData> Provinces { get; set; } = new();
    public List<SerializedArmyData> Armies { get; set; } = new();
    public List<SerializedTreasuryData> Treasuries { get; set; } = new();
    public List<SerializedMissionData> Missions { get; set; } = new();
    public List<SerializedFactionData> Factions { get; set; } = new();
}

/// <summary>
/// Deterministic JSON serializer and deserializer for campaign save states including missions and diplomacy.
/// </summary>
public static class CampaignSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string SerializeToJson(CampaignEngine engine)
    {
        var data = new CampaignSaveData
        {
            SimulationTick = engine.SimulationTick,
            CampaignTurn = engine.CampaignTurn,
            TicksPerTurn = engine.TicksPerTurn
        };

        foreach (var p in engine.Map.GetAllProvinces())
        {
            var pData = new SerializedProvinceData
            {
                Id = p.Id.Value,
                Name = p.Name,
                PosX = p.Position.X,
                PosY = p.Position.Y,
                Terrain = p.Terrain.ToString(),
                NodeType = p.NodeType.ToString(),
                OwnerFaction = p.OwnerFaction.Value.ToString(),
                GarrisonDefenseBonus = p.GarrisonDefenseBonus,
                GoldYield = p.ResourceYields.Gold,
                FoodYield = p.ResourceYields.Food,
                WoodYield = p.ResourceYields.Wood,
                StoneYield = p.ResourceYields.Stone,
                IronYield = p.ResourceYields.Iron,
                GarrisonUnits = new List<StrategicUnitSpec>(p.GarrisonUnits)
            };
            for (int i = 0; i < p.ConnectedProvinceIds.Count; i++)
            {
                pData.ConnectedProvinceIds.Add(p.ConnectedProvinceIds[i].Value);
            }
            data.Provinces.Add(pData);
        }

        foreach (var army in engine.GetAllArmies())
        {
            var aData = new SerializedArmyData
            {
                Id = army.Id.Value,
                FactionId = army.FactionId.Value.ToString(),
                Name = army.Name,
                CurrentProvinceId = army.CurrentProvinceId.Value,
                DestinationProvinceId = army.DestinationProvinceId?.Value,
                MovementTicksRemaining = army.MovementTicksRemaining,
                TotalMovementTicksForEdge = army.TotalMovementTicksForEdge,
                Units = new List<StrategicUnitSpec>(army.Units),
                AttachedHero = army.AttachedHero?.Clone(),
                Stance = army.Stance.ToString(),
                BaseMovementSpeed = army.BaseMovementSpeed
            };

            foreach (var wp in army.Waypoints)
            {
                aData.Waypoints.Add(wp.Value);
            }

            data.Armies.Add(aData);
        }

        // Treasuries for distinct factions
        var factions = new[] { FactionId.Player, FactionId.Enemy, FactionId.Neutral };
        for (int i = 0; i < factions.Length; i++)
        {
            var treasury = engine.GetTreasury(factions[i]);
            data.Treasuries.Add(new SerializedTreasuryData
            {
                FactionId = factions[i].Value.ToString(),
                Gold = treasury.Gold,
                Food = treasury.Food,
                Wood = treasury.Wood,
                Stone = treasury.Stone,
                Iron = treasury.Iron
            });
        }

        // Missions serialization
        foreach (var mState in engine.Missions.GetAllMissions())
        {
            var mDef = mState.Definition;
            data.Missions.Add(new SerializedMissionData
            {
                Id = mDef.Id,
                Name = mDef.Name,
                Description = mDef.Description,
                Type = mDef.Type.ToString(),
                IssuingFactionId = mDef.IssuingFactionId,
                TargetFactionId = mDef.TargetFactionId,
                TargetProvinceId = mDef.TargetProvinceId.Value,
                DestinationProvinceId = mDef.DestinationProvinceId?.Value,
                DurationTicks = mDef.DurationTicks,
                TargetQuantity = mDef.TargetQuantity,
                RequiredFood = mDef.RequiredResources.Food,
                RequiredIron = mDef.RequiredResources.Iron,
                RequiredGold = mDef.RequiredResources.Gold,
                GoldReward = mDef.GoldReward,
                XpReward = mDef.XpReward,
                ReputationReward = mDef.ReputationReward,
                IsPrimaryCampaign = mDef.IsPrimaryCampaign,
                Status = mState.Status.ToString(),
                StartTick = mState.StartTick,
                ElapsedTicks = mState.ElapsedTicks,
                CurrentProgress = mState.CurrentProgress,
                CompletedTick = mState.CompletedTick,
                FailedTick = mState.FailedTick,
                FailureReason = mState.FailureReason,
                AssignedArmyId = mState.AssignedArmyId?.Value
            });
        }

        // Factions & Diplomacy serialization
        foreach (var fDef in engine.Diplomacy.GetAllFactions())
        {
            data.Factions.Add(new SerializedFactionData
            {
                Id = fDef.Id,
                Name = fDef.Name,
                Culture = fDef.Culture,
                HomeProvinceId = fDef.HomeProvinceId.Value,
                Reputation = engine.Diplomacy.GetReputation(fDef.Id),
                ColorHex = fDef.ColorHex,
                TradeModifier = fDef.TradeModifier,
                Description = fDef.Description
            });
        }

        return JsonSerializer.Serialize(data, JsonOptions);
    }

    public static Result<CampaignEngine> DeserializeFromJson(string json)
    {
        try
        {
            var data = JsonSerializer.Deserialize<CampaignSaveData>(json, JsonOptions);
            if (data == null)
            {
                return Result<CampaignEngine>.Failure(new GameError("EMPTY_DATA", "Failed to deserialize campaign save data."));
            }

            var provinces = new List<StrategicProvince>();
            foreach (var pData in data.Provinces)
            {
                Enum.TryParse<TerrainType>(pData.Terrain, true, out var terrain);
                Enum.TryParse<StrategicNodeType>(pData.NodeType, true, out var nodeType);
                var ownerFaction = int.TryParse(pData.OwnerFaction, out int ownerVal) ? new FactionId(ownerVal) : FactionId.Neutral;
                var yields = new ResourceCost(Food: pData.FoodYield, Wood: pData.WoodYield, Gold: pData.GoldYield, Stone: pData.StoneYield, Iron: pData.IronYield);

                var connList = new List<ProvinceId>();
                for (int i = 0; i < pData.ConnectedProvinceIds.Count; i++)
                {
                    connList.Add(new ProvinceId(pData.ConnectedProvinceIds[i]));
                }

                var province = new StrategicProvince(
                    id: new ProvinceId(pData.Id),
                    name: pData.Name,
                    position: new Vector2D(pData.PosX, pData.PosY),
                    connectedProvinceIds: connList,
                    terrain: terrain,
                    nodeType: nodeType,
                    ownerFaction: ownerFaction,
                    resourceYields: yields,
                    garrisonDefenseBonus: pData.GarrisonDefenseBonus
                );

                if (pData.GarrisonUnits != null)
                {
                    province.GarrisonUnits.AddRange(pData.GarrisonUnits);
                }

                provinces.Add(province);
            }

            var map = new StrategicMap(provinces);
            var diplomacy = new FactionDiplomacyManager();
            var missions = new MissionEngine();

            var engine = new CampaignEngine(map, diplomacy: diplomacy, missions: missions, ticksPerTurn: data.TicksPerTurn);
            engine.RestoreTickState(data.SimulationTick, data.CampaignTurn);

            // Restore treasuries
            foreach (var tData in data.Treasuries)
            {
                var faction = int.TryParse(tData.FactionId, out int tVal) ? new FactionId(tVal) : FactionId.Neutral;
                var inv = new ResourceCost(Food: tData.Food, Wood: tData.Wood, Gold: tData.Gold, Stone: tData.Stone, Iron: tData.Iron);
                engine.SetTreasury(faction, inv);
            }

            // Restore factions
            if (data.Factions != null)
            {
                foreach (var fData in data.Factions)
                {
                    var fDef = new FactionDefinition(
                        fData.Id,
                        fData.Name,
                        fData.Culture,
                        new ProvinceId(fData.HomeProvinceId),
                        fData.Reputation,
                        fData.ColorHex,
                        fData.TradeModifier,
                        fData.Description
                    );
                    diplomacy.RegisterFaction(fDef);
                    diplomacy.SetReputation(fData.Id, fData.Reputation);
                }
            }

            // Restore missions
            if (data.Missions != null)
            {
                foreach (var mData in data.Missions)
                {
                    Enum.TryParse<MissionType>(mData.Type, true, out var mType);
                    Enum.TryParse<MissionStatus>(mData.Status, true, out var mStatus);

                    var mDef = new MissionDefinition(
                        mData.Id,
                        mData.Name,
                        mData.Description,
                        mType,
                        mData.IssuingFactionId,
                        mData.TargetFactionId,
                        new ProvinceId(mData.TargetProvinceId),
                        !string.IsNullOrEmpty(mData.DestinationProvinceId) ? new ProvinceId(mData.DestinationProvinceId) : null as ProvinceId?,
                        mData.DurationTicks,
                        mData.TargetQuantity,
                        new ResourceCost(Food: mData.RequiredFood, Iron: mData.RequiredIron, Gold: mData.RequiredGold),
                        mData.GoldReward,
                        mData.XpReward,
                        mData.ReputationReward,
                        mData.IsPrimaryCampaign
                    );

                    missions.RegisterMission(mDef);
                    if (missions.TryGetMission(mData.Id, out var rState) && rState != null)
                    {
                        rState.Status = mStatus;
                        rState.StartTick = mData.StartTick;
                        rState.ElapsedTicks = mData.ElapsedTicks;
                        rState.CurrentProgress = mData.CurrentProgress;
                        rState.CompletedTick = mData.CompletedTick;
                        rState.FailedTick = mData.FailedTick;
                        rState.FailureReason = mData.FailureReason;
                        if (mData.AssignedArmyId.HasValue)
                        {
                            rState.AssignedArmyId = new StrategicArmyId(mData.AssignedArmyId.Value);
                        }

                        if (mStatus == MissionStatus.Active)
                        {
                            missions.AcceptMission(mData.Id, mData.StartTick, rState.AssignedArmyId);
                        }
                    }
                }
            }

            // Restore armies
            foreach (var aData in data.Armies)
            {
                Enum.TryParse<StrategicStance>(aData.Stance, true, out var stance);
                var armyFaction = int.TryParse(aData.FactionId, out int aVal) ? new FactionId(aVal) : FactionId.Player;
                var army = new StrategicArmy(
                    id: new StrategicArmyId(aData.Id),
                    factionId: armyFaction,
                    name: aData.Name,
                    startingProvinceId: new ProvinceId(aData.CurrentProvinceId),
                    units: aData.Units,
                    hero: aData.AttachedHero,
                    stance: stance,
                    baseMovementSpeed: aData.BaseMovementSpeed
                );

                if (!string.IsNullOrEmpty(aData.DestinationProvinceId))
                {
                    army.DestinationProvinceId = new ProvinceId(aData.DestinationProvinceId);
                    army.MovementTicksRemaining = aData.MovementTicksRemaining;
                    army.TotalMovementTicksForEdge = aData.TotalMovementTicksForEdge;
                }

                if (aData.Waypoints != null)
                {
                    for (int i = 0; i < aData.Waypoints.Count; i++)
                    {
                        army.Waypoints.Enqueue(new ProvinceId(aData.Waypoints[i]));
                    }
                }

                engine.RegisterArmy(army);
            }

            return Result<CampaignEngine>.Success(engine);
        }
        catch (Exception ex)
        {
            return Result<CampaignEngine>.Failure(new GameError("DESERIALIZATION_ERROR", ex.Message));
        }
    }
}
