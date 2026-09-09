namespace Washu.Todo.Commands;

using FluentValidation;
using Framework.Notifications;
using Identity;
using Microsoft.EntityFrameworkCore;
using TodoList = Todo.TodoList;

public static class CreateList
{
    public class Model
    {
        public string ListName { get; set; } = string.Empty;
    }
    

    public class Validator : AbstractValidator<Model>
    {
        public Validator() =>
            RuleFor(m => m.ListName)
                .SetValidator(new TodoValidators.ListName());
    }

    public class Command(IDbContextFactory<TodoDbContext> dbContextFactory)
    {
        public async Task<Message> ExecuteAsync(Guid userId, Model model)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            if (await dbContext.TodoLists.AnyAsync(l => l.Name == model.ListName))
                return Message.Error("List Creation Error", "Todo List with the given name already exists");
            dbContext.TodoLists.Add(TodoList.Create(userId, model));
            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                return Message.Error("List Creation Error", "There was an unknown error while creating your list");
            }
            return new Message
            {
                Type = MessageType.Success,
                Title = "List Creation Success",
                Details = $"Successfully created list: {model.ListName}"
            };
        }
    }

}