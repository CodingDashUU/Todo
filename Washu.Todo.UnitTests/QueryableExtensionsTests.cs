using Washu.Todo.Lists.Features;
using Washu.Todo.Tasks;
using Washu.Todo.Extensions;
namespace Washu.Todo.UnitTests;

using FluentAssertions;

public class QueryableExtensionsTests
{
    [Fact]
    public void ByTodoList_FiltersListsById()
    {
        var list1 = new TodoList(Guid.CreateVersion7(), "List 1");
        var list2 = new TodoList(Guid.CreateVersion7(), "List 2");

        var query = new List<TodoList> { list1, list2 }.AsQueryable();

        var result = query.ByTodoList(list1.Id).ToList();

        result.Should().ContainSingle();
        result[0].Id.Should().Be(list1.Id);
    }

    [Fact]
    public void ForTodoList_FiltersTasksByTodoListId()
    {
        var targetListId = Guid.CreateVersion7();
        var otherListId = Guid.CreateVersion7();

        var task1 = new TaskEntry("Task 1", DateTimeOffset.UtcNow.AddDays(1), targetListId);
        var task2 = new TaskEntry("Task 2", DateTimeOffset.UtcNow.AddDays(1), otherListId);
        var task3 = new TaskEntry("Task 3", DateTimeOffset.UtcNow.AddDays(2), targetListId);

        var query = new List<TaskEntry> { task1, task2, task3 }.AsQueryable();

        var result = query.ForTodoList(targetListId).ToList();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.TodoListId == targetListId);
    }

    [Fact]
    public void ByTask_FiltersTasksById()
    {
        var listId = Guid.CreateVersion7();

        var task1 = new TaskEntry("Task 1", DateTimeOffset.UtcNow.AddDays(1), listId);
        var task2 = new TaskEntry("Task 2", DateTimeOffset.UtcNow.AddDays(1), listId);
        var targetTaskId = task1.Id;

        var query = new List<TaskEntry> { task1, task2 }.AsQueryable();

        var result = query.ByTask(targetTaskId).ToList();

        result.Should().ContainSingle();
        result[0].Id.Should().Be(targetTaskId);
    }

    [Fact]
    public async Task ApplyDataGridFiltersAsync_FiltersSortsAndPaginates()
    {
        var listId = Guid.CreateVersion7();
        var task1 = new TaskEntry("apple", DateTimeOffset.UtcNow.AddDays(1), listId);
        var task2 = new TaskEntry("Banana", DateTimeOffset.UtcNow.AddDays(2), listId);
        var task3 = new TaskEntry("Cherry", DateTimeOffset.UtcNow.AddDays(3), listId);

        var query = new List<TaskEntry> { task1, task2, task3 }.AsQueryable();
        var args = new global::Radzen.LoadDataArgs
        {
            Filter = "i => i.Name.Contains(\"a\")",
            OrderBy = "Name desc",
            Skip = 1,
            Top = 1
        };

        var callbackInvoked = false;
        var filteredCount = 0;

        var resultQuery = await query.ApplyDataGridFiltersAsync(args, filtered =>
        {
            callbackInvoked = true;
            filteredCount = filtered.Count();
            return ValueTask.CompletedTask;
        });

        callbackInvoked.Should().BeTrue("onFilteredAndSorted callback must be executed");
        filteredCount.Should().Be(2, "Apple and Banana both contain 'a'");

        var pagedResults = resultQuery.ToList();
        pagedResults.Should().ContainSingle();
        pagedResults[0].Name.Should().Be("apple", "Banana is 1st in descending order, so Skip(1).Take(1) yields apple");
    }

    [Fact]
    public async Task ApplyDataGridDropDownFiltersAsync_AppliesCaseInsensitiveNameFilterAndPaging()
    {
        var userId = Guid.CreateVersion7();
        var list1 = new TodoList(userId, "Groceries");
        var list2 = new TodoList(userId, "Hardware Projects");
        var list3 = new TodoList(userId, "Gardening");

        var query = new List<TodoList> { list1, list2, list3 }.AsQueryable();
        var args = new global::Radzen.LoadDataArgs
        {
            Filter = "ar",
            OrderBy = "Name asc",
            Skip = 0,
            Top = 2
        };

        var callbackCount = 0;

        var resultQuery = await query.ApplyDataGridDropDownFiltersAsync(args, filtered =>
        {
            callbackCount = filtered.Count();
            return ValueTask.CompletedTask;
        });

        callbackCount.Should().Be(2, "Hardware Projects and Gardening both contain 'ar' case-insensitively");
        var results = resultQuery.ToList();
        results.Should().HaveCount(2);
        results[0].Name.Should().Be("Gardening");
        results[1].Name.Should().Be("Hardware Projects");
    }
}
