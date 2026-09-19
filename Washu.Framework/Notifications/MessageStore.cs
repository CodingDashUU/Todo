namespace Washu.Framework.Notifications;

using Microsoft.Extensions.Caching.Memory;

// Created this to store a notification specifically for a user that lasts between page navigations
public sealed class MessageStore(IMemoryCache cache)
{
    public string Store(params Message[] messages)
    {
        var id = Guid.CreateVersion7().ToString();

        cache.Set(id, messages, TimeSpan.FromMinutes(1));

        return id;
    }

    public Message[] Take(string id)
    {
        if (!cache.TryGetValue(id, out Message[]? messages))
            return [];

        cache.Remove(id);

        return messages ?? [];
    }
}