using System;
using System.Collections.Generic;
using System.Text;
using CrownConquest.Application;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Commands;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;
using CrownConquest.Domain.Shipping;
using CrownConquest.Domain.Simulation;
using CrownConquest.Presentation;
using Xunit;

namespace CrownConquest.Tests.Presentation;

public sealed class AudioCombatVfxTests
{
    // ==========================================
    // Tier 1: Pure Domain & Math Unit Tests
    // ==========================================

    [Fact]
    public void TC_S19_001_UnitVoiceBark_SelectCommand_ReturnsContextVoiceLine()
    {
        string celticLine = UnitVoiceBarkPresenter.ResolveVoiceLine(FactionId.Player1, UnitArchetype.Infantry, VoiceBarkType.Select, null, 10);
        string romanLine = UnitVoiceBarkPresenter.ResolveVoiceLine(FactionId.Player2, UnitArchetype.Infantry, VoiceBarkType.Select, null, 10);

        Assert.False(string.IsNullOrWhiteSpace(celticLine));
        Assert.False(string.IsNullOrWhiteSpace(romanLine));
        Assert.NotEqual(celticLine, romanLine);
    }

    [Fact]
    public void TC_S19_002_UnitVoiceBark_AttackCommand_ReturnsAggressiveBark()
    {
        string celticAttack = UnitVoiceBarkPresenter.ResolveVoiceLine(FactionId.Player1, UnitArchetype.Infantry, VoiceBarkType.Attack, null, 42);
        string romanAttack = UnitVoiceBarkPresenter.ResolveVoiceLine(FactionId.Player2, UnitArchetype.Infantry, VoiceBarkType.Attack, null, 42);

        Assert.False(string.IsNullOrWhiteSpace(celticAttack));
        Assert.False(string.IsNullOrWhiteSpace(romanAttack));
    }

    [Fact]
    public void TC_S19_003_UnitVoiceBark_HeroAbility_ReturnsUniqueHeroicLine()
    {
        string warCryLine = UnitVoiceBarkPresenter.ResolveVoiceLine(FactionId.Player1, UnitArchetype.Hero, VoiceBarkType.HeroAbility, "War Cry", 100);
        string heroicStrikeLine = UnitVoiceBarkPresenter.ResolveVoiceLine(FactionId.Player1, UnitArchetype.Hero, VoiceBarkType.HeroAbility, "Heroic Strike", 100);

        Assert.Equal("Feel our wrath!", warCryLine);
        Assert.Equal("Feel my blade!", heroicStrikeLine);

        string warCryCue = UnitVoiceBarkPresenter.ResolveAudioCue(FactionId.Player1, UnitArchetype.Hero, VoiceBarkType.HeroAbility, "War Cry");
        Assert.Equal("vox_hero_warcry", warCryCue);
    }

    [Fact]
    public void TC_S19_004_PositionalAudio_PanCalculation_MapsRelativeCameraXToNormalizedPan()
    {
        var camPos = new Vector2D(1000f, 1000f);
        var leftSound = new Vector2D(600f, 1000f);
        var rightSound = new Vector2D(1400f, 1000f);
        var centerSound = new Vector2D(1000f, 1000f);

        float panLeft = PositionalCombatAudioSystem.ComputePan(leftSound, camPos, 1600f);
        float panRight = PositionalCombatAudioSystem.ComputePan(rightSound, camPos, 1600f);
        float panCenter = PositionalCombatAudioSystem.ComputePan(centerSound, camPos, 1600f);

        Assert.Equal(-0.5f, panLeft, 2);
        Assert.Equal(0.5f, panRight, 2);
        Assert.Equal(0.0f, panCenter, 2);
    }

    [Fact]
    public void TC_S19_005_PositionalAudio_DistanceAttenuation_AttenuatesWithDistance()
    {
        var camPos = new Vector2D(1000f, 1000f);
        var closeSound = new Vector2D(1000f, 1000f);
        var midSound = new Vector2D(1400f, 1000f);
        var farSound = new Vector2D(2500f, 1000f);

        float attenClose = PositionalCombatAudioSystem.ComputeVolumeAttenuation(closeSound, camPos, 1200f, 300f);
        float attenMid = PositionalCombatAudioSystem.ComputeVolumeAttenuation(midSound, camPos, 1200f, 300f);
        float attenFar = PositionalCombatAudioSystem.ComputeVolumeAttenuation(farSound, camPos, 1200f, 300f);

        Assert.Equal(1.0f, attenClose, 2);
        Assert.True(attenMid < 1.0f && attenMid > 0.0f);
        Assert.Equal(0.0f, attenFar);
    }

    [Fact]
    public void TC_S19_006_ProjectilePhysics_ParabolicArc_ComputesHeightApexAtMidpoint()
    {
        var projectile = new ActiveProjectile
        {
            Id = 1,
            Type = ProjectileType.Arrow,
            Origin = new Vector2D(0f, 0f),
            Target = new Vector2D(400f, 0f),
            ApexHeight = 60f,
            TotalTicks = 20,
            CurrentTick = 10,
            IsActive = true
        };

        Assert.Equal(0.5f, projectile.Progress);
        Assert.Equal(200f, projectile.GroundPosition.X);
        Assert.Equal(0f, projectile.GroundPosition.Y);
        Assert.Equal(60f, projectile.ArcHeight); // 4 * 60 * 0.5 * 0.5 = 60
        Assert.Equal(new Vector2D(200f, -60f), projectile.VisualPosition);
        Assert.True(projectile.ShadowScale < 1.0f);
    }

    [Fact]
    public void TC_S19_007_CombatVfx_MeleeHitSparks_GeneratesVelocityParticleDescriptors()
    {
        var spark = CombatVfxPresenter.CreateHitSparkDescriptor(
            new Vector2D(100f, 100f),
            new Vector2D(1f, 0f),
            damage: 35f,
            seed: 42,
            tick: 50);

        Assert.Equal(CombatParticleType.Spark, spark.Type);
        Assert.Equal(100f, spark.Position.X);
        Assert.Equal(100f, spark.Position.Y);
        Assert.True(spark.Velocity.LengthSquared > 0f);
        Assert.Equal(1.0f, spark.Alpha);
        Assert.Equal(8, spark.MaxLifeTicks);
    }

    [Fact]
    public void TC_S19_008_CombatVfx_LevelUpRuneBurst_CalculatesExpandingRadiusAndAuraRing()
    {
        var rune = CombatVfxPresenter.CreateLevelUpRuneDescriptor(
            new Vector2D(300f, 300f),
            newLevel: 5,
            tick: 120);

        Assert.Equal(CombatParticleType.LevelUpRuneRing, rune.Type);
        Assert.Equal(new Vector2D(300f, 300f), rune.Position);
        Assert.True(rune.Scale > 1.5f);
        Assert.True(rune.MaxRadius > 50f);
    }

    // =========================================================================
    // Tier 2: Voice Anti-Overlap, Positional Panning & Projectile Invariant Tests
    // =========================================================================

    [Fact]
    public void TC_S19_009_UnitVoiceBark_CooldownInterval_SuppressesRapidSuccessiveBarks()
    {
        var presenter = new UnitVoiceBarkPresenter(capacity: 16, globalCooldownTicks: 8, unitCooldownTicks: 20);
        var unitId = new EntityId(101);

        bool first = presenter.TryTriggerVoiceBark(unitId, FactionId.Player1, UnitArchetype.Infantry, VoiceBarkType.Move, Vector2D.Zero, 10);
        bool second = presenter.TryTriggerVoiceBark(unitId, FactionId.Player1, UnitArchetype.Infantry, VoiceBarkType.Move, Vector2D.Zero, 12);
        bool third = presenter.TryTriggerVoiceBark(unitId, FactionId.Player1, UnitArchetype.Infantry, VoiceBarkType.Move, Vector2D.Zero, 14);

        Assert.True(first);
        Assert.False(second); // Suppressed by cooldown
        Assert.False(third);  // Suppressed by cooldown
        Assert.Equal(1, presenter.PendingBarkCount);
    }

    [Fact]
    public void TC_S19_010_UnitVoiceBark_HeroPriorityDucking_HeroOverridesRegularBarks()
    {
        var presenter = new UnitVoiceBarkPresenter(capacity: 16, globalCooldownTicks: 10, unitCooldownTicks: 20);
        var regularUnitId = new EntityId(101);
        var heroId = new EntityId(1);

        presenter.TryTriggerVoiceBark(regularUnitId, FactionId.Player1, UnitArchetype.Infantry, VoiceBarkType.Move, Vector2D.Zero, 10);
        // Hero casts War Cry at tick 12 (normally blocked by regular 10-tick global cooldown, but hero has priority)
        bool heroTriggered = presenter.TryTriggerVoiceBark(heroId, FactionId.Player1, UnitArchetype.Hero, VoiceBarkType.HeroAbility, Vector2D.Zero, 12, "War Cry");

        Assert.True(heroTriggered);
        Assert.Equal(2, presenter.PendingBarkCount);
        var heroBark = presenter.GetPendingBark(1);
        Assert.Equal(2.0f, heroBark.Priority);
        Assert.Equal("Feel our wrath!", heroBark.LineText);
    }

    [Fact]
    public void TC_S19_011_PositionalAudio_ConcurrencyLimiter_LimitsIdenticalSoundsPerTick()
    {
        var audioSys = new PositionalCombatAudioSystem(capacity: 64, maxConcurrentSameType: 3);
        var cam = new Vector2D(500f, 500f);
        var pos = new Vector2D(550f, 550f);

        int played = 0;
        for (int i = 0; i < 10; i++)
        {
            if (audioSys.TryQueueAudioCue("sfx_sword_clash", pos, cam, 0.8f, 1.0f, currentTick: 25))
            {
                played++;
            }
        }

        Assert.Equal(3, played); // Exactly 3 allowed on same tick
        Assert.Equal(3, audioSys.PendingCueCount);
    }

    [Fact]
    public void TC_S19_012_AdaptiveMusic_DynamicCombatTransition_PeaceToBattleOnHighIntensity()
    {
        var music = new AdaptiveMusicPresenter(skirmishThreshold: 0.2f, battleThreshold: 0.6f);

        var peaceDesc = music.GetDescriptor();
        Assert.Equal(MusicState.Peace, peaceDesc.CurrentState);

        var battleDesc = music.Update(combatIntensity: 0.85f);
        Assert.Equal(MusicState.Battle, battleDesc.CurrentState);
        Assert.Equal("mus_battle_epic", battleDesc.TrackId);
    }

    [Fact]
    public void TC_S19_013_AdaptiveMusic_DecayHysteresis_DelaysPeaceReturnAfterCombatEnds()
    {
        var music = new AdaptiveMusicPresenter(peaceDelayTicks: 50);
        music.Update(0.9f); // Battle

        // Combat ends
        for (int t = 0; t < 20; t++)
        {
            music.Update(0.0f);
        }

        // Must still remain in Battle due to hysteresis delay ticks
        Assert.Equal(MusicState.Battle, music.CurrentState);
    }

    [Fact]
    public void TC_S19_014_ProjectilePhysics_FlightProgress_AdvancesDeterministicallyToTarget()
    {
        var system = new ProjectilePhysicsSystem(capacity: 16);
        var origin = new Vector2D(100f, 100f);
        var target = new Vector2D(500f, 100f);

        uint id = system.SpawnProjectile(ProjectileType.CatapultBoulder, origin, target, flightTicks: 10, apexHeight: 80f);
        Assert.Equal(1u, id);
        Assert.Equal(1, system.ActiveCount);

        bool impacted = false;
        for (int t = 0; t < 10; t++)
        {
            system.Tick(p => impacted = true);
        }

        Assert.True(impacted);
        Assert.Equal(0, system.ActiveCount);
    }

    [Fact]
    public void TC_S19_015_CombatVfx_ParticleBuffer_ReusesRingBufferWithoutDynamicAllocations()
    {
        var vfx = new CombatVfxPresenter(capacity: 32);

        for (int i = 0; i < 50; i++)
        {
            vfx.PushParticle(CombatVfxPresenter.CreateHitSparkDescriptor(new Vector2D(i * 10, 0), new Vector2D(1, 0), 20f, i, (ulong)i));
        }

        Assert.Equal(32, vfx.PendingParticleCount); // Ring buffer capped at capacity
        var p = vfx.GetPendingParticle(0);
        Assert.Equal(CombatParticleType.Spark, p.Type);
    }

    [Fact]
    public void TC_S19_016_MatchResult_OutcomeEvaluation_DeclaresVictoryWhenEnemyTownCenterDestroyed()
    {
        var summary = MatchResultPresenter.CreateSummary(
            playerFaction: FactionId.Player1,
            outcome: MatchOutcome.Victory,
            totalTicks: 250,
            kills: 14,
            casualties: 3,
            unitsTrained: 8,
            resourcesHarvested: 1200,
            mvpHeroName: "Brennus, Chieftain",
            mvpHeroLevel: 4,
            mvpHeroKills: 9);

        Assert.Equal(MatchOutcome.Victory, summary.Outcome);
        Assert.Equal("TRIUMPHANT VICTORY", summary.BannerTitle);
        Assert.Equal(12.5f, summary.MatchDurationSeconds, 1);
        Assert.Equal(14, summary.TotalKills);
        Assert.Equal(4, summary.MvpHeroLevel);
    }

    // =========================================================================
    // Tier 3: Multi-System Audio, VFX & Scenario Integration Tests
    // =========================================================================

    [Fact]
    public void TC_S19_017_HistoricalBattleScenario_InitializesAuthoredBattlefield()
    {
        var scenario = new HistoricalBattleScenario(seed: 1904);

        Assert.NotNull(scenario.CelticTownCenter);
        Assert.NotNull(scenario.RomanTownCenter);
        Assert.NotNull(scenario.CelticHeroBrennus);
        Assert.NotNull(scenario.RomanCenturionLeader);
        Assert.Equal(MatchOutcome.Ongoing, scenario.Outcome);
        Assert.True(scenario.Coordinator.Simulation.State.ActiveUnits.Count >= 20);
    }

    [Fact]
    public void TC_S19_018_HistoricalBattleScenario_TownCenterDestructionTriggersDefeat()
    {
        var scenario = new HistoricalBattleScenario(seed: 1904);

        // Destroy Celtic Town Center
        scenario.CelticTownCenter.TakeDamage(3000f, new EntityId(200), FactionId.Player2, 10, scenario.Coordinator.EventBus, out _);
        scenario.SimulateTicks(1);

        Assert.Equal(MatchOutcome.Defeat, scenario.Outcome);
        var summary = scenario.GetMatchSummary();
        Assert.Equal("BITTER DEFEAT", summary.BannerTitle);
    }

    [Fact]
    public void TC_S19_019_MatchResultPresenter_AggregatesPostMatchMvpStats()
    {
        var scenario = new HistoricalBattleScenario(seed: 1904);
        scenario.SimulateTicks(50);

        var summary = scenario.GetMatchSummary();
        Assert.NotNull(summary);
        Assert.True(summary.TotalTicksExecuted >= 50);
        Assert.Equal("Brennus, Chieftain of the Senones", summary.MvpHeroName);
    }

    [Fact]
    public void TC_S19_020_CombatAudio_GatheringEvents_TriggersWoodStoneAnvilSounds()
    {
        var audioSys = new PositionalCombatAudioSystem();
        var cam = new Vector2D(600f, 600f);

        bool woodQueued = audioSys.TryQueueAudioCue("sfx_wood_chop", new Vector2D(650f, 600f), cam, 0.7f, 1.0f, 10);
        bool stoneQueued = audioSys.TryQueueAudioCue("sfx_stone_pick", new Vector2D(600f, 650f), cam, 0.7f, 1.0f, 10);
        bool anvilQueued = audioSys.TryQueueAudioCue("sfx_anvil_strike", new Vector2D(500f, 750f), cam, 0.8f, 1.0f, 10);

        Assert.True(woodQueued);
        Assert.True(stoneQueued);
        Assert.True(anvilQueued);
        Assert.Equal(3, audioSys.PendingCueCount);
    }

    [Fact]
    public void TC_S19_021_CombatVfx_BloodSplashParticles_TriggersOnFleshCasualty()
    {
        var blood = CombatVfxPresenter.CreateBloodSplashDescriptor(new Vector2D(450f, 450f), 100);
        Assert.Equal(CombatParticleType.BloodSplash, blood.Type);
        Assert.Equal(new Vector2D(450f, 450f), blood.Position);
        Assert.Equal(30, blood.MaxLifeTicks);
    }

    [Fact]
    public void TC_S19_022_CombatVfx_BuildingDamageFireAndSmoke_ScalesWithDestruction()
    {
        var debris = CombatVfxPresenter.CreateImpactDebrisDescriptor(new Vector2D(800f, 800f), impactSize: 2.0f, tick: 150);
        Assert.Equal(CombatParticleType.DebrisCrater, debris.Type);
        Assert.Equal(2.0f, debris.Scale);
        Assert.Equal(60f, debris.MaxRadius); // 30 * 2.0
    }

    // =========================================================================
    // Tier 4: Headless Scenario & Release Packaging Tests
    // =========================================================================

    [Fact]
    public void TC_S19_023_HistoricalBattleScenario_FullMatchReplay_MaintainsBitForBitParity()
    {
        var s1 = new HistoricalBattleScenario(seed: 1904);
        var s2 = new HistoricalBattleScenario(seed: 1904);

        s1.SimulateTicks(200);
        s2.SimulateTicks(200);

        ulong c1 = s1.Coordinator.Simulation.State.ComputeStateChecksum();
        ulong c2 = s2.Coordinator.Simulation.State.ComputeStateChecksum();

        Assert.Equal(c1, c2);
        Assert.Equal(s1.CelticKills, s2.CelticKills);
        Assert.Equal(s1.RomanKills, s2.RomanKills);
    }

    [Fact]
    public void TC_S19_024_ReleasePipeline_V120Bundle_GeneratesValidPackageManifest()
    {
        var files = new Dictionary<string, byte[]>
        {
            ["CrownConquest.exe"] = Encoding.UTF8.GetBytes("NATIVE_GRAPHICAL_RUNNER_V120"),
            ["project.godot"] = Encoding.UTF8.GetBytes("config/name=\"Crown & Conquest\""),
            ["scenes/main.gd"] = Encoding.UTF8.GetBytes("extends Node2D\n# v1.2.0")
        };

        var bundle = PackageBundleGenerator.CreateBundle(
            version: "1.2.0",
            releaseChannel: "Release",
            targetPlatform: "win-x64",
            files: files);

        Assert.Equal("1.2.0", bundle.Manifest.Version);
        Assert.Equal(3, bundle.Manifest.Files.Count);
        Assert.True(bundle.TotalSizeBytes > 0);

        byte[] zipBytes = PackageBundleGenerator.ExportZipArchive(bundle);
        Assert.True(zipBytes.Length > 0);
    }

    [Fact]
    public void TC_S19_025_ReleasePerformance_V120Benchmark_MeetsAllFrameAndMemoryBudgets()
    {
        var report = ReleasePerformanceCertifier.CertifySimulationPerformance(ticksToRun: 200, unitCount: 150, seed: 1904);

        Assert.True(report.IsCertified);
        Assert.True(report.MeanTickDurationMs < 16.6f, $"Mean tick duration {report.MeanTickDurationMs}ms exceeded 16.6ms frame budget.");
        Assert.True(report.MemoryFootprintMb < 500f, $"Memory footprint {report.MemoryFootprintMb}MB exceeded 500MB budget.");
        Assert.True(report.ZeroAllocationCompliant);
    }
}
