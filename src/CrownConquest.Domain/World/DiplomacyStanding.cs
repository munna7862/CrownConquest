namespace CrownConquest.Domain.World;

/// <summary>
/// Categorical standing of diplomatic relations between factions.
/// </summary>
public enum DiplomacyStanding
{
    AtWar,      // Rep <= -60
    Hostile,    // Rep -59 .. -20
    Neutral,    // Rep -19 .. +19
    Friendly,   // Rep +20 .. +59
    Allied      // Rep >= +60
}
