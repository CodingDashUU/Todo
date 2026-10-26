namespace Washu.Testing;

using Framework.Identity;

public class MockCurrentUser(Guid? userId) : ICurrentUser
{
    public Guid? UserId => userId;
}