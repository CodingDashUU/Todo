namespace Washu.Todo.UnitTests;

using FluentAssertions;

public class QueryableExtensionsTests
{
    [Fact]
    public void ByTodoList_FiltersListsById()
    {
        var targetListId = Guid.CreateVersion7();
        var otherListId = Guid.CreateVersion7();

        var list1 = new TodoList(Guid.CreateVersion7(), "List 1") { Id = targetListId };
        var list2 = new TodoList(Guid.CreateVersion7(), "List 2") { Id = otherListId };

        var query = new List<TodoList> { list1, list2 }.AsQueryable();

        var result = query.ByTodoList(targetListId).ToList();

        result.Should().ContainSingle();
        result[0].Id.Should().Be(targetListId);
    }

    [Fact]
    public void ForTodoList_FiltersTasksByTodoListId()
    {
        var targetListId = Guid.CreateVersion7();
        var otherListId = Guid.CreateVersion7();

        var task1 = new TaskEntry("Task 1", DateTimeOffset.UtcNow.AddDays(1), Guid.CreateVersion7(), targetListId);
        var task2 = new TaskEntry("Task 2", DateTimeOffset.UtcNow.AddDays(1), Guid.CreateVersion7(), otherListId);
        var task3 = new TaskEntry("Task 3", DateTimeOffset.UtcNow.AddDays(2), Guid.CreateVersion7(), targetListId);

        var query = new List<TaskEntry> { task1, task2, task3 }.AsQueryable();

        var result = query.ForTodoList(targetListId).ToList();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.TodoListId == targetListId);
    }

    [Fact]
    public void ByTask_FiltersTasksById()
    {
        var targetTaskId = Guid.CreateVersion7();
        var otherTaskId = Guid.CreateVersion7();
        var listId = Guid.CreateVersion7();

        var task1 = new TaskEntry("Task 1", DateTimeOffset.UtcNow.AddDays(1), targetTaskId, listId);
        var task2 = new TaskEntry("Task 2", DateTimeOffset.UtcNow.AddDays(1), otherTaskId, listId);

        var query = new List<TaskEntry> { task1, task2 }.AsQueryable();

        var result = query.ByTask(targetTaskId).ToList();

        result.Should().ContainSingle();
        result[0].Id.Should().Be(targetTaskId);
    }
}
