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

    public string ConnectionString => _dbContainer.GetConnectionString();

    private DbContextOptions<ApplicationDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

    public ApplicationDbContext CreateDbContext() => new(CreateOptions());

    public TContext CreateDbContext<TContext>() where TContext : DbContext
    {
        var options = new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return (TContext)Activator.CreateInstance(typeof(TContext), options)!;
    }

    public IDbContextFactory<ApplicationDbContext> CreateDbContextFactory() => CreateDbContextFactory<ApplicationDbContext>();

    public IDbContextFactory<TContext> CreateDbContextFactory<TContext>() where TContext : DbContext
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<TContext>(options =>
            options.UseNpgsql(ConnectionString));
        return services.BuildServiceProvider().GetRequiredService<IDbContextFactory<TContext>>();
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        // Ensure base ApplicationDbContext schema exists
        await using var context = CreateDbContext();
        await context.Database.EnsureCreatedAsync();

        // Ensure TodoDbContext tables (TodoLists, Tasks) exist if using the same database
        await using var connection = new Npgsql.NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        const string ddl = """
            CREATE TABLE IF NOT EXISTS "TodoLists" (
                "Id" uuid NOT NULL,
                "UserId" uuid NOT NULL,
                "Name" text NOT NULL,
                "CreationDate" timestamp with time zone NOT NULL,
                "LastModified" timestamp with time zone NOT NULL,
                "VersionId" uuid NOT NULL,
                CONSTRAINT "PK_TodoLists" PRIMARY KEY ("Id"),
                CONSTRAINT "CK_TodoList_CreationDate_LTE_LastModified" CHECK ("CreationDate" <= "LastModified"),
                CONSTRAINT "CK_TodoList_Name_AllowedChar" CHECK (length("Name") > 0 AND translate("Name", 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 !#$&()/?', '') = ''),
                CONSTRAINT "CK_TodoList_Name_MaxLength" CHECK (length("Name") > 0 AND length("Name") <= 64),
                CONSTRAINT "FK_TodoLists_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS "Tasks" (
                "Id" uuid NOT NULL,
                "TodoListId" uuid NOT NULL,
                "Name" text NOT NULL,
                "IsCompleted" boolean NOT NULL,
                "DateCreated" timestamp with time zone NOT NULL,
                "GoalDate" timestamp with time zone NOT NULL,
                "DateCompleted" timestamp with time zone NULL,
                CONSTRAINT "PK_Tasks" PRIMARY KEY ("Id"),
                CONSTRAINT "CK_TaskEntry_DateCreated_LTE_GoalDate" CHECK ("DateCreated" <= "GoalDate"),
                CONSTRAINT "CK_TaskEntry_DateCreated_LTE_N_DateCompleted" CHECK ("DateCompleted" IS NULL OR "DateCreated" <= "DateCompleted"),
                CONSTRAINT "CK_TaskEntry_Name_AllowedChar" CHECK (length("Name") > 0 AND translate("Name", 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 !@$%&(){}[]/.,><', '') = ''),
                CONSTRAINT "CK_TaskEntry_Name_MaxLength" CHECK (length("Name") > 0 AND length("Name") <= 64),
                CONSTRAINT "FK_Tasks_TodoLists_TodoListId" FOREIGN KEY ("TodoListId") REFERENCES "TodoLists" ("Id") ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS "IX_Tasks_Name" ON "Tasks" ("Name");
            CREATE INDEX IF NOT EXISTS "IX_Tasks_TodoListId" ON "Tasks" ("TodoListId");
            CREATE INDEX IF NOT EXISTS "IX_TodoLists_UserId" ON "TodoLists" ("UserId");
        """;

        await using var command = new Npgsql.NpgsqlCommand(ddl, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync() => await _dbContainer.DisposeAsync();
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlTestCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSql Collection";
}
