namespace Washu.Todo.IntegrationTests;

using Framework.IntegrationTests.Fixtures;
using Identity;

public sealed class TodoPostgreSqlFixture() : PostgreSqlFixture<TodoDbContext>(new TodoListInterceptor());