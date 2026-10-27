namespace Washu.Framework.IntegrationTests.Fixtures;

using Identity;
using Testing;

public sealed class ApplicationPostgreSqlFixture : PostgreSqlFixture<ApplicationDbContext>;
[CollectionDefinition(Name)]
public sealed class PostgreSqlTestCollection : ICollectionFixture<ApplicationPostgreSqlFixture>
{
    public const string Name = "PostgreSql Collection";
}