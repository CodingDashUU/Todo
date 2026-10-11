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
}