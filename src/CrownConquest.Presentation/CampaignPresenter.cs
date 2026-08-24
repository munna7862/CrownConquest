using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.World;

namespace CrownConquest.Presentation;

public sealed class ProvinceViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public Vector2D Position { get; init; }
    public string TerrainName { get; init; } = "Plains";
    public string NodeType { get; init; } = "Settlement";
    public string OwnerFaction { get; init; } = "Neutral";
    public int StationedArmyCount { get; init; }
    public int GarrisonCount { get; init; }
    public float DefenseBonus { get; init; } = 1.0f;
}

public sealed class ArmyViewModel
{
    public int Id { get; init; }
    public string Faction { get; init; } = "Player";
    public string Name { get; init; } = string.Empty;
    public string CurrentProvince { get; init; } = string.Empty;
    public string? DestinationProvince { get; init; }
    public float MovementProgressNormalized { get; init; }
    public int UnitCount { get; init; }
    public string? HeroName { get; init; }
    public float TotalPower { get; init; }
}

/// <summary>
/// Presentation layer view model generator for the strategic campaign world map.
/// Completely decoupled from simulation mutations.
/// </summary>
public sealed class CampaignPresenter
{
    private readonly CampaignEngine _engine;

    public CampaignPresenter(CampaignEngine engine)
    {
        _engine = engine;
    }

    public List<ProvinceViewModel> GetProvinceViewModels()
    {
        var list = new List<ProvinceViewModel>();
        foreach (var p in _engine.Map.GetAllProvinces())
        {
            list.Add(new ProvinceViewModel
            {
                Id = p.Id.Value,
                Name = p.Name,
                Position = p.Position,
                TerrainName = p.Terrain.ToString(),
                NodeType = p.NodeType.ToString(),
                OwnerFaction = p.OwnerFaction.ToString(),
                StationedArmyCount = p.StationedArmyIds.Count,
                GarrisonCount = p.GarrisonUnits.Count,
                DefenseBonus = p.GarrisonDefenseBonus
            });
        }
        return list;
    }

    public List<ArmyViewModel> GetArmyViewModels()
    {
        var list = new List<ArmyViewModel>();
        foreach (var a in _engine.GetAllArmies())
        {
            float progress = 0f;
            if (a.IsInTransit && a.TotalMovementTicksForEdge > 0)
            {
                progress = 1.0f - ((float)a.MovementTicksRemaining / a.TotalMovementTicksForEdge);
            }

            list.Add(new ArmyViewModel
            {
                Id = a.Id.Value,
                Faction = a.FactionId.ToString(),
                Name = a.Name,
                CurrentProvince = a.CurrentProvinceId.Value,
                DestinationProvince = a.DestinationProvinceId?.Value,
                MovementProgressNormalized = progress,
                UnitCount = a.UnitCount,
                HeroName = a.AttachedHero?.HeroName,
                TotalPower = a.TotalCombatPower
            });
        }
        return list;
    }

    public Dictionary<string, float> GetTerritoryControlDistribution()
    {
        var result = new Dictionary<string, float>();
        var dist = _engine.TerritoryManager.GetOwnershipDistribution();
        int total = _engine.Map.ProvinceCount;

        foreach (var kvp in dist)
        {
            result[kvp.Key.ToString()] = total > 0 ? (float)kvp.Value / total : 0f;
        }
        return result;
    }
}
