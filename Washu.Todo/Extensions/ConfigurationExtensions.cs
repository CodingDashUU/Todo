using Washu.Todo.Lists;
using Washu.Todo.Lists.Features;
using Washu.Todo.Tasks.Features;

namespace Washu.Todo.Extensions;

public static class ConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddTodoHandlers()
        {
            services.AddSingleton<CreateListHandler>();
            services.AddSingleton<CreateTaskHandler>();
            services.AddSingleton<ToggleTaskHandler>();
            services.AddSingleton<UpdateTaskHandler>();
            services.AddSingleton<DeleteTaskHandler>();
            services.AddSingleton<DeleteListHandler>();
            services.AddSingleton<UpdateListHandler>();
            return services;
        }
    }
}