namespace Washu.Todo.IntegrationTests;

using Framework.Identity;

public class MockCurrentUser(Guid? userId) : ICurrentUser
{
    public Guid? UserId => userId;
}