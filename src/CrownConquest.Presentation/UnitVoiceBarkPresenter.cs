using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Entities;
using CrownConquest.Domain.Events;

namespace CrownConquest.Presentation;

/// <summary>
/// Types of context speech / voice bark commands.
/// </summary>
public enum VoiceBarkType
{
    Select,
    Move,
    Attack,
    HeroAbility,
    UnderAttack,
    Death
}

/// <summary>
/// Describes a unit voice line to be vocalized or displayed.
/// </summary>
public readonly record struct VoiceBarkDescriptor(
    EntityId UnitId,
    FactionId Faction,
    VoiceBarkType BarkType,
    string LineText,
    string AudioCueKey,
    Vector2D Position,
    float Priority,
    ulong TriggerTick);

/// <summary>
/// Context speech system managing authentic Celtic and Roman unit voice lines,
/// anti-overlap cooldown intervals, and hero priority ducking.
/// </summary>
public sealed class UnitVoiceBarkPresenter
{
    private readonly VoiceBarkDescriptor[] _buffer;
    private int _writeIndex;
    private int _count;
    private readonly int _capacity;
    private ulong _lastGlobalBarkTick;
    private ulong _lastHeroBarkTick;
    private readonly int _globalCooldownTicks;
    private readonly int _unitCooldownTicks;
    private readonly (EntityId UnitId, ulong Tick)[] _recentUnitBarks;
    private int _recentUnitBarkCount;

    // Faction Speech Banks
    private static readonly string[] CelticSelectLines =
    {
        "Chieftain?",
        "Ready for battle!",
        "Orders, commander?",
        "By the sacred oak!",
        "The tribe stands ready."
    };

    private static readonly string[] CelticMoveLines =
    {
        "Moving!",
        "Onward!",
        "March!",
        "Swift as the wind.",
        "To glory!"
    };

    private static readonly string[] CelticAttackLines =
    {
        "Charge!",
        "For the gods!",
        "No mercy!",
        "Strike them down!",
        "Blood and victory!"
    };

    private static readonly string[] RomanSelectLines =
    {
        "Ave, Centurion!",
        "Legion ready!",
        "Orders, Tribune?",
        "Rome commands!",
        "At your word."
    };

    private static readonly string[] RomanMoveLines =
    {
        "Advancing!",
        "Form ranks and march!",
        "In step!",
        "For the Senate and Rome!",
        "Double time!"
    };

    private static readonly string[] RomanAttackLines =
    {
        "Attack!",
        "Hold the line and thrust!",
        "Crush the barbarians!",
        "Glory to Rome!",
        "Vae victis!"
    };

    public int PendingBarkCount => _count;

    public UnitVoiceBarkPresenter(int capacity = 64, int globalCooldownTicks = 8, int unitCooldownTicks = 20)
    {
        _capacity = capacity;
        _buffer = new VoiceBarkDescriptor[capacity];
        _writeIndex = 0;
        _count = 0;
        _lastGlobalBarkTick = 0;
        _lastHeroBarkTick = 0;
        _globalCooldownTicks = globalCooldownTicks;
        _unitCooldownTicks = unitCooldownTicks;
        _recentUnitBarks = new (EntityId, ulong)[32];
        _recentUnitBarkCount = 0;
    }

    /// <summary>
    /// Attempts to trigger a voice bark for a unit, respecting anti-overlap cooldowns and priority.
    /// </summary>
    public bool TryTriggerVoiceBark(
        EntityId unitId,
        FactionId faction,
        UnitArchetype unitType,
        VoiceBarkType barkType,
        Vector2D position,
        ulong currentTick,
        string? heroAbilityName = null)
    {
        bool isHero = unitType == UnitArchetype.Hero;
        float priority = isHero ? 2.0f : 1.0f;

        // Check hero priority override vs regular global cooldown
        if (!isHero)
        {
            if (currentTick < _lastGlobalBarkTick + (ulong)_globalCooldownTicks)
            {
                return false; // Global anti-chatter suppression
            }

            // Check per-unit cooldown
            for (int i = 0; i < _recentUnitBarkCount; i++)
            {
                if (_recentUnitBarks[i].UnitId == unitId)
                {
                    if (currentTick < _recentUnitBarks[i].Tick + (ulong)_unitCooldownTicks)
                    {
                        return false;
                    }
                    _recentUnitBarks[i] = (unitId, currentTick);
                    break;
                }
            }
        }

        // Generate line text and audio cue
        string lineText = ResolveVoiceLine(faction, unitType, barkType, heroAbilityName, currentTick);
        string audioCue = ResolveAudioCue(faction, unitType, barkType, heroAbilityName);

        var descriptor = new VoiceBarkDescriptor(
            UnitId: unitId,
            Faction: faction,
            BarkType: barkType,
            LineText: lineText,
            AudioCueKey: audioCue,
            Position: position,
            Priority: priority,
            TriggerTick: currentTick);

        Push(descriptor);

        _lastGlobalBarkTick = currentTick;
        if (isHero)
        {
            _lastHeroBarkTick = currentTick;
        }

        RecordRecentUnitBark(unitId, currentTick);
        return true;
    }

    public VoiceBarkDescriptor GetPendingBark(int index)
    {
        if (index < 0 || index >= _count)
            throw new ArgumentOutOfRangeException(nameof(index));
        int readIndex = (_writeIndex - _count + index + _capacity) % _capacity;
        return _buffer[readIndex];
    }

    public void ConsumeAll() => _count = 0;

    public static string ResolveVoiceLine(
        FactionId faction,
        UnitArchetype unitType,
        VoiceBarkType barkType,
        string? heroAbilityName,
        ulong tick)
    {
        if (unitType == UnitArchetype.Hero)
        {
            if (!string.IsNullOrEmpty(heroAbilityName))
            {
                if (heroAbilityName.Contains("War Cry", StringComparison.OrdinalIgnoreCase))
                    return "Feel our wrath!";
                if (heroAbilityName.Contains("Heroic Strike", StringComparison.OrdinalIgnoreCase) ||
                    heroAbilityName.Contains("Strike", StringComparison.OrdinalIgnoreCase))
                    return "Feel my blade!";
                return $"For glory and honor! ({heroAbilityName})";
            }

            return barkType switch
            {
                VoiceBarkType.Select => "I lead the charge!",
                VoiceBarkType.Move => "Follow my banner!",
                VoiceBarkType.Attack => "Taste Celtic iron!",
                _ => "Victory is ours!"
            };
        }

        bool isRoman = faction == FactionId.Player2;
        int seed = (int)(tick ^ (ulong)unitType);

        return barkType switch
        {
            VoiceBarkType.Select => isRoman
                ? RomanSelectLines[Math.Abs(seed) % RomanSelectLines.Length]
                : CelticSelectLines[Math.Abs(seed) % CelticSelectLines.Length],
            VoiceBarkType.Move => isRoman
                ? RomanMoveLines[Math.Abs(seed) % RomanMoveLines.Length]
                : CelticMoveLines[Math.Abs(seed) % CelticMoveLines.Length],
            VoiceBarkType.Attack => isRoman
                ? RomanAttackLines[Math.Abs(seed) % RomanAttackLines.Length]
                : CelticAttackLines[Math.Abs(seed) % CelticAttackLines.Length],
            VoiceBarkType.UnderAttack => isRoman
                ? "The cohort is under assault!"
                : "The village is under attack!",
            VoiceBarkType.Death => "Aaargh!",
            _ => "Ready!"
        };
    }

    public static string ResolveAudioCue(
        FactionId faction,
        UnitArchetype unitType,
        VoiceBarkType barkType,
        string? heroAbilityName)
    {
        string factionPrefix = faction == FactionId.Player2 ? "vox_roman" : "vox_celtic";
        string typePrefix = unitType == UnitArchetype.Hero ? "hero" : "infantry";

        if (unitType == UnitArchetype.Hero && !string.IsNullOrEmpty(heroAbilityName))
        {
            if (heroAbilityName.Contains("War Cry", StringComparison.OrdinalIgnoreCase))
                return "vox_hero_warcry";
            if (heroAbilityName.Contains("Strike", StringComparison.OrdinalIgnoreCase))
                return "vox_hero_strike";
            return "vox_hero_ability";
        }

        return $"{factionPrefix}_{typePrefix}_{barkType.ToString().ToLowerInvariant()}";
    }

    private void Push(VoiceBarkDescriptor descriptor)
    {
        _buffer[_writeIndex] = descriptor;
        _writeIndex = (_writeIndex + 1) % _capacity;
        if (_count < _capacity) _count++;
    }

    private void RecordRecentUnitBark(EntityId unitId, ulong tick)
    {
        for (int i = 0; i < _recentUnitBarkCount; i++)
        {
            if (_recentUnitBarks[i].UnitId == unitId)
            {
                _recentUnitBarks[i] = (unitId, tick);
                return;
            }
        }

        if (_recentUnitBarkCount < _recentUnitBarks.Length)
        {
            _recentUnitBarks[_recentUnitBarkCount++] = (unitId, tick);
        }
        else
        {
            _recentUnitBarks[0] = (unitId, tick);
        }
    }
}
