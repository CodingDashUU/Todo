using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Washu.Framework.Extensions;
namespace Washu.Framework.Extensions;

using AspNetCore;
using Blazor;
using global::Radzen;
using Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notifications;
using System.Threading.RateLimiting;
using ZiggyCreatures.Caching.Fusion;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
    private IServiceCollection AddDbContextFactory<TBaseContext, TDerivedContext>(
        Action<DbContextOptionsBuilder>? optionsAction = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TBaseContext : DbContext
        where TDerivedContext : TBaseContext =>
        services.AddDbContextFactory<TBaseContext, TDerivedContext>(
            (_, builder) => optionsAction?.Invoke(builder), 
            lifetime);

    private IServiceCollection AddDbContextFactory<TBaseContext, TDerivedContext>(
        Action<IServiceProvider, DbContextOptionsBuilder> optionsAction,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TBaseContext : DbContext
        where TDerivedContext : TBaseContext
    {
        services.AddDbContextFactory<TDerivedContext>(optionsAction, lifetime);
        services.TryAdd(new ServiceDescriptor(
            typeof(IDbContextFactory<TBaseContext>),
            sp => new DbContextFactoryAdapter<TBaseContext, TDerivedContext>(
                sp.GetRequiredService<IDbContextFactory<TDerivedContext>>()),
            lifetime));
        return services;
    }

    public IServiceCollection AddFusionCaching()
    {
        services.AddMemoryCache();
        services.AddFusionCache().WithDefaultEntryOptions(new FusionCacheEntryOptions
        {
            Duration = TimeSpan.FromMinutes(5), 
            IsFailSafeEnabled = true,
            FailSafeMaxDuration = TimeSpan.FromHours(2),
            FailSafeThrottleDuration = TimeSpan.FromSeconds(30),
            FactorySoftTimeout = TimeSpan.FromMilliseconds(500), 
            FactoryHardTimeout = TimeSpan.FromSeconds(2), 
            JitterMaxDuration = TimeSpan.FromSeconds(2)
        });
        return services;
    }
    public IServiceCollection AddAppAuthenticationStateProvider() =>
        services.AddScoped<AuthenticationStateProvider, AppAuthenticationStateProvider>();

    public IServiceCollection AddNoOpAntiforgeryProvider()
    {
        services.AddScoped<AntiforgeryStateProvider, NoOpAntiforgeryStateProvider>();
        services.RemoveAll<IAntiforgery>();
        services.AddSingleton<IAntiforgery, NullAntiforgery>();
        return services;
    }
    public IServiceCollection AddWfIdentityServices()
    {
        services.AddSingleton<AddRolesHandler>();
        services.AddSingleton<ChangeUsernameHandler>();
        services.AddSingleton<DeleteUserHandler>();
        services.AddSingleton<FindUserHandler>();
        services.AddSingleton<SignInHandler>();
        services.AddSingleton<HasPermissionHandler>();
        services.AddSingleton<GetExternalLoginInfoHandler>();
        services.AddSingleton<ApplicationUserStore>();
        services.AddScoped<GetUserService>();
        services.AddSingleton<CurrentUser>();
        services.AddSingleton<IDeleteEntityTask, DeleteUserTask>();
        services.AddHostedService<DeleteEntityWorker>();
        return services;
    }
    public IServiceCollection AddWfDbContextFactory<TDbContext>(ConfigurationManager configuration, params HashSet<IInterceptor> interceptors)
    where TDbContext : ApplicationDbContext
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        return services.AddDbContextFactory<ApplicationDbContext, TDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null
                    ));
            options.AddInterceptors([new AuditableEntityInterceptor(), new SoftDeletableEntityInterceptor(), ..interceptors]);
        });
    }
    public IServiceCollection AddUserTimeZoneProvider() => services.AddScoped<UserTimeZoneProvider>(sp =>
    {
        var httpContext = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
        var tzCookie = httpContext?.Request.Cookies[CookieNames.ClientTimeZone];
        try
        {
            var instance = new UserTimeZoneProvider { TimeZoneId = !string.IsNullOrEmpty(tzCookie) ? tzCookie : "UTC" };
            // Check if it is valid
            _ = instance.TimeZoneInfo;
            return instance;
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return new UserTimeZoneProvider();
        }
    });

    public IServiceCollection AddDeleteEntityTask<TEntityTask>()
    where TEntityTask : class, IDeleteEntityTask
    {
        return services.AddSingleton<IDeleteEntityTask, TEntityTask>();
    }
    public IServiceCollection AddWfRateLimiting() => services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy(RateLimiterPolicy.BlazorHandshakeRateLimiter, context =>
                {
                    if (context.Connection.RemoteIpAddress?.ToString() is { } ipAddress)
                        return RateLimitPartition.GetSlidingWindowLimiter(
                            partitionKey: $"ip:{ipAddress}",
                            factory: _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = 200,
                                Window = TimeSpan.FromMinutes(1),
                                SegmentsPerWindow = 6,
                                QueueLimit = 0
                            });
                    return RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: "anonymous",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 50,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            QueueLimit = 0
                        });
                });
                options.AddPolicy(RateLimiterPolicy.StandardRateLimiter, httpContext =>
                {
                    if (httpContext.User.UserId is { } userId)
                        return RateLimitPartition.GetSlidingWindowLimiter(
                            partitionKey: $"user:{userId}",
                            factory: _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = 100,
                                Window = TimeSpan.FromMinutes(1),
                                SegmentsPerWindow = 4,
                                QueueLimit = 0
                            });
                    if (httpContext.Connection.RemoteIpAddress?.ToString() is { } ipAddress)
                        return RateLimitPartition.GetSlidingWindowLimiter(
                            partitionKey: $"ip:{ipAddress}",
                            factory: _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = 30,
                                Window = TimeSpan.FromMinutes(1),
                                SegmentsPerWindow = 4,
                                QueueLimit = 0
                            });
                    return RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: "anonymous",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 4,
                            QueueLimit = 0
                        });
                });
            });

            public IServerSideBlazorBuilder AddWfRadzenBlazorServer()
        {
            services.AddRadzenCookieThemeService(options =>
            {
                options.Name = CookieNames.Theme;
                options.Duration = TimeSpan.FromDays(365);
            });
            services.AddSingleton<MessageStore>();
            services.AddRazorComponents()
                .AddInteractiveServerComponents();
            services.AddRadzenComponents();
            return services.AddServerSideBlazor(options =>
                {
                    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(1);
                    options.DisconnectedCircuitMaxRetained = 50;
                    options.MaxBufferedUnacknowledgedRenderBatches = 5;
                    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
                })
                .AddHubOptions(options =>
                {
                    options.EnableDetailedErrors = false;
                    options.MaximumReceiveMessageSize = 32 * 1024;
                    options.HandshakeTimeout = TimeSpan.FromSeconds(5);
                    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                });
        }
    }

    extension(ConfigureHostBuilder host)
    {
        public void UseWfLogging()
        {
            host.UseSerilog((_, _, configuration) => configuration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Components.RenderTree.Renderer", LogEventLevel.Error)
                .MinimumLevel.Override("Microsoft.AspNetCore.SignalR", LogEventLevel.Warning)
                .MinimumLevel.Override("ZiggyCreatures.Caching.Fusion.FusionCache", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProcessName()
                .Enrich.WithProcessId()
                .Enrich.WithEnvironmentName()
        
                .WriteTo.Console(outputTemplate: 
                    "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
        
                .WriteTo.File(
                    path: "logs/app-log-.txt",
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    retainedFileCountLimit: 30)
        
                .WriteTo.File(
                    new JsonFormatter(renderMessage: true), 
                    path: "logs/app-json-log-.json",
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    retainedFileCountLimit: 30)
            );
        }
    }
    
}
file sealed class NoOpAntiforgeryStateProvider : AntiforgeryStateProvider
{
    public override AntiforgeryRequestToken? GetAntiforgeryToken() => null;
}
file sealed class NullAntiforgery : IAntiforgery
{
    private static readonly AntiforgeryTokenSet DummyTokenSet = new(
        requestToken: "null_request_token",
        cookieToken: null,
        formFieldName: "__RequestVerificationToken",
        headerName: "X-CSRF-TOKEN"
    );

    public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext) => DummyTokenSet;

    public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => DummyTokenSet;

    public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);

    public Task ValidateRequestAsync(HttpContext httpContext) =>  Task.CompletedTask;

    public void SetCookieTokenAndHeader(HttpContext httpContext) {}
}
file sealed class DbContextFactoryAdapter<TBaseContext, TDerivedContext>(
    IDbContextFactory<TDerivedContext> innerFactory) : IDbContextFactory<TBaseContext>
    where TBaseContext : DbContext
    where TDerivedContext : TBaseContext
{
    private readonly IDbContextFactory<TDerivedContext> _innerFactory = innerFactory ?? throw new ArgumentNullException(nameof(innerFactory));
    
    public TBaseContext CreateDbContext() =>
        _innerFactory.CreateDbContext();

    public async Task<TBaseContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
        await _innerFactory.CreateDbContextAsync(cancellationToken);
}