using System.Net;
using System.Net.Mail;
using TickerQ.DependencyInjection;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Users;
using Washu.Todo.Components;
using Washu.Todo.Persistence;
using Washu.Todo.TodoList;

await WebApplication.BuildAndRunApplicationAsync<TodoDbContext, App>(args, configureOptions: options =>
{
    options.CronJobs = [new CronJobRequest(nameof(DeleteTodoListJob), "0 35 * * * *", TimeSpan.FromHours(23))];
    options.RegisterAdditionalServices = builder =>
    {
        builder.Services
            .AddFluentEmail(builder.Configuration.GetValue<string>("Email:FromEmail"), "Todo Application")
            .AddSmtpSender(() => new SmtpClient(builder.Configuration.GetValue<string>("Email:SmtpHost"), builder.Configuration.GetValue<int>("Email:SmtpPort"))
            {
                Credentials = new NetworkCredential(builder.Configuration.GetValue<string>("Email:FromEmail"), builder.Configuration.GetValue<string>("Email:AppPassword")),
                EnableSsl = true
            });
        builder.Services.AddSingleton<TimeTickerEntityQueue>();
        builder.Services.AddHostedService<TimeTickerEntitySchedueler>();
        builder.Services.AddMediator(opts =>
        {
            opts.PipelineBehaviors = [typeof(UserValidatorBehaviour<,>), typeof(MessageValidatorBehaviour<,>), typeof(DatabaseHandlerBehaviour<,>)];
            opts.ServiceLifetime = ServiceLifetime.Scoped;
        });
        builder.Services.MapTicker<DeleteTodoListJob>().WithMaxConcurrency(1);
    };
});
