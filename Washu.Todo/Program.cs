using Washu.Todo.Components;
using Washu.Framework.Extensions;
using Washu.Framework.Identity;
using Washu.Todo.Extensions;
using Washu.Todo.Lists;
using Washu.Todo.Persistence;

await WebApplication.BuildAndRunApplicationAsync<TodoDbContext, App>(args, configureOptions: options =>
{
    options.ApplicationName = typeof(Program).Assembly.GetName().Name!;
    options.CronJobs = [new CronJobRequest(nameof(DeleteTodoListJob), "0 3 * * SUN")];
    options.RegisterAdditionalServices = builder =>
    {
        builder.Services.AddTodoHandlers();
    };
});
