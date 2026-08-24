using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Events;

namespace CrownConquest.Domain.World;

/// <summary>
/// Authoritative simulation coordinator for campaign mission lifecycle, objective evaluations, and reward disbursements.
/// </summary>
public sealed class MissionEngine
{
    private readonly Dictionary<string, MissionDefinition> _definitions = new();
    private readonly Dictionary<string, MissionRuntimeState> _runtimeStates = new();
    private readonly List<string> _activeMissionIds = new();
    private readonly List<string> _completedMissionIds = new();
    private readonly List<string> _failedMissionIds = new();

    public IReadOnlyList<string> ActiveMissionIds => _activeMissionIds;
    public IReadOnlyList<string> CompletedMissionIds => _completedMissionIds;
    public IReadOnlyList<string> FailedMissionIds => _failedMissionIds;

    public void RegisterMission(MissionDefinition mission)
    {
        _definitions[mission.Id] = mission;
        if (!_runtimeStates.ContainsKey(mission.Id))
        {
            _runtimeStates[mission.Id] = new MissionRuntimeState(mission);
        }
    }

    public bool TryGetMission(string missionId, out MissionRuntimeState? state)
    {
        return _runtimeStates.TryGetValue(missionId, out state);
    }

    public MissionRuntimeState? GetMission(string missionId)
    {
        _runtimeStates.TryGetValue(missionId, out var state);
        return state;
    }

    public IEnumerable<MissionRuntimeState> GetAllMissions()
    {
        foreach (var kvp in _runtimeStates)
        {
            yield return kvp.Value;
        }
    }

    public Result AcceptMission(
        string missionId,
        int currentTick,
        StrategicArmyId? armyId = null,
        int? heroEntityId = null,
        DomainEventBus? eventBus = null)
    {
        if (!_runtimeStates.TryGetValue(missionId, out var state) || state == null)
        {
            return Result.Failure(new GameError("MISSION_NOT_FOUND", $"Mission {missionId} is not registered."));
        }

        if (state.Status == MissionStatus.Active)
        {
            return Result.Failure(new GameError("MISSION_ALREADY_ACTIVE", $"Mission {missionId} is already active."));
        }

        if (state.IsTerminal)
        {
            return Result.Failure(new GameError("MISSION_ALREADY_FINISHED", $"Mission {missionId} has already finished."));
        }

        state.Start(currentTick, armyId, heroEntityId);
        _activeMissionIds.Add(missionId);

        eventBus?.Publish(new MissionStartedEvent((ulong)currentTick, state.MissionId, state.Type, state.Definition.IssuingFactionId));
        return Result.Success();
    }

    public Result AbandonMission(string missionId, int currentTick, DomainEventBus? eventBus = null)
    {
        if (!_runtimeStates.TryGetValue(missionId, out var state) || state == null)
        {
            return Result.Failure(new GameError("MISSION_NOT_FOUND", $"Mission {missionId} is not registered."));
        }

        if (state.Status != MissionStatus.Active)
        {
            return Result.Failure(new GameError("MISSION_NOT_ACTIVE", $"Mission {missionId} is not active."));
        }

        state.Fail("Abandoned by commander.", currentTick);
        _activeMissionIds.Remove(missionId);
        _failedMissionIds.Add(missionId);

        eventBus?.Publish(new MissionFailedEvent((ulong)currentTick, state.MissionId, state.Type, state.FailureReason ?? "Abandoned"));
        return Result.Success();
    }

    public void ReportCasualties(string targetFactionId, ProvinceId provinceId, int casualties, int currentTick, DomainEventBus? eventBus = null)
    {
        for (int i = 0; i < _activeMissionIds.Count; i++)
        {
            var mId = _activeMissionIds[i];
            if (_runtimeStates.TryGetValue(mId, out var state) && state != null && state.IsActive)
            {
                if (state.Type == MissionType.Destroy &&
                    state.Definition.TargetProvinceId == provinceId &&
                    (string.IsNullOrEmpty(state.Definition.TargetFactionId) || state.Definition.TargetFactionId == targetFactionId))
                {
                    state.CurrentProgress += casualties;
                    eventBus?.Publish(new MissionProgressUpdatedEvent((ulong)currentTick, state.MissionId, state.CurrentProgress, state.TargetQuantity));
                }
            }
        }
    }

    public void ReportConvoyArrival(StrategicArmyId convoyArmyId, ProvinceId arrivalProvinceId, int currentTick, DomainEventBus? eventBus = null)
    {
        for (int i = 0; i < _activeMissionIds.Count; i++)
        {
            var mId = _activeMissionIds[i];
            if (_runtimeStates.TryGetValue(mId, out var state) && state != null && state.IsActive)
            {
                if (state.Type == MissionType.Escort &&
                    state.AssignedArmyId == convoyArmyId &&
                    state.Definition.DestinationProvinceId == arrivalProvinceId)
                {
                    state.CurrentProgress = state.TargetQuantity;
                    eventBus?.Publish(new MissionProgressUpdatedEvent((ulong)currentTick, state.MissionId, state.CurrentProgress, state.TargetQuantity));
                }
            }
        }
    }

    public void ReportConvoyDestruction(StrategicArmyId convoyArmyId, int currentTick, DomainEventBus? eventBus = null)
    {
        for (int i = 0; i < _activeMissionIds.Count; i++)
        {
            var mId = _activeMissionIds[i];
            if (_runtimeStates.TryGetValue(mId, out var state) && state != null && state.IsActive)
            {
                if (state.Type == MissionType.Escort && state.AssignedArmyId == convoyArmyId)
                {
                    state.Fail("Convoy destroyed.", currentTick);
                }
            }
        }
    }

    public void EvaluateMissions(
        int currentTick,
        CampaignEngine campaign,
        FactionDiplomacyManager diplomacy,
        DomainEventBus? eventBus = null)
    {
        for (int i = _activeMissionIds.Count - 1; i >= 0; i--)
        {
            var mId = _activeMissionIds[i];
            if (!_runtimeStates.TryGetValue(mId, out var state) || state == null || !state.IsActive)
            {
                continue;
            }

            state.ElapsedTicks++;

            switch (state.Type)
            {
                case MissionType.Defend:
                    EvaluateDefend(state, currentTick, campaign);
                    break;

                case MissionType.Destroy:
                    EvaluateDestroy(state, currentTick);
                    break;

                case MissionType.Capture:
                    EvaluateCapture(state, currentTick, campaign);
                    break;

                case MissionType.Escort:
                    EvaluateEscort(state, currentTick, campaign);
                    break;

                case MissionType.ResourceControl:
                    EvaluateResourceControl(state, currentTick, campaign);
                    break;
            }

            // Handle Completion
            if (state.IsCompleted)
            {
                _activeMissionIds.RemoveAt(i);
                _completedMissionIds.Add(state.MissionId);

                ApplyMissionRewards(state, currentTick, campaign, diplomacy, eventBus);
            }
            // Handle Failure or Expiry
            else if (state.IsTerminal)
            {
                _activeMissionIds.RemoveAt(i);
                _failedMissionIds.Add(state.MissionId);

                if (state.Status == MissionStatus.Expired)
                {
                    eventBus?.Publish(new MissionExpiredEvent((ulong)currentTick, state.MissionId, state.Type));
                }
                else
                {
                    eventBus?.Publish(new MissionFailedEvent((ulong)currentTick, state.MissionId, state.Type, state.FailureReason ?? "Failed"));
                }
            }
        }
    }

    private void EvaluateDefend(MissionRuntimeState state, int currentTick, CampaignEngine campaign)
    {
        if (campaign.Map.TryGetProvince(state.Definition.TargetProvinceId, out var prov) && prov != null)
        {
            // If province was taken by enemy, fail
            if (prov.OwnerFaction == FactionId.Enemy)
            {
                state.Fail("Target province captured by enemy.", currentTick);
                return;
            }
        }

        // Check if assigned army is dead
        if (state.AssignedArmyId.HasValue && !campaign.TryGetArmy(state.AssignedArmyId.Value, out _))
        {
            state.Fail("Defending army destroyed.", currentTick);
            return;
        }

        state.CurrentProgress = state.ElapsedTicks;

        // If held until full duration, complete
        if (state.ElapsedTicks >= state.Definition.DurationTicks)
        {
            state.Complete(currentTick);
        }
    }

    private void EvaluateDestroy(MissionRuntimeState state, int currentTick)
    {
        if (state.CurrentProgress >= state.TargetQuantity)
        {
            state.Complete(currentTick);
            return;
        }

        if (state.ElapsedTicks >= state.Definition.DurationTicks)
        {
            state.Expire(currentTick);
        }
    }

    private void EvaluateCapture(MissionRuntimeState state, int currentTick, CampaignEngine campaign)
    {
        if (campaign.Map.TryGetProvince(state.Definition.TargetProvinceId, out var prov) && prov != null)
        {
            // Check if player controls province
            if (prov.OwnerFaction == FactionId.Player)
            {
                state.CurrentProgress++;
                if (state.CurrentProgress >= state.TargetQuantity)
                {
                    state.Complete(currentTick);
                    return;
                }
            }
        }

        if (state.ElapsedTicks >= state.Definition.DurationTicks)
        {
            state.Expire(currentTick);
        }
    }

    private void EvaluateEscort(MissionRuntimeState state, int currentTick, CampaignEngine campaign)
    {
        if (state.AssignedArmyId.HasValue)
        {
            if (!campaign.TryGetArmy(state.AssignedArmyId.Value, out var army) || army == null || !army.HasUnits)
            {
                state.Fail("Convoy destroyed in transit.", currentTick);
                return;
            }

            if (state.Definition.DestinationProvinceId.HasValue &&
                army.CurrentProvinceId == state.Definition.DestinationProvinceId.Value &&
                !army.IsInTransit)
            {
                state.Complete(currentTick);
                return;
            }
        }

        if (state.ElapsedTicks >= state.Definition.DurationTicks)
        {
            state.Expire(currentTick);
        }
    }

    private void EvaluateResourceControl(MissionRuntimeState state, int currentTick, CampaignEngine campaign)
    {
        var treasury = campaign.GetTreasury(FactionId.Player);
        var required = state.Definition.RequiredResources;

        int totalHarvested = treasury.Food + treasury.Iron + treasury.Gold;
        state.CurrentProgress = totalHarvested;

        bool foodMet = required.Food <= 0 || treasury.Food >= required.Food;
        bool ironMet = required.Iron <= 0 || treasury.Iron >= required.Iron;
        bool goldMet = required.Gold <= 0 || treasury.Gold >= required.Gold;

        if (foodMet && ironMet && goldMet && (state.TargetQuantity <= 0 || totalHarvested >= state.TargetQuantity))
        {
            state.Complete(currentTick);
            return;
        }

        if (state.ElapsedTicks >= state.Definition.DurationTicks)
        {
            state.Expire(currentTick);
        }
    }

    private void ApplyMissionRewards(
        MissionRuntimeState state,
        int currentTick,
        CampaignEngine campaign,
        FactionDiplomacyManager diplomacy,
        DomainEventBus? eventBus)
    {
        var def = state.Definition;

        // Reward gold to player treasury
        if (def.GoldReward > 0)
        {
            var treasury = campaign.GetTreasury(FactionId.Player);
            var updatedTreasury = treasury with { Gold = treasury.Gold + def.GoldReward };
            campaign.SetTreasury(FactionId.Player, updatedTreasury);
        }

        // Apply diplomacy changes
        if (def.ReputationReward != 0 && !string.IsNullOrEmpty(def.IssuingFactionId))
        {
            diplomacy.ModifyReputation(def.IssuingFactionId, def.ReputationReward, (ulong)currentTick);
        }

        if (!string.IsNullOrEmpty(def.TargetFactionId) && def.ReputationReward != 0)
        {
            int penalty = -Math.Max(5, def.ReputationReward / 2);
            diplomacy.ModifyReputation(def.TargetFactionId, penalty, (ulong)currentTick);
        }

        eventBus?.Publish(new MissionCompletedEvent(
            (ulong)currentTick,
            state.MissionId,
            state.Type,
            def.IssuingFactionId,
            def.GoldReward,
            def.XpReward,
            def.ReputationReward
        ));
    }
}
