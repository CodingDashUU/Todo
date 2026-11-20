namespace Washu.Framework;

using Identity.Users;

public class ErrorHandler
{
    public required Action RedirectToSignIn { get; set; }
    public required Action RefreshData { get; set; }
    public required Func<Task> ShowValidationErrors { get; set; }
    public async Task HandleAsync(Error error)
    {
        switch (error.Type)
        {
            case HandlerError:
                switch (error.Code)
                {
                    case UserErrors.Codes.UserNotFound:
                        RedirectToSignIn();
                        break;
                    case GenericErrors.Codes.ConcurrencyError:
                        RefreshData();
                        break;   
                }
                break;
            case ValidationError:
                await ShowValidationErrors();
                break;
        }
    }
}