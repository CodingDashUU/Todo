namespace Washu.Todo;

using Commands;

public static class ConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddTodoCommands()
        {
            services.AddScoped<CreateListHandler>();
            services.AddScoped<CreateTaskHandler>();
            services.AddScoped<ToggleTaskHandler>();
            services.AddScoped<UpdateTaskHandler>();
            services.AddScoped<DeleteTaskHandler>();
            services.AddScoped<DeleteListHandler>();
            return services;
        }
    }
}