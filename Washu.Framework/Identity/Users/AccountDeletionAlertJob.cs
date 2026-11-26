namespace Washu.Framework.Identity.Users;

using FluentEmail.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;
using System.Threading.Channels;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

public sealed class AccountDeletionAlertJob(
    ILogger<AccountDeletionAlertJob> logger,
    IServiceScopeFactory scopeFactory
    ) : ITickerFunction<AccountDeletionAlertContext>
{
    [TickerFunction(nameof(AccountDeletionAlertJob))]
    public async Task ExecuteAsync(TickerFunctionContext<AccountDeletionAlertContext> context,
        CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var fluentEmail = scope.ServiceProvider.GetRequiredService<IFluentEmail>();
        try
        {
            logger.LogInformation("Sending account deletion alert email to user with id {UserId}", context.Request.UserId);
            var result = await fluentEmail
                .To(context.Request.AccountEmail)
                .Subject("Account Notice")
                .Body($"The account associated with {context.Request.AccountEmail} has been deleted")
                .SendAsync(cancellationToken);
            if (!result.Successful)
                logger.LogError(
                    "Failed to send account deletion alert email to user with id {UserId}. Reasons: {@Errors}"
                    ,context.Request.UserId, result.ErrorMessages);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to send account deletion alert email to user with id {UserId}"
                ,context.Request.UserId);
        }
    }
}

public sealed record AccountDeletionAlertContext(Guid UserId, string AccountEmail);


public sealed class TimeTickerEntityQueue
{
    private readonly Channel<TimeTickerEntity> _channel =
        Channel.CreateBounded<TimeTickerEntity>(new BoundedChannelOptions(128)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

    public bool TryEnqueue(TimeTickerEntity entity) => _channel.Writer.TryWrite(entity);

    public ValueTask<TimeTickerEntity> DequeueAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAsync(cancellationToken);
}

public sealed class TimeTickerEntitySchedueler(
    TimeTickerEntityQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<TimeTickerEntitySchedueler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            TimeTickerEntity ticker;
            try
            {
                ticker = await queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                using var scope = scopeFactory.CreateScope();
                var tickerManager = scope.ServiceProvider.GetRequiredService<ITimeTickerManager<TimeTickerEntity>>();
                await tickerManager.AddAsync(ticker, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to schedule time ticker entity {TickerName}",
                    ticker.Function);
            }
        }
    }
}