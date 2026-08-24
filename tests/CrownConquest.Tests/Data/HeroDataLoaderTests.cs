using System;
using System.IO;
using CrownConquest.Data;
using CrownConquest.Data.Loaders;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using Xunit;

namespace CrownConquest.Tests.Data;

public class HeroDataLoaderTests
{
    [Fact]
    public void DataLoader_LoadsHeroesAndAbilities_Successfully()
    {
        // TC-S05-018: JSON Loaders for Heroes and Abilities
        string baseDir = AppContext.BaseDirectory;
        string searchDir = baseDir;
        while (!Directory.Exists(Path.Combine(searchDir, "data", "definitions")) && Directory.GetParent(searchDir) != null)
        {
            searchDir = Directory.GetParent(searchDir)!.FullName;
        }


        string heroesPath = Path.Combine(searchDir, "data", "definitions", "heroes.json");
        string abilitiesPath = Path.Combine(searchDir, "data", "definitions", "abilities.json");
        string unitsPath = Path.Combine(searchDir, "data", "definitions", "units.json");
        string curvesPath = Path.Combine(searchDir, "data", "definitions", "xp_curves.json");

        var heroRes = DataLoader.LoadHeroesFromFile(heroesPath);
        Assert.True(heroRes.IsSuccess, heroRes.Error.Message);
        Assert.NotNull(heroRes.Value);
        Assert.True(heroRes.Value.Count >= 3);

        var abilityRes = DataLoader.LoadAbilitiesFromFile(abilitiesPath);
        Assert.True(abilityRes.IsSuccess, abilityRes.Error.Message);
        Assert.NotNull(abilityRes.Value);
        Assert.True(abilityRes.Value.Count >= 6);



        var unitsRes = DataLoader.LoadUnitsFromFile(unitsPath);
        Assert.True(unitsRes.IsSuccess);

        var curvesRes = DataLoader.LoadProgressionCurvesFromFile(curvesPath);
        Assert.True(curvesRes.IsSuccess);

        // Factory creation verification
        var factory = new UnitFactory(
            unitDefs: unitsRes.Value,
            curveDefs: curvesRes.Value,
            heroDefs: heroRes.Value,
            abilityDefs: abilityRes.Value);

        var heroUnitRes = factory.CreateHeroUnit(
            new EntityId(101),
            new FactionId(1),
            "celtic_warlord",
            new Vector2D(10f, 10f));

        Assert.True(heroUnitRes.IsSuccess);
        var hero = heroUnitRes.Value!;
        Assert.True(hero.IsHero);
        Assert.NotNull(hero.HeroState);
        Assert.Equal("Brennus", hero.HeroState.HeroName);
        Assert.Equal(HeroClass.Warlord, hero.HeroState.Class);
        Assert.True(hero.HeroState.Abilities.Count >= 2);
        Assert.NotNull(hero.HeroState.ActiveAura);
    }
}
