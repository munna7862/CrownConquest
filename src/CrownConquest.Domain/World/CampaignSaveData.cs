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

public sealed class CampaignSaveData
{
    public int SimulationTick { get; set; }
    public int CampaignTurn { get; set; }
    public int TicksPerTurn { get; set; } = 100;
    public List<SerializedProvinceData> Provinces { get; set; } = new();
    public List<SerializedArmyData> Armies { get; set; } = new();
    public List<SerializedTreasuryData> Treasuries { get; set; } = new();
}

/// <summary>
/// Deterministic JSON serializer and deserializer for campaign save states.
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
            var engine = new CampaignEngine(map, ticksPerTurn: data.TicksPerTurn);
            engine.RestoreTickState(data.SimulationTick, data.CampaignTurn);

            // Restore treasuries
            foreach (var tData in data.Treasuries)
            {
                var faction = int.TryParse(tData.FactionId, out int tVal) ? new FactionId(tVal) : FactionId.Neutral;
                var inv = new ResourceCost(Food: tData.Food, Wood: tData.Wood, Gold: tData.Gold, Stone: tData.Stone, Iron: tData.Iron);
                engine.SetTreasury(faction, inv);
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
