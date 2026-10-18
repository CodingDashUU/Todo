namespace Washu.Todo.UnitTests;

using FluentAssertions;

public class TaskEntryTests
{
    [Fact]
    public void Constructor_InitializesPropertiesCorrectly()
    {
        var id = Guid.CreateVersion7();
        var todoListId = Guid.CreateVersion7();
        var goalDate = DateTimeOffset.UtcNow.AddDays(2);
        const string name = "Buy Groceries";

        var task = new TaskEntry(name, goalDate, id, todoListId);

        task.Id.Should().Be(id);
        task.TodoListId.Should().Be(todoListId);
        task.Name.Should().Be(name);
        task.GoalDate.Should().Be(goalDate.ToUniversalTime());
        task.IsCompleted.Should().BeFalse();
        task.DateCompleted.Should().BeNull();
        task.DateCreated.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Toggle_WhenIncomplete_MarksCompletedAndSetsDateCompleted()
    {
        var task = new TaskEntry("Task 1", DateTimeOffset.UtcNow.AddDays(1), Guid.CreateVersion7(), Guid.CreateVersion7());

        task.Toggle();

        task.IsCompleted.Should().BeTrue();
        task.DateCompleted.Should().NotBeNull();
        task.DateCompleted!.Value.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Toggle_WhenCompleted_MarksIncompleteAndClearsDateCompleted()
    {
        var task = new TaskEntry("Task 1", DateTimeOffset.UtcNow.AddDays(1), Guid.CreateVersion7(), Guid.CreateVersion7());
        task.Toggle();
        task.IsCompleted.Should().BeTrue();

        task.Toggle();

        task.IsCompleted.Should().BeFalse();
        task.DateCompleted.Should().BeNull();
    }

    [Fact]
    public void ChangeGoalDate_WithDifferentDate_UpdatesGoalDateAndResetsCompletion()
    {
        var initialGoalDate = DateTimeOffset.UtcNow.AddDays(1);
        var task = new TaskEntry("Task 1", initialGoalDate, Guid.CreateVersion7(), Guid.CreateVersion7());
        task.Toggle();
        task.IsCompleted.Should().BeTrue();
        task.DateCompleted.Should().NotBeNull();

        var newGoalDate = DateTimeOffset.UtcNow.AddDays(5);
        task.ChangeGoalDate(newGoalDate);

        task.GoalDate.Should().Be(newGoalDate.ToUniversalTime());
        task.IsCompleted.Should().BeFalse();
        task.DateCompleted.Should().BeNull();
    }

    [Fact]
    public void ChangeGoalDate_WithSameDate_DoesNotResetTask()
    {
        var goalDate = DateTimeOffset.UtcNow.AddDays(1);
        var task = new TaskEntry("Task 1", goalDate, Guid.CreateVersion7(), Guid.CreateVersion7());
        task.Toggle();
        var completedDate = task.DateCompleted;

        task.ChangeGoalDate(goalDate);

        task.GoalDate.Should().Be(goalDate.ToUniversalTime());
        task.IsCompleted.Should().BeTrue();
        task.DateCompleted.Should().Be(completedDate);
    }
}
