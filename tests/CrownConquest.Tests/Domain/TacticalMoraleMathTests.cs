using System;
using CrownConquest.Domain.Combat;
using Xunit;

namespace CrownConquest.Tests.Domain;

public sealed class TacticalMoraleMathTests
{
    [Fact]
    public void TC_S06_06_MoraleLevelEvaluation_TransitionsAccuratelyAcrossThresholds()
    {
        var morale = new MoraleState(100f);
        Assert.Equal(MoraleLevel.Confident, morale.Level);

        morale.SetMorale(85f);
        Assert.Equal(MoraleLevel.Confident, morale.Level);

        morale.SetMorale(79.9f);
        Assert.Equal(MoraleLevel.Steady, morale.Level);

        morale.SetMorale(50.0f);
        Assert.Equal(MoraleLevel.Steady, morale.Level);

        morale.SetMorale(49.9f);
        Assert.Equal(MoraleLevel.Wavering, morale.Level);

        morale.SetMorale(25.0f);
        Assert.Equal(MoraleLevel.Wavering, morale.Level);

        morale.SetMorale(24.9f);
        Assert.Equal(MoraleLevel.Breaking, morale.Level);

        morale.SetMorale(1.0f);
        Assert.Equal(MoraleLevel.Breaking, morale.Level);

        morale.SetMorale(0.0f);
        Assert.Equal(MoraleLevel.Routed, morale.Level);
        Assert.True(morale.IsRouted);
    }

    [Fact]
    public void TC_S06_07_MoraleDrainAndRecovery_EvaluatesShocksAndRalliesCorrectly()
    {
        var morale = new MoraleState(100f);

        // Friendly casualty shock: -10
        morale.ApplyShock(10f);
        Assert.Equal(90f, morale.CurrentMorale);

        // Flanking shock: -15
        morale.ApplyShock(15f);
        Assert.Equal(75f, morale.CurrentMorale);
        Assert.Equal(MoraleLevel.Steady, morale.Level);

        // Cavalry charge shock: -25
        morale.ApplyShock(25f);
        Assert.Equal(50f, morale.CurrentMorale);
        Assert.Equal(MoraleLevel.Steady, morale.Level);

        // Hero death shock: -30
        morale.ApplyShock(30f);
        Assert.Equal(20f, morale.CurrentMorale);
        Assert.Equal(MoraleLevel.Breaking, morale.Level);

        // Break completely into Routed
        morale.ApplyShock(25f);
        Assert.Equal(0f, morale.CurrentMorale);
        Assert.True(morale.IsRouted);

        // Rally restores to at least 25 (recovers from Routed to Wavering)
        morale.Rally(25f);
        Assert.Equal(25f, morale.CurrentMorale);
        Assert.Equal(MoraleLevel.Wavering, morale.Level);
        Assert.False(morale.IsRouted);

        // Passive recovery
        morale.Recover(10f);
        Assert.Equal(35f, morale.CurrentMorale);
    }
}
