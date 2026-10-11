namespace Washu.Framework.UnitTests;

using FluentAssertions;
using Washu.Framework.AspNetCore;

public class CircuitRateLimiterTests
{
    [Fact]
    public void CheckAndUpdateTokens_ThrottlesWhenMaxTokensExceeded_AndReplenishesOnTime()
    {
        // Arrange
        var connId = $"Connection-{Guid.CreateVersion7():N}";
        var startTime = DateTimeOffset.UtcNow;
        
        for (var i = 0; i < 50; i++)
        {
            var allowed = CircuitRateLimitingFilter.CheckAndUpdateTokens(connId, startTime);
            allowed.Should().BeTrue($"Token {i + 1} should be granted");
        }
        
        var overflowAllowed = CircuitRateLimitingFilter.CheckAndUpdateTokens(connId, startTime);
        overflowAllowed.Should().BeFalse("The token limit has been reached");
        
        var oneSecondLater = startTime.AddSeconds(1);
        var replenishedAllowed = CircuitRateLimitingFilter.CheckAndUpdateTokens(connId, oneSecondLater);
        
        replenishedAllowed.Should().BeTrue("The tokens should replenish after 1 second");
    }

    [Fact]
    public void CheckAndUpdateTokens_IsolatesLimitsBetweenDifferentConnections()
    {
        var conn1 = $"Connection-{Guid.CreateVersion7():N}";
        var conn2 = $"Connection-{Guid.CreateVersion7():N}";
        var now = DateTimeOffset.UtcNow;

        // Exhaust tokens for connection 1
        for (var i = 0; i < 50; i++) CircuitRateLimitingFilter.CheckAndUpdateTokens(conn1, now);
        CircuitRateLimitingFilter.CheckAndUpdateTokens(conn1, now).Should().BeFalse("Connection 1 has exhausted all tokens");

        // Connection 2 should have its own full bucket of 50 tokens unaffected
        for (var i = 0; i < 50; i++)
        {
            var allowed = CircuitRateLimitingFilter.CheckAndUpdateTokens(conn2, now);
            allowed.Should().BeTrue($"Connection 2 token {i + 1} should be granted independently");
        }
    }

    [Fact]
    public void CheckAndUpdateTokens_CapsTokensAtMaxLimit()
    {
        var connId = $"Connection-{Guid.CreateVersion7():N}";
        var startTime = DateTimeOffset.UtcNow;

        // Start by granting 1 token
        CircuitRateLimitingFilter.CheckAndUpdateTokens(connId, startTime).Should().BeTrue();

        // Advance 10 seconds (at 25 tokens/sec, 250 tokens would accumulate if uncapped)
        var tenSecondsLater = startTime.AddSeconds(10);

        // MaxTokens is 50, so connection should be able to consume at most 50 tokens
        for (var i = 0; i < 50; i++)
        {
            var allowed = CircuitRateLimitingFilter.CheckAndUpdateTokens(connId, tenSecondsLater);
            allowed.Should().BeTrue($"Token {i + 1} should be within the capped MaxTokens limit");
        }

        var overflow = CircuitRateLimitingFilter.CheckAndUpdateTokens(connId, tenSecondsLater);
        overflow.Should().BeFalse("Tokens must be capped at MaxTokens (50) and not accumulate unboundedly");
    }

    [Fact]
    public void CheckAndUpdateTokens_ReplenishesTokensProportionallyToElapsedSeconds()
    {
        var connId = $"Connection-{Guid.CreateVersion7():N}";
        var startTime = DateTimeOffset.UtcNow;

        // Exhaust all 50 tokens
        for (var i = 0; i < 50; i++) CircuitRateLimitingFilter.CheckAndUpdateTokens(connId, startTime);
        CircuitRateLimitingFilter.CheckAndUpdateTokens(connId, startTime).Should().BeFalse("Bucket is empty");

        // At 25 tokens/sec, 0.2 seconds should replenish 5 tokens
        var pointTwoSecondsLater = startTime.AddSeconds(0.2);

        for (var i = 0; i < 5; i++)
        {
            var allowed = CircuitRateLimitingFilter.CheckAndUpdateTokens(connId, pointTwoSecondsLater);
            allowed.Should().BeTrue($"Refilled token {i + 1} should be available after 0.2 seconds");
        }

        var sixthAttempt = CircuitRateLimitingFilter.CheckAndUpdateTokens(connId, pointTwoSecondsLater);
        sixthAttempt.Should().BeFalse("Only 5 tokens should have replenished after 0.2 seconds");
    }
}