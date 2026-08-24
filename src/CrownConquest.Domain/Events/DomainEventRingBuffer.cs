using System;
using System.Collections.Generic;

namespace CrownConquest.Domain.Events;

/// <summary>
/// Lightweight event record stored in the telemetry ring buffer.
/// </summary>
public readonly record struct TelemetryEventRecord(
    ulong Tick,
    string EventTypeName,
    int PrimaryEntityId,
    int SecondaryEntityId,
    float Value1,
    float Value2);

/// <summary>
/// Fixed-capacity, zero-allocation circular ring buffer for high-frequency domain event telemetry.
/// Overwrites oldest events when capacity is exceeded without triggering GC allocations.
/// </summary>
public sealed class DomainEventRingBuffer
{
    private readonly TelemetryEventRecord[] _buffer;
    private readonly int _capacity;
    private int _head;
    private int _count;
    private ulong _totalPushed;

    public int Capacity => _capacity;
    public int Count => _count;
    public ulong TotalPushed => _totalPushed;

    public DomainEventRingBuffer(int capacity = 512)
    {
        _capacity = capacity > 0 ? capacity : 512;
        _buffer = new TelemetryEventRecord[_capacity];
        _head = 0;
        _count = 0;
        _totalPushed = 0;
    }

    public void Clear()
    {
        _head = 0;
        _count = 0;
        _totalPushed = 0;
    }

    public void Push(ulong tick, string eventTypeName, int primaryId = 0, int secondaryId = 0, float val1 = 0f, float val2 = 0f)
    {
        _buffer[_head] = new TelemetryEventRecord(tick, eventTypeName, primaryId, secondaryId, val1, val2);
        _head = (_head + 1) % _capacity;
        if (_count < _capacity)
        {
            _count++;
        }
        _totalPushed++;
    }

    public TelemetryEventRecord GetAt(int index)
    {
        if (index < 0 || index >= _count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        int start = (_head - _count + _capacity) % _capacity;
        int actualIndex = (start + index) % _capacity;
        return _buffer[actualIndex];
    }

    public void CopyTo(List<TelemetryEventRecord> destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        destination.Clear();
        int start = (_head - _count + _capacity) % _capacity;
        for (int i = 0; i < _count; i++)
        {
            destination.Add(_buffer[(start + i) % _capacity]);
        }
    }
}
