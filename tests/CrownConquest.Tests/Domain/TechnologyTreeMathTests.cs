using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using Xunit;

namespace CrownConquest.Tests.Domain;

public sealed class TechnologyTreeMathTests
{
    [Fact]
    public void TechModifiers_CumulativeAggregation()
    {
        // TC-S04-003: Modifiers accumulate additively across multiple researched techs
        var baseMods = TechModifiers.Zero;

        var forgingMods = new TechModifiers(MeleeAttackBonus: 2, CavalryAttackBonus: 2);
        var ironWeaponsMods = new TechModifiers(MeleeAttackBonus: 3, CavalryAttackBonus: 3);
        var scaleArmorMods = new TechModifiers(MeleeArmorBonus: 2, CavalryArmorBonus: 2);
        var fletchingMods = new TechModifiers(RangedAttackBonus: 1, RangedRangeBonus: 1.0f);
        var husbandryMods = new TechModifiers(CavalrySpeedBonus: 1.0f);

        var combined = baseMods
            .Combine(forgingMods)
            .Combine(ironWeaponsMods)
            .Combine(scaleArmorMods)
            .Combine(fletchingMods)
            .Combine(husbandryMods);

        Assert.Equal(5, combined.MeleeAttackBonus);
        Assert.Equal(2, combined.MeleeArmorBonus);
        Assert.Equal(1, combined.RangedAttackBonus);
        Assert.Equal(1.0f, combined.RangedRangeBonus);
        Assert.Equal(5, combined.CavalryAttackBonus);
        Assert.Equal(2, combined.CavalryArmorBonus);
        Assert.Equal(1.0f, combined.CavalrySpeedBonus);
    }

    [Fact]
    public void ResearchQueue_Enqueue_Advance_Dequeue()
    {
        // TC-S04-004: ResearchQueue tracks progression and completes at duration
        var queue = new ResearchQueue(maxQueueSize: 3);
        var tech = new TechnologyDefinition(
            "forging",
            "Forging",
            "Melee dmg +2",
            TechCategory.Military,
            CivilizationEra.Classical,
            new ResourceCost(Food: 150, Gold: 50),
            researchDurationTicks: 40,
            new TechModifiers(MeleeAttackBonus: 2));

        var item = new ResearchQueueItem(tech, 40, tech.Cost);
        bool enqueued = queue.TryEnqueue(item);
        Assert.True(enqueued);
        Assert.Equal(1, queue.Count);
        Assert.False(queue.IsEmpty);

        var current = queue.CurrentItem;
        Assert.NotNull(current);
        Assert.Equal(0f, current!.ProgressNormalized);

        current.AdvanceTicks(20);
        Assert.Equal(0.5f, current.ProgressNormalized);
        Assert.False(current.IsCompleted);

        current.AdvanceTicks(20);
        Assert.Equal(1.0f, current.ProgressNormalized);
        Assert.True(current.IsCompleted);

        var dequeued = queue.TryDequeue();
        Assert.Same(item, dequeued);
        Assert.True(queue.IsEmpty);
    }

    [Fact]
    public void TechManager_PrerequisiteValidation()
    {
        var factionId = new FactionId(1);
        var techManager = new FactionTechManager(factionId);

        var forging = new TechnologyDefinition(
            "forging",
            "Forging",
            "Tier 1",
            TechCategory.Military,
            CivilizationEra.Classical,
            ResourceCost.Zero,
            40,
            new TechModifiers(MeleeAttackBonus: 2),
            requiredBuildingTypes: new[] { "blacksmith" });

        var ironWeapons = new TechnologyDefinition(
            "iron_weapons",
            "Iron Weapons",
            "Tier 2",
            TechCategory.Military,
            CivilizationEra.Imperial,
            ResourceCost.Zero,
            60,
            new TechModifiers(MeleeAttackBonus: 3),
            requiredTechIds: new[] { "forging" },
            requiredBuildingTypes: new[] { "blacksmith" });

        var blacksmith = new BuildingEntity(
            new EntityId(10),
            factionId,
            "blacksmith",
            new Vector2D(10f, 10f),
            new Vector2D(3f, 3f),
            startsConstructed: true);

        var buildings = new List<BuildingEntity> { blacksmith };

        // 1. Archaic Era cannot research Forging (Requires Classical)
        Assert.False(techManager.CanResearch(forging, CivilizationEra.Archaic, buildings, out string eraReason));
        Assert.Contains("Requires Bronze / Classical Era", eraReason);

        // 2. Classical Era without Blacksmith cannot research Forging
        Assert.False(techManager.CanResearch(forging, CivilizationEra.Classical, Array.Empty<BuildingEntity>(), out string bldgReason));
        Assert.Contains("Requires constructed building 'blacksmith'", bldgReason);

        // 3. Classical Era with Blacksmith CAN research Forging
        Assert.True(techManager.CanResearch(forging, CivilizationEra.Classical, buildings, out _));

        // 4. Cannot research Iron Weapons without Forging
        Assert.False(techManager.CanResearch(ironWeapons, CivilizationEra.Imperial, buildings, out string prereqReason));
        Assert.Contains("Missing prerequisite technology 'forging'", prereqReason);

        // 5. Unlock Forging -> Iron Weapons becomes researchable in Imperial Era
        techManager.TryUnlockTechnology(forging, blacksmith.Id, 1UL, null);
        Assert.True(techManager.IsResearched("forging"));
        Assert.Equal(2, techManager.Modifiers.MeleeAttackBonus);

        Assert.True(techManager.CanResearch(ironWeapons, CivilizationEra.Imperial, buildings, out _));
    }
}
