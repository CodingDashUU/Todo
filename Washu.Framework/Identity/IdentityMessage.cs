namespace Washu.Framework.Identity;

using Notifications;

public sealed record IdentityMessage(Message CoreMessage, bool RedirectToSignIn = false);