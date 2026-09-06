namespace Washu.Framework.Notifications;

public readonly struct Message
{
    public MessageType Type { get; init;  }
    public string Title { get; init;  }
    public string Details { get; init; }

    public static Message Error(string title = "", string details = "")
    => new() { Type = MessageType.Error, Title = title, Details = details };
    
    public static Message Info(string title = "", string details = "")
        => new() { Type = MessageType.Info, Title = title, Details = details };
    
    public static Message Success(string title = "", string details = "")
        => new() { Type = MessageType.Success, Title = title, Details = details };
}

public enum MessageType { Error, Info, Success }