namespace Washu.Framework.Notifications;

public sealed record Message(MessageType Type, string Title, string Details)
{
    public static Message Error(string title, string details)
    => new(MessageType.Error, title, details);
    
    public static Message Info(string title, string details)
        => new(MessageType.Info, title, details);
    
    public static Message Success(string title, string details)
        => new(MessageType.Success, title, details);
}

public enum MessageType { Error, Info, Success }