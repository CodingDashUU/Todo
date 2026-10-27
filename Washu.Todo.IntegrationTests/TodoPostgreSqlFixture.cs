namespace Washu.Todo.IntegrationTests;

using Identity;
using Testing;

public sealed class TodoPostgreSqlFixture : PostgreSqlFixture<TodoDbContext>;