using System.Collections.Generic;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Economy;

namespace CrownConquest.Domain.World;

/// <summary>
/// Domain model for an authoritative province/region on the strategic world map.
/// </summary>
public sealed class StrategicProvince
{
    public ProvinceId Id { get; }
    public string Name { get; }
    public Vector2D Position { get; }
    public List<ProvinceId> ConnectedProvinceIds { get; } = new();
    public TerrainType Terrain { get; set; } = TerrainType.Plains;
    public StrategicNodeType NodeType { get; set; } = StrategicNodeType.Settlement;
    public FactionId OwnerFaction { get; set; } = FactionId.Neutral;
    public ResourceCost ResourceYields { get; set; } = new(Food: 10, Wood: 10, Gold: 10, Stone: 10, Iron: 5);
    public float GarrisonDefenseBonus { get; set; } = 1.0f;

    public List<StrategicArmyId> StationedArmyIds { get; } = new();
    public List<StrategicUnitSpec> GarrisonUnits { get; } = new();

    public StrategicProvince(
        ProvinceId id,
        string name,
        Vector2D position,
        IEnumerable<ProvinceId>? connectedProvinceIds = null,
        TerrainType terrain = TerrainType.Plains,
        StrategicNodeType nodeType = StrategicNodeType.Settlement,
        FactionId ownerFaction = default,
        ResourceCost? resourceYields = null,
        float garrisonDefenseBonus = 1.0f)
    {
        Id = id;
        Name = name;
        Position = position;
        Terrain = terrain;
        NodeType = nodeType;
        OwnerFaction = ownerFaction == default ? FactionId.Neutral : ownerFaction;
        ResourceYields = resourceYields ?? new ResourceCost(Food: 10, Wood: 10, Gold: 10, Stone: 10, Iron: 5);
        GarrisonDefenseBonus = garrisonDefenseBonus > 0f ? garrisonDefenseBonus : 1.0f;

        if (connectedProvinceIds != null)
        {
            ConnectedProvinceIds.AddRange(connectedProvinceIds);
        }
    }

    public bool IsConnectedTo(ProvinceId otherId)
    {
        for (int i = 0; i < ConnectedProvinceIds.Count; i++)
        {
            if (ConnectedProvinceIds[i] == otherId)
                return true;
        }
        return false;
    }
}
