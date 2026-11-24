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
    options.CronJobs = [nameof(DeleteTodoListJob)];
    options.RegisterAdditionalServices = builder =>
    {
        builder.Services.AddMediator(opts =>
        {
            opts.PipelineBehaviors = [typeof(UserValidatorBehaviour<,>), typeof(MessageValidatorBehaviour<,>), typeof(DatabaseHandlerBehaviour<,>)];
            opts.ServiceLifetime = ServiceLifetime.Scoped;
        });
    };
});
