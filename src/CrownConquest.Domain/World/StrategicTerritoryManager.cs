using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.World;

/// <summary>
/// Domain manager tracking territorial ownership and province control ratios across the strategic map.
/// </summary>
public sealed class StrategicTerritoryManager
{
    private readonly StrategicMap _map;

    public StrategicTerritoryManager(StrategicMap map)
    {
        _map = map;
    }

    public int GetControlledProvinceCount(FactionId factionId)
    {
        int count = 0;
        foreach (var province in _map.GetAllProvinces())
        {
            if (province.OwnerFaction == factionId)
            {
                count++;
            }
        }
        return count;
    }

    public float GetControlPercentage(FactionId factionId)
    {
        int total = _map.ProvinceCount;
        if (total == 0) return 0f;
        return (float)GetControlledProvinceCount(factionId) / total;
    }

    public bool TransferOwnership(ProvinceId provinceId, FactionId newOwner)
    {
        if (_map.TryGetProvince(provinceId, out var province) && province != null)
        {
            province.OwnerFaction = newOwner;
            return true;
        }
        return false;
    }

    public Dictionary<FactionId, int> GetOwnershipDistribution()
    {
        var dict = new Dictionary<FactionId, int>();
        foreach (var province in _map.GetAllProvinces())
        {
            dict.TryGetValue(province.OwnerFaction, out int current);
            dict[province.OwnerFaction] = current + 1;
        }
        return dict;
    }
}
