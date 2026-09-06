namespace Washu.Todo;

using Commands;

public static class ConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddTodoCommands() 
        {
            services.AddScoped<CreateList.Command>();
            services.AddScoped<CreateTask.Command>();
            services.AddScoped<ToggleTask.Command>();
            services.AddScoped<UpdateTask.Command>();
            return services;
        }
    }
}