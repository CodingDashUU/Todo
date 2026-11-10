using Washu.Todo.Persistence;

namespace Washu.Todo.IntegrationTests;

using Testing;

public sealed class TodoPostgreSqlFixture : PostgreSqlFixture<TodoDbContext>;