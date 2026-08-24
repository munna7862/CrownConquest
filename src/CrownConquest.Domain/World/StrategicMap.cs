using System;
using System.Collections.Generic;
using CrownConquest.Domain.Combat;
using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.World;

/// <summary>
/// Authoritative graph data structure representing the connected provinces of the strategic world map.
/// Provides deterministic shortest-path queries and province spatial lookups.
/// </summary>
public sealed class StrategicMap
{
    private readonly Dictionary<ProvinceId, StrategicProvince> _provinces = new();
    private readonly List<ProvinceId> _provinceOrder = new();

    public IReadOnlyList<ProvinceId> ProvinceIds => _provinceOrder;
    public int ProvinceCount => _provinces.Count;

    public StrategicMap(IEnumerable<StrategicProvince>? provinces = null)
    {
        if (provinces != null)
        {
            foreach (var p in provinces)
            {
                AddProvince(p);
            }
        }
    }

    public void AddProvince(StrategicProvince province)
    {
        if (!_provinces.ContainsKey(province.Id))
        {
            _provinces[province.Id] = province;
            _provinceOrder.Add(province.Id);
        }
        else
        {
            _provinces[province.Id] = province;
        }
    }

    public bool TryGetProvince(ProvinceId id, out StrategicProvince? province)
    {
        return _provinces.TryGetValue(id, out province);
    }

    public StrategicProvince? GetProvince(ProvinceId id)
    {
        _provinces.TryGetValue(id, out var p);
        return p;
    }

    public IEnumerable<StrategicProvince> GetAllProvinces()
    {
        for (int i = 0; i < _provinceOrder.Count; i++)
        {
            yield return _provinces[_provinceOrder[i]];
        }
    }

    public void AddBidirectionalConnection(ProvinceId a, ProvinceId b)
    {
        if (_provinces.TryGetValue(a, out var provA) && !provA.ConnectedProvinceIds.Contains(b))
        {
            provA.ConnectedProvinceIds.Add(b);
        }
        if (_provinces.TryGetValue(b, out var provB) && !provB.ConnectedProvinceIds.Contains(a))
        {
            provB.ConnectedProvinceIds.Add(a);
        }
    }

    /// <summary>
    /// Finds the shortest province path (sequence of ProvinceIds) between start and goal using BFS.
    /// Returns empty list if unreachable or start equals goal.
    /// </summary>
    public List<ProvinceId> FindPath(ProvinceId startId, ProvinceId goalId)
    {
        var path = new List<ProvinceId>();
        if (startId == goalId || !_provinces.ContainsKey(startId) || !_provinces.ContainsKey(goalId))
        {
            return path;
        }

        var visited = new HashSet<ProvinceId>();
        var parentMap = new Dictionary<ProvinceId, ProvinceId>();
        var queue = new Queue<ProvinceId>();

        queue.Enqueue(startId);
        visited.Add(startId);

        bool found = false;
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == goalId)
            {
                found = true;
                break;
            }

            if (_provinces.TryGetValue(current, out var province))
            {
                for (int i = 0; i < province.ConnectedProvinceIds.Count; i++)
                {
                    var neighbor = province.ConnectedProvinceIds[i];
                    if (!visited.Contains(neighbor) && _provinces.ContainsKey(neighbor))
                    {
                        visited.Add(neighbor);
                        parentMap[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        if (found)
        {
            var curr = goalId;
            while (curr != startId)
            {
                path.Add(curr);
                curr = parentMap[curr];
            }
            path.Reverse();
        }

        return path;
    }
}
