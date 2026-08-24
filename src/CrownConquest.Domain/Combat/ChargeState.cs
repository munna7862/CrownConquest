using System;

namespace CrownConquest.Domain.Combat;

/// <summary>
/// Tracks cavalry charge momentum, buildup ticks, and impact readiness.
/// </summary>
public sealed class ChargeState
{
    public const int TicksToFullCharge = 20; // 1 second at 20 ticks/sec
    public const float MaxChargeSpeedMultiplier = 1.4f;
    public const float MaxChargeDamageMultiplier = 2.0f; // +100% impact damage
    public const float ChargeMoraleShock = 25.0f;

    public int MomentumTicks { get; private set; }
    public bool IsCharging => MomentumTicks >= TicksToFullCharge;
    public float MomentumProgress => Math.Clamp((float)MomentumTicks / TicksToFullCharge, 0f, 1.0f);

    public float CurrentSpeedMultiplier => 1.0f + (MomentumProgress * (MaxChargeSpeedMultiplier - 1.0f));

    public void IncrementMomentum()
    {
        if (MomentumTicks < TicksToFullCharge)
        {
            MomentumTicks++;
        }
    }

    public void Reset()
    {
        MomentumTicks = 0;
    }

    public void Discharge()
    {
        MomentumTicks = 0;
    }

    public void SetMomentum(int ticks)
    {
        MomentumTicks = Math.Clamp(ticks, 0, TicksToFullCharge);
    }
}
