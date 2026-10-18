namespace Washu.Todo.IntegrationTests;

using Washu.Framework.IntegrationTests.Fixtures;
using Xunit;

[CollectionDefinition(Name)]
public sealed class TodoPostgreSqlTestCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "Todo PostgreSql Collection";
}
