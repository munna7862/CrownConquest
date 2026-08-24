using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Events;

namespace CrownConquest.Domain.World;

public readonly record struct ArmyMovedOnMapEvent(ulong SimulationTick, StrategicArmyId ArmyId, ProvinceId FromProvince, ProvinceId ToProvince) : IDomainEvent;
public readonly record struct ArmyArrivedAtProvinceEvent(ulong SimulationTick, StrategicArmyId ArmyId, ProvinceId ProvinceId) : IDomainEvent;
public readonly record struct BattleEngagementStartedEvent(ulong SimulationTick, StrategicArmyId AttackerArmyId, ProvinceId ProvinceId, StrategicArmyId? DefenderArmyId) : IDomainEvent;
public readonly record struct BattleEngagementResolvedEvent(ulong SimulationTick, StrategicArmyId AttackerArmyId, ProvinceId ProvinceId, FactionId VictorFaction, int AttackerCasualties, int DefenderCasualties) : IDomainEvent;
public readonly record struct ProvinceCapturedEvent(ulong SimulationTick, ProvinceId ProvinceId, FactionId PreviousOwner, FactionId NewOwner) : IDomainEvent;
public readonly record struct ArmyDestroyedEvent(ulong SimulationTick, StrategicArmyId ArmyId, FactionId FactionId, ProvinceId ProvinceId) : IDomainEvent;
public readonly record struct CampaignTurnAdvancedEvent(ulong SimulationTick, int TurnNumber) : IDomainEvent;
public readonly record struct CampaignResourceYieldCollectedEvent(ulong SimulationTick, FactionId FactionId, ResourceCost TotalYield) : IDomainEvent;
