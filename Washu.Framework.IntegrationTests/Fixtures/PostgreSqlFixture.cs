namespace Washu.Framework.IntegrationTests.Fixtures;

using Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("washu_framework_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private string ConnectionString => _dbContainer.GetConnectionString();

    private DbContextOptions<ApplicationDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

    public ApplicationDbContext CreateDbContext() => new(CreateOptions());

    public IDbContextFactory<ApplicationDbContext> CreateDbContextFactory()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<ApplicationDbContext>(options =>
            options.UseNpgsql(ConnectionString));
        return services.BuildServiceProvider().GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        await using var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _dbContainer.DisposeAsync();
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlTestCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSql Collection";
}
