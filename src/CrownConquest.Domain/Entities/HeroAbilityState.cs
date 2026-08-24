using System;

namespace CrownConquest.Domain.Entities;

/// <summary>
/// Runtime state of an ability learned by a hero, tracking cooldown ticks and availability.
/// </summary>
public sealed class HeroAbilityState
{
    public HeroAbilityDefinition Definition { get; }
    public int CooldownRemainingTicks { get; internal set; }
    public int Level { get; internal set; } = 1;

    public bool IsReady => CooldownRemainingTicks == 0;
    public float CooldownNormalized => Definition.CooldownTicks > 0
        ? (float)CooldownRemainingTicks / Definition.CooldownTicks
        : 0f;

    public HeroAbilityState(HeroAbilityDefinition definition, int level = 1)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        CooldownRemainingTicks = 0;
        Level = level;
    }

    public void TriggerCooldown()
    {
        CooldownRemainingTicks = Definition.CooldownTicks;
    }

    public void DecrementCooldown()
    {
        if (CooldownRemainingTicks > 0)
        {
            CooldownRemainingTicks--;
        }
    }

    public void ResetCooldown()
    {
        CooldownRemainingTicks = 0;
    }
}
