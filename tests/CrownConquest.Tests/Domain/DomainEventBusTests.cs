using CrownConquest.Domain.Common;
using CrownConquest.Domain.Events;
using Xunit;

namespace CrownConquest.Tests.Domain;

public class DomainEventBusTests
{
    [Fact]
    public void DomainEventBus_PublishAndSubscribe_ShouldInvokeHandlers()
    {
        var bus = new DomainEventBus();
        int receivedCount = 0;
        EntityId receivedId = EntityId.None;

        void OnUnitSpawned(in UnitSpawnedEvent evt)
        {
            receivedCount++;
            receivedId = evt.UnitId;
        }

        bus.Subscribe<UnitSpawnedEvent>(OnUnitSpawned);

        var spawnEvent = new UnitSpawnedEvent(
            SimulationTick: 1,
            UnitId: new EntityId(10),
            FactionId: FactionId.Player1,
            UnitType: "celtic_swordsman",
            Position: new Vector2D(5f, 5f));

        bus.Publish(in spawnEvent);

        Assert.Equal(1, receivedCount);
        Assert.Equal(new EntityId(10), receivedId);
    }

    [Fact]
    public void DomainEventBus_Unsubscribe_ShouldStopDeliveringEvents()
    {
        var bus = new DomainEventBus();
        int receivedCount = 0;

        void OnUnitSpawned(in UnitSpawnedEvent evt)
        {
            receivedCount++;
        }

        bus.Subscribe<UnitSpawnedEvent>(OnUnitSpawned);
        Assert.Equal(1, bus.GetSubscriberCount<UnitSpawnedEvent>());

        var evt = new UnitSpawnedEvent(1, new EntityId(1), FactionId.Player1, "test", Vector2D.Zero);
        bus.Publish(in evt);
        Assert.Equal(1, receivedCount);

        bus.Unsubscribe<UnitSpawnedEvent>(OnUnitSpawned);
        Assert.Equal(0, bus.GetSubscriberCount<UnitSpawnedEvent>());

        bus.Publish(in evt);
        Assert.Equal(1, receivedCount); // Count should not increment
    }

    [Fact]
    public void DomainEventBus_MultipleSubscribers_ShouldAllReceiveEvents()
    {
        var bus = new DomainEventBus();
        int sub1 = 0;
        int sub2 = 0;

        bus.Subscribe<DamageDealtEvent>((in DamageDealtEvent e) => sub1++);
        bus.Subscribe<DamageDealtEvent>((in DamageDealtEvent e) => sub2++);

        var dmg = new DamageDealtEvent(1, new EntityId(1), new EntityId(2), 20f, 80f, false);
        bus.Publish(in dmg);

        Assert.Equal(1, sub1);
        Assert.Equal(1, sub2);
    }
}
