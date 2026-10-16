namespace Washu.Todo;

using Features;

public static class ConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddTodoCommands()
        {
            services.AddSingleton<CreateListHandler>();
            services.AddSingleton<CreateTaskHandler>();
            services.AddSingleton<ToggleTaskHandler>();
            services.AddSingleton<UpdateTaskHandler>();
            services.AddSingleton<DeleteTaskHandler>();
            services.AddSingleton<DeleteListHandler>();
            return services;
        }
    }
}