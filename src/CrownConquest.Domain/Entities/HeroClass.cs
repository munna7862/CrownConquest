using System;

namespace CrownConquest.Domain.Entities;

public enum HeroClass
{
    Warlord,
    Druid,
    Centurion,
    Ranger
}

public static class HeroClassExtensions
{
    public static string GetDisplayName(this HeroClass heroClass) => heroClass switch
    {
        HeroClass.Warlord => "Warlord",
        HeroClass.Druid => "Druid",
        HeroClass.Centurion => "Centurion",
        HeroClass.Ranger => "Ranger",
        _ => heroClass.ToString()
    };
}
