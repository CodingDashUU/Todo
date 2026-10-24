namespace Washu.Todo.UnitTests;

using FluentAssertions;
using Washu.Framework.Identity;

public class TodoListTests
{
    [Fact]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        var userId = Guid.CreateVersion7();
        const string name = "Shopping List";

        var list = new TodoList(userId, name);

        list.Id.Should().NotBeEmpty("A valid version 7 GUID should be generated for Id");
        list.UserId.Should().Be(userId);
        list.Name.Should().Be(name);
        list.Tasks.Should().NotBeNull();
        list.Tasks.Should().BeEmpty();
        list.CreationDate.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        list.LastModified.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        list.VersionId.Should().NotBeEmpty("A valid version 7 GUID should be generated for VersionId");
    }

    [Fact]
    public void TodoList_ImplementsIOwnedByUser()
    {
        var list = new TodoList(Guid.CreateVersion7(), "Test List");

        list.Should().BeAssignableTo<IOwnedByUser>();
    }

    [Fact]
    public void ToString_ReturnsListName()
    {
        var list = new TodoList(Guid.CreateVersion7(), "Work Projects");

        list.ToString().Should().Be("Work Projects");
    }

    [Fact]
    public void ChangeVersionId_GeneratesNewVersionId()
    {
        var list = new TodoList(Guid.CreateVersion7(), "Work Projects");
        var originalVersion = list.VersionId;

        list.ChangeVersionId();

        list.VersionId.Should().NotBe(originalVersion, "ChangeVersionId must assign a new GUID");
        list.VersionId.Should().NotBeEmpty();
    }

    [Fact]
    public void TodoListNone_HasExpectedSentinelValues()
    {
        var none = TodoList.None;

        none.Id.Should().Be(Guid.Empty);
        none.Name.Should().Be("None");
        none.VersionId.Should().Be(Guid.Empty);
        none.Tasks.Should().BeEmpty();
    }
}
