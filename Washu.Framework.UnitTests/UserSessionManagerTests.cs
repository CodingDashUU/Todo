namespace Washu.Framework.UnitTests;

using FluentAssertions;
using Washu.Framework.Identity;

public class UserSessionManagerTests
{
    [Fact]
    public async Task OnRefreshSessionAsync_InvokesRegisteredSessionCallbacks()
    {
        var manager = new UserSessionManager();
        var userId = Guid.CreateVersion7();
        var session1 = Guid.CreateVersion7();
        var session2 = Guid.CreateVersion7();

        var session1Invoked = false;
        var session2Invoked = false;

        manager.AddSession(userId, session1, () => { session1Invoked = true; return Task.CompletedTask; });
        manager.AddSession(userId, session2, () => { session2Invoked = true; return Task.CompletedTask; });

        await manager.OnRefreshSessionAsync(userId);

        session1Invoked.Should().BeTrue("Session 1 callback should be invoked");
        session2Invoked.Should().BeTrue("Session 2 callback should be invoked");
    }

    [Fact]
    public async Task OnRefreshSessionAsync_SkipsExcludedSessionId()
    {
        var manager = new UserSessionManager();
        var userId = Guid.CreateVersion7();
        var activeSession = Guid.CreateVersion7();
        var otherSession = Guid.CreateVersion7();

        var activeInvoked = false;
        var otherInvoked = false;

        manager.AddSession(userId, activeSession, () => { activeInvoked = true; return Task.CompletedTask; });
        manager.AddSession(userId, otherSession, () => { otherInvoked = true; return Task.CompletedTask; });

        // Refresh all sessions for user EXCEPT activeSession
        await manager.OnRefreshSessionAsync(userId, excludedSessionId: activeSession);

        activeInvoked.Should().BeFalse("The excluded session should NOT be refreshed");
        otherInvoked.Should().BeTrue("Other sessions for the user should be refreshed");
    }

    [Fact]
    public async Task RemoveSession_RemovesRegisteredCallback()
    {
        var manager = new UserSessionManager();
        var userId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();

        var invoked = false;
        manager.AddSession(userId, sessionId, () => { invoked = true; return Task.CompletedTask; });

        manager.RemoveSession(userId, sessionId);

        await manager.OnRefreshSessionAsync(userId);

        invoked.Should().BeFalse("Removed session callback must not be invoked on refresh");
    }

    [Fact]
    public async Task OnRefreshSessionAsync_WhenUserHasNoSessions_DoesNotThrow()
    {
        var manager = new UserSessionManager();
        var unknownUserId = Guid.CreateVersion7();

        var act = async () => await manager.OnRefreshSessionAsync(unknownUserId);

        await act.Should().NotThrowAsync("Refreshing sessions for unknown user should gracefully complete");
    }
}
