namespace Washu.Todo.IntegrationTests;

using Xunit;

[CollectionDefinition(Name)]
public sealed class TodoPostgreSqlTestCollection : ICollectionFixture<TodoPostgreSqlFixture>
{
    public const string Name = "Todo PostgreSql Collection";
}
