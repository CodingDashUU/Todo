namespace Washu.Framework.UnitTests;

using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Washu.Framework.Notifications;
using ZiggyCreatures.Caching.Fusion;

public class MessageStoreTests
{
    [Fact]
    public void StoreAndTake_RetrievesMessagesAndEvictsFromStore()
    {
        using var cache = new FusionCache(new FusionCacheOptions());
        var store = new MessageStore(cache);

        var msg1 = Message.Success("Success", "Operation completed");
        var msg2 = Message.Error("Error", "Something failed");

        var id = store.Store(msg1, msg2);

        id.Should().NotBeNullOrWhiteSpace("Store should generate a non-empty key");
        Guid.TryParse(id, out _).Should().BeTrue("The generated message ID should be a valid GUID string");

        var retrieved = store.Take(id);
        retrieved.Should().HaveCount(2, "Take should return all stored messages");
        retrieved[0].Should().Be(msg1);
        retrieved[1].Should().Be(msg2);

        // Taking a second time should return empty array because the message is one-time use (evicted)
        var secondTake = store.Take(id);
        secondTake.Should().BeEmpty("Messages should be removed from the store after being taken");
    }

    [Fact]
    public void Take_WithUnknownId_ReturnsEmptyArray()
    {
        using var cache = new FusionCache(new FusionCacheOptions());
        var store = new MessageStore(cache);

        var retrieved = store.Take(Guid.NewGuid().ToString());

        retrieved.Should().NotBeNull("Take should never return null");
        retrieved.Should().BeEmpty("Take with non-existent ID should return an empty array");
    }
}
