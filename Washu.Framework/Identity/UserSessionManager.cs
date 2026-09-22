namespace Washu.Framework.Identity;

using System.Collections.Concurrent;

public sealed class UserSessionManager
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Func<Task>>> _sessions = [];

    public void AddSession(Guid userId, Guid sessionId, Func<Task> callback)
    {
        var dict = _sessions.GetOrAdd(userId, _ => []);
        dict[sessionId] = callback;
    }

    public void RemoveSession(Guid userId, Guid sessionId)
    {
        if(!_sessions.TryGetValue(userId, out var sessions)) 
            return;
        sessions.TryRemove(sessionId, out _);
        if (sessions.IsEmpty) _sessions.TryRemove(userId, out _);
    }
    public void Invalidate(Guid userId)
    {
        if (!_sessions.Remove(userId, out var sessions))
            return;

        foreach (var callback in sessions)
            callback.Value();
    }
}