using CrownConquest.Domain.Common;
using CrownConquest.Domain.Events;

namespace CrownConquest.Domain.Entities;

/// <summary>
/// Authoritative individual unit progression state machine.
/// Implements immediate level-up and veterancy rank evaluation.
/// </summary>
public sealed class VeterancyState
{
    private static readonly int[] DefaultXpThresholds =
    [
        0,     // Level 1 (Base)
        100,   // Level 2
        250,   // Level 3 (Experienced)
        450,   // Level 4
        700,   // Level 5 (Veteran)
        1000,  // Level 6
        1350,  // Level 7 (Elite)
        1750,  // Level 8
        2200,  // Level 9 (Legendary)
        2700   // Level 10
    ];

    private readonly int[] _xpThresholds;

    public EntityId EntityId { get; }
    public int Level { get; private set; }
    public int CurrentXp { get; private set; }
    public int KillCount { get; private set; }
    public VeterancyRank Rank { get; private set; }

    public int XpToNextLevel => Level < _xpThresholds.Length
        ? _xpThresholds[Level] - CurrentXp
        : 0;

    public VeterancyState(EntityId entityId, int initialLevel = 1, int initialXp = 0, int[]? customThresholds = null)
    {
        EntityId = entityId;
        _xpThresholds = customThresholds ?? DefaultXpThresholds;
        Level = Math.Max(1, initialLevel);
        CurrentXp = Math.Max(0, initialXp);
        KillCount = 0;
        Rank = VeterancyRankExtensions.GetRankForLevel(Level);
    }

    /// <summary>
    /// Award XP directly to the unit, evaluating level-up and rank transitions immediately.
    /// </summary>
    public void AwardXp(
        int xp,
        ulong simulationTick,
        DomainEventBus eventBus,
        out bool leveledUp,
        out bool rankChanged)
    {
        leveledUp = false;
        rankChanged = false;

        if (xp <= 0)
        {
            return;
        }

        CurrentXp += xp;
        eventBus.Publish(new UnitGainedXpEvent(simulationTick, EntityId, xp, CurrentXp, XpToNextLevel));

        int oldLevel = Level;
        VeterancyRank oldRank = Rank;

        while (Level < _xpThresholds.Length && CurrentXp >= _xpThresholds[Level])
        {
            Level++;
            leveledUp = true;
        }

        if (leveledUp)
        {
            float hpBonus = (Level - oldLevel) * 15f;
            float dmgBonus = (Level - oldLevel) * 2.5f;

            eventBus.Publish(new UnitLevelUpEvent(
                simulationTick,
                EntityId,
                oldLevel,
                Level,
                hpBonus,
                dmgBonus));

            VeterancyRank newRank = VeterancyRankExtensions.GetRankForLevel(Level);
            if (newRank != oldRank)
            {
                Rank = newRank;
                rankChanged = true;
                eventBus.Publish(new VeterancyRankChangedEvent(
                    simulationTick,
                    EntityId,
                    oldRank,
                    newRank));
            }
        }
    }

    /// <summary>
    /// Record a battlefield kill attribution.
    /// </summary>
    public void RecordKill()
    {
        KillCount++;
    }
}
