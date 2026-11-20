namespace Washu.Framework;

public static class GenericErrors
{
    public static readonly Error ServerError = Error.HandlerError(Codes.ServerError, "There was an unknown issue while processing your request");
    public static readonly Error ConcurrencyError = Error.HandlerError(Codes.ConcurrencyError, "Your data was modified or deleted by another session, please try again");
    public static class Codes
    {
        public const string ServerError = "Generic.ServerError";
        public const string ConcurrencyError = "Generic.ConcurrencyError";
    }
}