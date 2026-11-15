using Mediator;
using System.Reflection;
using Washu.Todo.Components;
using Washu.Framework.Extensions;
using Washu.Framework.Identity;
using Washu.Framework.Identity.Users.DeleteUser;
using Washu.Todo.Extensions;
using Washu.Todo.Lists;
using Washu.Todo.Persistence;
using Washu.Todo.Tasks.Features;

await WebApplication.BuildAndRunApplicationAsync<TodoDbContext, App>(args, configureOptions: options =>
{
    options.CronJobs = [new CronJobRequest(nameof(DeleteTodoListJob), "0 3 * * SUN")];
    options.RegisterAdditionalServices = builder =>
    {
        builder.Services.AddMediator(opts =>
        {
            opts.Assemblies = [typeof(ToggleTaskHandler).Assembly, typeof(DeleteUserHandler).Assembly];
        });
    };
});