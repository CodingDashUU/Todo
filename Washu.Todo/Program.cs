using Washu.Framework.Identity;
using Washu.Todo;
using Washu.Todo.Components;
using Washu.Framework.Extensions;
using Washu.Todo.Extensions;
using Washu.Todo.Lists;
using Washu.Todo.Persistence;

await WebApplication.BuildAndRunApplicationAsync<TodoDbContext, App>(args, configureOptions: options =>
{
    options.RegisterAdditionalServices = builder =>
    {
        builder.Services.AddDeleteEntityTask<DeleteTodoListTask>();
        builder.Services.AddTodoHandlers();
    };
});
