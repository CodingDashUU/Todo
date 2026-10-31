namespace Washu.Todo.IntegrationTests;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testing;
using Washu.Framework.Identity.Entities;
using Framework.Notifications;
using Features;
using Identity;

[Collection(TodoPostgreSqlTestCollection.Name)]
public class TodoHandlersTests(TodoPostgreSqlFixture fixture)
{
    private static async Task<ApplicationUser> CreateTestUserAsync(TodoDbContext context)
    {
        var guid = Guid.CreateVersion7().ToString("N");
        var user = new ApplicationUser($"User_{guid}", $"{guid}@example.com", $"sub_{guid}");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    #region CreateListHandler Tests

    [Fact]
    public async Task CreateListHandler_CreatesNewList_AndRejectsDuplicateNameForSameUser()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var handler = new CreateListHandler(factory, new MockCurrentUser(user.Id));
        var listName = $"List{Guid.CreateVersion7():N}";

        // 1. Create list successfully
        var result = await handler.HandleAsync(new CreateListCommand(listName));
        result.Type.Should().Be(MessageType.Success);

        await using var verifyContext = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var list = await verifyContext.TodoLists.FirstOrDefaultAsync(l => l.UserId == user.Id && l.Name == listName);
        list.Should().NotBeNull();

        // 2. Attempt duplicate creation for same user
        var duplicateResult = await handler.HandleAsync(new CreateListCommand(listName));
        duplicateResult.Type.Should().Be(MessageType.Error);
        duplicateResult.Details.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateListHandler_AllowsSameListName_ForDifferentUsers()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var user1 = await CreateTestUserAsync(context);
        var user2 = await CreateTestUserAsync(context);

        var handler1 = new CreateListHandler(factory, new MockCurrentUser(user1.Id));
        var handler2 = new CreateListHandler(factory, new MockCurrentUser(user2.Id));
        const string sharedListName = "Shared Work";

        var result1 = await handler1.HandleAsync(new CreateListCommand(sharedListName));
        var result2 = await handler2.HandleAsync(new CreateListCommand(sharedListName));

        result1.Type.Should().Be(MessageType.Success);
        result2.Type.Should().Be(MessageType.Success);

        await using var verifyContext = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var count = await verifyContext.TodoLists.CountAsync(l => l.Name == sharedListName);
        count.Should().Be(2);
    }

    #endregion

    #region UpdateListHandler Tests

    [Fact]
    public async Task UpdateListHandler_UpdatesListName_AndChecksConcurrencyVersion()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var list = new TodoList(user.Id, "Old Name");
        context.TodoLists.Add(list);
        await context.SaveChangesAsync();

        var handler = new UpdateListHandler(factory, new MockCurrentUser(user.Id));

        // 1. Attempt update with wrong RowVersion
        var staleResult = await handler.HandleAsync(new UpdateListCommand(list.Id, 99, "New Name"));
        staleResult.Type.Should().Be(MessageType.Error);
        staleResult.Details.Should().Contain("already modified");

        // 2. Successful update
        var successResult = await handler.HandleAsync(new UpdateListCommand(list.Id, list.RowVersion, "New Name"));
        successResult.Type.Should().Be(MessageType.Success);

        await using var verifyContext = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var updatedList = await verifyContext.TodoLists.FirstAsync(l => l.Id == list.Id);
        updatedList.Name.Should().Be("New Name");
        updatedList.RowVersion.Should().NotBe(list.RowVersion);
        
    }

    [Fact]
    public async Task UpdateListHandler_ReturnsError_WhenListDoesNotExist()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var handler = new UpdateListHandler(factory, new MockCurrentUser(user.Id));
        var nonExistentListId = Guid.CreateVersion7();

        var result = await handler.HandleAsync(new UpdateListCommand(nonExistentListId, 99, "New Name"));

        result.Type.Should().Be(MessageType.Error);
        result.Details.Should().Contain("Todo list does not exist");
    }

    [Fact]
    public async Task UpdateListHandler_ReturnsError_WhenAnotherListWithTargetNameAlreadyExists()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var list1 = new TodoList(user.Id, "List One");
        var list2 = new TodoList(user.Id, "List Two");
        context.TodoLists.AddRange(list1, list2);
        await context.SaveChangesAsync();

        var handler = new UpdateListHandler(factory, new MockCurrentUser(user.Id));

        // Attempt to rename list1 to "List Two" which already exists for this user
        var result = await handler.HandleAsync(new UpdateListCommand(list1.Id, list1.RowVersion, "List Two"));

        result.Type.Should().Be(MessageType.Error);
        result.Details.Should().Contain("already exists");
    }

    [Fact]
    public async Task UpdateListHandler_ReturnsError_WhenUserDoesNotOwnList()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var owner = await CreateTestUserAsync(context);
        var otherUser = await CreateTestUserAsync(context);

        var list = new TodoList(owner.Id, "Owner List");
        context.TodoLists.Add(list);
        await context.SaveChangesAsync();

        var handler = new UpdateListHandler(factory, new MockCurrentUser(otherUser.Id));

        var result = await handler.HandleAsync(new UpdateListCommand(list.Id, list.RowVersion, "Stolen List"));

        result.Type.Should().Be(MessageType.Error);
        result.Details.Should().Contain("Todo list does not exist");
    }

    #endregion

    #region DeleteListHandler Tests

    [Fact]
    public async Task DeleteListHandler_DeletesList()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var list = new TodoList(user.Id, "To Delete");
        context.TodoLists.Add(list);
        await context.SaveChangesAsync();

        var handler = new DeleteListHandler(factory, new MockCurrentUser(user.Id));

        var result = await handler.HandleAsync(new DeleteListCommand(list.Id));
        result.Type.Should().Be(MessageType.Success);

        await using var verifyContext = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var exists = await verifyContext.TodoLists.AnyAsync(l => l.Id == list.Id);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteListHandler_ReturnsError_WhenListDoesNotExist()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var handler = new DeleteListHandler(factory, new MockCurrentUser(user.Id));

        var result = await handler.HandleAsync(new DeleteListCommand(Guid.CreateVersion7()));

        result.Type.Should().Be(MessageType.Error);
        result.Details.Should().Contain("List was not found");
    }

    [Fact]
    public async Task DeleteListHandler_ReturnsError_WhenUserDoesNotOwnList()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var owner = await CreateTestUserAsync(context);
        var otherUser = await CreateTestUserAsync(context);

        var list = new TodoList(owner.Id, "Private List");
        context.TodoLists.Add(list);
        await context.SaveChangesAsync();

        var handler = new DeleteListHandler(factory, new MockCurrentUser(otherUser.Id));

        var result = await handler.HandleAsync(new DeleteListCommand(list.Id));

        result.Type.Should().Be(MessageType.Error);
        result.Details.Should().Contain("List was not found");

        await using var verifyContext = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var exists = await verifyContext.TodoLists.AnyAsync(l => l.Id == list.Id);
        exists.Should().BeTrue();
    }

    #endregion

    #region CreateTaskHandler Tests

    [Fact]
    public async Task CreateTaskHandler_CreatesTask_AndRejectsDuplicateNameInSameList()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var list = new TodoList(user.Id, "Tasks List");
        context.TodoLists.Add(list);
        await context.SaveChangesAsync();

        var handler = new CreateTaskHandler(factory, new MockCurrentUser(user.Id));
        var goalDate = DateTimeOffset.UtcNow.AddDays(2);

        // 1. Successful creation
        var result = await handler.HandleAsync(new CreateTaskCommand(list.Id, list.RowVersion, "Task Alpha", goalDate));
        result.Type.Should().Be(MessageType.Success);

        await using var verifyContext = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var task = await verifyContext.Tasks.FirstOrDefaultAsync(t => t.TodoListId == list.Id && t.Name == "Task Alpha");
        task.Should().NotBeNull();

        // 2. Duplicate task creation in same list
        var updatedList = await verifyContext.TodoLists.FirstAsync(l => l.Id == list.Id);
        var duplicateResult = await handler.HandleAsync(new CreateTaskCommand(list.Id, updatedList.RowVersion, "Task Alpha", goalDate));
        duplicateResult.Type.Should().Be(MessageType.Error);
        duplicateResult.Details.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateTaskHandler_AllowsSameTaskName_InDifferentLists()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var list1 = new TodoList(user.Id, "List A");
        var list2 = new TodoList(user.Id, "List B");
        context.TodoLists.AddRange(list1, list2);
        await context.SaveChangesAsync();

        var handler = new CreateTaskHandler(factory, new MockCurrentUser(user.Id));
        const string taskName = "Common Task";
        var goalDate = DateTimeOffset.UtcNow.AddDays(1);

        var result1 = await handler.HandleAsync(new CreateTaskCommand(list1.Id, list1.RowVersion, taskName, goalDate));
        var result2 = await handler.HandleAsync(new CreateTaskCommand(list2.Id, list2.RowVersion, taskName, goalDate));

        result1.Type.Should().Be(MessageType.Success);
        result2.Type.Should().Be(MessageType.Success);
    }

    [Fact]
    public async Task CreateTaskHandler_ReturnsError_WhenListDoesNotExist()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        var handler = new CreateTaskHandler(factory, new MockCurrentUser(Guid.CreateVersion7()));

        var result = await handler.HandleAsync(new CreateTaskCommand(Guid.CreateVersion7(), 99, "Ghost Task", DateTimeOffset.UtcNow.AddDays(1)));

        result.Type.Should().Be(MessageType.Error);
        result.Details.Should().Contain("List does not exist");
    }

    [Fact]
    public async Task CreateTaskHandler_ReturnsError_WhenListVersionIsStale()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var list = new TodoList(user.Id, "Stale Task List");
        context.TodoLists.Add(list);
        await context.SaveChangesAsync();

        var handler = new CreateTaskHandler(factory, new MockCurrentUser(user.Id));
        const int wrongVersion = 99;

        var result = await handler.HandleAsync(new CreateTaskCommand(list.Id, wrongVersion, "Task 1", DateTimeOffset.UtcNow.AddDays(1)));

        result.Type.Should().Be(MessageType.Error);
        result.Details.Should().Contain("already modified");
    }

    #endregion

    #region ToggleTaskHandler Tests

    [Fact]
    public async Task ToggleTaskHandler_TogglesTaskCompletion()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var list = new TodoList(user.Id, "Toggle List");
        var task = new TaskEntry("Toggle Me", DateTimeOffset.UtcNow.AddDays(1), list.Id);
        list.Tasks.Add(task);
        context.TodoLists.Add(list);
        await context.SaveChangesAsync();

        var handler = new ToggleTaskHandler(factory, new MockCurrentUser(user.Id));

        // 1. Toggle to completed
        var result1 = await handler.HandleAsync(new ToggleTaskCommand(task.Id, list.Id, list.RowVersion));
        result1.Type.Should().Be(MessageType.Success);

        await using var verifyContext1 = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var toggledTask = await verifyContext1.Tasks.FirstAsync(t => t.Id == task.Id);
        toggledTask.IsCompleted.Should().BeTrue();
        toggledTask.DateCompleted.Should().NotBeNull();

        var updatedList = await verifyContext1.TodoLists.FirstAsync(l => l.Id == list.Id);

        // 2. Toggle back to incomplete
        var result2 = await handler.HandleAsync(new ToggleTaskCommand(task.Id, list.Id, updatedList.RowVersion));
        result2.Type.Should().Be(MessageType.Success);

        await using var verifyContext2 = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var untoggledTask = await verifyContext2.Tasks.FirstAsync(t => t.Id == task.Id);
        untoggledTask.IsCompleted.Should().BeFalse();
        untoggledTask.DateCompleted.Should().BeNull();
    }

    [Fact]
    public async Task ToggleTaskHandler_ReturnsError_WhenListDoesNotExist()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        var handler = new ToggleTaskHandler(factory, new MockCurrentUser(Guid.CreateVersion7()));

        var result = await handler.HandleAsync(new ToggleTaskCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), 99));

        result.Type.Should().Be(MessageType.Error);
        result.Details.Should().Contain("Todo list does not exist");
    }

    [Fact]
    public async Task ToggleTaskHandler_ReturnsError_WhenListVersionIsStale()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var list = new TodoList(user.Id, "Toggle Stale List");
        var task = new TaskEntry("Task 1", DateTimeOffset.UtcNow.AddDays(1), list.Id);
        list.Tasks.Add(task);
        context.TodoLists.Add(list);
        await context.SaveChangesAsync();

        var handler = new ToggleTaskHandler(factory, new MockCurrentUser(user.Id));

        var result = await handler.HandleAsync(new ToggleTaskCommand(task.Id, list.Id, 99));

        result.Type.Should().Be(MessageType.Error);
        result.Details.Should().Contain("already modified");
    }

    [Fact]
    public async Task ToggleTaskHandler_ReturnsError_WhenTaskDoesNotExist()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var list = new TodoList(user.Id, "Toggle Empty List");
        context.TodoLists.Add(list);
        await context.SaveChangesAsync();

        var handler = new ToggleTaskHandler(factory, new MockCurrentUser(user.Id));

        var result = await handler.HandleAsync(new ToggleTaskCommand(Guid.CreateVersion7(), list.Id, list.RowVersion));

        result.Type.Should().Be(MessageType.Error);
        result.Details.Should().Contain("Task does not exist");
    }

    #endregion

    #region UpdateTaskHandler Tests

    [Fact]
    public async Task UpdateTaskHandler_UpdatesTaskNameAndGoalDate()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var list = new TodoList(user.Id, "Update List");
        var task = new TaskEntry("Initial Task", DateTimeOffset.UtcNow.AddDays(1), list.Id);
        list.Tasks.Add(task);
        context.TodoLists.Add(list);
        await context.SaveChangesAsync();

        var handler = new UpdateTaskHandler(factory, new MockCurrentUser(user.Id));
        var newGoalDate = DateTimeOffset.UtcNow.AddDays(4);

        var result = await handler.HandleAsync(new UpdateTaskCommand(task.Id, "Updated Task Name", newGoalDate, list.Id, list.RowVersion));
        result.Type.Should().Be(MessageType.Success);

        await using var verifyContext = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var updatedTask = await verifyContext.Tasks.FirstAsync(t => t.Id == task.Id);
        updatedTask.Name.Should().Be("Updated Task Name");
        updatedTask.GoalDate.Should().BeCloseTo(newGoalDate.ToUniversalTime(), TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task UpdateTaskHandler_ReturnsInfo_WhenNoPropertiesChanged()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var goalDate = DateTimeOffset.UtcNow.AddDays(2);
        var list = new TodoList(user.Id, "No Change List");
        var task = new TaskEntry("Same Name", goalDate, list.Id);
        list.Tasks.Add(task);
        context.TodoLists.Add(list);
        await context.SaveChangesAsync();

        var handler = new UpdateTaskHandler(factory, new MockCurrentUser(user.Id));

        await using var readContext = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var persistedTask = await readContext.Tasks.FirstAsync(t => t.Id == task.Id);

        var result = await handler.HandleAsync(new UpdateTaskCommand(persistedTask.Id, "Same Name", persistedTask.GoalDate, list.Id, list.RowVersion));

        result.Type.Should().Be(MessageType.Info);
        result.Details.Should().Contain("Task has not been modified");
    }

    [Fact]
    public async Task UpdateTaskHandler_ReturnsError_WhenNewTaskNameCollidesInSameList()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var list = new TodoList(user.Id, "Collision List");
        var task1 = new TaskEntry("Task One", DateTimeOffset.UtcNow.AddDays(1), list.Id);
        var task2 = new TaskEntry("Task Two", DateTimeOffset.UtcNow.AddDays(2), list.Id);
        list.Tasks.AddRange([task1, task2]);
        context.TodoLists.Add(list);
        await context.SaveChangesAsync();

        var handler = new UpdateTaskHandler(factory, new MockCurrentUser(user.Id));

        // Attempt to rename task1 to "Task Two"
        var result = await handler.HandleAsync(new UpdateTaskCommand(task1.Id, "Task Two", task1.GoalDate, list.Id, list.RowVersion));

        result.Type.Should().Be(MessageType.Error);
        result.Details.Should().Contain("already exists");
    }

    [Fact]
    public async Task UpdateTaskHandler_ReturnsError_WhenListDoesNotExist()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        var handler = new UpdateTaskHandler(factory, new MockCurrentUser(Guid.CreateVersion7()));

        var result = await handler.HandleAsync(new UpdateTaskCommand(Guid.CreateVersion7(), "Name", DateTimeOffset.UtcNow.AddDays(1), Guid.CreateVersion7(), 99));

        result.Type.Should().Be(MessageType.Error);
        result.Details.Should().Contain("Todo list does not exist");
    }

    [Fact]
    public async Task UpdateTaskHandler_ReturnsError_WhenListVersionIsStale()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var list = new TodoList(user.Id, "Stale Update Task List");
        var task = new TaskEntry("Task 1", DateTimeOffset.UtcNow.AddDays(1), list.Id);
        list.Tasks.Add(task);
        context.TodoLists.Add(list);
        await context.SaveChangesAsync();

        var handler = new UpdateTaskHandler(factory, new MockCurrentUser(user.Id));

        var result = await handler.HandleAsync(new UpdateTaskCommand(task.Id, "New Name", task.GoalDate, list.Id, 99));

        result.Type.Should().Be(MessageType.Error);
        result.Details.Should().Contain("already modified");
    }

    [Fact]
    public async Task UpdateTaskHandler_ReturnsError_WhenTaskDoesNotExist()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var list = new TodoList(user.Id, "No Task List");
        context.TodoLists.Add(list);
        await context.SaveChangesAsync();

        var handler = new UpdateTaskHandler(factory, new MockCurrentUser(user.Id));

        var result = await handler.HandleAsync(new UpdateTaskCommand(Guid.CreateVersion7(), "New Name", DateTimeOffset.UtcNow.AddDays(1), list.Id, list.RowVersion));

        result.Type.Should().Be(MessageType.Error);
        result.Details.Should().Contain("Task does not exist");
    }

    #endregion

    #region DeleteTaskHandler Tests

    [Fact]
    public async Task DeleteTaskHandler_DeletesTask_AndUpdatesListVersion()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var list = new TodoList(user.Id, "Delete Task List");
        var task = new TaskEntry("Task to Delete", DateTimeOffset.UtcNow.AddDays(1), list.Id);
        list.Tasks.Add(task);
        context.TodoLists.Add(list);
        await context.SaveChangesAsync();

        var handler = new DeleteTaskHandler(factory, new MockCurrentUser(user.Id));

        var result = await handler.HandleAsync(new DeleteTaskCommand(task.Id, list.Id, list.RowVersion));
        result.Type.Should().Be(MessageType.Success);

        await using var verifyContext = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var taskExists = await verifyContext.Tasks.AnyAsync(t => t.Id == task.Id);
        taskExists.Should().BeFalse();

        var updatedList = await verifyContext.TodoLists.FirstAsync(l => l.Id == list.Id);
        updatedList.RowVersion.Should().NotBe(list.RowVersion);
    }

    [Fact]
    public async Task DeleteTaskHandler_ReturnsError_WhenListDoesNotExist()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        var handler = new DeleteTaskHandler(factory, new MockCurrentUser(Guid.CreateVersion7()));

        var result = await handler.HandleAsync(new DeleteTaskCommand(Guid.CreateVersion7(), Guid.CreateVersion7(), 99));

        result.Type.Should().Be(MessageType.Error);
        result.Details.Should().Contain("Todo list does not exist");
    }

    [Fact]
    public async Task DeleteTaskHandler_ReturnsError_WhenListVersionIsStale()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var list = new TodoList(user.Id, "Stale Delete List");
        var task = new TaskEntry("Task 1", DateTimeOffset.UtcNow.AddDays(1), list.Id);
        list.Tasks.Add(task);
        context.TodoLists.Add(list);
        await context.SaveChangesAsync();

        var handler = new DeleteTaskHandler(factory, new MockCurrentUser(user.Id));

        var result = await handler.HandleAsync(new DeleteTaskCommand(task.Id, list.Id, 99));

        result.Type.Should().Be(MessageType.Error);
        result.Details.Should().Contain("already modified");
    }

    [Fact]
    public async Task DeleteTaskHandler_ReturnsError_WhenTaskDoesNotExist()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var list = new TodoList(user.Id, "Empty Delete List");
        context.TodoLists.Add(list);
        await context.SaveChangesAsync();

        var handler = new DeleteTaskHandler(factory, new MockCurrentUser(user.Id));

        var result = await handler.HandleAsync(new DeleteTaskCommand(Guid.CreateVersion7(), list.Id, list.RowVersion));

        result.Type.Should().Be(MessageType.Error);
        result.Details.Should().Contain("Task does not exist");
    }

    #endregion
}
