namespace Washu.Framework.IntegrationTests.Fixtures;

using Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

public class PostgreSqlFixture<TDbContext>(params HashSet<IInterceptor> interceptors) : IAsyncLifetime
where TDbContext : ApplicationDbContext
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("washu_framework_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _dbContainer.GetConnectionString();

    public IDbContextFactory<TDbContext> CreateDbContextFactory() => CreateDbContextFactory<TDbContext>();

    public IDbContextFactory<TContext> CreateDbContextFactory<TContext>() where TContext : DbContext
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<TContext>(options =>
            options.UseNpgsql(ConnectionString).AddInterceptors([new VersionedEntityInterceptor(), new AuditedEntityInterceptor(), ..interceptors]));
        return services.BuildServiceProvider().GetRequiredService<IDbContextFactory<TContext>>();
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await using var context = await CreateDbContextFactory().CreateDbContextAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _dbContainer.DisposeAsync();
}

public sealed class ApplicationPostgreSqlFixture : PostgreSqlFixture<ApplicationDbContext>;
[CollectionDefinition(Name)]
public sealed class PostgreSqlTestCollection : ICollectionFixture<ApplicationPostgreSqlFixture>
{
    public const string Name = "PostgreSql Collection";
}
