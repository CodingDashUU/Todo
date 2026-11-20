using FluentValidation;
using Washu.Framework.Extensions;
using Washu.Framework.Identity;
using Washu.Framework.Identity.Users;
using Washu.Todo;
using Washu.Todo.Components;
using Washu.Todo.Persistence;
using Washu.Todo.Tasks;
using Washu.Todo.TodoList;

await WebApplication.BuildAndRunApplicationAsync<TodoDbContext, App>(args, configureOptions: options =>
{
    options.CronJobs = [new CronJobRequest(nameof(DeleteTodoListJob), "0 3 * * SUN")];
    options.RegisterAdditionalServices = builder =>
    {
        builder.Services.AddMediator(opts =>
        {
            opts.Assemblies = [typeof(ToggleTaskHandler).Assembly, typeof(DeleteUserHandler).Assembly];
            opts.PipelineBehaviors = [typeof(DatabaseHandlerBehaviour<,>), typeof(MessageValidatorBehaviour<,>)];
            opts.ServiceLifetime = ServiceLifetime.Scoped;
        });
    };
});