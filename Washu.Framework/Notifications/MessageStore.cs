namespace Washu.Framework.Notifications;

using Microsoft.Extensions.Caching.Memory;
using ZiggyCreatures.Caching.Fusion;

// Created this to store a notification specifically for a user that lasts between page navigations
public sealed class MessageStore(IFusionCache cache)
{
    public string Store(params Message[] messages)
    {
        var id = $"{Guid.CreateVersion7()}";

        cache.Set($"message:{id}", messages, TimeSpan.FromMinutes(1));

        return id;
    }

    public Message[] Take(string id)
    {
        var messages = cache.TryGet<Message[]>($"message:{id}");
        if (!messages.HasValue)
            return [];

        cache.Remove($"message:{id}");

        return messages.Value ?? [];
    }
}