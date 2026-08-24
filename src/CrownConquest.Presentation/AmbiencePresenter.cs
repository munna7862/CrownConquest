using System;
using CrownConquest.Domain.Combat;

namespace CrownConquest.Presentation;

// ─────────────────────────────────────────────────
// Ambience Zone System — Terrain-based ambient audio
// ─────────────────────────────────────────────────

/// <summary>
/// Ambient audio zone types mapped to terrain biomes.
/// </summary>
public enum AmbienceZoneType
{
    Plains,
    Forest,
    Desert,
    Mountain,
    Water,
    Settlement,
    Battlefield
}

/// <summary>
/// Describes the current ambient audio zone for environmental sound.
/// </summary>
public readonly record struct AmbienceZoneDescriptor(
    AmbienceZoneType ZoneType,
    string TrackId,
    float Volume,
    float CrossfadeDuration,
    bool IsTransitioning);

/// <summary>
/// Presenter that resolves ambient audio zones from terrain and game state.
/// </summary>
public sealed class AmbiencePresenter
{
    private AmbienceZoneType _currentZone;
    private AmbienceZoneType _previousZone;
    private bool _isTransitioning;
    private float _transitionProgress;
    private readonly float _crossfadeDuration;

    public AmbienceZoneType CurrentZone => _currentZone;
    public AmbienceZoneType PreviousZone => _previousZone;
    public bool IsTransitioning => _isTransitioning;
    public float TransitionProgress => _transitionProgress;

    public AmbiencePresenter(float crossfadeDuration = 2.0f)
    {
        _currentZone = AmbienceZoneType.Plains;
        _previousZone = AmbienceZoneType.Plains;
        _isTransitioning = false;
        _transitionProgress = 1.0f;
        _crossfadeDuration = crossfadeDuration;
    }

    /// <summary>
    /// Maps a terrain type to an ambience zone.
    /// </summary>
    public static AmbienceZoneType MapTerrainToZone(TerrainType terrain) => terrain switch
    {
        TerrainType.Forest => AmbienceZoneType.Forest,
        TerrainType.Hills => AmbienceZoneType.Mountain,
        TerrainType.Water => AmbienceZoneType.Water,
        TerrainType.Marsh => AmbienceZoneType.Water,
        _ => AmbienceZoneType.Plains
    };

    /// <summary>
    /// Updates the ambience zone based on the dominant terrain under the camera.
    /// </summary>
    public AmbienceZoneDescriptor UpdateZone(TerrainType dominantTerrain, bool isCombatActive)
    {
        var newZone = isCombatActive ? AmbienceZoneType.Battlefield : MapTerrainToZone(dominantTerrain);

        if (newZone != _currentZone)
        {
            _previousZone = _currentZone;
            _currentZone = newZone;
            _isTransitioning = true;
            _transitionProgress = 0f;
        }
        else if (_isTransitioning)
        {
            _transitionProgress = Math.Min(1.0f, _transitionProgress + (1.0f / (_crossfadeDuration * 60f)));
            if (_transitionProgress >= 1.0f)
            {
                _isTransitioning = false;
            }
        }

        return GetDescriptor();
    }

    public AmbienceZoneDescriptor GetDescriptor()
    {
        return new AmbienceZoneDescriptor(
            ZoneType: _currentZone,
            TrackId: GetTrackId(_currentZone),
            Volume: 0.4f,
            CrossfadeDuration: _crossfadeDuration,
            IsTransitioning: _isTransitioning);
    }

    /// <summary>
    /// Gets the ambient track identifier for a zone type.
    /// </summary>
    public static string GetTrackId(AmbienceZoneType zoneType) => zoneType switch
    {
        AmbienceZoneType.Plains => "amb_plains_wind",
        AmbienceZoneType.Forest => "amb_forest_birds",
        AmbienceZoneType.Desert => "amb_desert_heat",
        AmbienceZoneType.Mountain => "amb_mountain_wind",
        AmbienceZoneType.Water => "amb_water_stream",
        AmbienceZoneType.Settlement => "amb_settlement_bustle",
        AmbienceZoneType.Battlefield => "amb_battle_chaos",
        _ => "amb_plains_wind"
    };
}
