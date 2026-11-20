namespace Washu.Todo;

using Framework;

public static class TodoErrors
{
    public static readonly Error ListNotFound = Error.HandlerError(Codes.ListNotFound, "List could not be found");
    public static readonly Error ListTooLong = Error.HandlerError(Codes.ListTooLong, "Todo list may not contain more than 1000 tasks");
    public static readonly Error ListAlreadyExists = Error.HandlerError(Codes.ListAlreadyExists, "The list specified already exists");
    public static readonly Error TaskNotFound = Error.HandlerError(Codes.TaskNotFound, "Task could not be found");
    public static class Codes
    {
        public const string ListNotFound = "List.NotFound";
        public const string ListTooLong = "List.TooLong";
        public const string ListAlreadyExists = "List.AlreadyExists";
        public const string TaskNotFound = "Task.NotFound";
    }
}