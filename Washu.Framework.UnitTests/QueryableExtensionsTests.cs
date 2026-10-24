namespace Washu.Framework.UnitTests;

using FluentAssertions;
using Washu.Framework.Extensions;
using Washu.Framework.Identity;

public class QueryableExtensionsTests
{
    private sealed class FakeOwnedItem : IOwnedByUser
    {
        public Guid UserId { get; init; }
        public string Title { get; set; } = string.Empty;
    }

    private sealed class TestCurrentUser(Guid? userId) : ICurrentUser
    {
        public Guid? UserId => userId;
    }

    [Fact]
    public void ForCurrentUser_WithAuthenticatedUser_FiltersQueryByUserId()
    {
        var targetUserId = Guid.CreateVersion7();
        var otherUserId = Guid.CreateVersion7();

        var items = new List<FakeOwnedItem>
        {
            new() { UserId = targetUserId, Title = "Target Item 1" },
            new() { UserId = otherUserId, Title = "Other User Item" },
            new() { UserId = targetUserId, Title = "Target Item 2" }
        }.AsQueryable();

        var currentUser = new TestCurrentUser(targetUserId);

        var filtered = items.ForCurrentUser(currentUser).ToList();

        filtered.Should().HaveCount(2);
        filtered.Should().OnlyContain(x => x.UserId == targetUserId);
    }

    [Fact]
    public void ForCurrentUser_WhenUserIdIsNull_ThrowsInvalidOperationException()
    {
        var items = new List<FakeOwnedItem>
        {
            new() { UserId = Guid.CreateVersion7(), Title = "Item" }
        }.AsQueryable();

        var currentUser = new TestCurrentUser(null);

        var act = () => items.ForCurrentUser(currentUser);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("UserId was not found in the session data");
    }
}
