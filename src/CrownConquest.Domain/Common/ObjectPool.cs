using System;
using System.Collections.Generic;

namespace CrownConquest.Domain.Common;

/// <summary>
/// High-performance generic object pool to recycle instances and eliminate GC allocations in hot simulation loops.
/// </summary>
/// <typeparam name="T">Reference type to pool</typeparam>
public sealed class ObjectPool<T> where T : class
{
    private readonly Func<T> _factory;
    private readonly Action<T>? _resetAction;
    private readonly Stack<T> _pool;
    private readonly int _maxCapacity;

    private int _rentCount;
    private int _returnCount;
    private int _createdCount;

    public int AvailableCount => _pool.Count;
    public int MaxCapacity => _maxCapacity;
    public int RentCount => _rentCount;
    public int ReturnCount => _returnCount;
    public int CreatedCount => _createdCount;

    public ObjectPool(Func<T> factory, Action<T>? resetAction = null, int initialCapacity = 32, int maxCapacity = 512)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _resetAction = resetAction;
        _maxCapacity = maxCapacity > 0 ? maxCapacity : 512;
        _pool = new Stack<T>(initialCapacity);

        Warm(initialCapacity);
    }

    public void Warm(int count)
    {
        int toCreate = Math.Min(count, _maxCapacity - _pool.Count);
        for (int i = 0; i < toCreate; i++)
        {
            _pool.Push(_factory());
            _createdCount++;
        }
    }

    public T Rent()
    {
        _rentCount++;
        if (_pool.Count > 0)
        {
            return _pool.Pop();
        }

        _createdCount++;
        return _factory();
    }

    public void Return(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _returnCount++;

        _resetAction?.Invoke(item);

        if (_pool.Count < _maxCapacity)
        {
            _pool.Push(item);
        }
    }

    public void Clear()
    {
        _pool.Clear();
        _rentCount = 0;
        _returnCount = 0;
        _createdCount = 0;
    }
}
