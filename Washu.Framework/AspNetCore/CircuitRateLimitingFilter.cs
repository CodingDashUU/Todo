namespace Washu.Framework.AspNetCore;

using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

public sealed class CircuitRateLimitingFilter : IHubFilter
{
    private static readonly ConcurrentDictionary<string, ConnectionTokenState> Tracking = new();

    private const double MaxTokens = 50.0;
    private const double RefillRatePerSecond = 25.0;

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext, 
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var connectionId = invocationContext.Context.ConnectionId;
        var now = DateTimeOffset.UtcNow;
        var isAllowed = CheckAndUpdateTokens(connectionId, now);
        if (!isAllowed) invocationContext.Context.Abort();
        return await next(invocationContext);
    }

    public async Task OnDisconnectedAsync(
        HubLifetimeContext context, 
        Exception? exception, 
        Func<HubLifetimeContext, Exception?, Task> next)
    {
        Tracking.TryRemove(context.Context.ConnectionId, out _);
        await next(context, exception);
    }

    private static bool CheckAndUpdateTokens(string connectionId, DateTimeOffset now)
    {
        var state = Tracking.GetOrAdd(connectionId, _ => new ConnectionTokenState(MaxTokens, now));

        lock (state)
        {
            var elapsedSeconds = (now - state.LastRefill).TotalSeconds;
            state.Tokens = Math.Min(MaxTokens, state.Tokens + (elapsedSeconds * RefillRatePerSecond));
            state.LastRefill = now;
            if (!(state.Tokens >= 1.0)) return false;
            state.Tokens -= 1.0;
            return true;
        }
    }
    private sealed class ConnectionTokenState(double initialTokens, DateTimeOffset now)
    {
        public double Tokens { get; set; } = initialTokens;
        public DateTimeOffset LastRefill { get; set; } = now;
    }
}