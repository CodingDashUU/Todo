using Washu.Framework.Identity;
using Washu.Todo;
using Washu.Todo.Components;
using Washu.Todo.Identity;
using Washu.Framework.Extensions;

await WebApplication.BuildAndRunApplicationAsync<TodoDbContext, App>(args, configureOptions: options =>
{
    options.RegisterAdditionalServices = builder =>
    {
        builder.Services.AddDeleteEntityTask<DeleteTodoListTask>();
        builder.Services.AddTodoHandlers();
    };
});
