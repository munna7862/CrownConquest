using System;
using System.Collections.Generic;
using CrownConquest.Domain.World;

namespace CrownConquest.Presentation;

public sealed class MissionViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Type { get; init; } = "Defend";
    public string IssuingFaction { get; init; } = string.Empty;
    public string? TargetFaction { get; init; }
    public string TargetProvince { get; init; } = string.Empty;
    public string? DestinationProvince { get; init; }
    public string Status { get; init; } = "Inactive";
    public int CurrentProgress { get; init; }
    public int TargetQuantity { get; init; }
    public float ProgressFraction { get; init; }
    public int ElapsedTicks { get; init; }
    public int DurationTicks { get; init; }
    public int GoldReward { get; init; }
    public int XpReward { get; init; }
    public int ReputationReward { get; init; }
    public bool IsPrimary { get; init; }
}

public sealed class FactionDiplomacyViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Culture { get; init; } = string.Empty;
    public string Standing { get; init; } = "Neutral";
    public int Reputation { get; init; }
    public string ColorHex { get; init; } = "#FFFFFF";
    public double TradeBonusMultiplier { get; init; }
    public bool IsAtWar { get; init; }
    public bool IsAllied { get; init; }
}

/// <summary>
/// Presentation layer view model generator for missions, objectives HUD, and diplomacy standings.
/// </summary>
public sealed class CampaignMissionPresenter
{
    private readonly CampaignEngine _engine;

    public CampaignMissionPresenter(CampaignEngine engine)
    {
        _engine = engine;
    }

    public List<MissionViewModel> GetActiveMissionViewModels()
    {
        var list = new List<MissionViewModel>();
        for (int i = 0; i < _engine.Missions.ActiveMissionIds.Count; i++)
        {
            var mId = _engine.Missions.ActiveMissionIds[i];
            if (_engine.Missions.TryGetMission(mId, out var state) && state != null)
            {
                list.Add(ToViewModel(state));
            }
        }
        return list;
    }

    public List<MissionViewModel> GetAllMissionViewModels()
    {
        var list = new List<MissionViewModel>();
        foreach (var state in _engine.Missions.GetAllMissions())
        {
            list.Add(ToViewModel(state));
        }
        return list;
    }

    public List<FactionDiplomacyViewModel> GetFactionDiplomacyViewModels()
    {
        var list = new List<FactionDiplomacyViewModel>();
        foreach (var f in _engine.Diplomacy.GetAllFactions())
        {
            list.Add(new FactionDiplomacyViewModel
            {
                Id = f.Id,
                Name = f.Name,
                Culture = f.Culture,
                Standing = _engine.Diplomacy.GetStanding(f.Id).ToString(),
                Reputation = _engine.Diplomacy.GetReputation(f.Id),
                ColorHex = f.ColorHex,
                TradeBonusMultiplier = _engine.Diplomacy.GetTradeBonusModifier(f.Id),
                IsAtWar = _engine.Diplomacy.IsAtWar(f.Id),
                IsAllied = _engine.Diplomacy.IsAllied(f.Id)
            });
        }
        return list;
    }

    private static MissionViewModel ToViewModel(MissionRuntimeState state)
    {
        var def = state.Definition;
        return new MissionViewModel
        {
            Id = def.Id,
            Name = def.Name,
            Description = def.Description,
            Type = def.Type.ToString(),
            IssuingFaction = def.IssuingFactionId,
            TargetFaction = def.TargetFactionId,
            TargetProvince = def.TargetProvinceId.Value,
            DestinationProvince = def.DestinationProvinceId?.Value,
            Status = state.Status.ToString(),
            CurrentProgress = state.CurrentProgress,
            TargetQuantity = state.TargetQuantity,
            ProgressFraction = state.ProgressFraction,
            ElapsedTicks = state.ElapsedTicks,
            DurationTicks = def.DurationTicks,
            GoldReward = def.GoldReward,
            XpReward = def.XpReward,
            ReputationReward = def.ReputationReward,
            IsPrimary = def.IsPrimaryCampaign
        };
    }
}
